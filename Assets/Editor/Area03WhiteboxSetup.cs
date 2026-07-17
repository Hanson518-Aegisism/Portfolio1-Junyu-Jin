using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Area03WhiteboxSetup
{
    [MenuItem("Tools/Level/Build Area03 MainHall Whitebox")]
    public static void Build()
    {
        GameObject level = GameObject.Find("level");
        if (level == null)
        {
            Debug.LogError("level plane not found");
            return;
        }

        GameObject existing = GameObject.Find("Area03_MainHall");
        if (existing != null)
            Object.DestroyImmediate(existing);

        Bounds b = level.GetComponent<Renderer>().bounds;
        float y = b.min.y + 0.02f;
        Vector3 c = new Vector3(b.center.x, y, b.center.z);

        float hallW = 18f, hallD = 14f, wallH = 4f, wallT = 0.4f;
        float stubL = 6f, stubW = 3f;
        float hw = hallW * 0.5f, hd = hallD * 0.5f;

        Color wallC = new Color(0.35f, 0.35f, 0.38f);
        Color darkC = new Color(0.22f, 0.22f, 0.24f);
        Color accent = new Color(0.55f, 0.45f, 0.2f);
        Color battery = new Color(0.2f, 0.55f, 0.35f);

        GameObject root = new GameObject("Area03_MainHall");
        root.transform.position = c;

        Transform walls = CreateChild(root.transform, "Walls");
        Transform debris = CreateChild(root.transform, "Debris");
        Transform lockers = CreateChild(root.transform, "Lockers");
        Transform exits = CreateChild(root.transform, "Exits");

        Box("Wall_N_L", walls, c + new Vector3(-(hw + stubW) * 0.25f, wallH * 0.5f, hd), new Vector3(hw - stubW * 0.5f, wallH, wallT), wallC);
        Box("Wall_N_R", walls, c + new Vector3((hw + stubW) * 0.25f, wallH * 0.5f, hd), new Vector3(hw - stubW * 0.5f, wallH, wallT), wallC);
        Box("Wall_S_L", walls, c + new Vector3(-(hw + stubW * 1.2f) * 0.25f, wallH * 0.5f, -hd), new Vector3(hw - stubW * 0.7f, wallH, wallT), wallC);
        Box("Wall_S_R", walls, c + new Vector3((hw + stubW * 1.2f) * 0.25f, wallH * 0.5f, -hd), new Vector3(hw - stubW * 0.7f, wallH, wallT), wallC);
        Box("Wall_W_N", walls, c + new Vector3(-hw, wallH * 0.5f, hd * 0.35f), new Vector3(wallT, wallH, hd - stubW * 0.5f), wallC);
        Box("Wall_W_S", walls, c + new Vector3(-hw, wallH * 0.5f, -hd * 0.35f), new Vector3(wallT, wallH, hd - stubW * 0.5f), wallC);
        Box("Wall_E_N", walls, c + new Vector3(hw, wallH * 0.5f, hd * 0.35f), new Vector3(wallT, wallH, hd - stubW * 0.5f), wallC);
        Box("Wall_E_S", walls, c + new Vector3(hw, wallH * 0.5f, -hd * 0.35f), new Vector3(wallT, wallH, hd - stubW * 0.5f), wallC);

        MakeStub(exits, "Exit_North_ToOffice", c, Vector3.forward, hw, hd, stubL, stubW, wallH, wallT, y, wallC, darkC);
        MakeStub(exits, "Exit_South_ToMine", c, Vector3.back, hw, hd, stubL, stubW * 1.3f, wallH, wallT, y, wallC, darkC);
        MakeStub(exits, "Exit_West_ToTunnel", c, Vector3.left, hw, hd, stubL, stubW, wallH, wallT, y, wallC, darkC);
        MakeStub(exits, "Exit_East_ToEngine", c, Vector3.right, hw, hd, stubL, stubW, wallH, wallT, y, wallC, darkC);

        Box("Debris_1", debris, c + new Vector3(-3f, 0.7f, -1f), new Vector3(2.2f, 1.4f, 1.5f), darkC);
        Box("Debris_2", debris, c + new Vector3(4f, 0.6f, 2f), new Vector3(1.8f, 1.2f, 2.5f), darkC);
        Box("Debris_3", debris, c + new Vector3(1f, 0.75f, -4f), new Vector3(2.5f, 1.5f, 1.2f), darkC);
        Box("Debris_4", debris, c + new Vector3(-5f, 0.55f, 3f), new Vector3(1.5f, 1.1f, 2f), darkC);
        Box("Debris_5", debris, c + new Vector3(5.5f, 0.5f, -3f), new Vector3(2f, 1f, 1.4f), darkC);

        Box("Locker_K1", lockers, c + new Vector3(-hw + 1.2f, 1.1f, 2f), new Vector3(0.8f, 2.2f, 0.6f), accent);
        Box("Locker_K2", lockers, c + new Vector3(-2f, 1.1f, hd - 1.2f), new Vector3(0.8f, 2.2f, 0.6f), accent);
        Box("Locker_K3", lockers, c + new Vector3(3f, 1.1f, -hd + 1.5f), new Vector3(0.8f, 2.2f, 0.6f), accent);

        Box("BatteryUpgrade_B2", root.transform, c + new Vector3(hw - 1.5f, 0.6f, 0f), new Vector3(1.2f, 1.2f, 0.8f), battery);

        GameObject ps = new GameObject("PlayerStart");
        ps.transform.SetParent(root.transform, false);
        ps.transform.position = c + new Vector3(0f, 0.1f, 3f);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Area03_MainHall whitebox built on level plane.");
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void Box(string name, Transform parent, Vector3 pos, Vector3 scale, Color col)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", col);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void MakeStub(
        Transform exits, string name, Vector3 c, Vector3 dir,
        float hw, float hd, float stubL, float openW, float wallH, float wallT, float y,
        Color wallC, Color darkC)
    {
        Transform e = CreateChild(exits, name);
        float edge = Mathf.Abs(dir.x) > 0f ? hw : hd;
        Vector3 mid = c + dir * (edge + stubL * 0.5f);
        mid.y = y;

        if (Mathf.Abs(dir.x) > 0f)
        {
            Box(name + "_Floor", e, new Vector3(mid.x, y + 0.05f, mid.z), new Vector3(stubL, 0.1f, openW), darkC);
            Box(name + "_WL", e, mid + new Vector3(0f, wallH * 0.5f, openW * 0.5f), new Vector3(stubL, wallH, wallT), wallC);
            Box(name + "_WR", e, mid + new Vector3(0f, wallH * 0.5f, -openW * 0.5f), new Vector3(stubL, wallH, wallT), wallC);
        }
        else
        {
            Box(name + "_Floor", e, new Vector3(mid.x, y + 0.05f, mid.z), new Vector3(openW, 0.1f, stubL), darkC);
            Box(name + "_WL", e, mid + new Vector3(openW * 0.5f, wallH * 0.5f, 0f), new Vector3(wallT, wallH, stubL), wallC);
            Box(name + "_WR", e, mid + new Vector3(-openW * 0.5f, wallH * 0.5f, 0f), new Vector3(wallT, wallH, stubL), wallC);
        }
    }
}
