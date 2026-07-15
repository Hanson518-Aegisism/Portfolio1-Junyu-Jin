using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Player interaction for the emergency steam valve (E key).
/// Finishes turning the handle first, then starts the steam burst.
/// </summary>
public class SteamValveInteract : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SteamTrapController trapController;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private string playerTag = "Player";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string idlePromptFormat = "Press E to open emergency valve ({0}/{1})";
    [SerializeField] private string coolingPrompt = "Valve cooling — cannot open";
    [SerializeField] private string burstingPrompt = "Valve locked — venting steam";
    [SerializeField] private string exhaustedPrompt = "Pressure exhausted";

    [Header("Valve Animation")]
    [SerializeField] private Transform valveHandle;
    [Tooltip("Local axis through the wheel center (usually Y for this yellow handle).")]
    [SerializeField] private Vector3 localSpinAxis = Vector3.up;
    [SerializeField] private float openAngleDegrees = 160f;
    [Tooltip("Seconds the valve takes to finish rotating. Steam only starts after this completes.")]
    [Min(0.01f)]
    [SerializeField] private float turnDuration = 1.2f;
    [SerializeField] private bool returnHandleAfterBurst = true;

    [Header("Audio")]
    [SerializeField] private AudioSource interactAudioSource;
    [SerializeField] private AudioClip turnSound;
    [Tooltip("Optional short click when the valve finishes turning.")]
    [SerializeField] private AudioClip turnEndSound;
    [Range(0f, 5f)]
    [SerializeField] private float turnVolume = 3f;
    [Tooltip("Stretch/compress turnSound so it lasts exactly as long as Turn Duration.")]
    [SerializeField] private bool syncTurnSoundToDuration = true;

    private Transform playerTransform;
    private bool isBusy;
    private bool returnHandlePending;
    private Quaternion closedLocalRotation;
    private Coroutine turnRoutine;

    private void Awake()
    {
        EnsureAudioSource();
        CacheClosedRotation();
    }

    private void OnEnable()
    {
        if (trapController != null)
            trapController.OnStateChanged += HandleTrapStateChanged;
    }

    private void OnDisable()
    {
        if (trapController != null)
            trapController.OnStateChanged -= HandleTrapStateChanged;

        if (turnRoutine != null)
        {
            StopCoroutine(turnRoutine);
            turnRoutine = null;
        }

        StopTurnSound();
        isBusy = false;
        SetPromptVisible(false);
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
            playerTransform = player.transform;

        CacheClosedRotation();
        if (valveHandle != null)
            valveHandle.localRotation = closedLocalRotation;

        SetPromptVisible(false);
    }

    private void CacheClosedRotation()
    {
        if (valveHandle != null)
            closedLocalRotation = valveHandle.localRotation;
    }

    private Quaternion GetOpenLocalRotation()
    {
        Vector3 axis = localSpinAxis.sqrMagnitude > 0.0001f ? localSpinAxis.normalized : Vector3.up;
        return closedLocalRotation * Quaternion.AngleAxis(openAngleDegrees, axis);
    }

    private void Update()
    {
        if (playerTransform == null && !TryFindPlayer())
            return;

        if (trapController == null)
            return;

        bool inRange = Vector3.Distance(transform.position, playerTransform.position) <= interactDistance;
        if (!inRange)
        {
            SetPromptVisible(false);
            return;
        }

        UpdatePromptText();
        SetPromptVisible(true);

        if (isBusy || !trapController.CanInteract)
            return;

        if (Input.GetKeyDown(interactKey))
            BeginTurn();
    }

    private bool TryFindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
            return false;

        playerTransform = player.transform;
        return true;
    }

    private void BeginTurn()
    {
        if (isBusy || trapController == null || !trapController.CanInteract)
            return;

        returnHandlePending = false;

        if (turnRoutine != null)
            StopCoroutine(turnRoutine);

        turnRoutine = StartCoroutine(TurnThenSprayRoutine());
    }

    private IEnumerator TurnThenSprayRoutine()
    {
        isBusy = true;

        Quaternion startRotation = valveHandle != null ? valveHandle.localRotation : Quaternion.identity;
        Quaternion targetRotation = GetOpenLocalRotation();
        float duration = Mathf.Max(0.01f, turnDuration);

        StartSyncedTurnSound(duration);

        // 1) Fully finish valve rotation first.
        if (valveHandle != null)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                valveHandle.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            valveHandle.localRotation = targetRotation;
        }

        StopTurnSound();
        PlayTurnEndSound();

        // 2) Only after the valve has stopped turning, start steam.
        if (trapController != null)
            trapController.TryActivate();

        returnHandlePending = returnHandleAfterBurst;
        turnRoutine = null;
        isBusy = false;
    }

    private void HandleTrapStateChanged(SteamTrapController.State state)
    {
        if (!returnHandlePending || valveHandle == null)
            return;

        if (state == SteamTrapController.State.Cooling
            || state == SteamTrapController.State.Idle
            || state == SteamTrapController.State.Exhausted)
        {
            valveHandle.localRotation = closedLocalRotation;
            returnHandlePending = false;
        }
    }

    private void UpdatePromptText()
    {
        if (promptText == null || trapController == null)
            return;

        if (isBusy && trapController.CurrentState == SteamTrapController.State.Idle)
        {
            promptText.text = "Turning valve...";
            return;
        }

        switch (trapController.CurrentState)
        {
            case SteamTrapController.State.Idle:
                promptText.text = string.Format(
                    idlePromptFormat,
                    trapController.RemainingUses,
                    trapController.MaxUses);
                break;
            case SteamTrapController.State.Bursting:
                promptText.text = burstingPrompt;
                break;
            case SteamTrapController.State.Cooling:
                promptText.text = coolingPrompt;
                break;
            case SteamTrapController.State.Exhausted:
                promptText.text = exhaustedPrompt;
                break;
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptText == null)
            return;

        if (promptText.gameObject.activeSelf != visible)
            promptText.gameObject.SetActive(visible);
    }

    private void EnsureAudioSource()
    {
        if (interactAudioSource == null)
        {
            interactAudioSource = GetComponent<AudioSource>();
            if (interactAudioSource == null)
                interactAudioSource = gameObject.AddComponent<AudioSource>();
        }

        interactAudioSource.playOnAwake = false;
        interactAudioSource.loop = false;
        interactAudioSource.spatialBlend = 0f;
        interactAudioSource.volume = 1f;
        interactAudioSource.pitch = 1f;
    }

    private void StartSyncedTurnSound(float duration)
    {
        EnsureAudioSource();
        if (interactAudioSource == null || turnSound == null)
            return;

        interactAudioSource.Stop();
        interactAudioSource.clip = turnSound;
        interactAudioSource.loop = false;
        interactAudioSource.volume = turnVolume;

        if (syncTurnSoundToDuration && turnSound.length > 0.01f)
        {
            // pitch > 1 shortens the clip, pitch < 1 stretches it to match turnDuration.
            float pitch = turnSound.length / duration;
            interactAudioSource.pitch = Mathf.Clamp(pitch, 0.35f, 2.5f);
        }
        else
        {
            interactAudioSource.pitch = 1f;
        }

        interactAudioSource.Play();
    }

    private void StopTurnSound()
    {
        if (interactAudioSource == null)
            return;

        if (interactAudioSource.isPlaying)
            interactAudioSource.Stop();

        interactAudioSource.pitch = 1f;
        interactAudioSource.clip = null;
    }

    private void PlayTurnEndSound()
    {
        EnsureAudioSource();
        if (interactAudioSource == null)
            return;

        AudioClip endClip = turnEndSound != null ? turnEndSound : null;
        if (endClip == null)
            return;

        interactAudioSource.pitch = 1f;
        interactAudioSource.PlayOneShot(endClip, turnVolume);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        turnDuration = Mathf.Max(0.01f, turnDuration);
        turnVolume = Mathf.Max(0f, turnVolume);
    }
#endif
}
