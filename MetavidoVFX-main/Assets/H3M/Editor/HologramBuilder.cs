using UnityEditor;
using UnityEngine;
using System.Linq;

namespace H3M.Editor
{
    public static class HologramBuilder
    {
        public static void BuildIOS()
        {
            Debug.Log("[H3M] Starting Command Line Build for iOS...");

            var scenePath = "Assets/Scenes/H3M_Mirror_MVP.unity";

            // Validate Scene
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == scenePath))
            {
                Debug.Log($"[H3M] Scene {scenePath} not in build settings. Adding it.");
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = "Builds/iOS",
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("[H3M] Build Succeeded!");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[H3M] Build Failed: " + report.summary);
                EditorApplication.Exit(1);
            }
        }
    }
}
