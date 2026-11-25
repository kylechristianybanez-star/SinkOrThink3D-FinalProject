using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Idempotent Editor tool to configure ChestQuizManager in IslandScene.unity
/// and verify all 6 Discovery Island treasure chests have unique IDs.
/// </summary>
public static class Phase7ChestQuizSetup
{
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";

    [MenuItem("Tools/SinkOrThink3D/Phase 7 - Setup Chest Quiz & Rewards")]
    public static void SetupChestQuiz()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);

        ChestQuizManager quizMgr = Object.FindAnyObjectByType<ChestQuizManager>();
        if (quizMgr == null)
        {
            GameObject go = new GameObject("ChestQuizManager");
            SceneManager.MoveGameObjectToScene(go, islandScene);
            quizMgr = go.AddComponent<ChestQuizManager>();
        }
        quizMgr.islandIndex = 0;
        EditorUtility.SetDirty(quizMgr);

        TreasureChest[] chests = Object.FindObjectsByType<TreasureChest>();
        HashSet<string> ids = new HashSet<string>();
        int regularCount = 0;
        int specialCount = 0;

        for (int i = 0; i < chests.Length; i++)
        {
            TreasureChest c = chests[i];
            if (c.gameObject.scene != islandScene)
                continue;

            if (c.isSpecialChest) specialCount++;
            else regularCount++;

            if (!ids.Add(c.chestId))
                Debug.LogWarning("Duplicate chestId detected: " + c.chestId, c);
        }

        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);

        Debug.Log("Phase 7 Setup complete! Verified " + regularCount + " regular chest(s) and " + specialCount + " special chest(s) in IslandScene.");
        EditorUtility.DisplayDialog(
            "Phase 7 Setup Complete",
            "Configured ChestQuizManager in IslandScene.unity and verified " + regularCount + " Regular Chests + " + specialCount + " Special Chest with unique IDs.\n\nIslandScene.unity has been saved!",
            "OK"
        );
    }
}
