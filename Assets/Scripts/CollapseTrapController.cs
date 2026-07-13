using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// One-shot collapse trap with three stages:
/// 1) Approach warning: early trigger zone, camera shake and rumble audio.
/// 2) Collapse delay: optional wait after the main trigger is entered.
/// 3) Collapse: falling object groups, impact audio, and particle effects.
/// </summary>
public class CollapseTrapController : MonoBehaviour
{
    [Serializable]
    public class ApproachWarningSettings
    {
        [Header("Camera Shake")]
        public bool enableCameraShake = true;
        public CameraShake.Profile cameraShake = CameraShake.Profile.Default;

        [Header("Audio")]
        [Tooltip("3D audio source on the approach warning trigger. Auto-finds one on that object if empty.")]
        public AudioSource audioSource;
        public AudioClip warningSound;
        [Range(0f, 3f)] public float warningVolume = 2f;

        [Header("Effects")]
        [Tooltip("Objects activated when the approach warning starts.")]
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

        [Header("Group Camera Shake")]
        [Tooltip("Play an extra shake when this group starts falling. Uses its own profile below.")]
        public bool enableGroupCameraShake;
        public CameraShake.Profile groupCameraShake = new CameraShake.Profile
        {
            duration = 0.45f,
            positionIntensity = 0.15f,
            rotationIntensity = 2f,
            frequency = 20f
        };
    }

    [Serializable]
    public class CollapsePhaseSettings
    {
        [Tooltip("Seconds to wait after entering the collapse trigger before debris starts falling.")]
        public float delayBeforeCollapse = 2.5f;

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

        [Header("Camera Shake")]
        [Tooltip("Screen shake when debris starts falling.")]
        public bool enableCameraShake = true;
        public CameraShake.Profile cameraShake = new CameraShake.Profile
        {
            duration = 1.2f,
            positionIntensity = 0.22f,
            rotationIntensity = 3.5f,
            frequency = 24f
        };

        [Header("Audio")]
        [Tooltip("3D audio source on the collapse trigger. Auto-finds one on that object if empty.")]
        public AudioSource audioSource;
        public AudioClip collapseSound;
        [Range(0f, 3f)] public float collapseVolume = 2f;

        [Header("Effects")]
        [Tooltip("Objects activated when the collapse phase starts.")]
        public GameObject[] effectObjects;
    }

    [Header("Approach Warning Trigger")]
    [Tooltip("Earlier trigger zone that plays rumble/shake before the player reaches the collapse zone.")]
    [SerializeField] private Collider approachWarningTrigger;

    [Header("Approach Warning")]
    [SerializeField] private ApproachWarningSettings approachWarning = new ApproachWarningSettings();

    [Header("Collapse Trigger")]
    [SerializeField] private Collider collapseTrigger;

    [Header("Collapse Phase")]
    [SerializeField] private CollapsePhaseSettings collapsePhase = new CollapsePhaseSettings();

    [Header("After Collapse")]
    [SerializeField] private GameObject blocker;
    [SerializeField] private bool enableBlockerAfterCollapse = true;

    [Header("Trigger Filter")]
    [SerializeField] private string triggerTag = "Player";

    [Header("3D Audio")]
    [Tooltip("3D audio sources are expected on each trigger object. These values apply to both.")]
    [SerializeField] private float audioMinDistance = 2f;
    [SerializeField] private float audioMaxDistance = 50f;
    [SerializeField] private float audioSourceVolume = 1f;

    public bool HasApproachWarningTriggered { get; private set; }
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
        ResolveAudioSources();
        CacheFallingGroups();
    }

    private void Start()
    {
        SetFallingGroupsVisible(false);
        SetVisibleFallFromCurrentGroups(true);
    }

    private void ResolveAudioSources()
    {
        if (approachWarning.audioSource == null && approachWarningTrigger != null)
            approachWarning.audioSource = FindAudioSourceOnTrigger(approachWarningTrigger.transform);

        if (collapsePhase.audioSource == null && collapseTrigger != null)
            collapsePhase.audioSource = FindAudioSourceOnTrigger(collapseTrigger.transform);

        ConfigureSpatialAudio(approachWarning.audioSource);
        ConfigureSpatialAudio(collapsePhase.audioSource);
    }

    private AudioSource FindAudioSourceOnTrigger(Transform triggerRoot)
    {
        AudioSource source = triggerRoot.GetComponent<AudioSource>();
        if (source != null)
            return source;

        return triggerRoot.GetComponentInChildren<AudioSource>(true);
    }

    private void ConfigureSpatialAudio(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = audioMinDistance;
        source.maxDistance = audioMaxDistance;
        source.volume = audioSourceVolume;
    }

    public void NotifyApproachWarningEnter(Collider other)
    {
        if (HasApproachWarningTriggered)
            return;

        if (!other.CompareTag(triggerTag))
            return;

        HasApproachWarningTriggered = true;

        if (approachWarningTrigger != null)
            approachWarningTrigger.enabled = false;

        if (approachWarning.enableCameraShake)
            CameraShake.ShakeIfAvailable(approachWarning.cameraShake);

        PlayOneShot(approachWarning.audioSource, approachWarning.warningSound, approachWarning.warningVolume);
        SetObjectsActive(approachWarning.effectObjects, true);
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

        if (collapseTrigger != null)
            collapseTrigger.enabled = false;

        yield return new WaitForSeconds(Mathf.Max(0f, collapsePhase.delayBeforeCollapse));

        yield return StartCoroutine(CollapseFallRoutine());

        if (enableBlockerAfterCollapse && blocker != null)
            blocker.SetActive(true);

        OnRouteBlocked?.Invoke();
    }

    private IEnumerator CollapseFallRoutine()
    {
        if (collapsePhase.enableCameraShake)
            CameraShake.ShakeIfAvailable(collapsePhase.cameraShake);

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

            if (group.enableGroupCameraShake)
                CameraShake.ShakeIfAvailable(group.groupCameraShake);

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
            if (objects[i] == null)
                continue;

            objects[i].SetActive(active);

            if (active)
                PlayEffectParticles(objects[i]);
        }
    }

    private static void PlayEffectParticles(GameObject effectRoot)
    {
        ParticleSystem[] particleSystems = effectRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystemRenderer renderer = particleSystems[i].GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.enabled = true;

            particleSystems[i].Clear(true);
            particleSystems[i].Play(true);
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
        ResolveAudioSources();
    }
#endif
}
