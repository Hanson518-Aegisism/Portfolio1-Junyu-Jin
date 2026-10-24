using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SciFiDoorAnimatedBuilder
{
    const string Root="Assets/sci-fi-door/";
    static SciFiDoorAnimatedBuilder(){EditorApplication.delayCall+=()=>
    {
        Inspect();if(!File.Exists(Root+"Animations/Door_Opening.anim"))Build();
        if(File.Exists("Library/SciFiDoorAnimatedCompleted.txt")&&!File.Exists("Library/SciFiDoorBehaviourReport-v2.txt"))ValidateBehaviour();
    };}
    static int Find(int[] parents,int v){while(parents[v]!=v){parents[v]=parents[parents[v]];v=parents[v];}return v;}
    static List<List<int>> Components(Mesh mesh,bool weld)
    {
        int[] p=Enumerable.Range(0,mesh.vertexCount).ToArray();
        Action<int,int> join=(a,b)=>p[Find(p,a)]=Find(p,b);
        var vertices=mesh.vertices;var tris=mesh.triangles;
        if(weld)
        {
            var positions=new Dictionary<Vector3Int,int>();
            for(int i=0;i<vertices.Length;i++)
            {
                var v=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));
                if(positions.TryGetValue(key,out int other))join(i,other);else positions[key]=i;
            }
        }
        for(int i=0;i<tris.Length;i+=3){join(tris[i],tris[i+1]);join(tris[i],tris[i+2]);}
        var groups=new Dictionary<int,List<int>>();
        for(int i=0;i<tris.Length;i+=3)
        {
            int key=Find(p,tris[i]);if(!groups.TryGetValue(key,out var list)){list=new List<int>();groups[key]=list;}
            list.Add(i);
        }
        return groups.Values.OrderByDescending(g=>g.Count).ToList();
    }
    static void Inspect()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Library/SciFiDoorComponents.txt"))return;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/SciFiDoor.prefab");if(prefab==null)return;
        var model=prefab.GetComponentInChildren<MeshFilter>();var mesh=model.sharedMesh;
        var sb=new StringBuilder();var vertices=mesh.vertices;var tris=mesh.triangles;
        for(int mode=0;mode<2;mode++)
        {
            var groups=Components(mesh,mode==1);sb.AppendLine("Weld="+(mode==1)+" components="+groups.Count);
            for(int i=0;i<groups.Count;i++)
            {
                var g=groups[i];var b=new Bounds(model.transform.TransformPoint(vertices[tris[g[0]]]),Vector3.zero);
                foreach(int t in g)for(int j=0;j<3;j++)b.Encapsulate(model.transform.TransformPoint(vertices[tris[t+j]]));
                sb.AppendLine(i+" triangles="+g.Count+" center="+b.center.ToString("F4")+" size="+b.size.ToString("F4")+" min="+b.min.ToString("F4")+" max="+b.max.ToString("F4"));
            }
        }
        File.WriteAllText("Library/SciFiDoorComponents.txt",sb.ToString());
    }

    [MenuItem("Tools/Sci-Fi Door/Build Animated Sliding Door")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var report=new StringBuilder();GameObject root=null;
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            Directory.CreateDirectory(Root+"Meshes");Directory.CreateDirectory(Root+"Animations");AssetDatabase.Refresh();
            string path=Root+"Prefabs/SciFiDoor.prefab";
            if(!File.Exists(Root+"Prefabs/SciFiDoor_Static.prefab"))
            {
                var old=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if(old.GetComponent<SciFiSlidingDoor>()==null)
                    {old.name="SciFiDoor_Static";PrefabUtility.SaveAsPrefabAsset(old,Root+"Prefabs/SciFiDoor_Static.prefab");}
                }
                finally{PrefabUtility.UnloadPrefabContents(old);}
            }
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"source/Sci-Fi_Door_anim.fbx");
            var filter=source.GetComponentInChildren<MeshFilter>();var mesh=filter.sharedMesh;
            var vertices=mesh.vertices.Select(v=>filter.transform.TransformPoint(v)).ToArray();
            var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(v);
            var offset=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            for(int i=0;i<vertices.Length;i++)vertices[i]-=offset;
            var tris=mesh.triangles;var groups=Components(mesh,true);
            var parts=new[]{new List<int>(),new List<int>(),new List<int>()};
            foreach(var g in groups)
            {
                var b=new Bounds(vertices[tris[g[0]]],Vector3.zero);
                foreach(var triangle in g)for(int j=0;j<3;j++)b.Encapsulate(vertices[tris[triangle+j]]);
                // Extract complete disconnected door shells and their lock/button islands.
                // No triangles are cut, synthesized or removed; the original UVs remain intact.
                bool panel=b.min.x>=-1.49f && b.max.x<=0.735f && b.min.y>=0.06f && b.max.y<=3.0f &&
                    b.min.z>=-0.46f && b.max.z<=0.46f && b.size.x<1.2f;
                int part=panel?(b.center.x< -0.3779f?1:2):0;
                parts[part].AddRange(g);
            }
            if(parts[1].Count<400||parts[2].Count<400)throw new Exception("Panel island extraction did not match expected source.");
            if(parts.Sum(p=>p.Count)*3!=tris.Length)throw new Exception("Triangle conservation failed.");
            root=new GameObject("SciFiDoor");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            string[] names={"Frame","LeftPanel","RightPanel"};
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/SciFiDoor_URP.mat");
            for(int i=0;i<3;i++)
            {
                var partMesh=CreateMesh(mesh,filter.transform,vertices,parts[i],names[i]);
                var go=new GameObject(names[i]);go.transform.SetParent(root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=partMesh;
                go.AddComponent<MeshRenderer>().sharedMaterial=material;
                if(i==0)go.AddComponent<MeshCollider>().sharedMesh=partMesh;
                else
                {
                    var collider=go.AddComponent<BoxCollider>();collider.center=partMesh.bounds.center;collider.size=partMesh.bounds.size;
                    var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                }
                report.AppendLine(names[i]+" triangles="+parts[i].Count+" bounds="+partMesh.bounds);
            }
            var opening=Clip("Door_Opening",0f,1.2f,true);
            var closing=Clip("Door_Closing",1.2f,0f,true);
            var closed=Clip("Door_Closed",0f,0f,false);
            var opened=Clip("Door_Opened",1.2f,1.2f,false);
            string controllerPath=Root+"Animations/SciFiDoor.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm=controller.layers[0].stateMachine;
            foreach(var old in sm.states)sm.RemoveState(old.state);
            var sClosed=sm.AddState("Closed");sClosed.motion=closed;
            var sOpening=sm.AddState("Opening");sOpening.motion=opening;
            var sOpened=sm.AddState("Opened");sOpened.motion=opened;
            var sClosing=sm.AddState("Closing");sClosing.motion=closing;
            sm.defaultState=sClosed;
            foreach(var pair in new[]{new[]{sOpening,sOpened},new[]{sClosing,sClosed}})
            {
                var transition=pair[0].AddTransition(pair[1]);transition.hasExitTime=true;transition.exitTime=1f;transition.duration=0f;
            }
            EditorUtility.SetDirty(controller);
            var animator=root.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var audio=root.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1f;audio.minDistance=1.5f;audio.maxDistance=15f;
            var door=root.AddComponent<SciFiSlidingDoor>();
            var settings=new SerializedObject(door);
            settings.FindProperty("doorAnimator").objectReferenceValue=animator;
            settings.FindProperty("audioSource").objectReferenceValue=audio;
            settings.ApplyModifiedPropertiesWithoutUndo();
            closed.SampleAnimation(root,0f);
            var saved=PrefabUtility.SaveAsPrefabAsset(root,path);
            if(saved==null)throw new Exception("Animated prefab save failed.");
            AssetDatabase.SaveAssets();
            Validate(root,opening,closing,report);
            closed.SampleAnimation(root,0f);
            SciFiDoorPrefabBuilder.Render(root,Root+"SciFiDoor_Closed.png",1f);
            opening.SampleAnimation(root,opening.length);
            SciFiDoorPrefabBuilder.Render(root,Root+"SciFiDoor_Open.png",1f);
            report.AppendLine("SUCCESS: original prefab upgraded; separate static prefab retained.");
            File.WriteAllText("Library/SciFiDoorAnimatedCompleted.txt","success");
            Selection.activeObject=saved;EditorGUIUtility.PingObject(saved);
            Debug.Log("SciFiDoor upgraded: E to open/close; animated leaves with moving colliders and anti-crush protection.");
        }
        catch(Exception e){report.AppendLine("FAILED: "+e);Debug.LogException(e);}
        finally
        {
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
            File.WriteAllText("Library/SciFiDoorAnimatedReport.txt",report.ToString());
        }
    }

    static Mesh CreateMesh(Mesh source,Transform transform,Vector3[] positions,List<int> triangleStarts,string name)
    {
        var normals=source.normals;var tangents=source.tangents;var uvs=source.uv;var indices=source.triangles;
        var remap=new Dictionary<int,int>();var v=new List<Vector3>();var n=new List<Vector3>();var t=new List<Vector4>();var uv=new List<Vector2>();var output=new List<int>();
        foreach(int start in triangleStarts)for(int j=0;j<3;j++)
        {
            int index=indices[start+j];
            if(!remap.TryGetValue(index,out int mapped))
            {
                mapped=v.Count;remap[index]=mapped;v.Add(positions[index]);
                n.Add(transform.TransformDirection(normals[index]).normalized);uv.Add(uvs[index]);
                if(tangents.Length==source.vertexCount)
                {
                    var original=tangents[index];var dir=transform.TransformDirection(new Vector3(original.x,original.y,original.z)).normalized;
                    t.Add(new Vector4(dir.x,dir.y,dir.z,original.w));
                }
            }
            output.Add(mapped);
        }
        var mesh=new Mesh{name="SciFiDoor_"+name};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(output,0);
        if(t.Count==v.Count)mesh.SetTangents(t);else mesh.RecalculateTangents();mesh.RecalculateBounds();
        string path=Root+"Meshes/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }

    static AnimationClip Clip(string name,float from,float to,bool easing)
    {
        string path=Root+"Animations/"+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null){clip=new AnimationClip{name=name,frameRate=60};AssetDatabase.CreateAsset(clip,path);}
        float length=easing?1.2f:1f/60f;
        foreach(string part in new[]{"LeftPanel","RightPanel"})
        {
            float sign=part=="LeftPanel"?-1f:1f;
            var curve=AnimationCurve.EaseInOut(0,from*sign,length,to*sign);
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(part,typeof(Transform),"m_LocalPosition.x"),curve);
        }
        EditorUtility.SetDirty(clip);return clip;
    }

    static void Validate(GameObject root,AnimationClip opening,AnimationClip closing,StringBuilder report)
    {
        var left=root.transform.Find("LeftPanel");var right=root.transform.Find("RightPanel");
        opening.SampleAnimation(root,0f);
        int closedHits=ApertureHits(root);
        opening.SampleAnimation(root,0.6f);
        if(Mathf.Abs(left.localPosition.x+0.6f)>0.001f||Mathf.Abs(right.localPosition.x-0.6f)>0.001f)throw new Exception("Midpoint animation failed.");
        opening.SampleAnimation(root,opening.length);
        int openHits=ApertureHits(root);
        if(openHits!=0)throw new Exception("Door opening remains visually blocked at "+openHits+" sample rays.");
        if(closedHits<10)throw new Exception("Closed door geometry is incomplete.");
        if(left.GetComponent<BoxCollider>().transform!=left||right.GetComponent<BoxCollider>().transform!=right)throw new Exception("Panel colliders not attached.");
        closing.SampleAnimation(root,closing.length);
        if(left.localPosition.sqrMagnitude>0.000001f||right.localPosition.sqrMagnitude>0.000001f)throw new Exception("Closing failed.");
        // Exercise the actual Animator state machine, rather than only sampling clips.
        var animator=root.GetComponent<Animator>();animator.Rebind();animator.Update(0);
        animator.Play("Opening",0,0);for(int i=0;i<90;i++)animator.Update(1f/60f);
        if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Opened"))throw new Exception("Animator open hold failed.");
        animator.Play("Closing",0,0);for(int i=0;i<90;i++)animator.Update(1f/60f);
        if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Closed"))throw new Exception("Animator close hold failed.");
        report.AppendLine("Verified: original triangles conserved; closed/mid/open/close poses; Animator endpoint holds; panel colliders move with leaves.");
        report.AppendLine("Aperture geometry rays: closed="+closedHits+"/12, fully open="+openHits+"/12.");
    }

    static int ApertureHits(GameObject root)
    {
        int hits=0;
        foreach(float x in new[]{-1.12f,-0.62f,-0.12f,0.38f})foreach(float y in new[]{0.6f,1.4f,2.2f})
        {
            var origin=new Vector3(x,y,-2f);bool hit=false;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var vertices=filter.sharedMesh.vertices;var tris=filter.sharedMesh.triangles;
                for(int i=0;i<tris.Length&&!hit;i+=3)
                {
                    Vector3 a=filter.transform.TransformPoint(vertices[tris[i]]),b=filter.transform.TransformPoint(vertices[tris[i+1]]),c=filter.transform.TransformPoint(vertices[tris[i+2]]);
                    var e1=b-a;var e2=c-a;var p=Vector3.Cross(Vector3.forward,e2);float det=Vector3.Dot(e1,p);
                    if(Mathf.Abs(det)<0.0000001f)continue;
                    float inv=1f/det;var offset=origin-a;float u=Vector3.Dot(offset,p)*inv;
                    if(u<0||u>1)continue;
                    var q=Vector3.Cross(offset,e1);float v=Vector3.Dot(Vector3.forward,q)*inv;
                    if(v<0||u+v>1)continue;
                    float distance=Vector3.Dot(e2,q)*inv;if(distance>=0&&distance<=4f)hit=true;
                }
            }
            if(hit)hits++;
        }
        return hits;
    }

    [MenuItem("Tools/Sci-Fi Door/Validate Interaction And Safety")]
    public static void ValidateBehaviour()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var report=new StringBuilder();
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SciFiSlidingDoor door=null;
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        Action<string> call=name=>typeof(SciFiSlidingDoor).GetMethod(name,flags).Invoke(door,null);
        Action<string,object> set=(name,value)=>typeof(SciFiSlidingDoor).GetField(name,flags).SetValue(door,value);
        try
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/SciFiDoor.prefab"),scene);
            go.transform.position=new Vector3(10000,0,10000);
            door=go.GetComponent<SciFiSlidingDoor>();call("Awake");call("OnEnable");call("Start");
            var animator=go.GetComponent<Animator>();
            if(!door.SetOpen(true)||!door.IsOpen)throw new Exception("Open API failed.");
            animator.Update(0.6f);set("moveStartedAt",Time.time-0.6f);
            float before=go.transform.Find("LeftPanel").localPosition.x;
            Physics.SyncTransforms();
            if(!door.SetOpen(false))throw new Exception("Mid-opening close rejected without obstruction.");
            float after=go.transform.Find("LeftPanel").localPosition.x;
            if(Mathf.Abs(before-after)>0.02f)throw new Exception("Direction reversal caused a visual snap.");
            report.AppendLine("Open/close APIs and smooth reversal passed.");
            animator.Play("Opened",0,0);animator.Update(0);
            set("targetOpen",true);set("moving",false);
            var actor=new GameObject("DoorSafetyTestActor");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            actor.transform.position=go.transform.TransformPoint(new Vector3(-0.3779f,0f,0f));
            var cc=actor.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=0.3f;cc.center=Vector3.up*0.9f;
            Physics.SyncTransforms();
            if(door.SetOpen(false))throw new Exception("Door closed on an actor in the opening.");
            report.AppendLine("Closing rejected while a CharacterController occupies the doorway.");
            cc.enabled=false;actor.transform.position+=Vector3.forward*5f;cc.enabled=true;Physics.SyncTransforms();
            if(!door.SetOpen(false))
            {
                foreach(var hit in (Collider[])typeof(SciFiSlidingDoor).GetField("overlaps",flags).GetValue(door))
                    if(hit!=null)report.AppendLine("Overlap: "+hit.name+" at "+hit.transform.position+" own="+hit.transform.IsChildOf(go.transform));
                throw new Exception("Door did not close after the actor left.");
            }
            cc.enabled=false;actor.transform.position=go.transform.TransformPoint(new Vector3(-0.3779f,0f,0f));cc.enabled=true;Physics.SyncTransforms();
            call("Update");
            if(!door.IsOpen)throw new Exception("Closing door failed to reopen for an entering actor.");
            report.AppendLine("Closing door reverses to open when an actor enters. SUCCESS.");
        }
        catch(Exception e){report.AppendLine("FAILED: "+e);Debug.LogException(e);}
        finally
        {
            if(door!=null)call("OnDisable");
            EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            File.WriteAllText("Library/SciFiDoorBehaviourReport-v2.txt",report.ToString());
        }
    }
}
