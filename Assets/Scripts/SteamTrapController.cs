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
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip burstStartSound;
    [SerializeField] private AudioClip steamLoopSound;
    [Range(0f, 2f)] [SerializeField] private float burstStartVolume = 1f;
    [Range(0f, 2f)] [SerializeField] private float steamLoopVolume = 0.85f;
    [SerializeField] private float audioMinDistance = 2f;
    [SerializeField] private float audioMaxDistance = 40f;

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
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = audioMinDistance;
        audioSource.maxDistance = audioMaxDistance;
    }

    private void PlayBurstStart()
    {
        if (audioSource == null || burstStartSound == null)
            return;

        audioSource.PlayOneShot(burstStartSound, burstStartVolume);
    }

    private void StartSteamLoop()
    {
        if (audioSource == null || steamLoopSound == null)
            return;

        audioSource.clip = steamLoopSound;
        audioSource.loop = true;
        audioSource.volume = steamLoopVolume;
        audioSource.Play();
    }

    private void StopSteamLoop()
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying && audioSource.loop)
            audioSource.Stop();

        audioSource.loop = false;
        audioSource.clip = null;
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
