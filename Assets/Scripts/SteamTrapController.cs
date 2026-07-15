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
        Cooling,
        Exhausted
    }

    [Header("Uses")]
    [SerializeField] private int maxUses = 3;

    [Header("Timing")]
    [SerializeField] private float burstDuration = 15f;
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

    public event Action<State> OnStateChanged;
    public event Action<int> OnUsesChanged;

    private Coroutine cycleRoutine;

    private void Awake()
    {
        RemainingUses = Mathf.Max(0, maxUses);
        ConfigureAudio();
        SetSteamEffectsActive(false);
        // Keep the vision zone collider enabled so enter/exit tracking stays valid.
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
        SetState(State.Bursting);
        SetSteamEffectsActive(true);
        PlayBurstStart();
        StartSteamLoop();

        yield return new WaitForSeconds(Mathf.Max(0.01f, burstDuration));

        StopSteamLoop();
        SetSteamEffectsActive(false);
        if (visionObscurer != null)
            visionObscurer.ForceClear();

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

    private void SetState(State next)
    {
        CurrentState = next;
        OnStateChanged?.Invoke(CurrentState);
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
                systems[s].Clear(true);
                systems[s].Play(true);
            }
        }
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
        source.volume = Mathf.Clamp01(steamLoopVolume);
        // Allow perceived loudness above 1 via output mixer isn't available; boost with PlayOneShot if needed.
        // Use volume > 1 when Unity allows (AudioSource.volume max is not hard-clamped to 1 in all versions).
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
        cooldownDuration = Mathf.Max(0f, cooldownDuration);
        ConfigureAudio();
    }
#endif
}
