using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Idempotent Editor tool to place a realistic wooden Dock Repair Workbench
/// beside the dock in IslandScene.unity with repair particles and DockRepairStation.
/// </summary>
public static class Phase8InventoryAndRepairSetup
{
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";

    [MenuItem("Tools/SinkOrThink3D/Phase 8 - Setup Inventory, Hotbar & Dock Repair")]
    public static void SetupInventoryAndRepair()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);

        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardFrameWood.mat");
        Material brassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardBrass.mat");
        Material sparkleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatSparkleParticle.mat");

        GameObject oldStation = FindRootObject(islandScene, "DockRepairWorkbench");
        if (oldStation != null)
            Object.DestroyImmediate(oldStation);

        GameObject bench = new GameObject("DockRepairWorkbench");
        SceneManager.MoveGameObjectToScene(bench, islandScene);
        bench.transform.position = new Vector3(16.15f, 0.96f, 147.2f);
        bench.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        BoxCollider col = bench.AddComponent<BoxCollider>();
        col.isTrigger = false;
        col.center = new Vector3(0f, 0.55f, 0f);
        col.size = new Vector3(1.8f, 1.1f, 1.0f);

        // Table top and 4 legs
        CreateDecorPart(bench.transform, "TableTop", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f), Vector3.zero, new Vector3(1.7f, 0.14f, 0.9f), woodMat);
        CreateDecorPart(bench.transform, "Leg_FL", PrimitiveType.Cube, new Vector3(-0.72f, 0.4f, 0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat);
        CreateDecorPart(bench.transform, "Leg_FR", PrimitiveType.Cube, new Vector3(0.72f, 0.4f, 0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat);
        CreateDecorPart(bench.transform, "Leg_BL", PrimitiveType.Cube, new Vector3(-0.72f, 0.4f, -0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat);
        CreateDecorPart(bench.transform, "Leg_BR", PrimitiveType.Cube, new Vector3(0.72f, 0.4f, -0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat);

        // Spare repair planks + brass hammer prop on workbench
        CreateDecorPart(bench.transform, "SparePlank_1", PrimitiveType.Cube, new Vector3(-0.35f, 0.97f, 0f), new Vector3(0f, 12f, 0f), new Vector3(0.85f, 0.07f, 0.25f), woodMat);
        CreateDecorPart(bench.transform, "SparePlank_2", PrimitiveType.Cube, new Vector3(-0.32f, 1.04f, 0.05f), new Vector3(0f, -8f, 0f), new Vector3(0.85f, 0.07f, 0.25f), woodMat);
        CreateDecorPart(bench.transform, "HammerHandle", PrimitiveType.Cylinder, new Vector3(0.45f, 0.96f, 0f), new Vector3(90f, 25f, 0f), new Vector3(0.05f, 0.24f, 0.05f), woodMat);
        CreateDecorPart(bench.transform, "HammerHead", PrimitiveType.Cube, new Vector3(0.52f, 0.98f, 0.18f), new Vector3(0f, 25f, 0f), new Vector3(0.22f, 0.10f, 0.11f), brassMat);

        // Repair burst particles
        GameObject fxGo = new GameObject("RepairWorkbenchParticles");
        fxGo.transform.SetParent(bench.transform, false);
        fxGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        fxGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.8f;
        main.startLifetime = 0.75f;
        main.startSpeed = 2.2f;
        main.startSize = 0.14f;
        main.startColor = new Color(0.45f, 1f, 0.55f, 1f);
        main.maxParticles = 30;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24) });
        ParticleSystemRenderer pr = fxGo.GetComponent<ParticleSystemRenderer>();
        if (pr != null && sparkleMat != null)
            pr.sharedMaterial = sparkleMat;

        DockRepairStation station = bench.AddComponent<DockRepairStation>();
        station.repairBurstParticles = ps;
        station.interactRange = 4.4f;

        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);

        Debug.Log("Phase 8 Setup complete! Placed DockRepairWorkbench in IslandScene.unity.");
        EditorUtility.DisplayDialog(
            "Phase 8 Setup Complete",
            "Placed DockRepairWorkbench on the Discovery Island dock (10 Coins = +20 HP). Persistent Bottom Hotbar (1/2) and [I] Inventory UI auto-initialize at runtime.\n\nIslandScene.unity has been saved!",
            "OK"
        );
    }

    private static void CreateDecorPart(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;

        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Object.DestroyImmediate(col);

        if (mat != null && go.GetComponent<MeshRenderer>() != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static GameObject FindRootObject(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
                return roots[i];
        }
        return null;
    }
}
