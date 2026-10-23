using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class TombSoulPrefabBuilder
{
    const string Root = "Assets/Tomb-Soul/";
    const string Version = "tombsoul-prefabs-v6";
    static TombSoulPrefabBuilder() { EditorApplication.delayCall += AutoBuild; }

    static void AutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Version, false)) return;
        if (File.Exists(Root + "Prefabs/TombSoul_Enemy.prefab") && File.Exists(Root + "Prefabs/TombSoul_Visual.prefab")) return;
        SessionState.SetBool(Version, true);
        Build();
    }

    [MenuItem("Tools/Tomb Soul/Build Ready To Use Prefabs")]
    public static void Build()
    {
        GameObject visual = null;
        GameObject enemy = null;
        var report = new StringBuilder();
        try
        {
            Directory.CreateDirectory(Root + "Prefabs");
            Directory.CreateDirectory(Root + "Materials");
            Directory.CreateDirectory(Root + "Animations");
            AssetDatabase.Refresh();
            ConfigureTexture("FantasmaTumba_TexCuerpo.png", false);
            ConfigureTexture("FantasmaTumba_NormalCuerpo.png", true);
            foreach (string name in new[] { "TPose", "Idle", "Crawl" }) ConfigureModel(name);
            var idle = LoadClip("Idle");
            var crawl = LoadClip("Crawl");
            if (idle == null || crawl == null) throw new Exception("Idle/Crawl animation missing.");
            report.AppendLine($"Idle: {idle.name}, {idle.length:F3}s; Crawl: {crawl.name}, {crawl.length:F3}s");
            string[] idlePaths = AnimationUtility.GetCurveBindings(idle).Select(b => b.path).Distinct().ToArray();
            string[] crawlPaths = AnimationUtility.GetCurveBindings(crawl).Select(b => b.path).Distinct().ToArray();
            report.AppendLine("Animation paths: Idle=" + idlePaths.Length + ", Crawl=" + crawlPaths.Length);

            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/TombSoul_Body.mat");
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new Exception("URP Lit shader unavailable.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, Root + "Materials/TombSoul_Body.mat");
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "textures/FantasmaTumba_TexCuerpo.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "textures/FantasmaTumba_NormalCuerpo.png"));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 1f);
            material.SetFloat("_Smoothness", 0.25f);
            material.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(material);

            string controllerPath = Root + "Animations/TombSoul.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            foreach (var old in controller.layers[0].stateMachine.states) controller.layers[0].stateMachine.RemoveState(old.state);
            controller.parameters = new[] {
                new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float },
                new AnimatorControllerParameter { name = "CrawlRate", type = AnimatorControllerParameterType.Float, defaultFloat = 1f }
            };
            var sm = controller.layers[0].stateMachine;
            var idleState = sm.AddState("Idle"); idleState.motion = idle;
            var crawlState = sm.AddState("Crawl"); crawlState.motion = crawl;
            crawlState.speedParameterActive = true; crawlState.speedParameter = "CrawlRate";
            sm.defaultState = idleState;
            var moving = idleState.AddTransition(crawlState);
            moving.hasExitTime = false; moving.duration = 0.15f;
            moving.AddCondition(AnimatorConditionMode.Greater, 0.08f, "Speed");
            var stopping = crawlState.AddTransition(idleState);
            stopping.hasExitTime = false; stopping.duration = 0.15f;
            stopping.AddCondition(AnimatorConditionMode.Less, 0.06f, "Speed");
            EditorUtility.SetDirty(controller);

            visual = new GameObject("TombSoul_Visual");
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "fbx/TombSoul-TPose.fbx");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            model.name = "Model";
            model.transform.SetParent(visual.transform, false);
            // The mesh source wraps its deform skeleton in Armature; animation FBXs do not.
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.updateWhenOffscreen = true;
            var originalAnimator = model.GetComponent<Animator>();
            if (originalAnimator != null) UnityEngine.Object.DestroyImmediate(originalAnimator);
            var rig = model.transform.Find("Armature").gameObject;
            var animator = rig.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (string path in crawlPaths)
                if (!string.IsNullOrEmpty(path) && rig.transform.Find(path) == null) throw new Exception("Crawl binding missing: " + path);
            idle.SampleAnimation(rig, 0f);
            var bounds = BoundsOf(model);
            report.AppendLine("Imported Idle bounds: " + bounds);
            if (bounds.size.y < 0.001f) throw new Exception("Invalid model bounds.");
            model.transform.localScale *= 1.8f / bounds.size.y;
            bounds = BoundsOf(model);
            model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            visual.AddComponent<TombSoulAnimationDriver>();
            PrefabUtility.SaveAsPrefabAsset(visual, Root + "Prefabs/TombSoul_Visual.prefab");
            report.AppendLine("Visual height normalized to 1.8m, origin at ground, root motion off.");

            enemy = new GameObject("TombSoul_Enemy");
            var child = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/TombSoul_Visual.prefab"));
            child.transform.SetParent(enemy.transform, false);
            var nav = enemy.AddComponent<NavMeshAgent>();
            nav.radius = 0.4f; nav.height = 1.8f; nav.baseOffset = 0f;
            nav.speed = 3.5f; nav.acceleration = 12f; nav.angularSpeed = 360f; nav.stoppingDistance = 0.5f;
            var collider = enemy.AddComponent<CapsuleCollider>();
            collider.radius = 0.4f; collider.height = 1.8f; collider.center = Vector3.up * 0.9f;
            enemy.AddComponent<EnemyChasePlayer>();
            TombSoulEnemyInstaller.ConfigureScream(enemy);
            enemy.AddComponent<NightVisionMark>().Configure(NightVisionMark.Kind.Threat, child.GetComponentsInChildren<Renderer>(true));
            PrefabUtility.SaveAsPrefabAsset(enemy, Root + "Prefabs/TombSoul_Enemy.prefab");
            AssetDatabase.SaveAssets();
            // Sample both clips and record whether the skeleton actually changes.
            idle.SampleAnimation(rig, Mathf.Min(0.3f, idle.length * 0.5f));
            var pose = model.GetComponentsInChildren<Transform>().Select(t => t.localRotation).ToArray();
            crawl.SampleAnimation(rig, crawl.length * 0.5f);
            int changed = model.GetComponentsInChildren<Transform>().Select((t, i) => Quaternion.Angle(t.localRotation, pose[i]) > 0.1f).Count(v => v);
            if (changed == 0) throw new Exception("Animation validation: no bones changed between Idle and Crawl.");
            report.AppendLine("Animation validation passed: changed bone rotations=" + changed);
            idle.SampleAnimation(rig, 0f);
            RenderPreview(visual, Root + "TombSoul_Idle.png");
            crawl.SampleAnimation(rig, crawl.length * 0.5f);
            RenderPreview(visual, Root + "TombSoul_Preview.png");
            report.AppendLine("SUCCESS: both prefabs saved, animation bindings and material checked.");
            Debug.Log("Tomb Soul prefabs created successfully.");
        }
        catch (Exception e) { report.AppendLine("FAILED: " + e); Debug.LogException(e); }
        finally
        {
            if (visual != null) UnityEngine.Object.DestroyImmediate(visual);
            if (enemy != null) UnityEngine.Object.DestroyImmediate(enemy);
            File.WriteAllText("Library/TombSoulBuildReport.txt", report.ToString());
        }
    }

    static void ConfigureTexture(string name, bool normal)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "textures/" + name);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal;
        importer.SaveAndReimport();
    }

    static void ConfigureModel(string name)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(Root + "fbx/TombSoul-" + name + ".fbx");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importCameras = false; importer.importLights = false;
        var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.loopTime = true; clip.loopPose = true;
            clip.lockRootRotation = true; clip.lockRootHeightY = true; clip.lockRootPositionXZ = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    static AnimationClip LoadClip(string name) => AssetDatabase.LoadAllAssetsAtPath(Root + "fbx/TombSoul-" + name + ".fbx")
        .OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new Exception("No model renderers.");
        var b = new Bounds();
        bool first = true;
        foreach (var r in renderers)
        {
            var skin = r as SkinnedMeshRenderer;
            if (skin != null)
            {
                var mesh = new Mesh();
                skin.BakeMesh(mesh);
                foreach (var vertex in mesh.vertices)
                {
                    var point = skin.transform.TransformPoint(vertex);
                    if (first) { b = new Bounds(point, Vector3.zero); first = false; }
                    else b.Encapsulate(point);
                }
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            else { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        }
        return b;
    }

    static void RenderPreview(GameObject visual, string path)
    {
        var preview = new PreviewRenderUtility();
        Texture2D result = null;
        try
        {
            var clone = UnityEngine.Object.Instantiate(visual);
            preview.AddSingleGO(clone);
            var bounds = BoundsOf(clone);
            float distance = bounds.size.magnitude * 2.2f;
            preview.camera.transform.position = bounds.center + new Vector3(0.5f, 0.2f, 1f).normalized * distance;
            preview.camera.transform.LookAt(bounds.center);
            preview.camera.nearClipPlane = 0.01f; preview.camera.farClipPlane = 100f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.12f, 0.14f, 0.17f);
            preview.lights[0].intensity = 1.2f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35, 150, 0);
            preview.lights[1].intensity = 0.8f;
            preview.BeginStaticPreview(new Rect(0, 0, 768, 768));
            preview.camera.fieldOfView = 35f;
            preview.camera.aspect = 1f;
            preview.camera.transform.position = bounds.center + new Vector3(0.5f, 0.2f, 1f).normalized * distance;
            preview.camera.transform.LookAt(bounds.center);
            preview.camera.Render();
            result = preview.EndStaticPreview();
            File.WriteAllBytes(path, result.EncodeToPNG());
        }
        finally { if (result != null) UnityEngine.Object.DestroyImmediate(result); preview.Cleanup(); }
        AssetDatabase.ImportAsset(path);
    }
}
