using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SceneAtmosphereSetup
{
    const string Request = "Library/AtmosphereRequest.txt";
    const string RootName = "Scene_Atmosphere_ADD";
    static double nextCheck;
    static SceneAtmosphereSetup() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<nextCheck)return;
        nextCheck=EditorApplication.timeSinceStartup+1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        string command;
        try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
        catch(IOException) { return; }
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/Test.unity") return;
        try { if (command == "recover") RecoverWindow(); else if (command == "build") Build(); else if(command == "capture") Capture(); else if(command == "controls") InstallControls(); else if(command == "smoke") VerifySmoke(); else Inspect(); }
        catch (System.Exception e) { File.WriteAllText("Library/AtmosphereError.txt", e.ToString()); Debug.LogException(e); }
    }
    static void Inspect()
    {
        var sb = new StringBuilder();
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        sb.AppendLine("Pipeline="+pipeline+" Depth="+(pipeline != null && pipeline.supportsCameraDepthTexture));
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (new[]{"Firstroom","Mainhall","Minearea","Pathway","ceilings","Fix","Pipe trap"}.Contains(root.name))
            {
                var rs=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
                if(rs.Length>0) { var b=rs[0].bounds; foreach(var r in rs)b.Encapsulate(r.bounds); sb.AppendLine(root.name+" center="+b.center+" size="+b.size); }
            }
        }
        foreach(var c in Object.FindObjectsOfType<Camera>()) sb.AppendLine("Camera "+c.name+" pos="+c.transform.position+" angles="+c.transform.eulerAngles+" enabled="+c.enabled);
        var view=SceneView.lastActiveSceneView;
        if(view!=null) sb.AppendLine("SceneCamera pos="+view.camera.transform.position+" angles="+view.camera.transform.eulerAngles);
        File.WriteAllText("Library/AtmosphereInspection.txt",sb.ToString());
        Capture();
    }
    static void RecoverWindow()
    {
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        bool restored=EditorApplication.ExecuteMenuItem("Window/Layouts/Default");
        var view=EditorWindow.GetWindow<SceneView>();
        view.Show();
        view.Focus();
        File.WriteAllText("Library/AtmosphereWindowRecovery.txt","Scene and assets saved. Default layout command="+restored+"; Scene view shown.");
    }
    [MenuItem("Tools/Environment/Add Scene Volumetric Fog")]
    public static void Build()
    {
        if(GameObject.Find(RootName)!=null) { Debug.Log("Scene atmosphere already exists."); return; }
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Test.unity" || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Copy(scene.path,"Library/Test-before-atmosphere.unity",true);
        var shader=Shader.Find("Portfolio/Atmosphere/VolumetricFog");
        if(shader==null || !shader.isSupported) throw new System.Exception("Volumetric fog shader unavailable");
        Directory.CreateDirectory("Assets/Atmosphere");
        var mat=new Material(shader){name="Scene Volumetric Haze"};
        AssetDatabase.CreateAsset(mat,"Assets/Atmosphere/SceneVolumetricHaze.mat");
        var root=new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root,"Add scene volumetric atmosphere");
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name="Volumetric_Haze_WholeScene";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(root.transform,false);
        go.transform.position=new Vector3(-20,3,-82);
        go.transform.localScale=new Vector3(272,12,238);
        var renderer=go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=mat;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        renderer.lightProbeUsage=LightProbeUsage.Off;
        renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Library/AtmosphereBuilt.txt","Saved "+scene.path+" with isolated raymarched fog volume. Existing objects and RenderSettings unchanged.");
        Capture();
    }
    [MenuItem("Tools/Environment/Select Fog Controls")]
    public static void InstallControls()
    {
        var root=GameObject.Find(RootName);
        if(root==null)return;
        var controller=root.GetComponent<SceneFogController>();
        if(controller==null)controller=Undo.AddComponent<SceneFogController>(root);
        controller.Apply();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        Selection.activeGameObject=root;
        EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        SceneView.RepaintAll();
        File.WriteAllText("Library/AtmosphereControls.txt", "Saved fog controls. Concentration="+controller.concentration+" Opacity="+controller.maximumOpacity+" Brightness="+controller.brightness);
        Capture();
    }
    static void Shot(Camera source, string name, Vector3? pos=null, Vector3? look=null)
    {
        var go=new GameObject("AtmosphereVerificationCamera"){hideFlags=HideFlags.HideAndDontSave};
        var cam=go.AddComponent<Camera>();
        cam.CopyFrom(source); cam.enabled=false;
        cam.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        if(pos.HasValue) {cam.transform.position=pos.Value; cam.transform.LookAt(look.Value); cam.fieldOfView=70;}
        cam.nearClipPlane=0.08f; cam.farClipPlane=350;
        var data=cam.GetUniversalAdditionalCameraData(); data.requiresDepthTexture=true;
        var original=source.GetComponent<UniversalAdditionalCameraData>();
        if(original!=null) {data.renderPostProcessing=original.renderPostProcessing; data.volumeLayerMask=original.volumeLayerMask;}
        var rt=RenderTexture.GetTemporary(1200,750,24,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;
        try {
            cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt;
            var tex=new Texture2D(1200,750,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,1200,750),0,0); tex.Apply();
            File.WriteAllBytes("Library/Atmosphere-"+name+".png",tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        } finally { cam.targetTexture=null; RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(go); }
    }
    static void VerifySmoke()
    {
        InstallControls();
        var root=GameObject.Find(RootName);
        var controller=root!=null?root.GetComponent<SceneFogController>():null;
        if(controller==null || Camera.main==null)return;
        try
        {
            controller.ApplyAtTime(0);
            Shot(Camera.main,"Smoke-A");
            controller.ApplyAtTime(8);
            Shot(Camera.main,"Smoke-B");
            controller.ApplyAtTime(0);
            Shot(Camera.main,"Smoke-Mine",new Vector3(-73,2.2f,-162),new Vector3(-74,2,-120));
        }
        finally { controller.Apply(); }
        File.WriteAllText("Library/AtmosphereSmokeVerified.txt", "Saved drifting smoke. Speed="+controller.driftSpeed+" Size="+controller.smokeSize+" Breakup="+controller.smokeBreakup+". Captured same camera at 0 and 8 seconds.");
    }
    static void Capture()
    {
        var view=SceneView.lastActiveSceneView;
        var source=view!=null?view.camera:Camera.main;
        if(source==null)return;
        Shot(source,"Current");
        if(Camera.main!=null) Shot(Camera.main,"Gameplay");
        Shot(source,"Mine",new Vector3(-73,2.2f,-162),new Vector3(-74,2,-120));
        Shot(source,"Hall",new Vector3(15,2.2f,-68),new Vector3(20,2,-108));
        Shot(source,"Entrance",new Vector3(26,2.2f,10),new Vector3(21,2,-14));
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Atmosphere/SceneVolumetricHaze.mat");
        if(mat!=null) File.WriteAllText("Library/AtmosphereShaderCheck.txt",string.Join("\n",ShaderUtil.GetShaderMessages(mat.shader).Select(m=>m.severity+": "+m.message)));
    }
}

