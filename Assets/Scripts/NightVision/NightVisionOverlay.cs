using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen goggle mask and battery bar. The mask slides down when putting the goggles on
/// and up when taking them off.
/// </summary>
public class NightVisionOverlay : MonoBehaviour
{
    [SerializeField] Material maskMaterial;
    [SerializeField] Image maskImage;
    [SerializeField] Image chargeFill;
    [SerializeField] Text chargeReadout;

    Material runtimeMaterial;
    GameObject canvasObject;
    GameObject chargeCanvasObject;
    bool ownsCanvas;
    Sprite whiteSprite;

    public void SetVisual(float open, float drop, float charge01)
    {
        EnsureUi();
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat("_Open", open);
            runtimeMaterial.SetFloat("_Drop", drop);
        }

        bool showMask = drop < 0.999f || open > 0.001f;
        if (maskImage != null && maskImage.gameObject.activeSelf != showMask)
            maskImage.gameObject.SetActive(showMask);

        if (chargeFill == null)
            return;

        if (chargeCanvasObject != null && !chargeCanvasObject.activeSelf)
            chargeCanvasObject.SetActive(true);

        chargeFill.fillAmount = Mathf.Clamp01(charge01);
        chargeFill.color = ChargeColor(charge01);
        if (chargeReadout != null)
            chargeReadout.text = Mathf.RoundToInt(Mathf.Clamp01(charge01) * 100f) + "%";
    }

    void Awake()
    {
        EnsureUi();
        SetVisual(0f, 1f, 1f);
    }

    void OnDestroy()
    {
        if (ownsCanvas && canvasObject != null)
            Destroy(canvasObject);
        if (chargeCanvasObject != null)
            Destroy(chargeCanvasObject);

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
        if (whiteSprite != null)
            Destroy(whiteSprite);
    }

    void EnsureUi()
    {
        if (maskImage != null && runtimeMaterial != null)
            return;

        Shader shader = Shader.Find("UI/NightVisionMask");
        if (shader == null)
            return;

        if (runtimeMaterial == null)
        {
            runtimeMaterial = maskMaterial != null ? new Material(maskMaterial) : new Material(shader);
            runtimeMaterial.SetFloat("_Open", 0f);
            runtimeMaterial.SetFloat("_Drop", 1f);
        }

        if (maskImage != null)
        {
            maskImage.material = runtimeMaterial;
            maskImage.raycastTarget = false;
            return;
        }

        Canvas canvas = FindHudCanvas();
        if (canvas == null)
        {
            canvasObject = new GameObject("NightVisionCanvas");
            ownsCanvas = true;
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject maskObject = new GameObject("NightVisionMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        maskObject.transform.SetParent(canvas.transform, false);
        maskObject.transform.SetAsFirstSibling();
        maskImage = maskObject.GetComponent<Image>();
        maskImage.material = runtimeMaterial;
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;
        Stretch(maskImage.rectTransform);
        EnsureChargeBar();
    }

    void EnsureChargeBar()
    {
        if (chargeFill != null)
            return;

        chargeCanvasObject = new GameObject("NightVisionChargeCanvas");
        Canvas canvas = chargeCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = chargeCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Sprite sprite = WhiteSprite();
        Font font = BuiltinFont();

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(chargeCanvasObject.transform, false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = sprite;
        panelImage.color = new Color(0.02f, 0.08f, 0.03f, 0.92f);
        panelImage.raycastTarget = false;
        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 36f);
        panelRect.sizeDelta = new Vector2(640f, 72f);

        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        border.transform.SetParent(panel.transform, false);
        Image borderImage = border.GetComponent<Image>();
        borderImage.sprite = sprite;
        borderImage.color = new Color(0.35f, 0.95f, 0.4f, 1f);
        borderImage.raycastTarget = false;
        RectTransform borderRect = borderImage.rectTransform;
        borderRect.anchorMin = new Vector2(0f, 0.5f);
        borderRect.anchorMax = new Vector2(1f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.offsetMin = new Vector2(118f, -16f);
        borderRect.offsetMax = new Vector2(-132f, 16f);

        GameObject track = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        track.transform.SetParent(border.transform, false);
        Image trackImage = track.GetComponent<Image>();
        trackImage.sprite = sprite;
        trackImage.color = new Color(0.02f, 0.05f, 0.02f, 1f);
        trackImage.raycastTarget = false;
        Stretch(trackImage.rectTransform);
        trackImage.rectTransform.offsetMin = new Vector2(2f, 2f);
        trackImage.rectTransform.offsetMax = new Vector2(-2f, -2f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(track.transform, false);
        chargeFill = fillObject.GetComponent<Image>();
        chargeFill.sprite = sprite;
        chargeFill.color = new Color(0.25f, 0.95f, 0.3f, 1f);
        chargeFill.raycastTarget = false;
        chargeFill.type = Image.Type.Filled;
        chargeFill.fillMethod = Image.FillMethod.Horizontal;
        chargeFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        chargeFill.fillAmount = 1f;
        Stretch(chargeFill.rectTransform);

        CreateLabel(panel.transform, font, "电量", 28, TextAnchor.MiddleLeft, new Vector2(20f, 0f), new Vector2(96f, 72f), new Vector2(0f, 0.5f));
        chargeReadout = CreateLabel(panel.transform, font, "100%", 28, TextAnchor.MiddleRight, new Vector2(-16f, 0f), new Vector2(110f, 72f), new Vector2(1f, 0.5f));
    }

    static Text CreateLabel(Transform parent, Font font, string message, int size, TextAnchor anchor, Vector2 position, Vector2 sizeDelta, Vector2 anchorPoint)
    {
        GameObject labelObject = new GameObject(message, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        Text label = labelObject.GetComponent<Text>();
        label.font = font;
        label.text = message;
        label.fontSize = size;
        label.alignment = anchor;
        label.color = new Color(0.75f, 1f, 0.7f, 1f);
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = anchorPoint;
        rect.anchorMax = anchorPoint;
        rect.pivot = anchorPoint;
        rect.anchoredPosition = position;
        rect.sizeDelta = sizeDelta;
        return label;
    }

    Sprite WhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }

    static Font BuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    static Color ChargeColor(float charge01)
    {
        if (charge01 > 0.35f)
            return new Color(0.25f, 0.95f, 0.28f, 1f);
        if (charge01 > 0.15f)
            return new Color(0.95f, 0.78f, 0.1f, 1f);
        return new Color(0.95f, 0.15f, 0.08f, 1f);
    }

    static Canvas FindHudCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].gameObject.name == "Canvas")
                return canvases[i];
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
