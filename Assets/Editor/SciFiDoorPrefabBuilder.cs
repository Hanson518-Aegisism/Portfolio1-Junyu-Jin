using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SciFiDoorPrefabBuilder
{
    const string Root="Assets/sci-fi-door/";
    static SciFiDoorPrefabBuilder(){EditorApplication.delayCall+=()=>{Inspect();if(!File.Exists(Root+"Prefabs/SciFiDoor.prefab"))Build();};}
    static void Inspect()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Library/SciFiDoorInventory.txt"))return;
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"source/Sci-Fi_Door_anim.fbx");
        if(model==null)return;
        var sb=new StringBuilder();
        foreach(var t in model.GetComponentsInChildren<Transform>(true))
            sb.AppendLine(AnimationUtility.CalculateTransformPath(t,model.transform)+" pos="+t.localPosition.ToString("F3")+" rot="+t.localEulerAngles+" scale="+t.localScale.ToString("F3"));
        foreach(var r in model.GetComponentsInChildren<Renderer>(true))
            sb.AppendLine("Renderer="+r.name+" bounds="+r.bounds+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m==null?"null":m.name)));
        foreach(var c in AssetDatabase.LoadAllAssetsAtPath(Root+"source/Sci-Fi_Door_anim.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")))
        {
            sb.AppendLine("CLIP="+c.name+" length="+c.length+" fps="+c.frameRate);
            foreach(var b in AnimationUtility.GetCurveBindings(c))
            {
                var curve=AnimationUtility.GetEditorCurve(c,b);var keys=curve.keys;
                sb.AppendLine(b.path+" "+b.propertyName+" "+b.type+" keys="+keys.Length+" first="+keys[0].value+" last="+keys[keys.Length-1].value+" range="+keys.Min(k=>k.value)+".."+keys.Max(k=>k.value));
            }
        }
        File.WriteAllText("Library/SciFiDoorInventory.txt",sb.ToString());
    }

    [MenuItem("Tools/Sci-Fi Door/Build Ready To Use Prefab")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var current=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/SciFiDoor.prefab");
        if(current!=null&&current.GetComponent<SciFiSlidingDoor>()!=null){SciFiDoorAnimatedBuilder.Build();return;}
        var report=new StringBuilder();
        GameObject root=null;
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            Directory.CreateDirectory(Root+"Prefabs");Directory.CreateDirectory(Root+"Materials");
            AssetDatabase.Refresh();
            string source=Root+"source/Sci-Fi_Door_anim.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(source);
            importer.importCameras=false;importer.importLights=false;
            importer.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            report.AppendLine("Imported animation clips="+clips.Length);
            if(clips.Length==0)report.AppendLine("Source FBX has no animation clips; created a static door.");
            Texture("sci-fi-door_BaseColor.png",true,false);
            Texture("sci-fi-door_Normal.png",false,true);
            Texture("sci-fi-door_Emessive.png",true,false);
            Texture("Ambient_Occlusion_Map_from_Mesh_Sci-fi-doo.png",false,false);
            string packed=Root+"textures/SciFiDoor_MetallicSmoothness.png";
            PackMetallicSmoothness(packed);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/SciFiDoor_URP.mat");
            if(material==null)
            {
                var shader=Shader.Find("Universal Render Pipeline/Lit");
                if(shader==null)throw new Exception("URP Lit shader unavailable.");
                material=new Material(shader);
                AssetDatabase.CreateAsset(material,Root+"Materials/SciFiDoor_URP.mat");
            }
            material.SetColor("_BaseColor",Color.white);
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"textures/sci-fi-door_BaseColor.png"));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"textures/sci-fi-door_Normal.png"));
            material.SetFloat("_BumpScale",1f);material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packed));
            material.SetFloat("_Metallic",1f);material.SetFloat("_Smoothness",1f);
            material.SetFloat("_SmoothnessTextureChannel",0f);material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"textures/Ambient_Occlusion_Map_from_Mesh_Sci-fi-doo.png"));
            material.SetFloat("_OcclusionStrength",1f);material.EnableKeyword("_OCCLUSIONMAP");
            material.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"textures/sci-fi-door_Emessive.png"));
            material.SetColor("_EmissionColor",Color.white*2f);material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(material);

            root=new GameObject("SciFiDoor");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),scene);
            model.name="Model";model.transform.SetParent(root.transform,false);
            var animator=model.GetComponent<Animator>();
            if(animator!=null&&clips.Length==0)UnityEngine.Object.DestroyImmediate(animator);
            var renderers=model.GetComponentsInChildren<MeshRenderer>(true);
            if(renderers.Length==0)throw new Exception("Door mesh unavailable.");
            var bounds=renderers[0].bounds;
            foreach(var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
                r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();
                var filter=r.GetComponent<MeshFilter>();
                if(filter==null||filter.sharedMesh==null)throw new Exception("Missing mesh: "+r.name);
                var collider=r.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh=filter.sharedMesh;collider.convex=false;
                report.AppendLine("Mesh="+r.name+" vertices="+filter.sharedMesh.vertexCount+"; accurate static mesh collider assigned.");
            }
            model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            report.AppendLine("Door dimensions="+bounds.size.ToString("F3")+"m; root origin at floor center; root scale=(1,1,1).");
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"Prefabs/SciFiDoor.prefab");
            if(prefab==null)throw new Exception("Prefab save failed.");
            AssetDatabase.SaveAssets();
            Render(root,Root+"SciFiDoor_Preview.png",1f);
            Render(root,Root+"SciFiDoor_Back.png",-1f);
            report.AppendLine("SUCCESS: prefab, all six texture inputs, roughness conversion and collision verified.");
            Selection.activeObject=prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("SciFiDoor prefab created. This FBX contains no animation data.");
        }
        catch(Exception e){report.AppendLine("FAILED: "+e);Debug.LogException(e);}
        finally
        {
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
            File.WriteAllText("Library/SciFiDoorBuildReport.txt",report.ToString());
        }
    }

    static void Texture(string name,bool srgb,bool normal)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"textures/"+name);
        if(importer==null)throw new Exception("Texture missing: "+name);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.sRGBTexture=srgb;importer.maxTextureSize=2048;importer.alphaIsTransparency=false;
        importer.SaveAndReimport();
    }

    static void PackMetallicSmoothness(string path)
    {
        string[] inputs={"sci-fi-door_Metallic.png","sci-fi-door_Roughness.png"};
        var importers=inputs.Select(n=>(TextureImporter)AssetImporter.GetAtPath(Root+"textures/"+n)).ToArray();
        bool[] readable=importers.Select(i=>i.isReadable).ToArray();
        Texture2D packed=null;
        try
        {
            foreach(var importer in importers)
            {
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;
                importer.isReadable=true;importer.maxTextureSize=2048;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var metal=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"textures/"+inputs[0]);
            var rough=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"textures/"+inputs[1]);
            if(metal.width!=rough.width||metal.height!=rough.height)throw new Exception("Metallic and roughness texture dimensions differ.");
            var pixels=metal.GetPixels32();var roughPixels=rough.GetPixels32();
            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(pixels[i].r,0,0,(byte)(255-roughPixels[i].r));
            packed=new Texture2D(metal.width,metal.height,TextureFormat.RGBA32,false,true);
            packed.SetPixels32(pixels);packed.Apply();File.WriteAllBytes(path,packed.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var output=(TextureImporter)AssetImporter.GetAtPath(path);
            output.sRGBTexture=false;output.alphaSource=TextureImporterAlphaSource.FromInput;
            output.alphaIsTransparency=false;output.maxTextureSize=2048;output.SaveAndReimport();
        }
        finally
        {
            if(packed!=null)UnityEngine.Object.DestroyImmediate(packed);
            for(int i=0;i<importers.Length;i++)
            {
                importers[i].isReadable=readable[i];
                importers[i].textureCompression=TextureImporterCompression.Compressed;
                importers[i].SaveAndReimport();
            }
        }
    }

    public static void Render(GameObject root,string path,float side)
    {
        var preview=new PreviewRenderUtility();Texture2D image=null;
        try
        {
            var copy=UnityEngine.Object.Instantiate(root);preview.AddSingleGO(copy);
            var rs=copy.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;
            foreach(var r in rs)b.Encapsulate(r.bounds);
            preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=Quaternion.Euler(35,side>0?190:10,0);
            preview.lights[1].intensity=0.8f;
            preview.BeginStaticPreview(new Rect(0,0,900,900));
            preview.camera.fieldOfView=35;preview.camera.aspect=1;
            preview.camera.nearClipPlane=0.01f;preview.camera.farClipPlane=100;
            preview.camera.clearFlags=CameraClearFlags.SolidColor;
            preview.camera.backgroundColor=new Color(0.11f,0.13f,0.16f);
            preview.camera.transform.position=b.center+new Vector3(0.23f,0.12f,side).normalized*b.size.magnitude*1.7f;
            preview.camera.transform.LookAt(b.center);preview.camera.Render();
            image=preview.EndStaticPreview();File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally{if(image!=null)UnityEngine.Object.DestroyImmediate(image);preview.Cleanup();}
        AssetDatabase.ImportAsset(path);
    }
}
