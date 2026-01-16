// VFXLibrarySetup - Editor utilities to set up VFX Library and HUD-UI
// Menu: H3M > VFX Library > Setup

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MetavidoVFX.VFX;
using MetavidoVFX.UI;

namespace MetavidoVFX.Editor
{
    public static class VFXLibrarySetup
    {
        [MenuItem("H3M/VFX Library/Setup Complete System", false, 100)]
        public static void SetupCompleteSystem()
        {
            SetupALL_VFX();
            SetupHUD_UI();
            Debug.Log("[VFXLibrarySetup] Complete system setup done");
        }

        [MenuItem("H3M/VFX Library/Setup ALL_VFX", false, 110)]
        public static void SetupALL_VFX()
        {
            // Check if already exists
            var existing = GameObject.Find("ALL_VFX");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("ALL_VFX Exists",
                    "ALL_VFX already exists. Replace it?", "Replace", "Cancel"))
                {
                    return;
                }
                Undo.DestroyObjectImmediate(existing);
            }

            // Create ALL_VFX parent
            var allVfx = new GameObject("ALL_VFX");
            Undo.RegisterCreatedObjectUndo(allVfx, "Create ALL_VFX");

            // Add VFXLibraryManager
            var manager = allVfx.AddComponent<VFXLibraryManager>();

            // Position at origin
            allVfx.transform.position = Vector3.zero;

            Selection.activeGameObject = allVfx;
            Debug.Log("[VFXLibrarySetup] Created ALL_VFX with VFXLibraryManager");
            Debug.Log("[VFXLibrarySetup] VFX will auto-populate on Play. Use context menu 'Populate Library' to test in Editor.");
        }

        [MenuItem("H3M/VFX Library/Setup HUD-UI", false, 120)]
        public static void SetupHUD_UI()
        {
            // Check if already exists (look for HUD-UI-VFX to avoid conflicting with HUD-UI-K)
            var existing = GameObject.Find("HUD-UI-VFX");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("HUD-UI-VFX Exists",
                    "HUD-UI-VFX already exists. Replace it?", "Replace", "Cancel"))
                {
                    return;
                }
                Undo.DestroyObjectImmediate(existing);
            }

            // Create HUD-UI-VFX (named differently from HUD-UI-K)
            var hudUI = new GameObject("HUD-UI-VFX");
            Undo.RegisterCreatedObjectUndo(hudUI, "Create HUD-UI-VFX");

            // Parent to Systems if it exists (like HUD-UI-K)
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                hudUI.transform.SetParent(systems.transform);
                Debug.Log("[VFXLibrarySetup] Parented HUD-UI-VFX to Systems");
            }

            // Add UIDocument component (like HUD-UI-K)
            var uiDoc = hudUI.AddComponent<UnityEngine.UIElements.UIDocument>();

            // Try to load PanelSettings and UXML
            var panelSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>("Assets/UI/PanelSettings.asset");
            var sourceAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/VFXLibrary.uxml");

            if (panelSettings != null)
            {
                uiDoc.panelSettings = panelSettings;
                Debug.Log("[VFXLibrarySetup] Assigned PanelSettings");
            }
            else
            {
                Debug.LogWarning("[VFXLibrarySetup] PanelSettings.asset not found at Assets/UI/");
            }

            if (sourceAsset != null)
            {
                uiDoc.visualTreeAsset = sourceAsset;
                Debug.Log("[VFXLibrarySetup] Assigned VFXLibrary.uxml");
            }
            else
            {
                Debug.LogWarning("[VFXLibrarySetup] VFXLibrary.uxml not found at Assets/UI/");
            }

            // Add VFXToggleUI
            var toggleUI = hudUI.AddComponent<VFXToggleUI>();

            // Try to find ALL_VFX and link it
            var allVfx = GameObject.Find("ALL_VFX");
            if (allVfx != null)
            {
                var manager = allVfx.GetComponent<VFXLibraryManager>();
                if (manager != null)
                {
                    // Use SerializedObject to set the reference
                    var so = new SerializedObject(toggleUI);
                    var prop = so.FindProperty("libraryManager");
                    prop.objectReferenceValue = manager;
                    so.ApplyModifiedProperties();
                    Debug.Log("[VFXLibrarySetup] Linked HUD-UI-VFX to ALL_VFX");
                }
            }

            Selection.activeGameObject = hudUI;
            Debug.Log("[VFXLibrarySetup] Created HUD-UI-VFX with UIDocument + VFXToggleUI (UI Toolkit style)");
        }

        [MenuItem("H3M/VFX Library/Populate Library (Runtime)", false, 200)]
        public static void PopulateLibraryEditor()
        {
            var allVfx = GameObject.Find("ALL_VFX");
            if (allVfx == null)
            {
                Debug.LogError("[VFXLibrarySetup] ALL_VFX not found. Run Setup first.");
                return;
            }

            var manager = allVfx.GetComponent<VFXLibraryManager>();
            if (manager == null)
            {
                Debug.LogError("[VFXLibrarySetup] VFXLibraryManager not found on ALL_VFX");
                return;
            }

            if (!Application.isPlaying)
            {
                Debug.LogWarning("[VFXLibrarySetup] Library population works best in Play mode. Use context menu on component.");
                return;
            }

            manager.PopulateLibrary();
        }

        [MenuItem("H3M/VFX Library/Clear Library", false, 210)]
        public static void ClearLibraryEditor()
        {
            var allVfx = GameObject.Find("ALL_VFX");
            if (allVfx == null)
            {
                Debug.LogError("[VFXLibrarySetup] ALL_VFX not found");
                return;
            }

            var manager = allVfx.GetComponent<VFXLibraryManager>();
            if (manager == null)
            {
                Debug.LogError("[VFXLibrarySetup] VFXLibraryManager not found on ALL_VFX");
                return;
            }

            manager.ClearLibrary();
            Debug.Log("[VFXLibrarySetup] Library cleared");
        }

        [MenuItem("H3M/VFX Library/List VFX in Resources", false, 300)]
        public static void ListVFXInResources()
        {
            var assets = Resources.LoadAll<UnityEngine.VFX.VisualEffectAsset>("VFX");
            Debug.Log($"[VFXLibrarySetup] Found {assets.Length} VFX assets in Resources/VFX:");

            var categories = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>
            {
                { "People", new() },
                { "Environment", new() },
                { "Audio", new() },
                { "Other", new() }
            };

            foreach (var asset in assets)
            {
                string name = asset.name.ToLower();
                string category = "Other";

                if (name.Contains("people") || name.Contains("depth") || name.Contains("stencil") || name.Contains("body"))
                    category = "People";
                else if (name.Contains("environment") || name.Contains("env") || name.Contains("grid"))
                    category = "Environment";
                else if (name.Contains("audio") || name.Contains("sound") || name.Contains("wave"))
                    category = "Audio";

                categories[category].Add(asset.name);
            }

            foreach (var kvp in categories)
            {
                if (kvp.Value.Count > 0)
                {
                    Debug.Log($"\n[{kvp.Key}] ({kvp.Value.Count}):");
                    foreach (var name in kvp.Value)
                    {
                        Debug.Log($"  - {name}");
                    }
                }
            }
        }
    }
}
#endif
