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

    [Header("State")]
    [SerializeField] private bool startActive = true;

    public bool IsActive { get; private set; }
    public event Action<bool> OnActiveChanged;

    private void Awake()
    {
        if (killTrigger == null)
            killTrigger = GetComponentInChildren<BoxCollider>(true);
    }

    private void Start()
    {
        SetActive(startActive);
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

        OnActiveChanged?.Invoke(active);
    }

    public void Toggle()
    {
        SetActive(!IsActive);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (killTrigger == null)
            killTrigger = GetComponentInChildren<BoxCollider>(true);
    }
#endif
}
