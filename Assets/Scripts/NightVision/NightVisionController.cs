using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Toggles night vision with a short battery, a goggle transition, and a green clear view.
/// </summary>
[RequireComponent(typeof(NightVisionOverlay))]
public class NightVisionController : MonoBehaviour
{
    enum Phase
    {
        Off,
        Donning,
        On,
        Doffing
    }

    const float DonDuration = 0.45f;
    const float DoffDuration = 0.35f;
    const float DonDarkTime = 0.18f;
    const float DoffDarkTime = 0.14f;

    [SerializeField] Volume volume;
    [SerializeField] Light infraredLight;
    [SerializeField] NightVisionOverlay overlay;
    [SerializeField] PlayerDeath playerDeath;

    [Header("Battery")]
    [Tooltip("满电持续开启的秒数。在 PlayerCapsule 的 Night Vision Controller 上改这个数即可。")]
    [Min(0.5f)]
    public float durationSeconds = 8f;

    [SerializeField] float charge = 1f;

    [Header("Audio")]
    [Tooltip("留空就用内置的夜视仪开机声。")]
    [SerializeField] AudioClip powerOnSound;
    [Tooltip("留空就用内置的夜视仪关机声。")]
    [SerializeField] AudioClip powerOffSound;
    [Range(0f, 1f)]
    [SerializeField] float soundVolume = 0.9f;

    Phase phase = Phase.Off;
    float phaseTime;
    bool visionEnabled;
    float ambientBaseline = 1f;
    float baseFov = 40f;
    AmbientMode ambientModeBaseline = AmbientMode.Trilight;
    Color ambientLightBaseline = Color.gray;
    Color ambientSkyBaseline = Color.gray;
    Color ambientEquatorBaseline = Color.gray;
    Color ambientGroundBaseline = Color.gray;
    Camera viewCamera;
    StarterAssetsInputs inputs;
    AudioSource nightVisionAudio;

    public float Charge => charge;
    public bool IsOn => phase == Phase.On;

    void Awake()
    {
        inputs = GetComponent<StarterAssetsInputs>();
        if (overlay == null)
            overlay = GetComponent<NightVisionOverlay>();
        if (playerDeath == null)
            playerDeath = GetComponent<PlayerDeath>();

        viewCamera = Camera.main;
        if (viewCamera == null)
        {
            GameObject cameraObject = GameObject.Find("MainCamera");
            if (cameraObject != null)
                viewCamera = cameraObject.GetComponent<Camera>();
        }
        if (viewCamera != null)
            baseFov = viewCamera.fieldOfView;

        ambientBaseline = RenderSettings.ambientIntensity;
        ambientModeBaseline = RenderSettings.ambientMode;
        ambientLightBaseline = RenderSettings.ambientLight;
        ambientSkyBaseline = RenderSettings.ambientSkyColor;
        ambientEquatorBaseline = RenderSettings.ambientEquatorColor;
        ambientGroundBaseline = RenderSettings.ambientGroundColor;
        if (infraredLight != null)
            infraredLight.enabled = false;
        if (volume != null)
            volume.weight = 0f;

        NightVisionHighlightFeature.Enabled = false;
        EnsureAudio();
    }

