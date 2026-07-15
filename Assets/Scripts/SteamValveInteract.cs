using TMPro;
using UnityEngine;

/// <summary>
/// Player interaction for the emergency steam valve (E key).
/// Rotates the valve handle, then activates SteamTrapController.
/// Uses its own prompt UI so it does not fight TrapSwitch for SwitchPrompt.
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
    [SerializeField] private float turnDuration = 0.55f;
    [SerializeField] private bool returnHandleAfterBurst = true;

    [Header("Audio")]
    [SerializeField] private AudioSource interactAudioSource;
    [SerializeField] private AudioClip turnSound;

    private Transform playerTransform;
    private bool isTurning;
    private bool applyActivationAfterTurn;
    private bool returnHandlePending;
    private float turnElapsed;
    private Quaternion closedLocalRotation;
    private Quaternion turnStartRotation;
    private Quaternion turnTargetRotation;

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
        UpdateTurnAnimation();

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

        if (isTurning || !trapController.CanInteract)
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
        PlayTurnSound();
        applyActivationAfterTurn = true;
        returnHandlePending = false;

        if (valveHandle == null || turnDuration <= 0f)
        {
            CompleteTurn();
            return;
        }

        turnStartRotation = valveHandle.localRotation;
        turnTargetRotation = GetOpenLocalRotation();
        turnElapsed = 0f;
        isTurning = true;
    }

    private void UpdateTurnAnimation()
    {
        if (!isTurning || valveHandle == null)
            return;

        turnElapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, turnElapsed / Mathf.Max(0.01f, turnDuration));
        valveHandle.localRotation = Quaternion.Slerp(turnStartRotation, turnTargetRotation, t);

        if (turnElapsed >= turnDuration)
            CompleteTurn();
    }

    private void CompleteTurn()
    {
        isTurning = false;

        if (valveHandle != null)
            valveHandle.localRotation = GetOpenLocalRotation();

        if (!applyActivationAfterTurn || trapController == null)
            return;

        applyActivationAfterTurn = false;
        trapController.TryActivate();
        returnHandlePending = returnHandleAfterBurst;
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
        if (interactAudioSource != null)
            return;

        interactAudioSource = GetComponent<AudioSource>();
        if (interactAudioSource == null)
            interactAudioSource = gameObject.AddComponent<AudioSource>();

        interactAudioSource.playOnAwake = false;
        interactAudioSource.spatialBlend = 0f;
    }

    private void PlayTurnSound()
    {
        if (interactAudioSource == null || turnSound == null)
            return;

        interactAudioSource.PlayOneShot(turnSound);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