[CustomEditor(typeof(SceneFogController))]
public sealed class SceneFogControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("场景体积雾", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("运行时烟雾自动飘动。可手动开启编辑模式预览；疏密变化越高，烟团之间的空隙越明显。调整后保存场景。",MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("previewInEditor"),new GUIContent("编辑模式预览", "开启后持续重绘 Scene 视图；关闭可降低编辑器负载，运行时仍自动飘动"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("concentration"),new GUIContent("雾浓度", "0 = 无雾，100 = 浓雾"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumOpacity"),new GUIContent("最大遮挡程度", "提高后，远处物体会被雾遮挡得更多"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("brightness"),new GUIContent("雾亮度", "在暗场景中提高雾的可见度"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("heightFalloff"),new GUIContent("高度衰减", "越小，雾向高处覆盖得越多"));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("烟雾飘动", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("driftSpeed"),new GUIContent("飘动速度", "0 为静止，数值越大飘动越快"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("smokeSize"),new GUIContent("烟团大小", "增大形成大团烟雾，减小形成细碎烟雾"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("smokeBreakup"),new GUIContent("疏密变化", "0 为均匀雾，1 为疏密分明的烟团"));
        if(serializedObject.ApplyModifiedProperties())
        {
            ((SceneFogController)target).Apply();
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
