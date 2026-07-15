using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SteamTrapSceneSetup
{
    private const string PrefabRoot =
        "Assets/3D Models/Props/Industrial/Pipeline/Update_1.03/Prefabs/";

    [MenuItem("Tools/Steam Trap/Setup On Plane")]
    public static void SetupOnPlane()
    {
        GameObject existing = GameObject.Find("PipeTrap");
        if (existing != null)
            Object.DestroyImmediate(existing);

        EnsureSteamMaterial();

        GameObject plane = GameObject.Find("Pipe trap");
        Vector3 rootPos = new Vector3(-47.2f, 0f, 4.5f);
        Vector3 rootEuler = new Vector3(0f, 45f, 0f);
        if (plane != null)
        {
            Renderer planeRenderer = plane.GetComponent<Renderer>();
            if (planeRenderer != null)
            {
                Bounds bounds = planeRenderer.bounds;
                rootPos = new Vector3(-47.2f, bounds.min.y + 0.05f, 4.5f);
                Vector3 look = new Vector3(bounds.center.x, rootPos.y, bounds.center.z) - rootPos;
                look.y = 0f;
                if (look.sqrMagnitude > 0.001f)
                    rootEuler = Quaternion.LookRotation(look.normalized).eulerAngles;
            }
            else
            {
                rootPos = plane.transform.position + new Vector3(3f, 0.1f, -8f);
            }
        }

        GameObject root = new GameObject("PipeTrap");
        root.transform.position = rootPos;
        root.transform.eulerAngles = rootEuler;

        Transform visuals = CreateChild(root.transform, "Visuals", Vector3.zero, Vector3.zero);

        Transform supports = CreateChild(visuals, "Supports", Vector3.zero, Vector3.zero);
        Spawn("Small/S_Support_01.prefab", supports, new Vector3(-0.8f, 0f, 0f), Vector3.zero, 1f);
        Spawn("Small/S_Support_02.prefab", supports, new Vector3(0.8f, 0f, 0f), Vector3.zero, 1f);

        Transform pipes = CreateChild(visuals, "Pipes", Vector3.zero, Vector3.zero);
        Spawn("Medium/M_Pipe_Yellow_06.prefab", pipes, new Vector3(-1.4f, 1.4f, 0.1f), new Vector3(0f, 0f, 90f), 1f);
        Spawn("Medium/M_Pipe_L100_Yellow_01.prefab", pipes, new Vector3(0f, 1.4f, 0.1f), new Vector3(0f, 0f, 90f), 1f);
        Spawn("Medium/M_Pipe_L100_Yellow_01.prefab", pipes, new Vector3(1.2f, 1.4f, 0.1f), new Vector3(0f, 0f, 90f), 1f);
        Spawn("Medium/M_Pipe_Corner90_Yellow_13.prefab", pipes, new Vector3(-2.1f, 1.4f, 0.1f), new Vector3(0f, 90f, 0f), 1f);

        Transform valveRoot = CreateChild(visuals, "InteractValve", new Vector3(-0.2f, 1.55f, 0.55f), Vector3.zero);
        Spawn("Medium/M_Valve01_Yellow_BoltX8_01.prefab", valveRoot, Vector3.zero, new Vector3(0f, 180f, 0f), 1f);

        Transform handlePivot = CreateChild(valveRoot, "ValveHandlePivot", new Vector3(0f, 0.05f, 0.22f), Vector3.zero);
        Spawn("Medium/M_Valve_Handle_Yellow_01.prefab", handlePivot, Vector3.zero, Vector3.zero, 1f);

        Transform nozzles = CreateChild(visuals, "ExhaustNozzles", Vector3.zero, Vector3.zero);
        float[] nozzleOffsets = { -1.2f, -0.4f, 0.4f, 1.2f };
        for (int i = 0; i < nozzleOffsets.Length; i++)
        {
            Spawn(
                "Medium/M_PipeValve_Yellow_01.prefab",
                nozzles,
                new Vector3(nozzleOffsets[i], 1.55f, 0.65f),
                new Vector3(0f, 180f, 0f),
                1f);
        }

        Spawn("Manometr.prefab", visuals, new Vector3(0.85f, 2.05f, 0.45f), new Vector3(0f, 180f, 0f), 1.2f);

        Transform lightsRoot = CreateChild(visuals, "ChargeLights", new Vector3(0.85f, 2.35f, 0.55f), Vector3.zero);
        GameObject[] chargeLights = new GameObject[3];
        Material lightMat = CreateEmissiveRedMaterial();
        for (int i = 0; i < 3; i++)
        {
            GameObject lightObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lightObj.name = "ChargeLight_" + (i + 1);
            Object.DestroyImmediate(lightObj.GetComponent<Collider>());
            lightObj.transform.SetParent(lightsRoot, false);
            lightObj.transform.localPosition = new Vector3((i - 1) * 0.12f, 0f, 0f);
            lightObj.transform.localScale = Vector3.one * 0.06f;
            lightObj.GetComponent<MeshRenderer>().sharedMaterial = lightMat;

            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.15f, 0.05f);
            pointLight.intensity = 1.8f;
            pointLight.range = 1.2f;
            chargeLights[i] = lightObj;
        }

        Material steamMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SteamTrap/SteamParticle.mat");
        GameObject steamVfx = CreateChild(root.transform, "SteamVFX", new Vector3(0f, 1.5f, 1.2f), Vector3.zero).gameObject;
        steamVfx.SetActive(false);

        for (int i = 0; i < nozzleOffsets.Length; i++)
        {
            GameObject emitter = new GameObject("SteamEmitter_" + i);
            emitter.transform.SetParent(steamVfx.transform, false);
            emitter.transform.localPosition = new Vector3(nozzleOffsets[i], 0.05f, 0.1f);
            emitter.transform.localEulerAngles = new Vector3(-8f, 0f, 0f);
            ConfigureNozzleParticles(emitter.AddComponent<ParticleSystem>(), steamMat);
        }

        GameObject volumeFog = new GameObject("SteamVolumeFog");
        volumeFog.transform.SetParent(steamVfx.transform, false);
        volumeFog.transform.localPosition = new Vector3(0f, 0.4f, 3.5f);
        ConfigureVolumeParticles(volumeFog.AddComponent<ParticleSystem>(), steamMat);

        GameObject zoneObj = CreateChild(root.transform, "SteamVisionZone", new Vector3(0f, 1.4f, 3.8f), Vector3.zero).gameObject;
        BoxCollider box = zoneObj.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(6f, 3f, 8f);
        SteamVisionObscurer obscurer = zoneObj.AddComponent<SteamVisionObscurer>();

        GameObject audioObj = CreateChild(root.transform, "SteamAudio", new Vector3(0f, 1.5f, 1f), Vector3.zero).gameObject;
        AudioSource audio = audioObj.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.minDistance = 2f;
        audio.maxDistance = 40f;

        SteamTrapController controller = root.AddComponent<SteamTrapController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("maxUses").intValue = 3;
        so.FindProperty("burstDuration").floatValue = 15f;
        so.FindProperty("cooldownDuration").floatValue = 10f;
        SerializedProperty effectsProp = so.FindProperty("steamEffectObjects");
        effectsProp.arraySize = 1;
        effectsProp.GetArrayElementAtIndex(0).objectReferenceValue = steamVfx;
        so.FindProperty("steamVisionZone").objectReferenceValue = box;
        so.FindProperty("visionObscurer").objectReferenceValue = obscurer;
        SerializedProperty lightsProp = so.FindProperty("chargeLights");
        lightsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            lightsProp.GetArrayElementAtIndex(i).objectReferenceValue = chargeLights[i];
        so.FindProperty("audioSource").objectReferenceValue = audio;
        so.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soVis = new SerializedObject(obscurer);
        soVis.FindProperty("trapController").objectReferenceValue = controller;
        soVis.ApplyModifiedPropertiesWithoutUndo();

        TextMeshProUGUI promptTmp = null;
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform existingSteamPrompt = canvas.transform.Find("SteamValvePrompt");
            GameObject steamPromptGo;
            if (existingSteamPrompt != null)
            {
                steamPromptGo = existingSteamPrompt.gameObject;
            }
            else
            {
                TextMeshProUGUI template = null;
                TextMeshProUGUI[] prompts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
                for (int i = 0; i < prompts.Length; i++)
                {
                    TextMeshProUGUI candidate = prompts[i];
                    if (candidate == null || candidate.gameObject.name != "SwitchPrompt")
                        continue;
                    if (!candidate.gameObject.scene.IsValid())
                        continue;

                    template = candidate;
                    break;
                }

                if (template != null)
                {
                    steamPromptGo = (GameObject)Object.Instantiate(template.gameObject, canvas.transform);
                    steamPromptGo.name = "SteamValvePrompt";
                }
                else
                {
                    steamPromptGo = new GameObject("SteamValvePrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    steamPromptGo.transform.SetParent(canvas.transform, false);
                    RectTransform rect = steamPromptGo.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, 80f);
                    rect.sizeDelta = new Vector2(600f, 50f);
                }
            }

            steamPromptGo.SetActive(false);
            promptTmp = steamPromptGo.GetComponent<TextMeshProUGUI>();
            if (promptTmp != null)
                promptTmp.text = "Press E to open emergency valve (3/3)";
        }

        SteamValveInteract interact = valveRoot.gameObject.AddComponent<SteamValveInteract>();
        AudioClip turnClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/switch on.mp3");
        SerializedObject soInt = new SerializedObject(interact);
        soInt.FindProperty("trapController").objectReferenceValue = controller;
        soInt.FindProperty("promptText").objectReferenceValue = promptTmp;
        soInt.FindProperty("valveHandle").objectReferenceValue = handlePivot;
        soInt.FindProperty("localSpinAxis").vector3Value = Vector3.up;
        soInt.FindProperty("openAngleDegrees").floatValue = 160f;
        soInt.FindProperty("turnDuration").floatValue = 0.55f;
        soInt.FindProperty("turnSound").objectReferenceValue = turnClip;
        soInt.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("PipeTrap setup complete on Plane. Cooldown is adjustable on SteamTrapController.");
    }

    private static Transform CreateChild(Transform parent, string name, Vector3 localPos, Vector3 localEuler)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        return go.transform;
    }

    private static GameObject Spawn(string relativePath, Transform parent, Vector3 localPos, Vector3 localEuler, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + relativePath);
        if (prefab == null)
        {
            Debug.LogWarning("Missing pipeline prefab: " + relativePath);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPos;
        instance.transform.localEulerAngles = localEuler;
        instance.transform.localScale = Vector3.one * scale;
        return instance;
    }

    private static void EnsureSteamMaterial()
    {
        if (!AssetDatabase.IsValidFolder("Assets/SteamTrap"))
            AssetDatabase.CreateFolder("Assets", "SteamTrap");

        string matPath = "Assets/SteamTrap/SteamParticle.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Texture2D mistTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/StylizedWater2/Materials/Effects/Textures/WaterMist_5x5.png");

        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        ApplySoftSteamMaterial(mat, mistTex);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }

    private static void ApplySoftSteamMaterial(Material mat, Texture2D mistTex)
    {
        if (mistTex != null && mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", mistTex);

        Color steamColor = new Color(0.95f, 0.97f, 1f, 0.72f);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", steamColor);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", steamColor);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend"))
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);

        if (mat.HasProperty("_SoftParticlesEnabled"))
            mat.SetFloat("_SoftParticlesEnabled", 1f);
        if (mat.HasProperty("_SoftParticlesNearFadeDistance"))
            mat.SetFloat("_SoftParticlesNearFadeDistance", 0.5f);
        if (mat.HasProperty("_SoftParticlesFarFadeDistance"))
            mat.SetFloat("_SoftParticlesFarFadeDistance", 2.5f);
        if (mat.HasProperty("_SoftParticleFadeParams"))
            mat.SetVector("_SoftParticleFadeParams", new Vector4(0.5f, 1f / 2f, 0f, 0f));

        if (mat.HasProperty("_CameraFadingEnabled"))
            mat.SetFloat("_CameraFadingEnabled", 1f);
        if (mat.HasProperty("_CameraNearFadeDistance"))
            mat.SetFloat("_CameraNearFadeDistance", 0.4f);
        if (mat.HasProperty("_CameraFarFadeDistance"))
            mat.SetFloat("_CameraFarFadeDistance", 1.2f);
        if (mat.HasProperty("_CameraFadeParams"))
            mat.SetVector("_CameraFadeParams", new Vector4(0.4f, 1f / 0.8f, 0f, 0f));

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_SOFTPARTICLES_ON");
        mat.EnableKeyword("_FADING_ON");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetShaderPassEnabled("ShadowCaster", false);
        mat.SetShaderPassEnabled("DepthOnly", false);
        mat.renderQueue = 3000;
    }

    private static void ConfigureSoftSteamParticles(ParticleSystem ps)
    {
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.92f, 0.95f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.12f),
                new GradientAlphaKey(0.65f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.5f));

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Grid;
        tsa.numTilesX = 5;
        tsa.numTilesY = 5;
        tsa.animation = ParticleSystemAnimationType.WholeSheet;
        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        tsa.startFrame = new ParticleSystem.MinMaxCurve(0f, 24f);
        tsa.cycleCount = 1;
    }

    private static Material CreateEmissiveRedMaterial()
    {
        string path = "Assets/SteamTrap/ChargeLightRed.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        if (!AssetDatabase.IsValidFolder("Assets/SteamTrap"))
            AssetDatabase.CreateFolder("Assets", "SteamTrap");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_BaseColor", new Color(0.5f, 0.02f, 0.02f));
        mat.SetColor("_EmissionColor", new Color(2.5f, 0.15f, 0.05f));
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static void ConfigureNozzleParticles(ParticleSystem ps, Material steamMat)
    {
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 4.2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.4f, 3.0f);
        main.startColor = new Color(0.95f, 0.97f, 1f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 650;
        main.gravityModifier = -0.03f;

        var emission = ps.emission;
        emission.rateOverTime = 55f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.12f;

        ConfigureSoftSteamParticles(ps);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = steamMat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void ConfigureVolumeParticles(ParticleSystem ps, Material steamMat)
    {
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 9f;
        main.startSpeed = 0.15f;
        main.startSize = new ParticleSystem.MinMaxCurve(3.2f, 5.8f);
        main.startColor = new Color(0.93f, 0.95f, 0.98f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;

        var emission = ps.emission;
        emission.rateOverTime = 28f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6.5f, 2.6f, 8f);

        ConfigureSoftSteamParticles(ps);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = steamMat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
