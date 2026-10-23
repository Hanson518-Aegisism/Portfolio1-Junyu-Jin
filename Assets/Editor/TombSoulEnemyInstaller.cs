using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Upgrades the existing enemy in place, preserving scene object references and AI settings.</summary>
[InitializeOnLoad]
public static class TombSoulEnemyInstaller
{
    const string PrefabPath = "Assets/Tomb-Soul/Prefabs/TombSoul_Enemy.prefab";
    const string VisualPath = "Assets/Tomb-Soul/Prefabs/TombSoul_Visual.prefab";
    const string InstallKey = "TombSoul-EnemyTest-Installed-v1";
    static TombSoulEnemyInstaller() { EditorApplication.delayCall += AutoInstall; }

    static void AutoInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (File.Exists("Library/TombSoulEnemyInstall.done") || SessionState.GetBool(InstallKey, false)) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Test.unity") return;
        SessionState.SetBool(InstallKey, true);
        Install();
    }

    [MenuItem("Tools/Tomb Soul/Replace Enemy_Test")]
    public static void Install()
    {
        var report = new StringBuilder();
        try
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var enemies = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyChasePlayer>(true))
                .Where(e => e.name == "Enemy_Test").ToArray();
            if (enemies.Length != 1) throw new Exception("Expected exactly one Enemy_Test in active scene.");
            var enemy = enemies[0].gameObject;
            Undo.RegisterFullObjectHierarchyUndo(enemy, "Replace Enemy_Test with Tomb Soul");
            var visual = enemy.GetComponentInChildren<TombSoulAnimationDriver>(true);
            if (visual == null)
            {
                var oldCollider = enemy.GetComponent<CapsuleCollider>();
                Vector3 ground = enemy.transform.position;
                if (NavMesh.SamplePosition(ground, out var hit, 6f, NavMesh.AllAreas))
                {
                    ground = hit.position;
                    report.AppendLine("Placed on NavMesh: " + ground);
                }
                else
                {
                    if (oldCollider != null) ground.y = oldCollider.bounds.min.y;
                    report.AppendLine("No nearby NavMesh found: aligned to old collider bottom. Position=" + ground);
                }
                enemy.transform.localScale = Vector3.one;
                enemy.transform.position = ground;
                var oldRenderer = enemy.GetComponent<MeshRenderer>();
                if (oldRenderer != null) Undo.DestroyObjectImmediate(oldRenderer);
                var oldMesh = enemy.GetComponent<MeshFilter>();
                if (oldMesh != null) Undo.DestroyObjectImmediate(oldMesh);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath), scene);
                Undo.RegisterCreatedObjectUndo(model, "Add Tomb Soul model");
                model.transform.SetParent(enemy.transform, false);
                visual = model.GetComponent<TombSoulAnimationDriver>();
                var nav = enemy.GetComponent<NavMeshAgent>();
                nav.radius = 0.4f; nav.height = 1.8f; nav.baseOffset = 0f;
                if (oldCollider != null)
                {
                    oldCollider.radius = 0.4f; oldCollider.height = 1.8f;
                    oldCollider.center = Vector3.up * 0.9f;
                }
            }
            ConfigureScream(enemy);
            var mark = enemy.GetComponent<NightVisionMark>();
            if (mark == null) mark = Undo.AddComponent<NightVisionMark>(enemy);
            mark.Configure(NightVisionMark.Kind.Threat, visual.GetComponentsInChildren<Renderer>(true));
            EditorUtility.SetDirty(mark);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Tomb-Soul/Animations/TombSoul.controller");
            if (!controller.parameters.Any(p => p.name == "CrawlRate")) controller.AddParameter("CrawlRate", AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            foreach (var p in parameters) if (p.name == "CrawlRate") p.defaultFloat = 1f;
            controller.parameters = parameters;
            var crawl = controller.layers[0].stateMachine.states.First(s => s.state.name == "Crawl").state;
            crawl.speedParameterActive = true; crawl.speedParameter = "CrawlRate";
            EditorUtility.SetDirty(crawl); EditorUtility.SetDirty(controller);

            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { ConfigureScream(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed.");
            report.AppendLine("Scene saved; Enemy_Test object identity and chase settings preserved.");
            ValidateAnimation(enemy, report);
            File.WriteAllText("Library/TombSoulEnemyInstall.done", "success");
            Selection.activeGameObject = enemy;
            report.AppendLine("SUCCESS");
            Debug.Log("Enemy_Test replaced. Configure Chase Scream in EnemyChasePlayer.");
        }
        catch (Exception e) { report.AppendLine("FAILED: " + e); Debug.LogException(e); }
        finally { File.WriteAllText("Library/TombSoulEnemyInstallReport.txt", report.ToString()); }
    }

    public static void ConfigureScream(GameObject enemy)
    {
        var chase = enemy.GetComponent<EnemyChasePlayer>();
        var settings = new SerializedObject(chase);
        var property = settings.FindProperty("chaseScreamSource");
        var source = property.objectReferenceValue as AudioSource;
        if (source == null)
        {
            source = enemy.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = false;
            source.spatialBlend = 1f; source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3f; source.maxDistance = 80f;
            property.objectReferenceValue = source;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(chase);
    }

    static void ValidateAnimation(GameObject enemy, StringBuilder report)
    {
        var previewScene = EditorSceneManager.NewPreviewScene();
        GameObject clone = null;
        try
        {
            clone = UnityEngine.Object.Instantiate(enemy.GetComponentInChildren<TombSoulAnimationDriver>().gameObject);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone, previewScene);
            var animator = clone.GetComponentInChildren<Animator>();
            animator.Rebind(); animator.Update(0f);
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) throw new Exception("Initial Idle state failed.");
            animator.SetFloat("CrawlRate", 1f); animator.SetFloat("Speed", 2.2f);
            for (int i = 0; i < 30; i++) animator.Update(1f / 60f);
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Crawl")) throw new Exception("Moving transition failed.");
            var skin = clone.GetComponentInChildren<SkinnedMeshRenderer>();
            var mesh = new Mesh();
            try
            {
                skin.BakeMesh(mesh); var vertices = mesh.vertices;
                for (int i = 0; i < 30; i++) animator.Update(1f / 60f);
                skin.BakeMesh(mesh); var moved = mesh.vertices;
                if (!vertices.Where((v, i) => (v - moved[i]).sqrMagnitude > 0.000001f).Any())
                    throw new Exception("Skinned mesh does not animate.");
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
            animator.SetFloat("CrawlRate", 1.8f);
            animator.Update(0.1f);
            if (animator.GetCurrentAnimatorStateInfo(0).speedMultiplier < 1.7f) throw new Exception("Chase playback rate failed.");
            animator.SetFloat("Speed", 0f);
            for (int i = 0; i < 30; i++) animator.Update(1f / 60f);
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) throw new Exception("Stop transition failed.");
            report.AppendLine("Animator verified: Idle -> Crawl -> faster Crawl -> Idle; skinned vertices animate.");
            var audio = new SerializedObject(enemy.GetComponent<EnemyChasePlayer>());
            if (audio.FindProperty("chaseScreamSource").objectReferenceValue == null) throw new Exception("Scream source not assigned.");
            report.AppendLine("Scream source assigned; clip intentionally empty for user configuration.");
        }
        finally
        {
            if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }
}
