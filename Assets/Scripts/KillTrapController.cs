using System;
using UnityEngine;

/// <summary>
/// Controls an electric kill trap: toggles the trigger collider and related VFX.
/// </summary>
public class KillTrapController : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("The BoxCollider used as the kill zone trigger.")]
    [SerializeField] private BoxCollider killTrigger;

    [Header("Effects")]
    [Tooltip("Visual/audio effect objects shown when the trap is active (e.g. CableSparks).")]
    [SerializeField] private GameObject[] effectObjects;

    [Header("Audio")]
    [SerializeField] private AudioClip electricCurrentSound;
    [Tooltip("3D audio source at the electric trap location. Auto-finds child 'ElectricTrapAudio' if empty.")]
    [SerializeField] private AudioSource electricAudioSource;

    [Header("State")]
    [SerializeField] private bool startActive = true;

    public bool IsActive { get; private set; }
    public event Action<bool> OnActiveChanged;

    private void Awake()
    {
        if (killTrigger == null)
            killTrigger = GetComponentInChildren<BoxCollider>(true);

        ResolveElectricAudioSource();
        ConfigureElectricAudioSource();
    }

    private void Start()
    {
        SetActive(startActive);
    }

    private void Update()
    {
        if (!IsActive)
            return;

        MaintainElectricSound();
    }

    public void SetActive(bool active)
    {
        IsActive = active;

        if (killTrigger != null)
            killTrigger.enabled = active;

        if (effectObjects != null)
        {
            for (int i = 0; i < effectObjects.Length; i++)
            {
                if (effectObjects[i] != null)
                    effectObjects[i].SetActive(active);
            }
        }

        UpdateElectricSound(active);
        OnActiveChanged?.Invoke(active);
    }

    public void Toggle()
    {
        SetActive(!IsActive);
    }

    private void ResolveElectricAudioSource()
    {
        if (electricAudioSource != null)
            return;

        Transform audioTransform = transform.Find("ElectricTrapAudio");
        if (audioTransform != null)
            electricAudioSource = audioTransform.GetComponent<AudioSource>();
    }

    private void ConfigureElectricAudioSource()
    {
        if (electricAudioSource == null)
            return;

        electricAudioSource.playOnAwake = false;
        electricAudioSource.loop = true;
        electricAudioSource.spatialBlend = 1f;
        electricAudioSource.minDistance = 1f;
        electricAudioSource.maxDistance = 50f;
        electricAudioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private void MaintainElectricSound()
    {
        if (electricAudioSource == null || electricCurrentSound == null)
            return;

        if (electricAudioSource.isPlaying && electricAudioSource.clip == electricCurrentSound)
            return;

        UpdateElectricSound(true);
    }

    private void UpdateElectricSound(bool active)
    {
        if (electricAudioSource == null || electricCurrentSound == null)
            return;

        ConfigureElectricAudioSource();

        if (active)
        {
            electricAudioSource.clip = electricCurrentSound;
            electricAudioSource.loop = true;

            if (electricAudioSource.isPlaying && electricAudioSource.clip == electricCurrentSound)
                return;

            electricAudioSource.Stop();
            electricAudioSource.Play();
            return;
        }

        if (electricAudioSource.isPlaying)
            electricAudioSource.Stop();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (killTrigger == null)
            killTrigger = GetComponentInChildren<BoxCollider>(true);

        ResolveElectricAudioSource();
        ConfigureElectricAudioSource();
    }
#endif
}
