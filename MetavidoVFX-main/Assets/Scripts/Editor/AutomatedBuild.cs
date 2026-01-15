using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Linq;

public class AutomatedBuild
{
    [MenuItem("Metavido/Build iOS")]
    public static void BuildiOS()
    {
        Debug.Log("Starting AutomatedBuild.BuildiOS...");
        // Build HOLOGRAM Mirror MVP scene
        string scenePath = "Assets/Scenes/HOLOGRAM_Mirror_MVP.unity";

        // 2. Configure Build Settings
        var scenes = new[] { scenePath };

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/iOS", // Output folder for Xcode project
            target = BuildTarget.iOS,
            options = BuildOptions.CompressWithLz4 // Faster build
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

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
