using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// One-shot collapse trap: player enters a trigger zone, debris falls in, then the route is blocked.
/// </summary>
public class CollapseTrapController : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Collider triggerZone;

    [Header("Debris")]
    [SerializeField] private GameObject debrisVisual;

    [Header("Collapse Motion")]
    [SerializeField] private float fallHeight = 4f;
    [SerializeField] private float fallDuration = 0.55f;
    [SerializeField] private float pieceStagger = 0.07f;
    [SerializeField] private float settleDelay = 0.2f;
    [SerializeField] private float tumbleDegrees = 35f;

    [Header("Effects")]
    [SerializeField] private GameObject[] effectObjects;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collapseSound;

    [Header("Trigger Filter")]
    [SerializeField] private string triggerTag = "Player";

    public bool HasTriggered { get; private set; }
    public event Action OnRouteBlocked;

    private DebrisPiece[] debrisPieces;

    private struct DebrisPiece
    {
        public Transform transform;
        public Vector3 restWorldPosition;
        public Quaternion restWorldRotation;
        public Collider collider;
    }

    private void Awake()
    {
        if (triggerZone == null)
            triggerZone = GetComponentInChildren<BoxCollider>(true);

        CacheDebrisPieces();
    }

    private void Start()
    {
        SetDebrisVisible(false);
    }

    public void NotifyTriggerEnter(Collider other)
    {
        if (HasTriggered)
            return;

        if (!other.CompareTag(triggerTag))
            return;

        StartCoroutine(CollapseRoutine());
    }

    private void CacheDebrisPieces()
    {
        if (debrisVisual == null)
            return;

        Transform root = debrisVisual.transform;
        int count = root.childCount;
        debrisPieces = new DebrisPiece[count];

        for (int i = 0; i < count; i++)
        {
            Transform child = root.GetChild(i);
            debrisPieces[i] = new DebrisPiece
            {
                transform = child,
                restWorldPosition = child.position,
                restWorldRotation = child.rotation,
                collider = child.GetComponent<Collider>()
            };

            if (debrisPieces[i].collider != null)
                debrisPieces[i].collider.enabled = false;
        }
    }

    private IEnumerator CollapseRoutine()
    {
        HasTriggered = true;

        if (triggerZone != null)
            triggerZone.enabled = false;

        if (audioSource != null && collapseSound != null)
            audioSource.PlayOneShot(collapseSound);

        if (effectObjects != null)
        {
            for (int i = 0; i < effectObjects.Length; i++)
            {
                if (effectObjects[i] != null)
                    effectObjects[i].SetActive(true);
            }
        }

        debrisVisual.SetActive(true);

        if (debrisPieces == null || debrisPieces.Length == 0)
        {
            OnRouteBlocked?.Invoke();
            yield break;
        }

        float longestFinish = 0f;
        for (int i = 0; i < debrisPieces.Length; i++)
        {
            float startDelay = i * pieceStagger;
            StartCoroutine(AnimateDebrisFall(debrisPieces[i], startDelay));
            longestFinish = Mathf.Max(longestFinish, startDelay + fallDuration);
        }

        yield return new WaitForSeconds(longestFinish + settleDelay);

        for (int i = 0; i < debrisPieces.Length; i++)
        {
            if (debrisPieces[i].collider != null)
                debrisPieces[i].collider.enabled = true;
        }

        OnRouteBlocked?.Invoke();
    }

    private IEnumerator AnimateDebrisFall(DebrisPiece piece, float delay)
    {
        if (piece.transform == null)
            yield break;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Vector3 startPosition = piece.restWorldPosition + Vector3.up * fallHeight;
        Quaternion startRotation = piece.restWorldRotation * Quaternion.Euler(
            UnityEngine.Random.Range(-tumbleDegrees, tumbleDegrees),
            UnityEngine.Random.Range(-tumbleDegrees, tumbleDegrees),
            UnityEngine.Random.Range(-tumbleDegrees, tumbleDegrees));

        piece.transform.SetPositionAndRotation(startPosition, startRotation);

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            float fallCurve = t * t;

            piece.transform.position = Vector3.LerpUnclamped(startPosition, piece.restWorldPosition, fallCurve);
            piece.transform.rotation = Quaternion.Slerp(startRotation, piece.restWorldRotation, t);
            yield return null;
        }

        piece.transform.SetPositionAndRotation(piece.restWorldPosition, piece.restWorldRotation);
    }

    private void SetDebrisVisible(bool visible)
    {
        if (debrisVisual != null)
            debrisVisual.SetActive(visible);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (triggerZone == null)
            triggerZone = GetComponentInChildren<BoxCollider>(true);
    }
#endif
}
