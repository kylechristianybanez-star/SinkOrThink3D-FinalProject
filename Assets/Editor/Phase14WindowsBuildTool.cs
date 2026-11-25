using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Phase 14 Windows 64-bit Standalone Build Tool:
/// Validates required Build Settings scenes, prompts to save unsaved scenes,
/// builds Builds/Windows64/SinkOrThink3D.exe, and reveals the executable folder on success.
/// </summary>
public static class Phase14WindowsBuildTool
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";
    private const string OutputFolder = "Builds/Windows64";
    private const string OutputExePath = "Builds/Windows64/SinkOrThink3D.exe";

    [MenuItem("Tools/SinkOrThink3D/Phase 14 - Build Windows 64-Bit Executable")]
    public static void BuildWindows64()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string[] requiredScenes = new string[] { MainMenuPath, GameScenePath, IslandScenePath };
        List<string> enabledScenes = new List<string>();

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < requiredScenes.Length; i++)
        {
            string req = requiredScenes[i];
            bool foundEnabled = false;
            for (int j = 0; j < buildScenes.Length; j++)
            {
                if (buildScenes[j].enabled && buildScenes[j].path == req)
                {
                    foundEnabled = true;
                    break;
                }
            }

            if (!foundEnabled)
            {
                EditorUtility.DisplayDialog(
                    "Build Settings Incomplete",
                    "Missing enabled scene in File -> Build Profiles / Build Settings:\n" + req +
                    "\n\nPlease ensure MainMenu.unity, GameScene.unity, and IslandScene.unity are all added and checked.",
                    "OK"
                );
                return;
            }

            enabledScenes.Add(req);
        }

        string fullOutputDir = Path.GetFullPath(OutputFolder);
        if (!Directory.Exists(fullOutputDir))
            Directory.CreateDirectory(fullOutputDir);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = enabledScenes.ToArray(),
            locationPathName = OutputExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log("Starting Windows 64-bit Build -> " + OutputExePath);
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            float sizeMb = summary.totalSize / (1024f * 1024f);
            string fullExePath = Path.GetFullPath(OutputExePath);
            Debug.Log("Windows 64-bit Build Succeeded! Output: " + fullExePath + " (" + sizeMb.ToString("0.0") + " MB)");

            EditorUtility.RevealInFinder(fullExePath);
            EditorUtility.DisplayDialog(
                "Windows 64-Bit Build Complete",
                "Build succeeded!\n\nExecutable: " + OutputExePath + "\nTotal Size: " + sizeMb.ToString("0.0") + " MB",
                "OK"
            );
        }
        else
        {
            Debug.LogError("Windows 64-bit Build Failed: " + summary.result + " (Errors: " + summary.totalErrors + ")");
            EditorUtility.DisplayDialog(
                "Windows 64-Bit Build Failed",
                "Build finished with status: " + summary.result + "\nErrors: " + summary.totalErrors +
                "\nCheck the Unity Console for details.",
                "OK"
            );
        }
    }
}
