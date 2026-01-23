// XRRAISceneSetup.cs - Editor utilities for XRRAI Scene system
// Part of Spec 016: XRRAI Scene Format & Cross-Platform Export
//
// Provides menu commands for setting up scene management, testing save/load, and export.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using XRRAI.Scene;
using XRRAI.BrushPainting;
using XRRAI.VoiceToObject;

namespace XRRAI.Editor
{
    /// <summary>
    /// Editor menu commands for XRRAI Scene system setup and testing.
    /// </summary>
    public static class XRRAISceneSetup
    {
        private const string MENU_PREFIX = "H3M/XRRAI Scene/";

        #region Setup

        [MenuItem(MENU_PREFIX + "Setup Complete Scene System", priority = 0)]
        public static void SetupCompleteSystem()
        {
            Undo.SetCurrentGroupName("Setup XRRAI Scene System");
            int group = Undo.GetCurrentGroup();

            bool createdSceneManager = CreateXRRAISceneManager();
            bool createdGalleryManager = CreateIcosaGalleryManager();

            Undo.CollapseUndoOperations(group);

            string message = "XRRAI Scene System Setup Complete:\n";
            message += createdSceneManager ? "- Created XRRAISceneManager\n" : "- XRRAISceneManager exists\n";
            message += createdGalleryManager ? "- Created IcosaGalleryManager\n" : "- IcosaGalleryManager exists\n";
            message += "\nUse H3M > XRRAI Scene > Test to verify functionality.";

            EditorUtility.DisplayDialog("XRRAI Scene Setup", message, "OK");
            Debug.Log($"[XRRAISceneSetup] {message.Replace("\n", " ")}");
        }

        [MenuItem(MENU_PREFIX + "Components/Create XRRAISceneManager", priority = 10)]
        public static bool CreateXRRAISceneManager()
        {
            var existing = Object.FindFirstObjectByType<XRRAISceneManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[XRRAISceneSetup] XRRAISceneManager already exists");
                return false;
            }

            var go = new GameObject("XRRAISceneManager");
            Undo.RegisterCreatedObjectUndo(go, "Create XRRAISceneManager");

            var manager = go.AddComponent<XRRAISceneManager>();
            go.AddComponent<GLTFExporter>();

            // Link to BrushManager if exists
            var brushManager = Object.FindFirstObjectByType<BrushManager>();
            if (brushManager != null)
            {
                var so = new SerializedObject(manager);
                var prop = so.FindProperty("_brushManager");
                if (prop != null)
                {
                    prop.objectReferenceValue = brushManager;
                    so.ApplyModifiedProperties();
                }
            }

            Selection.activeGameObject = go;
            Debug.Log("[XRRAISceneSetup] Created XRRAISceneManager");
            return true;
        }

        [MenuItem(MENU_PREFIX + "Components/Create IcosaGalleryManager", priority = 11)]
        public static bool CreateIcosaGalleryManager()
        {
            var existing = Object.FindFirstObjectByType<IcosaGalleryManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[XRRAISceneSetup] IcosaGalleryManager already exists");
                return false;
            }

            var go = new GameObject("IcosaGalleryManager");
            Undo.RegisterCreatedObjectUndo(go, "Create IcosaGalleryManager");
            go.AddComponent<IcosaGalleryManager>();

