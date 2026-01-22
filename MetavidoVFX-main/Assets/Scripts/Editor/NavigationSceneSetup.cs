// NavigationSceneSetup.cs - Editor utility for setting up navigation scenes
// Creates MainMenu scene and adds UI to all spec demo scenes
// Uses UI Toolkit (UIDocument) for modern, performant UI

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace MetavidoVFX.Editor
{
    /// <summary>
    /// Editor utility for setting up the navigation system across all scenes.
    /// UI components use UI Toolkit (UIDocument) and create their visuals programmatically.
    /// </summary>
    public static class NavigationSceneSetup
    {
        private const string ScenesPath = "Assets/Scenes";
        private const string SpecDemosPath = "Assets/Scenes/SpecDemos";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        #region Menu Items

        [MenuItem("Tools/MetavidoVFX/Navigation/Setup All Scenes", priority = 100)]
        public static void SetupAllScenes()
        {
            if (!EditorUtility.DisplayDialog("Setup Navigation",
                "This will:\n" +
                "1. Create MainMenu scene\n" +
                "2. Add UI to all spec demo scenes\n" +
                "3. Update build settings\n\n" +
                "Continue?", "Yes", "Cancel"))
            {
                return;
            }

            // Save current scene
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            CreateMainMenuScene();
            AddUIToAllSpecScenes();
            UpdateBuildSettings();

            Debug.Log("[NavigationSceneSetup] Setup complete!");
            EditorUtility.DisplayDialog("Setup Complete",
                "Navigation system has been set up.\n\n" +
                "- MainMenu scene created\n" +
                "- UI added to spec scenes\n" +
                "- Build settings updated", "OK");
        }

        [MenuItem("Tools/MetavidoVFX/Navigation/Create MainMenu Scene Only", priority = 101)]
        public static void CreateMainMenuSceneOnly()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            CreateMainMenuScene();
            Debug.Log("[NavigationSceneSetup] MainMenu scene created");
        }

        [MenuItem("Tools/MetavidoVFX/Navigation/Add UI to Current Scene", priority = 102)]
        public static void AddUIToCurrentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name == "MainMenu")
            {
                Debug.LogWarning("[NavigationSceneSetup] MainMenu scene uses MainMenuUI, not SpecSceneUI");
                return;
            }

            AddSpecSceneUI();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[NavigationSceneSetup] Added UI to {scene.name}");
        }

        [MenuItem("Tools/MetavidoVFX/Navigation/Update Build Settings", priority = 103)]
        public static void UpdateBuildSettingsOnly()
        {
            UpdateBuildSettings();
            Debug.Log("[NavigationSceneSetup] Build settings updated");
        }

        #endregion

        #region Scene Creation

        private static void CreateMainMenuScene()
        {
            // Ensure directory exists
            if (!Directory.Exists(ScenesPath))
            {
                Directory.CreateDirectory(ScenesPath);
            }

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add Camera
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            camera.orthographic = false;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            // Add SceneNavigator (singleton)
            var navigatorObj = new GameObject("[SceneNavigator]");
            navigatorObj.AddComponent<UI.Navigation.SceneNavigator>();

            // Add MainMenuUI with UIDocument (creates UI programmatically in Start)
            var mainMenuObj = new GameObject("[MainMenuUI]");
            mainMenuObj.AddComponent<UIDocument>();
            mainMenuObj.AddComponent<UI.Navigation.MainMenuUI>();

            // Save scene
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[NavigationSceneSetup] Created {MainMenuScenePath}");
        }

        private static void AddSpecSceneUI()
        {
            var scene = EditorSceneManager.GetActiveScene();

            // Check if UI already exists
            var existingUI = Object.FindAnyObjectByType<UI.Navigation.SpecSceneUI>();
            if (existingUI != null)
            {
                Debug.Log($"[NavigationSceneSetup] SpecSceneUI already exists in {scene.name}");
                return;
            }

            // Add SceneNavigator if not present
            if (Object.FindAnyObjectByType<UI.Navigation.SceneNavigator>() == null)
            {
                var navigatorObj = new GameObject("[SceneNavigator]");
                navigatorObj.AddComponent<UI.Navigation.SceneNavigator>();
            }

            // Add SpecSceneUI with UIDocument (creates UI programmatically in Start)
            var specSceneObj = new GameObject("[SpecSceneUI]");
            specSceneObj.AddComponent<UIDocument>();
            specSceneObj.AddComponent<UI.Navigation.SpecSceneUI>();

            Debug.Log($"[NavigationSceneSetup] Added SpecSceneUI to {scene.name}");
        }

        private static void AddUIToAllSpecScenes()
        {
            // Get all spec scene paths
            var specScenes = new List<string>();

            if (Directory.Exists(SpecDemosPath))
            {
                var files = Directory.GetFiles(SpecDemosPath, "*.unity", SearchOption.AllDirectories);
                specScenes.AddRange(files);
            }

            // Also check for spec scenes in main Scenes folder
            if (Directory.Exists(ScenesPath))
            {
                var files = Directory.GetFiles(ScenesPath, "Spec*.unity", SearchOption.TopDirectoryOnly);
                specScenes.AddRange(files);
            }

            int processed = 0;
            foreach (var scenePath in specScenes)
            {
                // Skip MainMenu
                if (scenePath.Contains("MainMenu"))
                    continue;

                // Normalize path
                string normalizedPath = scenePath.Replace("\\", "/");

                try
                {
                    var scene = EditorSceneManager.OpenScene(normalizedPath, OpenSceneMode.Single);
                    AddSpecSceneUI();
                    EditorSceneManager.SaveScene(scene);
                    processed++;
                    Debug.Log($"[NavigationSceneSetup] Updated: {scene.name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[NavigationSceneSetup] Failed to process {normalizedPath}: {e.Message}");
                }
            }

            Debug.Log($"[NavigationSceneSetup] Processed {processed} spec scenes");
        }

        #endregion

        #region Build Settings

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();

            // Add MainMenu first (index 0)
            if (File.Exists(MainMenuScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));
            }

            // Add all spec demo scenes
            if (Directory.Exists(SpecDemosPath))
            {
                var specFiles = Directory.GetFiles(SpecDemosPath, "*.unity", SearchOption.AllDirectories);
                foreach (var file in specFiles)
                {
                    string normalizedPath = file.Replace("\\", "/");
                    scenes.Add(new EditorBuildSettingsScene(normalizedPath, true));
                }
            }

            // Add any loose spec scenes
            if (Directory.Exists(ScenesPath))
            {
                var specFiles = Directory.GetFiles(ScenesPath, "Spec*.unity", SearchOption.TopDirectoryOnly);
                foreach (var file in specFiles)
                {
                    string normalizedPath = file.Replace("\\", "/");
                    if (!scenes.Exists(s => s.path == normalizedPath))
                    {
                        scenes.Add(new EditorBuildSettingsScene(normalizedPath, true));
                    }
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[NavigationSceneSetup] Updated build settings with {scenes.Count} scenes");
        }

        #endregion
    }
}
#endif
