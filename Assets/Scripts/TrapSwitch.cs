using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Interactable switch that toggles a KillTrapController when the player presses E nearby.
/// </summary>
public class TrapSwitch : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private KillTrapController trapController;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string promptMessage = "Press E to turn on / off the switch";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Lever Animation")]
    [SerializeField] private Transform switchLever;
    [SerializeField] private Vector3 leverPositionOn = new Vector3(0f, 0.012f, 0f);
    [SerializeField] private Vector3 leverPositionOff = new Vector3(0f, -0.012f, 0f);
    [SerializeField] private float leverMoveDuration = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip switchOnSound;
    [SerializeField] private AudioClip switchOffSound;
    [SerializeField] private AudioClip electricCurrentSound;
    [Tooltip("Plays switch on/off sounds. Auto-created if empty.")]
    [SerializeField] private AudioSource switchAudioSource;
    [Tooltip("Loops electric current while trap is active. Auto-created if empty.")]
    [SerializeField] private AudioSource electricAudioSource;

    private Transform playerTransform;
    private bool isInitialized;
    private bool suppressLeverSync;
    private bool applyTrapAfterAnimation;
    private bool pendingTrapState;
    private bool isLeverAnimating;
    private float leverElapsed;
    private Vector3 leverAnimStart;
    private Vector3 leverAnimTarget;

    private void Awake()
    {
        if (switchLever == null)
            switchLever = transform.Find("switch_LOD0.001");

        EnsureAudioSources();

        if (switchAudioSource != null)
            ConfigureSwitchAudioSource();

        if (electricAudioSource != null)
            ConfigureElectricAudioSource();
    }

    private void EnsureAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (switchAudioSource == null && HasSwitchClip())
        {
            switchAudioSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
            ConfigureSwitchAudioSource();
        }

        if (electricAudioSource == null && electricCurrentSound != null)
        {
            electricAudioSource = sources.Length > 1 ? sources[1] : null;
            ConfigureElectricAudioSource();
        }
    }

    private void ConfigureSwitchAudioSource()
    {
        if (switchAudioSource == null)
            return;

        switchAudioSource.playOnAwake = false;
        switchAudioSource.loop = false;
        switchAudioSource.spatialBlend = 0f;
    }

    private void ConfigureElectricAudioSource()
    {
        if (electricAudioSource == null)
            return;

        electricAudioSource.playOnAwake = false;
        electricAudioSource.loop = true;
        electricAudioSource.spatialBlend = 1f;
        electricAudioSource.minDistance = 2f;
        electricAudioSource.maxDistance = 40f;
        electricAudioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private bool HasSwitchClip()
    {
        return switchOnSound != null || switchOffSound != null;
    }

    private bool HasAnyAudioClip()
    {
        return HasSwitchClip() || electricCurrentSound != null;
    }

    private void OnEnable()
    {
        if (trapController != null)
        {
            trapController.OnActiveChanged += HandleTrapActiveChanged;
            trapController.OnActiveChanged += UpdateElectricSound;
        }
    }

    private void OnDisable()
    {
        if (trapController != null)
        {
            trapController.OnActiveChanged -= HandleTrapActiveChanged;
            trapController.OnActiveChanged -= UpdateElectricSound;
        }

        UpdateElectricSound(false);
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
            playerTransform = player.transform;

        SetPromptVisible(false);
        StartCoroutine(InitializeLeverPosition());
    }

    private IEnumerator InitializeLeverPosition()
    {
        yield return null;
        SyncLeverPositionImmediate();
        UpdateElectricSound(trapController != null && trapController.IsActive);
        isInitialized = true;
    }

    private void Update()
    {
        UpdateLeverAnimation();

        if (playerTransform == null || trapController == null)
            return;

        bool inRange = Vector3.Distance(transform.position, playerTransform.position) <= interactDistance;
        SetPromptVisible(inRange);

        if (!inRange || isLeverAnimating)
            return;

        if (Input.GetKeyDown(interactKey))
            BeginToggle();
    }

    private void BeginToggle()
    {
        bool targetActive = !trapController.IsActive;
        PlaySwitchSound(targetActive);
        pendingTrapState = targetActive;
        applyTrapAfterAnimation = true;
        StartLeverAnimation(targetActive ? leverPositionOn : leverPositionOff);
    }

    private void HandleTrapActiveChanged(bool active)
    {
        if (!isInitialized || suppressLeverSync || isLeverAnimating || applyTrapAfterAnimation)
            return;

        StartLeverAnimation(active ? leverPositionOn : leverPositionOff);
    }

    private void StartLeverAnimation(Vector3 target)
    {
        if (switchLever == null)
            return;

        if (leverMoveDuration <= 0f)
        {
            switchLever.localPosition = target;
            CompleteLeverAnimation();
            return;
        }

        leverAnimStart = switchLever.localPosition;
        leverAnimTarget = target;
        leverElapsed = 0f;
        isLeverAnimating = true;
    }

    private void UpdateLeverAnimation()
    {
        if (!isLeverAnimating || switchLever == null)
            return;

        leverElapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, leverElapsed / leverMoveDuration);
        switchLever.localPosition = Vector3.Lerp(leverAnimStart, leverAnimTarget, t);

        if (leverElapsed >= leverMoveDuration)
            CompleteLeverAnimation();
    }

    private void CompleteLeverAnimation()
    {
        if (switchLever != null)
            switchLever.localPosition = leverAnimTarget;

        isLeverAnimating = false;

        if (!applyTrapAfterAnimation || trapController == null)
            return;

        applyTrapAfterAnimation = false;
        suppressLeverSync = true;
        trapController.SetActive(pendingTrapState);
        suppressLeverSync = false;
        UpdateElectricSound(pendingTrapState);
    }

    private void SyncLeverPositionImmediate()
    {
        if (switchLever == null || trapController == null)
            return;

        isLeverAnimating = false;
        applyTrapAfterAnimation = false;
        switchLever.localPosition = trapController.IsActive ? leverPositionOn : leverPositionOff;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptText == null)
            return;

        if (promptText.gameObject.activeSelf != visible)
            promptText.gameObject.SetActive(visible);

        if (visible)
            promptText.text = promptMessage;
    }

    private void PlaySwitchSound(bool turningOn)
    {
        if (switchAudioSource == null)
            return;

        AudioClip clip = turningOn ? switchOnSound : switchOffSound;
        if (clip != null)
            switchAudioSource.PlayOneShot(clip);
    }

    private void UpdateElectricSound(bool active)
    {
        if (electricAudioSource == null || electricCurrentSound == null)
            return;

        ConfigureElectricAudioSource();

        if (active)
        {
            if (!electricAudioSource.gameObject.activeInHierarchy)
                electricAudioSource.gameObject.SetActive(true);

            if (electricAudioSource.isPlaying && electricAudioSource.clip == electricCurrentSound)
                return;

            electricAudioSource.clip = electricCurrentSound;
            electricAudioSource.loop = true;
            electricAudioSource.Play();
            return;
        }

        if (electricAudioSource.isPlaying)
            electricAudioSource.Stop();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
