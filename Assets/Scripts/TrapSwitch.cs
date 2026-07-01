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

    private Transform playerTransform;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
            playerTransform = player.transform;

        SetPromptVisible(false);
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
