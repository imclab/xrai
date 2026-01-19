using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    [MenuItem("Build/Build iOS")]
    public static void BuildiOS()
    {
        string[] scenes = new string[]
        {
            "Assets/HOLOGRAM.unity"
        };

        // Open the scene to inject Debug Console
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenes[0]);

        // Check if Console exists
        if (GameObject.Find("IngameDebugConsole") == null)
        {
            // Find prefab by name
            string[] guids = AssetDatabase.FindAssets("IngameDebugConsole t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.name = "IngameDebugConsole"; // Ensure name matches for next check
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                    Debug.Log("Auto-Injected IngameDebugConsole for build.");
                }
            }
            else
            {
                Debug.LogWarning("IngameDebugConsole prefab not found. Skipping usage.");
            }
        }

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed");
            EditorApplication.Exit(1);
        }
    }
}
