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

    private Transform playerTransform;
    private Coroutine leverRoutine;

    private void OnEnable()
    {
        if (trapController != null)
            trapController.OnActiveChanged += HandleTrapActiveChanged;
    }

    private void OnDisable()
    {
        if (trapController != null)
            trapController.OnActiveChanged -= HandleTrapActiveChanged;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
            playerTransform = player.transform;

        SetPromptVisible(false);

        if (switchLever != null && trapController != null)
            switchLever.localPosition = trapController.IsActive ? leverPositionOn : leverPositionOff;
    }

    private void Update()
    {
        if (playerTransform == null || trapController == null)
            return;

        bool inRange = Vector3.Distance(transform.position, playerTransform.position) <= interactDistance;
        SetPromptVisible(inRange);

        if (!inRange)
            return;

        if (Input.GetKeyDown(interactKey))
            trapController.Toggle();
    }

    private void HandleTrapActiveChanged(bool active)
    {
        if (switchLever == null)
            return;

        if (leverRoutine != null)
            StopCoroutine(leverRoutine);

        leverRoutine = StartCoroutine(AnimateLever(active ? leverPositionOn : leverPositionOff));
    }

    private IEnumerator AnimateLever(Vector3 target)
    {
        Vector3 start = switchLever.localPosition;
        float elapsed = 0f;

        while (elapsed < leverMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / leverMoveDuration);
            switchLever.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }

        switchLever.localPosition = target;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
