// VFXLibraryManagerEditor - Custom inspector with populate and utility buttons
// Provides non-destructive repopulation (adds new VFX without removing existing)

using UnityEngine;
using UnityEditor;
using MetavidoVFX.VFX;

namespace MetavidoVFX.Editor
{
    [CustomEditor(typeof(VFXLibraryManager))]
    public class VFXLibraryManagerEditor : UnityEditor.Editor
    {
        private bool regenerateAll = false;
        private bool showAdvancedOptions = false;

        // Serialized properties for custom display
        private SerializedProperty directVFXAssets;
        private SerializedProperty resourceFolders;
        private SerializedProperty searchPaths;
        private SerializedProperty useProjectSearch;
        private SerializedProperty includeSubfolders;
        private SerializedProperty useExistingChildren;
        private SerializedProperty organizeByCategory;
        private SerializedProperty createCategoryContainers;
        private SerializedProperty removeLegacyComponents;
        private SerializedProperty startAllDisabled;
        private SerializedProperty maxActiveVFX;
        private SerializedProperty hologramVFX;

        void OnEnable()
        {
            directVFXAssets = serializedObject.FindProperty("directVFXAssets");
            resourceFolders = serializedObject.FindProperty("resourceFolders");
            searchPaths = serializedObject.FindProperty("searchPaths");
            useProjectSearch = serializedObject.FindProperty("useProjectSearch");
            includeSubfolders = serializedObject.FindProperty("includeSubfolders");
            useExistingChildren = serializedObject.FindProperty("useExistingChildren");
            organizeByCategory = serializedObject.FindProperty("organizeByCategory");
            createCategoryContainers = serializedObject.FindProperty("createCategoryContainers");
            removeLegacyComponents = serializedObject.FindProperty("removeLegacyComponents");
            startAllDisabled = serializedObject.FindProperty("startAllDisabled");
            maxActiveVFX = serializedObject.FindProperty("maxActiveVFX");
            hologramVFX = serializedObject.FindProperty("hologramVFX");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var manager = (VFXLibraryManager)target;

            // === ACTION BUTTONS ===
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                // Main populate button
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if (GUILayout.Button(new GUIContent("Populate ALL_VFX", "Add new VFX to library (preserves existing)"), GUILayout.Height(30)))
                {
                    PopulateLibrary(manager, regenerateAll);
                }
                GUI.backgroundColor = Color.white;

                // Regenerate checkbox
                EditorGUILayout.LabelField("", GUILayout.Width(10));
                regenerateAll = EditorGUILayout.ToggleLeft(
                    new GUIContent("Regenerate All", "Clear existing VFX and recreate from scratch"),
                    regenerateAll,
                    GUILayout.Width(110)
                );
            }

            EditorGUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                // Open VFX Rename Utility
                if (GUILayout.Button(new GUIContent("VFX Rename Utility", "Open batch rename window for VFX assets"), GUILayout.Height(25)))
                {
                    VFXRenameUtility.ShowWindow();
                }

                // List all VFX in project
                if (GUILayout.Button(new GUIContent("List All VFX", "Log all VFX assets in project to console"), GUILayout.Height(25)))
                {
                    manager.SendMessage("ListAllVFXInProject", SendMessageOptions.DontRequireReceiver);
                }
            }

            EditorGUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                // Clear library
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button(new GUIContent("Clear Library", "Remove all VFX children"), GUILayout.Height(22)))
                {
                    if (EditorUtility.DisplayDialog("Clear VFX Library",
                        "This will remove all VFX children from ALL_VFX. Continue?",
                        "Clear", "Cancel"))
                    {
                        manager.ClearLibrary();
                    }
                }
                GUI.backgroundColor = Color.white;

                // Rebuild from children
                if (GUILayout.Button(new GUIContent("Rebuild Lists", "Rebuild runtime lists from existing children"), GUILayout.Height(22)))
                {
                    manager.RebuildFromChildren();
                }
            }

            // Show current stats
            EditorGUILayout.Space(5);
            var childCount = manager.transform.childCount;
            var vfxCount = manager.transform.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true).Length;
            EditorGUILayout.HelpBox($"Current: {vfxCount} VFX in {childCount} children", MessageType.Info);

            // === VFX SOURCES ===
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("VFX Sources", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(directVFXAssets, new GUIContent("Direct References", "Drag VFX assets here"));
            EditorGUILayout.PropertyField(resourceFolders, new GUIContent("Resources Folders", "Folders inside Resources/"));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(useProjectSearch, new GUIContent("Use Project Search", "Search Assets folders (Editor only)"));
            if (useProjectSearch.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(searchPaths, new GUIContent("Search Paths", "Asset folders to search"));
                EditorGUILayout.PropertyField(includeSubfolders, new GUIContent("Include Subfolders"));
                EditorGUI.indentLevel--;

                // Quick add common paths button
                if (GUILayout.Button("Add Common VFX Paths", GUILayout.Height(20)))
                {
                    manager.SendMessage("AddCommonVFXPaths", SendMessageOptions.DontRequireReceiver);
                }
            }

            // === ADVANCED OPTIONS ===
            EditorGUILayout.Space(10);
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options", true);
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Behavior", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(useExistingChildren, new GUIContent("Use Existing Children", "On Start, rebuild from children instead of creating new"));

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Organization", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(organizeByCategory);
                EditorGUILayout.PropertyField(createCategoryContainers);

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Pipeline", EditorStyles.miniBoldLabel);
                if (removeLegacyComponents != null)
                    EditorGUILayout.PropertyField(removeLegacyComponents);

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Initial State", EditorStyles.miniBoldLabel);
                if (startAllDisabled != null)
                    EditorGUILayout.PropertyField(startAllDisabled);
                if (maxActiveVFX != null)
                    EditorGUILayout.PropertyField(maxActiveVFX);

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Hologram", EditorStyles.miniBoldLabel);
                if (hologramVFX != null)
                    EditorGUILayout.PropertyField(hologramVFX);

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Populate the library with new VFX, optionally regenerating all
        /// </summary>
        private void PopulateLibrary(VFXLibraryManager manager, bool regenerate)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[VFXLibrary] Cannot populate in Play mode. Stop the game first.");
                return;
            }

            if (regenerate)
            {
                // Full regeneration - clear and repopulate
                if (EditorUtility.DisplayDialog("Regenerate All VFX",
                    "This will remove ALL existing VFX and recreate them from configured sources.\n\nContinue?",
                    "Regenerate", "Cancel"))
                {
                    manager.PopulateLibrary();
                    Debug.Log("[VFXLibrary] Library regenerated from scratch");
                }
            }
            else
            {
                // Non-destructive - only add new VFX
                PopulateAdditive(manager);
            }
        }

        /// <summary>
        /// Add only new VFX that don't already exist in the library
        /// </summary>
        private void PopulateAdditive(VFXLibraryManager manager)
        {
            Undo.SetCurrentGroupName("Populate VFX Library (Additive)");
            int undoGroup = Undo.GetCurrentGroup();

            // Get existing VFX names
            var existingNames = new System.Collections.Generic.HashSet<string>();
            var existingVFX = manager.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
            foreach (var vfx in existingVFX)
            {
                if (vfx.visualEffectAsset != null)
                {
                    existingNames.Add(vfx.visualEffectAsset.name);
                }
            }

            int existingCount = existingNames.Count;
            Debug.Log($"[VFXLibrary] Found {existingCount} existing VFX");

            // Collect all available VFX assets
            var allAssets = new System.Collections.Generic.List<UnityEngine.VFX.VisualEffectAsset>();

            // Source 1: Direct references
            var directAssets = (UnityEngine.VFX.VisualEffectAsset[])typeof(VFXLibraryManager)
                .GetField("directVFXAssets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(manager);

            if (directAssets != null)
            {
                foreach (var asset in directAssets)
                {
                    if (asset != null) allAssets.Add(asset);
                }
            }

            // Source 2: Resources folders
            var resFolders = (string[])typeof(VFXLibraryManager)
                .GetField("resourceFolders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(manager);

            if (resFolders != null)
            {
                foreach (var folder in resFolders)
                {
                    if (!string.IsNullOrEmpty(folder))
                    {
                        var assets = Resources.LoadAll<UnityEngine.VFX.VisualEffectAsset>(folder);
                        allAssets.AddRange(assets);
                    }
                }
            }

            // Source 3: Project search
            var useSearch = (bool)typeof(VFXLibraryManager)
                .GetField("useProjectSearch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(manager);

            var paths = (string[])typeof(VFXLibraryManager)
                .GetField("searchPaths", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(manager);

            if (useSearch && paths != null && paths.Length > 0)
            {
                string[] guids = AssetDatabase.FindAssets("t:VisualEffectAsset", paths);
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(path);
                    if (asset != null) allAssets.Add(asset);
                }
            }

            // Find new assets (not in existing)
            var newAssets = new System.Collections.Generic.List<UnityEngine.VFX.VisualEffectAsset>();
            var seenNames = new System.Collections.Generic.HashSet<string>();

            foreach (var asset in allAssets)
            {
                if (!existingNames.Contains(asset.name) && !seenNames.Contains(asset.name))
                {
                    newAssets.Add(asset);
                    seenNames.Add(asset.name);
                }
            }

            if (newAssets.Count == 0)
            {
                Debug.Log("[VFXLibrary] No new VFX to add. Library is up to date.");
                return;
            }

            Debug.Log($"[VFXLibrary] Adding {newAssets.Count} new VFX...");

            // Get settings for creating VFX (new pipeline - always add VFXARBinder and VFXCategory)
            var startDisabledField = typeof(VFXLibraryManager)
                .GetField("startAllDisabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool startDisabled = startDisabledField != null && (bool)startDisabledField.GetValue(manager);

            var createContainersField = typeof(VFXLibraryManager)
                .GetField("createCategoryContainers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool createContainers = createContainersField != null && (bool)createContainersField.GetValue(manager);

            // Find or create category containers
            var categoryContainers = new System.Collections.Generic.Dictionary<VFXCategoryType, Transform>();
            if (createContainers)
            {
                foreach (VFXCategoryType cat in System.Enum.GetValues(typeof(VFXCategoryType)))
                {
                    string containerName = $"[{cat}]";
                    var existingContainer = manager.transform.Find(containerName);
                    if (existingContainer != null)
                    {
                        categoryContainers[cat] = existingContainer;
                    }
                    else
                    {
                        var container = new GameObject(containerName);
                        container.transform.SetParent(manager.transform);
                        container.transform.localPosition = Vector3.zero;
                        Undo.RegisterCreatedObjectUndo(container, "Create Category Container");
                        categoryContainers[cat] = container.transform;
                    }
                }
            }

            // Create new VFX
            int addedCount = 0;
            foreach (var asset in newAssets)
            {
                var category = DetectCategory(asset.name);

                var go = new GameObject(asset.name);
                Undo.RegisterCreatedObjectUndo(go, $"Create VFX {asset.name}");

                // Parent to category container or manager
                if (createContainers && categoryContainers.TryGetValue(category, out var parent))
                {
                    go.transform.SetParent(parent);
                }
                else
                {
                    go.transform.SetParent(manager.transform);
                }
                go.transform.localPosition = Vector3.zero;

                // Add VisualEffect
                var vfx = go.AddComponent<UnityEngine.VFX.VisualEffect>();
                vfx.visualEffectAsset = asset;

                // New pipeline: always add VFXARBinder and VFXCategory
                var arBinder = go.AddComponent<VFXARBinder>();
                arBinder.AutoDetectBindings();

                var catComp = go.AddComponent<VFXCategory>();
                catComp.SetCategory(category);

                // Start disabled if configured
                if (startDisabled)
                {
                    go.SetActive(false);
                    vfx.enabled = false;
                }

                addedCount++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            // Mark scene dirty
            EditorUtility.SetDirty(manager.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

            Debug.Log($"[VFXLibrary] Added {addedCount} new VFX. Total now: {existingCount + addedCount}");

            // Rebuild runtime lists
            manager.RebuildFromChildren();
        }

        /// <summary>
        /// Detect category from asset name (same logic as VFXLibraryManager)
        /// </summary>
        private VFXCategoryType DetectCategory(string assetName)
        {
            string name = assetName.ToLower();

            if (name.Contains("hand"))
                return VFXCategoryType.Hands;
            if (name.Contains("face"))
                return VFXCategoryType.Face;
            if (name.Contains("audio") || name.Contains("sound") || name.Contains("wave"))
                return VFXCategoryType.Audio;
            if (name.Contains("environment") || name.Contains("env") || name.Contains("grid") || name.Contains("world"))
                return VFXCategoryType.Environment;
            if (name.Contains("people") || name.Contains("body") || name.Contains("depth") || name.Contains("stencil"))
                return VFXCategoryType.People;

            return VFXCategoryType.People;
        }
    }
}
