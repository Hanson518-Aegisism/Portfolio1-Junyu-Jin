using UnityEngine;

/// <summary>Scene-local fog overrides. Changes preview immediately without editing the shared material.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Environment/Scene Fog Controller")]
public sealed class SceneFogController : MonoBehaviour
{
    [Range(0f, 100f)] public float concentration = 35f;
    [Range(0f, 1f)] public float maximumOpacity = 0.85f;
    [Range(0.1f, 4f)] public float brightness = 1.5f;
    [Range(0.05f, 2f)] public float heightFalloff = 0.18f;
    [Range(0f, 2f)] public float driftSpeed = 0.45f;
    [Range(2f, 15f)] public float smokeSize = 6f;
    [Range(0f, 1f)] public float smokeBreakup = 0.9f;
    public bool previewInEditor = false;
    [SerializeField] private Renderer fogRenderer;
    private MaterialPropertyBlock properties;
    private float nextPreview;

    private void OnEnable()
    {
        Apply();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= Preview;
        UnityEditor.EditorApplication.update += Preview;
        #endif
    }
    private void OnValidate() { Apply(); }
    private void OnDidApplyAnimationProperties() { Apply(); }
    private void Update()
    {
        if (Application.isPlaying) Apply();
    }
    #if UNITY_EDITOR
    private void Preview()
    {
        if (Application.isPlaying || !isActiveAndEnabled || !previewInEditor || driftSpeed <= 0f || Time.realtimeSinceStartup < nextPreview) return;
        nextPreview = Time.realtimeSinceStartup + 1f / 24f;
        Apply();
        UnityEditor.SceneView.RepaintAll();
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }
    #endif
    private void OnDisable()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= Preview;
        #endif
        if (fogRenderer != null) fogRenderer.SetPropertyBlock(null);
    }

    public void Apply()
    {
        ApplyAtTime(Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
    }

    public void ApplyAtTime(float time)
    {
        if (fogRenderer == null) fogRenderer = GetComponentInChildren<MeshRenderer>(true);
        if (fogRenderer == null) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        fogRenderer.GetPropertyBlock(properties);
        properties.SetFloat("_Density", Mathf.Clamp(concentration, 0f, 100f) * 0.0016f);
        properties.SetFloat("_MaxOpacity", Mathf.Clamp01(maximumOpacity));
        properties.SetFloat("_Brightness", Mathf.Clamp(brightness, 0.1f, 4f));
        properties.SetFloat("_HeightFalloff", Mathf.Clamp(heightFalloff, 0.05f, 2f));
        properties.SetFloat("_DriftSpeed", Mathf.Clamp(driftSpeed, 0f, 2f));
        properties.SetFloat("_SmokeScale", 1.8f / Mathf.Clamp(smokeSize, 2f, 15f));
        properties.SetFloat("_SmokeBreakup", Mathf.Clamp01(smokeBreakup));
        properties.SetFloat("_FogTime", time);
        fogRenderer.SetPropertyBlock(properties);
    }
}
