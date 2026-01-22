// NavigationSceneSetup.cs - Editor utility for setting up navigation scenes
// Creates MainMenu scene and adds UI to all spec demo scenes

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace MetavidoVFX.Editor
{
    /// <summary>
    /// Editor utility for setting up the navigation system across all scenes.
    /// </summary>
    public static class NavigationSceneSetup
    {
        private const string ScriptsPath = "Assets/Scripts/UI/Navigation";
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
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            camera.orthographic = false;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            // Add EventSystem
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();

            // Add Canvas
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Configure CanvasScaler for responsive UI
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Add Title
            var titleObj = CreateTextObject("Title", canvasObj.transform);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -40);
            titleRect.sizeDelta = new Vector2(800, 60);

            var titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.text = "MetavidoVFX Spec Demos";
            titleText.fontSize = 42;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Add Button Container with GridLayout
            var containerObj = new GameObject("ButtonContainer");
            containerObj.transform.SetParent(canvasObj.transform, false);

            var containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, 0.15f);
            containerRect.anchorMax = new Vector2(0.9f, 0.85f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            var gridLayout = containerObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(380, 70);
            gridLayout.spacing = new Vector2(20, 15);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.padding = new RectOffset(10, 10, 10, 10);

            // Add Version Text
            var versionObj = CreateTextObject("Version", canvasObj.transform);
            var versionRect = versionObj.GetComponent<RectTransform>();
            versionRect.anchorMin = new Vector2(1f, 0f);
            versionRect.anchorMax = new Vector2(1f, 0f);
            versionRect.pivot = new Vector2(1f, 0f);
            versionRect.anchoredPosition = new Vector2(-20, 20);
            versionRect.sizeDelta = new Vector2(300, 30);

            var versionText = versionObj.GetComponent<TextMeshProUGUI>();
            versionText.text = $"Unity {Application.unityVersion}";
            versionText.fontSize = 14;
            versionText.alignment = TextAlignmentOptions.BottomRight;
            versionText.color = new Color(0.6f, 0.6f, 0.6f, 1f);

            // Add MainMenuUI component
            var mainMenuUI = canvasObj.AddComponent<UI.Navigation.MainMenuUI>();

            // Use SerializedObject to set private fields
            var so = new SerializedObject(mainMenuUI);
            so.FindProperty("_buttonContainer").objectReferenceValue = containerObj.transform;
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_versionText").objectReferenceValue = versionText;
            so.FindProperty("_columns").intValue = 2;
            so.ApplyModifiedProperties();

            // Add SceneNavigator
            var navigatorObj = new GameObject("[SceneNavigator]");
            navigatorObj.AddComponent<UI.Navigation.SceneNavigator>();

            // Add DebugStatsHUD
            var debugHudObj = new GameObject("[DebugStatsHUD]");
            debugHudObj.AddComponent<UI.Navigation.DebugStatsHUD>();

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

            // Ensure EventSystem exists
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            // Find or create Canvas
            var canvas = Object.FindAnyObjectByType<Canvas>();
            GameObject canvasObj;

            if (canvas == null)
            {
                canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            // Create SpecSceneUI container
            var uiContainerObj = new GameObject("SpecSceneUI");
            uiContainerObj.transform.SetParent(canvasObj.transform, false);

            var containerRect = uiContainerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Add Title Text (top center)
            var titleObj = CreateTextObject("TitleText", uiContainerObj.transform);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(600, 50);

            var titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.text = scene.name;
            titleText.fontSize = 28;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Add Back Button (top left)
            var backButtonObj = CreateButton("BackButton", uiContainerObj.transform);
            var backButtonRect = backButtonObj.GetComponent<RectTransform>();
            backButtonRect.anchorMin = new Vector2(0f, 1f);
            backButtonRect.anchorMax = new Vector2(0f, 1f);
            backButtonRect.pivot = new Vector2(0f, 1f);
            backButtonRect.anchoredPosition = new Vector2(20, -20);
            backButtonRect.sizeDelta = new Vector2(120, 45);

            var backButtonText = backButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            backButtonText.text = "← Back";
            backButtonText.fontSize = 18;

            var backButtonImage = backButtonObj.GetComponent<Image>();
            backButtonImage.color = new Color(0.3f, 0.3f, 0.35f, 0.9f);

            // Add SpecSceneUI component
            var specSceneUI = uiContainerObj.AddComponent<UI.Navigation.SpecSceneUI>();

            var so = new SerializedObject(specSceneUI);
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_backButton").objectReferenceValue = backButtonObj.GetComponent<Button>();
            so.FindProperty("_autoSetTitle").boolValue = true;
            so.FindProperty("_autoCreateDebugHUD").boolValue = true;
            so.ApplyModifiedProperties();

            // Add DebugStatsHUD if not present
            var debugHud = Object.FindAnyObjectByType<UI.Navigation.DebugStatsHUD>();
            if (debugHud == null)
            {
                var debugHudObj = new GameObject("[DebugStatsHUD]");
                debugHudObj.AddComponent<UI.Navigation.DebugStatsHUD>();
            }

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

        #region UI Helpers

        private static GameObject CreateTextObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            obj.AddComponent<RectTransform>();
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.color = Color.white;

            return obj;
        }

        private static GameObject CreateButton(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            obj.AddComponent<RectTransform>();
            var image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.4f, 0.8f, 1f);

            var button = obj.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.4f, 0.8f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.9f, 1f);
            colors.pressedColor = new Color(0.15f, 0.3f, 0.6f, 1f);
            button.colors = colors;

            // Add text child
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return obj;
        }

        #endregion
    }
}
#endif