            Selection.activeGameObject = go;
            Debug.Log("[XRRAISceneSetup] Created IcosaGalleryManager");
            return true;
        }

        #endregion

        #region Testing

        [MenuItem(MENU_PREFIX + "Test/Save Current Scene", priority = 100)]
        public static void TestSaveScene()
        {
            var manager = Object.FindFirstObjectByType<XRRAISceneManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "XRRAISceneManager not found in scene.\nRun Setup first.", "OK");
                return;
            }

            string sceneName = "TestScene_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            manager.SetSceneMetadata(sceneName, "Test save from editor");

            if (manager.SaveScene())
            {
                EditorUtility.DisplayDialog("Success",
                    $"Scene saved to:\n{manager.CurrentFilePath}",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Save failed. Check console for details.", "OK");
            }
        }

        [MenuItem(MENU_PREFIX + "Test/Load Scene...", priority = 101)]
        public static void TestLoadScene()
        {
            var manager = Object.FindFirstObjectByType<XRRAISceneManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "XRRAISceneManager not found in scene.\nRun Setup first.", "OK");
                return;
            }

            string path = EditorUtility.OpenFilePanel("Load XRRAI Scene", manager.SaveDirectory, "xrrai");
            if (string.IsNullOrEmpty(path))
                return;

            if (manager.LoadScene(path))
            {
                EditorUtility.DisplayDialog("Success",
                    $"Scene loaded: {manager.CurrentScene.scene.name}\n" +
                    $"Strokes: {manager.CurrentScene.strokes.Count}",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Load failed. Check console for details.", "OK");
            }
        }

        [MenuItem(MENU_PREFIX + "Test/Export to GLB...", priority = 102)]
        public static async void TestExportGLB()
        {
            var manager = Object.FindFirstObjectByType<XRRAISceneManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "XRRAISceneManager not found in scene.\nRun Setup first.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export GLB", manager.SaveDirectory, "scene", "glb");
            if (string.IsNullOrEmpty(path))
                return;

            EditorUtility.DisplayProgressBar("Exporting", "Exporting to GLB...", 0.5f);

            try
            {
                string result = await manager.ExportGLTFAsync(path);
                EditorUtility.ClearProgressBar();

                if (!string.IsNullOrEmpty(result))
                {
                    EditorUtility.DisplayDialog("Success",
                        $"Exported to:\n{result}",
                        "OK");

                    // Optionally open in file explorer
                    EditorUtility.RevealInFinder(result);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Export failed. Check console for details.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Export failed: {ex.Message}", "OK");
            }
        }

        [MenuItem(MENU_PREFIX + "Test/List Saved Scenes", priority = 103)]
        public static void ListSavedScenes()
        {
            var manager = Object.FindFirstObjectByType<XRRAISceneManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "XRRAISceneManager not found in scene.\nRun Setup first.", "OK");
                return;
            }

            var scenes = manager.GetSavedScenes();
            var report = new System.Text.StringBuilder();
            report.AppendLine($"=== Saved Scenes ({scenes.Count}) ===\n");
            report.AppendLine($"Directory: {manager.SaveDirectory}\n");

            foreach (var scene in scenes)
            {
                var info = new FileInfo(scene);
                report.AppendLine($"- {info.Name} ({info.Length / 1024f:F1} KB)");
            }

            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Saved Scenes", report.ToString(), "OK");
        }

        [MenuItem(MENU_PREFIX + "Test/Open Save Directory", priority = 104)]
        public static void OpenSaveDirectory()
        {
            var manager = Object.FindFirstObjectByType<XRRAISceneManager>();
            string dir = manager?.SaveDirectory ?? Path.Combine(Application.persistentDataPath, "XRRAIScenes");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            EditorUtility.RevealInFinder(dir);
        }

        #endregion

        #region Verification

        [MenuItem(MENU_PREFIX + "Verify Setup", priority = 200)]
        public static void VerifySetup()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== XRRAI Scene System Report ===\n");

            // Check XRRAISceneManager
            var sceneManager = Object.FindFirstObjectByType<XRRAISceneManager>();
            if (sceneManager != null)
            {
                report.AppendLine("- XRRAISceneManager: Found");
                report.AppendLine($"   Save Directory: {sceneManager.SaveDirectory}");
                report.AppendLine($"   Saved Scenes: {sceneManager.GetSavedScenes().Count}");

                var exporter = sceneManager.GetComponent<GLTFExporter>();
                report.AppendLine($"   GLTFExporter: {(exporter != null ? "Attached" : "Missing")}");
            }
            else
            {
                report.AppendLine("- XRRAISceneManager: NOT FOUND");
            }

            // Check IcosaGalleryManager
            var galleryManager = Object.FindFirstObjectByType<IcosaGalleryManager>();
            if (galleryManager != null)
            {
                report.AppendLine($"- IcosaGalleryManager: Found");
                report.AppendLine($"   Authenticated: {galleryManager.IsAuthenticated}");
            }
            else
            {
                report.AppendLine("- IcosaGalleryManager: NOT FOUND");
            }

            // Check BrushManager
            var brushManager = Object.FindFirstObjectByType<BrushManager>();
            if (brushManager != null)
            {
                report.AppendLine($"- BrushManager: Found");
                report.AppendLine($"   Strokes: {brushManager.Strokes.Count}");
                report.AppendLine($"   Brushes: {brushManager.BrushCatalog.Count}");
            }
            else
            {
                report.AppendLine("- BrushManager: NOT FOUND (strokes won't save)");
            }

            // Summary
            report.AppendLine("\n=== Summary ===");
            bool ready = sceneManager != null;
            report.AppendLine(ready ? "System ready for use" : "Run Setup first");

            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("XRRAI Scene Verification", report.ToString(), "OK");
        }

        #endregion

        #region Demo Scene

        [MenuItem(MENU_PREFIX + "Create Spec 016 Demo Scene", priority = 300)]
        public static void CreateDemoScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Setup core components
            CreateXRRAISceneManager();
            CreateIcosaGalleryManager();

            // Add BrushManager if not exists
            var brushManager = Object.FindFirstObjectByType<BrushManager>();
            if (brushManager == null)
            {
                var brushGo = new GameObject("BrushManager");
                brushGo.AddComponent<BrushManager>();
            }

            // Add AR Session if available
#if UNITY_XR_ARFOUNDATION
            var arSession = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
            if (arSession == null)
            {
                var arGo = new GameObject("AR Session");
                arGo.AddComponent<UnityEngine.XR.ARFoundation.ARSession>();
            }

            var arSessionOrigin = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.XROrigin>();
            if (arSessionOrigin == null)
            {
                var originGo = new GameObject("XR Origin");
                originGo.AddComponent<UnityEngine.XR.ARFoundation.XROrigin>();
            }
#endif

            // Save scene
            string scenePath = "Assets/Scenes/SpecDemos/Spec016_XRRAI_Scene.unity";
            string dir = Path.GetDirectoryName(scenePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[XRRAISceneSetup] Created demo scene: {scenePath}");

            EditorUtility.DisplayDialog("Demo Scene Created",
                $"Spec 016 demo scene created at:\n{scenePath}\n\n" +
                "Components added:\n" +
                "- XRRAISceneManager\n" +
                "- GLTFExporter\n" +
                "- IcosaGalleryManager\n" +
                "- BrushManager",
                "OK");
        }

        #endregion
    }
}
