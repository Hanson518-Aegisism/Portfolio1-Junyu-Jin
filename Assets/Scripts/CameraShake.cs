using System.Collections;
using UnityEngine;

/// <summary>
/// Applies a short additive camera shake on top of Cinemachine or any camera rig.
/// Attach to the output camera (e.g. MainCamera). Runs after CinemachineBrain.
/// </summary>
[DefaultExecutionOrder(1000)]
public class CameraShake : MonoBehaviour
{
    [System.Serializable]
    public struct Profile
    {
        [Tooltip("How long the shake lasts in seconds.")]
        public float duration;

        [Tooltip("Maximum positional offset in world units.")]
        public float positionIntensity;

        [Tooltip("Maximum rotational offset in degrees.")]
        public float rotationIntensity;

        [Tooltip("How fast the shake oscillates.")]
        public float frequency;

        public static Profile Default => new Profile
        {
            duration = 0.6f,
            positionIntensity = 0.12f,
            rotationIntensity = 1.5f,
            frequency = 18f
        };
    }

    private static CameraShake instance;

    [SerializeField] private Transform shakeTarget;

    private Coroutine activeShake;
    private Vector3 shakePositionOffset;
    private Vector3 shakeRotationOffset;
    private bool isShaking;

    public static CameraShake Instance
    {
        get
        {
            if (instance != null)
                return instance;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return null;

            instance = mainCamera.GetComponent<CameraShake>();
            return instance;
        }
    }

    private Transform Target => shakeTarget != null ? shakeTarget : transform;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(this);
    }

    private void LateUpdate()
    {
        if (!isShaking)
            return;

        Transform target = Target;
        target.position += shakePositionOffset;
        target.rotation *= Quaternion.Euler(shakeRotationOffset);
    }

    public void Shake(Profile profile)
    {
        if (profile.duration <= 0f)
            return;

        if (activeShake != null)
            StopCoroutine(activeShake);

        activeShake = StartCoroutine(ShakeRoutine(profile));
    }

    public static void ShakeIfAvailable(Profile profile)
    {
        CameraShake shaker = Instance;
        if (shaker != null)
            shaker.Shake(profile);
    }

    private IEnumerator ShakeRoutine(Profile profile)
    {
        isShaking = true;
        float elapsed = 0f;
        float seed = Random.value * 1000f;

        while (elapsed < profile.duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / profile.duration);
            float damping = 1f - normalizedTime;
            float time = elapsed * profile.frequency;

            float offsetX = (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f;
            float offsetY = (Mathf.PerlinNoise(seed + 1f, time) - 0.5f) * 2f;
            float offsetZ = (Mathf.PerlinNoise(seed + 2f, time) - 0.5f) * 2f;

            shakePositionOffset = new Vector3(offsetX, offsetY, offsetZ) * profile.positionIntensity * damping;
            shakeRotationOffset = new Vector3(offsetX, offsetY, offsetZ) * profile.rotationIntensity * damping;
            yield return null;
        }

        shakePositionOffset = Vector3.zero;
        shakeRotationOffset = Vector3.zero;
        isShaking = false;
        activeShake = null;
    }
}
