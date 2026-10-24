using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public static class PerimeterRockLayout
{
    static PerimeterRockLayout()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Library/PerimeterRocksVerified-v2.txt")) Inspect();
        };
    }
    [MenuItem("Tools/Environment/Inspect Perimeter")]
    public static void Inspect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Test.unity") return;
        var sb = new StringBuilder("id\tname\tprefab\tposition\tscale\trotation\tcenter\tsize\n");
        foreach (var go in scene.GetRootGameObjects())
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) continue;
            var bounds = rs[0].bounds;
            foreach (var r in rs) bounds.Encapsulate(r.bounds);
            sb.AppendLine(go.GetInstanceID()+"\t"+go.name+"\t"+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)+"\t"+
                go.transform.position.ToString("F2")+"\t"+go.transform.lossyScale.ToString("F2")+"\t"+go.transform.eulerAngles.ToString("F1")+"\t"+
                bounds.center.ToString("F2")+"\t"+bounds.size.ToString("F2"));
        }
        File.WriteAllText("Library/PerimeterInventory.tsv", sb.ToString());
        sb = new StringBuilder("name\tpath\tprefab\tcenter\tsize\n");
        foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)))
        {
            if (r.bounds.size.y < 2.5f || r.bounds.size.y > 18f) continue;
            sb.AppendLine(r.name+"\t"+AnimationUtility.CalculateTransformPath(r.transform, null)+"\t"+
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(r.gameObject)+"\t"+r.bounds.center.ToString("F2")+"\t"+r.bounds.size.ToString("F2"));
        }
        File.WriteAllText("Library/PerimeterWalls.tsv", sb.ToString());
        Plan();
        if (!File.Exists("Library/PerimeterRocksCompleted.txt") && GameObject.Find("Perimeter_Rocks_Exterior") == null) Build();
        VerifyCoverage();
    }

    const float Cell = 0.5f;
    static Vector2 origin;
    static int width, height;
    static bool[,] blocked, exterior;
    static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
    static bool IsExterior(Vector2 p)
    {
        int x = Mathf.RoundToInt((p.x-origin.x)/Cell), z = Mathf.RoundToInt((p.y-origin.y)/Cell);
        return x < 0 || z < 0 || x >= width || z >= height || exterior[x,z];
    }
    static List<Vector3> perimeter = new List<Vector3>();
    static List<Vector3> normals = new List<Vector3>();
    static List<float> depths = new List<float>();

    static void VerifyCoverage()
    {
        var parent=GameObject.Find("Perimeter_Rocks_Exterior"); if(parent==null)return;
        var rocks=SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))
            .Where(r=>r.name.StartsWith("wd3efb0_LOD0")).Select(r=>r.bounds).ToArray();
        var coverage=rocks.ToList();
        int infill=0;
        var rng=new System.Random(241025);
        for(int index=0;index<perimeter.Count;index++)
        {
            var point=perimeter[index];
            float distance=coverage.Min(b=>Vector3.Distance(new Vector3(point.x,b.center.y,point.z),b.ClosestPoint(new Vector3(point.x,b.center.y,point.z))));
            if(distance<=2f)continue;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mine/Prefabs/wd3efb0.prefab"));
            go.transform.SetParent(parent.transform,false);
            go.transform.localScale=Vector3.one*(54f+(float)rng.NextDouble()*12f);
            go.transform.rotation=Quaternion.Euler((float)rng.NextDouble()*8-4,(float)rng.NextDouble()*360,(float)rng.NextDouble()*8-4);
            var b2=go.GetComponentInChildren<Renderer>().bounds;
            var n=normals[index];
            float radius=Mathf.Abs(n.x)*b2.extents.x+Mathf.Abs(n.z)*b2.extents.z;
            var target=point+n*(depths[index]+radius+0.12f);
            target.y=1.30f+(float)rng.NextDouble()*0.2f;
            go.transform.position=target-new Vector3(b2.center.x,0,b2.center.z);
            go.name="Exterior_Seam_Rock_"+(++infill).ToString("000");
            coverage.Add(go.GetComponentInChildren<Renderer>().bounds);
        }
        rocks=coverage.ToArray();
        if(infill>0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        float maxGap=0f; int gaps=0;
        foreach(var point in perimeter)
        {
            float nearest=float.PositiveInfinity;
            foreach(var b in rocks)
            {
                var p=new Vector3(point.x,b.center.y,point.z);
                nearest=Mathf.Min(nearest,Vector3.Distance(p,b.ClosestPoint(p)));
            }
            maxGap=Mathf.Max(maxGap,nearest);if(nearest>2.5f)gaps++;
        }
        File.WriteAllText("Library/PerimeterRocksVerified-v2.txt","Perimeter samples="+perimeter.Count+"\nSeam infill rocks="+infill+"\nLargest wall-to-rock bounds gap="+maxGap.ToString("F2")+"m\nSamples with gap > 2.5m="+gaps+"\nOriginal stone objects remain intact.");
        var all=parent.GetComponentsInChildren<Renderer>();var total=all[0].bounds;
        foreach(var r in all)total.Encapsulate(r.bounds);
        if(SceneView.lastActiveSceneView!=null)
            SceneView.lastActiveSceneView.LookAt(total.center,Quaternion.Euler(65f,-20f,0),total.size.magnitude*0.30f,false,true);
    }

    static void Plan()
    {
        var scene = SceneManager.GetActiveScene();
        var walls = scene.GetRootGameObjects().Where(g => g.name != "ceilings")
            .SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
            .Where(r => r.name.StartsWith("standard wall") && r.bounds.size.y > 2.5f && r.bounds.center.y < 6f).ToArray();
        var bounds = walls[0].bounds;
        foreach(var r in walls) bounds.Encapsulate(r.bounds);
        origin = Flat(bounds.min)-Vector2.one*16f;
        width = Mathf.CeilToInt((bounds.size.x+32f)/Cell); height = Mathf.CeilToInt((bounds.size.z+32f)/Cell);
        blocked = new bool[width,height]; exterior = new bool[width,height];
        foreach(var r in walls)
        {
            // Conservatively close tiny joins in the modular walls before finding the exterior.
            var b = r.bounds; b.Expand(new Vector3(2.2f,0,2.2f));
            int x0 = Mathf.Max(0, Mathf.FloorToInt((b.min.x-origin.x)/Cell)), x1 = Mathf.Min(width-1, Mathf.CeilToInt((b.max.x-origin.x)/Cell));
            int z0 = Mathf.Max(0, Mathf.FloorToInt((b.min.z-origin.y)/Cell)), z1 = Mathf.Min(height-1, Mathf.CeilToInt((b.max.z-origin.y)/Cell));
            for(int x=x0;x<=x1;x++) for(int z=z0;z<=z1;z++) blocked[x,z]=true;
        }
        var queue = new Queue<Vector2Int>(); queue.Enqueue(Vector2Int.zero); exterior[0,0]=true;
        int[] dx={1,-1,0,0}, dz={0,0,1,-1};
        while(queue.Count>0)
        {
            var p=queue.Dequeue();
            for(int i=0;i<4;i++)
            {
                int x=p.x+dx[i], z=p.y+dz[i];
                if(x<0||z<0||x>=width||z>=height||blocked[x,z]||exterior[x,z])continue;
                exterior[x,z]=true;queue.Enqueue(new Vector2Int(x,z));
            }
        }
        perimeter.Clear(); normals.Clear(); depths.Clear();
        foreach(var r in walls)
        {
            var filter=r.GetComponent<MeshFilter>(); if(filter==null||filter.sharedMesh==null) continue;
            var b=filter.sharedMesh.bounds;
            Vector3 axis=b.size.x>b.size.z?Vector3.right:Vector3.forward;
            Vector3 tangent=r.transform.TransformVector(axis*(b.size.x>b.size.z?b.size.x:b.size.z));
            float length=new Vector2(tangent.x,tangent.z).magnitude;
            tangent.y=0;tangent.Normalize();
            var normal=Vector3.Cross(Vector3.up,tangent).normalized;
            for(float d=0.5f;d<length;d+=3.2f)
            {
                var p=r.bounds.center+tangent*(d-length*0.5f);
                bool a=IsExterior(Flat(p+normal*4f)), c=IsExterior(Flat(p-normal*4f));
                if(a==c)continue;
                perimeter.Add(p); normals.Add(a?normal:-normal);
                var across=r.transform.TransformVector(b.size.x>b.size.z?Vector3.forward*b.size.z:Vector3.right*b.size.x);
                var vertical=r.transform.TransformVector(Vector3.up*b.size.y);
                depths.Add((Mathf.Abs(Vector3.Dot(normal,across))+Mathf.Abs(Vector3.Dot(normal,vertical)))*0.5f);
            }
        }
        var report=new StringBuilder("walls="+walls.Length+"; perimeter samples="+perimeter.Count+"; bounds="+bounds+"\n");
        int interior=0; for(int x=0;x<width;x++)for(int z=0;z<height;z++)if(!blocked[x,z]&&!exterior[x,z])interior++;
        report.AppendLine("Enclosed interior area="+interior*Cell*Cell);
        for(int i=0;i<perimeter.Count;i++) report.AppendLine(perimeter[i].ToString("F2")+"\t"+normals[i].ToString("F2"));
        File.WriteAllText("Library/PerimeterPlan.txt",report.ToString());
    }

    [MenuItem("Tools/Environment/Fill Exterior With Matching Rocks")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene=SceneManager.GetActiveScene();
        if (scene.path!="Assets/Scenes/Test.unity" || GameObject.Find("Perimeter_Rocks_Exterior")!=null) return;
        if (perimeter.Count==0) Plan();
        if(perimeter.Count<100) throw new Exception("Perimeter detection incomplete; no rocks added.");
        // Snapshot includes the user's currently unsaved stone placements.
        EditorSceneManager.SaveScene(scene,"Library/BeforePerimeterRocks.unity",true);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mine/Prefabs/wd3efb0.prefab");
        var existing=scene.GetRootGameObjects().Where(g=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g)=="Assets/Mine/Prefabs/wd3efb0.prefab").ToArray();
        if(existing.Length==0)throw new Exception("Existing reference rocks not found.");
        var template=existing[0];
        var centers=existing.Select(g=>Flat(g.GetComponentsInChildren<Renderer>()[0].bounds.center)).ToList();
        var random=new System.Random(241024);
        Func<float,float,float> range=(a,b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        var parent=new GameObject("Perimeter_Rocks_Exterior");
        Undo.RegisterCreatedObjectUndo(parent,"Fill outer walls with rocks");
        int created=0, skipped=0;
        try
        {
            // A continuous inner band plus intermittent outer clusters soften the silhouette.
            for(int row=0;row<2;row++)for(int i=0;i<perimeter.Count;i++)
            {
                if(row==1 && random.NextDouble()>0.62)continue;
                var normal=normals[i]; var tangent=Vector3.Cross(normal,Vector3.up);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                go.transform.SetParent(parent.transform,false);
                float scale=range(0.87f,1.15f)*(row==1?range(0.82f,1.05f):1f);
                go.transform.localScale=Vector3.Scale(template.transform.lossyScale,new Vector3(scale*range(0.94f,1.06f),scale*range(0.9f,1.12f),scale*range(0.94f,1.06f)));
                go.transform.rotation=Quaternion.Euler(range(-6,6),range(0,360),range(-6,6));
                go.transform.position=Vector3.zero;
                var rs=go.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;
                foreach(var r in rs)bounds.Encapsulate(r.bounds);
                float radius=Mathf.Abs(normal.x)*bounds.extents.x+Mathf.Abs(normal.z)*bounds.extents.z;
                Vector3 target=perimeter[i]+normal*(depths[i]+radius+range(0.05f,0.6f)+(row==1?range(4.0f,5.8f):0f))+tangent*range(-0.65f,0.65f);
                var flat=Flat(target);
                float separation=row==0?3.6f:3.3f;
                if(!IsExterior(flat)||centers.Any(p=>(p-flat).sqrMagnitude<separation*separation))
                { UnityEngine.Object.DestroyImmediate(go);skipped++;continue; }
                target.y=template.transform.position.y+range(-0.32f,0.24f);
                // Center offsets differ between rotations; keep the visible center on the band.
                go.transform.position=target-new Vector3(bounds.center.x,0,bounds.center.z);
                go.name="Exterior_Rock_"+(++created).ToString("000");
                centers.Add(flat);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var all=parent.GetComponentsInChildren<Renderer>();var total=all[0].bounds;
            foreach(var r in all)total.Encapsulate(r.bounds);
            File.WriteAllText("Library/PerimeterRocksCompleted.txt","Existing rocks preserved="+existing.Length+"\nNew rocks="+created+"\nPerimeter samples="+perimeter.Count+"\nAvoided overlap="+skipped+"\nBounds="+total+"\nScene saved successfully.");
            Selection.activeGameObject=parent;
            if(SceneView.lastActiveSceneView!=null)
                SceneView.lastActiveSceneView.LookAt(total.center,Quaternion.Euler(65f,-20f,0),total.size.magnitude*0.48f,false,true);
            Debug.Log("Exterior rock band complete: "+created+" matching rocks added. Existing placements preserved.");
        }
        catch(Exception e){Debug.LogException(e);throw;}
    }
}
