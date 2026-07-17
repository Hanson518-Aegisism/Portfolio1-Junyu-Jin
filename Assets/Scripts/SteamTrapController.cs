using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Limited-use steam pipe trap: burst for a fixed duration, then cooldown.
/// Obscures vision only — no damage / KillZone.
/// </summary>
public class SteamTrapController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Bursting,
        WindingDown,
        Cooling,
        Exhausted
    }

    [Header("Uses")]
    [SerializeField] private int maxUses = 3;

    [Header("Timing")]
    [SerializeField] private float burstDuration = 15f;
    [Tooltip("Seconds for steam to slow down and fade out after a burst ends.")]
    [SerializeField] private float shutdownDuration = 3.5f;
    [Tooltip("Seconds after a burst before the valve can be used again.")]
    [SerializeField] private float cooldownDuration = 10f;

    [Header("Effects")]
    [Tooltip("Steam VFX roots enabled while bursting.")]
    [SerializeField] private GameObject[] steamEffectObjects;
    [Tooltip("Trigger volume used for vision obscuring. Not a KillZone.")]
    [SerializeField] private Collider steamVisionZone;
    [SerializeField] private SteamVisionObscurer visionObscurer;

    [Header("Charge Lights (remaining uses)")]
    [SerializeField] private GameObject[] chargeLights;

    [Header("Audio")]
    [SerializeField] private AudioSource burstAudioSource;
    [SerializeField] private AudioSource loopAudioSource;
    [SerializeField] private AudioClip burstStartSound;
    [SerializeField] private AudioClip steamLoopSound;
    [Range(0f, 5f)] [SerializeField] private float burstStartVolume = 3.5f;
    [Range(0f, 5f)] [SerializeField] private float steamLoopVolume = 3.2f;
    [SerializeField] private float audioMinDistance = 4f;
    [SerializeField] private float audioMaxDistance = 55f;

    public State CurrentState { get; private set; } = State.Idle;
    public int RemainingUses { get; private set; }
    public int MaxUses => maxUses;
    public bool CanInteract => CurrentState == State.Idle && RemainingUses > 0;
    /// <summary>1 while bursting, fades to 0 during wind-down.</summary>
    public float SteamIntensity { get; private set; }

    public event Action<State> OnStateChanged;
    public event Action<int> OnUsesChanged;

    private Coroutine cycleRoutine;
    private ParticleSystem[] cachedParticleSystems;
    private float[] cachedEmissionRates;
    private float[] cachedSimulationSpeeds;

    private void Awake()
    {
        RemainingUses = Mathf.Max(0, maxUses);
        ConfigureAudio();
        CacheParticleSystems();
        SetSteamEffectsActive(false);
        if (steamVisionZone != null)
            steamVisionZone.enabled = true;
        if (visionObscurer != null)
            visionObscurer.ForceClear();
        RefreshChargeLights();
    }

    private void Start()
    {
        SetState(RemainingUses > 0 ? State.Idle : State.Exhausted);
    }

    public bool TryActivate()
    {
        if (!CanInteract)
            return false;

        RemainingUses--;
        OnUsesChanged?.Invoke(RemainingUses);
        RefreshChargeLights();

        if (cycleRoutine != null)
            StopCoroutine(cycleRoutine);

        cycleRoutine = StartCoroutine(BurstCycleRoutine());
        return true;
    }

    private IEnumerator BurstCycleRoutine()
    {
        SteamIntensity = 1f;
        SetState(State.Bursting);
        SetSteamEffectsActive(true);
        PlayBurstStart();
        StartSteamLoop();

        yield return new WaitForSeconds(Mathf.Max(0.01f, burstDuration));

        yield return WindDownSteamRoutine();

        if (RemainingUses <= 0)
        {
            SetState(State.Exhausted);
            cycleRoutine = null;
            yield break;
        }

        SetState(State.Cooling);
        yield return new WaitForSeconds(Mathf.Max(0f, cooldownDuration));

        SetState(State.Idle);
        cycleRoutine = null;
    }

    private IEnumerator WindDownSteamRoutine()
    {
        SetState(State.WindingDown);

        float duration = Mathf.Max(0.5f, shutdownDuration);
        // First portion: stop feeding new steam. Rest of time: let existing particles fade out.
        float stopEmitPortion = 0.35f;
        float stopEmitTime = duration * stopEmitPortion;
        AudioSource loopSource = loopAudioSource != null ? loopAudioSource : burstAudioSource;
        float loopPeakVolume = steamLoopVolume;

        float elapsed = 0f;
        bool stoppedEmitting = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth ease so vision/audio don't cliff at the end.
            float intensity = Mathf.Pow(1f - t, 1.6f);
            SteamIntensity = intensity;
            FadeSteamLoopVolume(loopSource, loopPeakVolume, intensity);

            if (!stoppedEmitting)
            {
                float emitT = Mathf.Clamp01(elapsed / stopEmitTime);
                float emitIntensity = 1f - Mathf.SmoothStep(0f, 1f, emitT);
                ApplyEmissionIntensity(emitIntensity);

                if (emitT >= 1f)
                {
                    StopSteamEmitting();
                    stoppedEmitting = true;
                }
            }

            yield return null;
        }

        SteamIntensity = 0f;
        StopSteamLoop();
        // Particles should already be mostly gone; disable without Clear so nothing "pops".
        DisableSteamEffectsSoft();
        if (visionObscurer != null)
            visionObscurer.ForceClear();
    }

    private void ApplyEmissionIntensity(float intensity)
    {
        if (cachedParticleSystems == null)
            return;

        for (int i = 0; i < cachedParticleSystems.Length; i++)
        {
            ParticleSystem ps = cachedParticleSystems[i];
            if (ps == null)
                continue;

            var emission = ps.emission;
            emission.rateOverTime = cachedEmissionRates[i] * intensity;
        }
    }

    private void StopSteamEmitting()
    {
        if (cachedParticleSystems == null)
            return;

        for (int i = 0; i < cachedParticleSystems.Length; i++)
        {
            ParticleSystem ps = cachedParticleSystems[i];
            if (ps == null)
                continue;

            // Keep alive particles; only stop spawning new ones.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            var emission = ps.emission;
            emission.rateOverTime = cachedEmissionRates[i];
        }
    }

    private void DisableSteamEffectsSoft()
    {
        if (steamEffectObjects == null)
            return;

        for (int i = 0; i < steamEffectObjects.Length; i++)
        {
            GameObject effect = steamEffectObjects[i];
            if (effect == null)
                continue;

            ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
            for (int s = 0; s < systems.Length; s++)
            {
                systems[s].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = systems[s].main;
                main.simulationSpeed = 1f;
            }

            effect.SetActive(false);
        }
    }

    private void FadeSteamLoopVolume(AudioSource source, float peakVolume, float intensity)
    {
        if (source == null || !source.isPlaying)
            return;

        source.volume = peakVolume * intensity;
    }

    private void SetState(State next)
    {
        CurrentState = next;
        OnStateChanged?.Invoke(CurrentState);
    }

    private void CacheParticleSystems()
    {
        if (steamEffectObjects == null || steamEffectObjects.Length == 0)
        {
            cachedParticleSystems = Array.Empty<ParticleSystem>();
            cachedEmissionRates = Array.Empty<float>();
            cachedSimulationSpeeds = Array.Empty<float>();
            return;
        }

        int count = 0;
        for (int i = 0; i < steamEffectObjects.Length; i++)
        {
            if (steamEffectObjects[i] == null)
                continue;
            count += steamEffectObjects[i].GetComponentsInChildren<ParticleSystem>(true).Length;
        }

        cachedParticleSystems = new ParticleSystem[count];
        cachedEmissionRates = new float[count];
        cachedSimulationSpeeds = new float[count];

        int index = 0;
        for (int i = 0; i < steamEffectObjects.Length; i++)
        {
            GameObject effect = steamEffectObjects[i];
            if (effect == null)
                continue;

            ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
            for (int s = 0; s < systems.Length; s++)
            {
                cachedParticleSystems[index] = systems[s];
                cachedEmissionRates[index] = systems[s].emission.rateOverTime.constant;
                cachedSimulationSpeeds[index] = systems[s].main.simulationSpeed;
                index++;
            }
        }
    }

    private void SetSteamEffectsActive(bool active)
    {
        if (steamEffectObjects == null)
            return;

        for (int i = 0; i < steamEffectObjects.Length; i++)
        {
            GameObject effect = steamEffectObjects[i];
            if (effect == null)
                continue;

            effect.SetActive(active);
            if (!active)
                continue;

            ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
            for (int s = 0; s < systems.Length; s++)
            {
                var main = systems[s].main;
                main.simulationSpeed = 1f;

                systems[s].Clear(true);
                systems[s].Play(true);
            }
        }

        if (active)
            CacheParticleSystems();
    }

    private void RefreshChargeLights()
    {
        if (chargeLights == null)
            return;

        for (int i = 0; i < chargeLights.Length; i++)
        {
            if (chargeLights[i] != null)
                chargeLights[i].SetActive(i < RemainingUses);
        }
    }

    private void ConfigureAudio()
    {
        if (burstAudioSource == null)
        {
            Transform burstTransform = transform.Find("SteamAudio");
            if (burstTransform != null)
                burstAudioSource = burstTransform.GetComponent<AudioSource>();
        }

        if (burstAudioSource == null)
            burstAudioSource = GetComponent<AudioSource>();

        if (loopAudioSource == null)
        {
            Transform loopTransform = transform.Find("SteamAudioLoop");
            if (loopTransform != null)
                loopAudioSource = loopTransform.GetComponent<AudioSource>();
        }

        ConfigureSpatialSource(burstAudioSource);
        ConfigureSpatialSource(loopAudioSource);
    }

    private void ConfigureSpatialSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = audioMinDistance;
        source.maxDistance = audioMaxDistance;
        source.volume = 1f;
    }

    private void PlayBurstStart()
    {
        if (burstAudioSource == null || burstStartSound == null)
            return;

        burstAudioSource.pitch = 1f;
        burstAudioSource.PlayOneShot(burstStartSound, burstStartVolume);
    }

    private void StartSteamLoop()
    {
        AudioSource source = loopAudioSource != null ? loopAudioSource : burstAudioSource;
        if (source == null || steamLoopSound == null)
            return;

        source.clip = steamLoopSound;
        source.loop = true;
        source.pitch = 1f;
        source.volume = steamLoopVolume;
        source.Play();
    }

    private void StopSteamLoop()
    {
        AudioSource source = loopAudioSource != null ? loopAudioSource : burstAudioSource;
        if (source == null)
            return;

        if (source.isPlaying)
            source.Stop();

        source.loop = false;
        source.clip = null;
        source.volume = 1f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxUses = Mathf.Max(0, maxUses);
        burstDuration = Mathf.Max(0.01f, burstDuration);
        shutdownDuration = Mathf.Max(0.01f, shutdownDuration);
        cooldownDuration = Mathf.Max(0f, cooldownDuration);
        ConfigureAudio();
    }
#endif
}
