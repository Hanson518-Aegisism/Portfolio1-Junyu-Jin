using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class SciFiSlidingDoor : MonoBehaviour
{
    static readonly List<SciFiSlidingDoor> Active = new List<SciFiSlidingDoor>();
    static readonly int Opening = Animator.StringToHash("Opening");
    static readonly int Closing = Animator.StringToHash("Closing");
    static readonly int Closed = Animator.StringToHash("Closed");
    static readonly int Opened = Animator.StringToHash("Opened");

    [Header("Animation")]
    [SerializeField] Animator doorAnimator;
    [Min(0.1f)] [SerializeField] float animationDuration = 1.2f;
    [SerializeField] bool startOpen;

    [Header("Interaction / 靠近按 E 开关")]
    [SerializeField] bool allowPlayerInteraction = true;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [Min(0.1f)] [SerializeField] float interactDistance = 2.75f;
    [SerializeField] Vector3 doorwayCenter = new Vector3(-0.3779f, 1.5f, 0f);

    [Header("Safety / 防夹")]
    [SerializeField] bool preventCrushing = true;
    [SerializeField] Vector3 safetyHalfExtents = new Vector3(1.08f, 1.45f, 0.7f);

    [Header("Optional Audio / 可选音效")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip openSound;
    [SerializeField] AudioClip closeSound;

    Transform player;
    PlayerDeath playerDeath;
    readonly Collider[] overlaps = new Collider[64];
    bool initialized;
    bool targetOpen;
    bool moving;
    float moveStartedAt;
    float startNormalized;

    public bool IsOpen => targetOpen;
    public bool IsMoving => moving;
    float Progress => Mathf.Clamp01(startNormalized + (Time.time - moveStartedAt) / Mathf.Max(0.1f, animationDuration));

    void Awake()
    {
        if (doorAnimator == null) doorAnimator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        targetOpen = startOpen;
    }

    void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) { player = go.transform; playerDeath = go.GetComponent<PlayerDeath>(); }
        if (doorAnimator != null)
        {
            doorAnimator.Play(startOpen ? Opened : Closed, 0, 0f);
            doorAnimator.Update(0f);
        }
        initialized = true;
    }

    void Update()
    {
        if (moving && !targetOpen && IsObstructed()) SetOpen(true);
        if (moving && Progress >= 1f) moving = false;
        if (!allowPlayerInteraction || player == null || (playerDeath != null && playerDeath.IsDead)) return;
        if (!Input.GetKeyDown(interactKey)) return;
        // A single key press operates only the nearest door, including adjacent installations.
        SciFiSlidingDoor closest = null;
        float best = float.PositiveInfinity;
        foreach (var door in Active)
        {
            if (!door.allowPlayerInteraction || door.interactKey != interactKey) continue;
            Vector3 p = door.transform.InverseTransformPoint(player.position);
            p.y = door.doorwayCenter.y;
            float distance = (door.transform.TransformPoint(p) - door.transform.TransformPoint(door.doorwayCenter)).sqrMagnitude;
            if (distance <= door.interactDistance * door.interactDistance && distance < best)
            { best = distance; closest = door; }
        }
        if (closest == this) Toggle();
    }

    public void Toggle() { SetOpen(!targetOpen); }
    public void Open() { SetOpen(true); }
    public void Close() { SetOpen(false); }

    public bool SetOpen(bool open)
    {
        if (!initialized || doorAnimator == null || targetOpen == open) return false;
        if (!open && IsObstructed()) return false;
        float normalized = moving ? 1f - Progress : 0f;
        targetOpen = open;
        moving = true;
        startNormalized = normalized;
        moveStartedAt = Time.time;
        doorAnimator.speed = 1.2f / Mathf.Max(0.1f, animationDuration);
        doorAnimator.Play(open ? Opening : Closing, 0, normalized);
        doorAnimator.Update(0f);
        AudioClip clip = open ? openSound : closeSound;
        if (audioSource != null && clip != null) { audioSource.Stop(); audioSource.PlayOneShot(clip); }
        return true;
    }

    bool IsObstructed()
    {
        if (!preventCrushing) return false;
        Vector3 scale = transform.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        int count = Physics.OverlapBoxNonAlloc(transform.TransformPoint(doorwayCenter), Vector3.Scale(safetyHalfExtents, scale),
            overlaps, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return true;
        for (int i = 0; i < count; i++)
        {
            var hit = overlaps[i];
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.GetComponentInParent<CharacterController>() != null || hit.GetComponentInParent<NavMeshAgent>() != null ||
                (hit.attachedRigidbody != null && !hit.attachedRigidbody.isKinematic)) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(doorwayCenter, safetyHalfExtents * 2f);
    }
}
