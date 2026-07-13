using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// One-shot collapse trap with two phases:
/// 1) Warning: camera shake, rumble audio, optional pre-fall effects.
/// 2) Collapse: falling object groups, impact audio, and particle effects.
/// </summary>
public class CollapseTrapController : MonoBehaviour
{
    [Serializable]
    public class WarningPhaseSettings
    {
        [Tooltip("Seconds to wait after the warning before debris starts falling.")]
        public float delayBeforeCollapse = 2.5f;

        [Header("Camera Shake")]
        public bool enableCameraShake = true;
        public CameraShake.Profile cameraShake = CameraShake.Profile.Default;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip warningSound;
        [Range(0f, 1f)] public float warningVolume = 1f;

        [Header("Effects")]
        [Tooltip("Objects activated when the warning starts (cracks, dust, lights, etc.).")]
        public GameObject[] effectObjects;
    }

    [Serializable]
    public class FallingGroupSettings
    {
        public enum FallMode
        {
            DropFromAbove,
            FallFromCurrentPose
        }

        [Tooltip("Parent object whose direct children will fall.")]
        public GameObject root;

        public FallMode fallMode = FallMode.DropFromAbove;

        [Tooltip("DropFromAbove: spawn height above rest pose. FallFromCurrentPose: drop distance below start pose.")]
        public float fallHeight = 4f;

        [Tooltip("Used only by FallFromCurrentPose.")]
        public float fallDistance = 4f;

        [Tooltip("Use the global collapse motion values on the controller.")]
        public bool useGlobalMotion = true;

        public float fallDuration = 0.55f;
        public float pieceStagger = 0.07f;
        public float tumbleDegrees = 35f;

        [Header("Group Audio")]
        public AudioClip fallSound;
        [Range(0f, 1f)] public float fallSoundVolume = 1f;

        [Header("Group Effects")]
        [Tooltip("Objects activated when this group starts falling.")]
        public GameObject[] fallStartEffects;
    }

    [Serializable]
    public class CollapsePhaseSettings
    {
        [Header("Falling Groups")]
        [Tooltip("Each entry is a parent whose children fall in sequence.")]
        public FallingGroupSettings[] fallingGroups;

        [Header("Global Motion")]
        public float fallHeight = 4f;
        [Tooltip("How far pieces drop when a group uses FallFromCurrentPose with global motion. Keep small so pieces stay visible on the ground.")]
        public float fallDistance = 1.5f;
        public float fallDuration = 0.55f;
        public float pieceStagger = 0.07f;
        public float settleDelay = 0.2f;
        public float tumbleDegrees = 35f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip collapseSound;
        [Range(0f, 1f)] public float collapseVolume = 1f;

        [Header("Effects")]
        [Tooltip("Objects activated when the collapse phase starts.")]
        public GameObject[] effectObjects;
    }

    [Header("Trigger")]
    [SerializeField] private Collider triggerZone;

    [Header("Warning Phase")]
    [SerializeField] private WarningPhaseSettings warningPhase = new WarningPhaseSettings();

    [Header("Collapse Phase")]
    [SerializeField] private CollapsePhaseSettings collapsePhase = new CollapsePhaseSettings();

    [Header("After Collapse")]
    [SerializeField] private GameObject blocker;
    [SerializeField] private bool enableBlockerAfterCollapse = true;

    [Header("Trigger Filter")]
    [SerializeField] private string triggerTag = "Player";

    public bool HasTriggered { get; private set; }
    public event Action OnRouteBlocked;

    private FallingPiece[][] cachedGroups;

    private struct FallingPiece
    {
        public Transform transform;
        public Vector3 restWorldPosition;
        public Quaternion restWorldRotation;
        public Vector3 landingWorldPosition;
        public Collider collider;
    }

    private void Awake()
    {
        if (triggerZone == null)
            triggerZone = GetComponentInChildren<BoxCollider>(true);

        CacheFallingGroups();
    }

    private void Start()
    {
        SetFallingGroupsVisible(false);
        SetVisibleFallFromCurrentGroups(true);
    }

    public void NotifyTriggerEnter(Collider other)
    {
        if (HasTriggered)
            return;

        if (!other.CompareTag(triggerTag))
            return;

        StartCoroutine(CollapseRoutine());
    }

    private void CacheFallingGroups()
    {
        if (collapsePhase.fallingGroups == null || collapsePhase.fallingGroups.Length == 0)
        {
            cachedGroups = Array.Empty<FallingPiece[]>();
            return;
        }

        cachedGroups = new FallingPiece[collapsePhase.fallingGroups.Length][];

        for (int groupIndex = 0; groupIndex < collapsePhase.fallingGroups.Length; groupIndex++)
        {
            GameObject root = collapsePhase.fallingGroups[groupIndex].root;
            FallingGroupSettings groupSettings = collapsePhase.fallingGroups[groupIndex];
            if (root == null)
            {
                cachedGroups[groupIndex] = Array.Empty<FallingPiece>();
                continue;
            }

            Transform rootTransform = root.transform;
            int count = rootTransform.childCount;
            cachedGroups[groupIndex] = new FallingPiece[count];

            for (int i = 0; i < count; i++)
            {
                Transform child = rootTransform.GetChild(i);
                Vector3 restPosition = child.position;
                Vector3 landingPosition = groupSettings.fallMode == FallingGroupSettings.FallMode.FallFromCurrentPose
                    ? restPosition - Vector3.up * GetFallDistance(groupSettings)
                    : restPosition;

                cachedGroups[groupIndex][i] = new FallingPiece
                {
                    transform = child,
                    restWorldPosition = restPosition,
                    restWorldRotation = child.rotation,
                    landingWorldPosition = landingPosition,
                    collider = child.GetComponent<Collider>()
                };

                if (cachedGroups[groupIndex][i].collider != null)
                    cachedGroups[groupIndex][i].collider.enabled = false;
            }
        }
    }

