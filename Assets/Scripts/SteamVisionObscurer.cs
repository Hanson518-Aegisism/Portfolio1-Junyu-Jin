using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thick white vision overlay while the player stands inside an active steam zone.
/// Vision only — no damage.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SteamVisionObscurer : MonoBehaviour
{
    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private SteamTrapController trapController;

    [Header("Overlay")]
    [SerializeField] private Image visionOverlay;
    [SerializeField] private Color fogColor = new Color(0.92f, 0.94f, 0.96f, 0.82f);
    [SerializeField] private float fadeInSpeed = 4f;
    [SerializeField] private float fadeOutSpeed = 2.5f;

    private int playersInside;
    private float currentAlpha;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Awake()
    {
        if (trapController == null)
            trapController = GetComponentInParent<SteamTrapController>();

        EnsureOverlay();
        ApplyOverlayAlpha(0f);
    }

    private void OnEnable()
    {
        if (trapController != null)
            trapController.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (trapController != null)
            trapController.OnStateChanged -= HandleStateChanged;

        ResetOccupancy();
    }

    private void Update()
    {
        float target = 0f;
        if (playersInside > 0 && trapController != null)
        {
            switch (trapController.CurrentState)
            {
                case SteamTrapController.State.Bursting:
                    target = fogColor.a;
                    break;
                case SteamTrapController.State.WindingDown:
                    target = fogColor.a * trapController.SteamIntensity;
                    break;
            }
        }

        float speed = target > currentAlpha ? fadeInSpeed : fadeOutSpeed;
        currentAlpha = Mathf.MoveTowards(currentAlpha, target, speed * Time.deltaTime);
        ApplyOverlayAlpha(currentAlpha);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playersInside++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playersInside = Mathf.Max(0, playersInside - 1);
    }

    /// <summary>
    /// Clears the overlay immediately without forgetting who is inside the zone.
    /// </summary>
    public void ForceClear()
    {
        currentAlpha = 0f;
        ApplyOverlayAlpha(0f);
    }

    public void ResetOccupancy()
    {
        playersInside = 0;
        ForceClear();
    }

    private void HandleStateChanged(SteamTrapController.State state)
    {
        if (state != SteamTrapController.State.Bursting)
        {
            // Keep counting players, but overlay will fade out via Update.
        }
    }

    private void EnsureOverlay()
    {
        if (visionOverlay != null)
            return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("SteamVisionCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Transform existing = canvas.transform.Find("SteamVisionOverlay");
        GameObject overlayObject;
        if (existing != null)
        {
            overlayObject = existing.gameObject;
            visionOverlay = overlayObject.GetComponent<Image>();
        }
        else
        {
            overlayObject = new GameObject("SteamVisionOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(canvas.transform, false);
            visionOverlay = overlayObject.GetComponent<Image>();
        }

        RectTransform rect = visionOverlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        visionOverlay.raycastTarget = false;
        visionOverlay.color = fogColor;
    }

    private void ApplyOverlayAlpha(float alpha)
    {
        if (visionOverlay == null)
            return;

        Color c = fogColor;
        c.a = alpha;
        visionOverlay.color = c;

        bool show = alpha > 0.001f;
        if (visionOverlay.gameObject.activeSelf != show)
            visionOverlay.gameObject.SetActive(show);
    }
}