    void Start()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.gameObject.name == "NightVisionCanvas")
                continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.sortingOrder <= 40)
                canvas.sortingOrder = 80;
        }
    }

    void LateUpdate()
    {
        if (playerDeath != null && playerDeath.IsDead)
        {
            if (phase != Phase.Off || visionEnabled)
                ForceOff();
            return;
        }

        if (inputs != null && inputs.nightVision)
        {
            inputs.nightVision = false;
            RequestToggle();
        }

        TickPhase();
    }

    public void RequestToggle()
    {
        if (phase == Phase.Off)
        {
            if (charge <= 0.01f)
                return;

            phase = Phase.Donning;
            phaseTime = 0f;
            PlayNightVisionSound(true);
        }
        else if (phase == Phase.On)
        {
            phase = Phase.Doffing;
            phaseTime = 0f;
            PlayNightVisionSound(false);
        }
    }

    public void AddCharge(float amount)
    {
        charge = Mathf.Clamp01(charge + amount);
    }

    public void UpgradeCapacity(float extraSeconds)
    {
        durationSeconds = Mathf.Max(0.5f, durationSeconds + extraSeconds);
    }

    void TickPhase()
    {
        if (phase == Phase.On)
        {
            charge = Mathf.Max(0f, charge - Time.deltaTime / Mathf.Max(0.05f, durationSeconds));
            ApplyVisual(1f, 0f);
            if (volume != null)
                volume.weight = 1f;

            if (charge <= 0f)
            {
                phase = Phase.Doffing;
                phaseTime = 0f;
                PlayNightVisionSound(false);
            }

            return;
        }

        if (phase == Phase.Off)
        {
            ApplyVisual(0f, 1f);
            return;
        }

        phaseTime += Time.deltaTime;
        if (phase == Phase.Donning)
            TickDon();
        else
            TickDoff();
    }

    void TickDon()
    {
        float drop = phaseTime < DonDarkTime ? 1f - phaseTime / DonDarkTime : 0f;
        float open = phaseTime <= DonDarkTime
            ? 0f
            : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(DonDarkTime, DonDuration, phaseTime));

        if (!visionEnabled && phaseTime >= DonDarkTime)
            SetVision(true);

        if (volume != null)
            volume.weight = visionEnabled ? open : 0f;

        ApplyFov(phaseTime / DonDuration);
        ApplyVisual(open, drop);

        if (phaseTime >= DonDuration)
        {
            phase = Phase.On;
            if (volume != null)
                volume.weight = 1f;
            ApplyVisual(1f, 0f);
            if (viewCamera != null)
                viewCamera.fieldOfView = baseFov;
        }
    }

    void TickDoff()
    {
        float open = phaseTime < DoffDarkTime
            ? Mathf.SmoothStep(1f, 0f, phaseTime / DoffDarkTime)
            : 0f;
        float drop = phaseTime <= DoffDarkTime
            ? 0f
            : Mathf.InverseLerp(DoffDarkTime, DoffDuration, phaseTime);

        if (visionEnabled && phaseTime >= DoffDarkTime)
            SetVision(false);

        if (volume != null)
            volume.weight = visionEnabled ? open : 0f;

        ApplyFov(phaseTime / DoffDuration);
        ApplyVisual(open, drop);

        if (phaseTime >= DoffDuration)
        {
            phase = Phase.Off;
            if (volume != null)
                volume.weight = 0f;
            if (viewCamera != null)
                viewCamera.fieldOfView = baseFov;
            ApplyVisual(0f, 1f);
        }
    }

    void ApplyVisual(float open, float drop)
    {
        if (overlay != null)
            overlay.SetVisual(open, drop, charge);
    }

    void ApplyFov(float normalizedTime)
    {
        if (viewCamera == null)
            return;

        float dip = Mathf.Sin(Mathf.Clamp01(normalizedTime) * Mathf.PI);
        viewCamera.fieldOfView = Mathf.Lerp(baseFov, baseFov - 6f, dip);
    }

    void SetVision(bool enabled)
    {
        visionEnabled = enabled;
        NightVisionHighlightFeature.Enabled = enabled;
        if (infraredLight != null)
        {
            infraredLight.enabled = enabled;
            if (enabled)
            {
                infraredLight.color = new Color(0.35f, 1f, 0.22f);
                infraredLight.intensity = 5f;
                infraredLight.range = 36f;
                infraredLight.spotAngle = 130f;
            }
        }

        if (enabled)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.75f, 0.16f);
            RenderSettings.ambientIntensity = 1.1f;
        }
        else
        {
            RenderSettings.ambientMode = ambientModeBaseline;
            RenderSettings.ambientLight = ambientLightBaseline;
            RenderSettings.ambientSkyColor = ambientSkyBaseline;
            RenderSettings.ambientEquatorColor = ambientEquatorBaseline;
            RenderSettings.ambientGroundColor = ambientGroundBaseline;
            RenderSettings.ambientIntensity = ambientBaseline;
        }
    }

    void ForceOff()
    {
        bool wasActive = phase != Phase.Off || visionEnabled;
        phase = Phase.Off;
        phaseTime = 0f;
        SetVision(false);
        if (volume != null)
            volume.weight = 0f;
        if (viewCamera != null)
            viewCamera.fieldOfView = baseFov;
        ApplyVisual(0f, 1f);
        if (wasActive)
            PlayNightVisionSound(false);
    }

    void EnsureAudio()
    {
        if (powerOnSound == null)
            powerOnSound = NightVisionSounds.CreatePowerOn();
        if (powerOffSound == null)
            powerOffSound = NightVisionSounds.CreatePowerOff();

        Transform existing = transform.Find("NightVisionAudio");
        if (existing != null)
            nightVisionAudio = existing.GetComponent<AudioSource>();

        if (nightVisionAudio == null)
        {
            GameObject audioObject = new GameObject("NightVisionAudio");
            audioObject.transform.SetParent(transform, false);
            nightVisionAudio = audioObject.AddComponent<AudioSource>();
        }

        nightVisionAudio.playOnAwake = false;
        nightVisionAudio.spatialBlend = 0f;
        nightVisionAudio.loop = false;
    }

    void PlayNightVisionSound(bool poweringOn)
    {
        if (nightVisionAudio == null)
            return;

        AudioClip clip = poweringOn ? powerOnSound : powerOffSound;
        if (clip == null)
            return;

        nightVisionAudio.Stop();
        nightVisionAudio.PlayOneShot(clip, soundVolume);
    }
}