    private IEnumerator CollapseRoutine()
    {
        HasTriggered = true;

        if (triggerZone != null)
            triggerZone.enabled = false;

        yield return StartCoroutine(WarningRoutine());

        yield return new WaitForSeconds(Mathf.Max(0f, warningPhase.delayBeforeCollapse));

        yield return StartCoroutine(CollapseFallRoutine());

        if (enableBlockerAfterCollapse && blocker != null)
            blocker.SetActive(true);

        OnRouteBlocked?.Invoke();
    }

    private IEnumerator WarningRoutine()
    {
        if (warningPhase.enableCameraShake)
            CameraShake.ShakeIfAvailable(warningPhase.cameraShake);

        PlayOneShot(warningPhase.audioSource, warningPhase.warningSound, warningPhase.warningVolume);
        SetObjectsActive(warningPhase.effectObjects, true);
        yield break;
    }

    private IEnumerator CollapseFallRoutine()
    {
        PlayOneShot(collapsePhase.audioSource, collapsePhase.collapseSound, collapsePhase.collapseVolume);
        SetObjectsActive(collapsePhase.effectObjects, true);

        if (collapsePhase.fallingGroups == null || collapsePhase.fallingGroups.Length == 0)
            yield break;

        float longestFinish = 0f;

        for (int groupIndex = 0; groupIndex < collapsePhase.fallingGroups.Length; groupIndex++)
        {
            FallingGroupSettings group = collapsePhase.fallingGroups[groupIndex];
            FallingPiece[] pieces = cachedGroups[groupIndex];

            if (group.root == null || pieces == null || pieces.Length == 0)
                continue;

            group.root.SetActive(true);
            PlayOneShot(collapsePhase.audioSource, group.fallSound, group.fallSoundVolume);
            SetObjectsActive(group.fallStartEffects, true);

            float fallHeight = group.useGlobalMotion ? collapsePhase.fallHeight : group.fallHeight;
            float fallDuration = group.useGlobalMotion ? collapsePhase.fallDuration : group.fallDuration;
            float pieceStagger = group.useGlobalMotion ? collapsePhase.pieceStagger : group.pieceStagger;
            float tumbleDegrees = group.useGlobalMotion ? collapsePhase.tumbleDegrees : group.tumbleDegrees;
            float fallDistance = GetFallDistance(group);

            for (int i = 0; i < pieces.Length; i++)
            {
                float startDelay = i * pieceStagger;
                StartCoroutine(AnimatePieceFall(
                    pieces[i],
                    group.fallMode,
                    fallHeight,
                    fallDistance,
                    fallDuration,
                    tumbleDegrees,
                    startDelay));
                longestFinish = Mathf.Max(longestFinish, startDelay + fallDuration);
            }
        }

        yield return new WaitForSeconds(longestFinish + collapsePhase.settleDelay);

        for (int groupIndex = 0; groupIndex < cachedGroups.Length; groupIndex++)
        {
            FallingPiece[] pieces = cachedGroups[groupIndex];
            if (pieces == null)
                continue;

            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i].collider != null)
                    pieces[i].collider.enabled = true;
            }
        }
    }

    private IEnumerator AnimatePieceFall(
        FallingPiece piece,
        FallingGroupSettings.FallMode fallMode,
        float fallHeight,
        float fallDistance,
        float fallDuration,
        float tumbleDegrees,
        float delay)
    {
        if (piece.transform == null)
            yield break;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Vector3 startPosition;
        Vector3 endPosition;
        Quaternion endRotation = piece.restWorldRotation;

        if (fallMode == FallingGroupSettings.FallMode.FallFromCurrentPose)
        {
            startPosition = piece.restWorldPosition;
            endPosition = piece.landingWorldPosition;
        }
        else
        {
            startPosition = piece.restWorldPosition + Vector3.up * fallHeight;
            endPosition = piece.restWorldPosition;
        }

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

            piece.transform.position = Vector3.LerpUnclamped(startPosition, endPosition, fallCurve);
            piece.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        piece.transform.SetPositionAndRotation(endPosition, endRotation);
    }

    private float GetFallDistance(FallingGroupSettings group)
    {
        if (group.fallMode != FallingGroupSettings.FallMode.FallFromCurrentPose)
            return 0f;

        return group.useGlobalMotion ? collapsePhase.fallDistance : group.fallDistance;
    }

    private void SetFallingGroupsVisible(bool visible)
    {
        if (collapsePhase.fallingGroups == null)
            return;

        for (int i = 0; i < collapsePhase.fallingGroups.Length; i++)
        {
            FallingGroupSettings group = collapsePhase.fallingGroups[i];
            if (group.root == null || group.fallMode == FallingGroupSettings.FallMode.FallFromCurrentPose)
                continue;

            group.root.SetActive(visible);
        }
    }

    private void SetVisibleFallFromCurrentGroups(bool visible)
    {
        if (collapsePhase.fallingGroups == null)
            return;

        for (int i = 0; i < collapsePhase.fallingGroups.Length; i++)
        {
            FallingGroupSettings group = collapsePhase.fallingGroups[i];
            if (group.root == null || group.fallMode != FallingGroupSettings.FallMode.FallFromCurrentPose)
                continue;

            group.root.SetActive(visible);
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

    private static void PlayOneShot(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
            return;

        source.PlayOneShot(clip, volume);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (triggerZone == null)
            triggerZone = GetComponentInChildren<BoxCollider>(true);
    }
#endif
}
