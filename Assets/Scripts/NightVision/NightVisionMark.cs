using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an object so night vision can recolor it: yellow for interactables, red for threats.
/// </summary>
public class NightVisionMark : MonoBehaviour
{
    public enum Kind
    {
        Interactable,
        Threat
    }

    public static readonly List<NightVisionMark> Active = new List<NightVisionMark>();

    static readonly Color InteractableColor = new Color(1f, 0.85f, 0.15f, 0.92f);
    static readonly Color ThreatColor = new Color(1f, 0.15f, 0.1f, 0.92f);

    [SerializeField] Kind kind = Kind.Interactable;
    [SerializeField] Renderer[] renderers;

    public Kind Category => kind;
    public Color HighlightColor => kind == Kind.Interactable ? InteractableColor : ThreatColor;
    public Renderer[] Renderers => renderers;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);
    }

    void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
    }

    public void Configure(Kind category, Renderer[] specificRenderers)
    {
        kind = category;
        if (specificRenderers != null && specificRenderers.Length > 0)
            renderers = specificRenderers;
        else if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);
    }
}
