using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;
using MetavidoVFX.VFX;

/// <summary>
/// Unified VFX Pipeline setup automation.
/// One-click setup, legacy management, bulk operations.
///
/// Pipeline: ARDepthSource (singleton compute) + VFXARBinder (lightweight per-VFX)
/// Menu: H3M/VFX Pipeline Master/
/// Goal: Auto setup, legacy removal, single-button operations
/// </summary>
public static class VFXPipelineMasterSetup
{
    // ═══════════════════════════════════════════════════════════════════════
    // MAIN SETUP
    // ═══════════════════════════════════════════════════════════════════════

    [MenuItem("H3M/VFX Pipeline Master/Setup Complete Pipeline (Recommended)", false, 0)]
    public static void SetupCompletePipeline()
    {
        EditorUtility.DisplayProgressBar("VFX Pipeline Setup", "Removing legacy components...", 0.1f);

        // 1. Remove legacy components first
        int legacyRemoved = RemoveLegacyComponentsInternal();

        EditorUtility.DisplayProgressBar("VFX Pipeline Setup", "Creating ARDepthSource...", 0.2f);

        // 2. Create ARDepthSource if missing
        var source = CreateARDepthSource();

        EditorUtility.DisplayProgressBar("VFX Pipeline Setup", "Creating AudioBridge...", 0.3f);

        // 3. Create AudioBridge if missing
        CreateAudioBridge();

        EditorUtility.DisplayProgressBar("VFX Pipeline Setup", "Adding VFXARBinder to all VFX...", 0.5f);

        // 4. Add VFXARBinder to all VFX with auto-detect
        int bindersAdded = AddVFXARBinderToAllVFX();

        EditorUtility.DisplayProgressBar("VFX Pipeline Setup", "Adding debug tools...", 0.8f);

        // 5. Add Dashboard and TestHarness
        AddPipelineDashboard();
        AddTestHarness();

        EditorUtility.ClearProgressBar();

        // Summary
        var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
        var binders = Object.FindObjectsByType<VFXARBinder>(FindObjectsSortMode.None);
        var audio = Object.FindFirstObjectByType<AudioBridge>();

        EditorUtility.DisplayDialog("VFX Pipeline Setup Complete",
            $"Hybrid Bridge Pipeline ready:\n\n" +
            $"ARDepthSource: {(source != null ? "Active" : "Failed")}\n" +
            $"AudioBridge: {(audio != null ? "Active" : "Failed")}\n" +
            $"VFXARBinder: {bindersAdded} added ({binders.Length} total)\n" +
            $"VFX in scene: {allVFX.Length}\n" +
            $"Legacy removed: {legacyRemoved}\n" +
            $"Dashboard: Added\n" +
            $"TestHarness: Added\n\n" +
            $"Press Tab to toggle dashboard.\n" +
            $"Press 1-9/Space/C/A for VFX shortcuts.",
            "OK");

        Debug.Log($"[VFXPipelineMasterSetup] Complete pipeline setup finished. " +
                  $"Source={source != null}, Binders={binders.Length}, LegacyRemoved={legacyRemoved}");
    }

    static int RemoveLegacyComponentsInternal()
    {
        int removed = 0;
        var legacyTypes = new[] { "VFXBinderManager", "VFXARDataBinder", "PeopleOcclusionVFXManager", "OptimizedARVFXBridge" };

        foreach (var typeName in legacyTypes)
        {
            var components = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(m => m.GetType().Name == typeName).ToArray();

            foreach (var c in components)
            {
                Undo.DestroyObjectImmediate(c);
                removed++;
            }
        }

        // Also remove empty VFXPropertyBinder
        var propertyBinders = Object.FindObjectsByType<UnityEngine.VFX.Utility.VFXPropertyBinder>(FindObjectsSortMode.None);
        foreach (var binder in propertyBinders)
        {
            var bindings = binder.GetPropertyBinders();
            if (bindings == null || bindings.Count() == 0)
            {
                Undo.DestroyObjectImmediate(binder);
                removed++;
            }
        }

        if (removed > 0)
            Debug.Log($"[VFXPipelineMasterSetup] Removed {removed} legacy components");

        return removed;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PIPELINE COMPONENTS
    // ═══════════════════════════════════════════════════════════════════════

    [MenuItem("H3M/VFX Pipeline Master/Pipeline Components/Create ARDepthSource", false, 100)]
    public static ARDepthSource CreateARDepthSource()
    {
        var existing = Object.FindFirstObjectByType<ARDepthSource>();
        if (existing != null)
        {
            Debug.Log($"[VFXPipelineMasterSetup] ARDepthSource already exists on {existing.gameObject.name}");
            Selection.activeGameObject = existing.gameObject;
            return existing;
        }

        // Create new
        var go = new GameObject("ARDepthSource");
        var source = go.AddComponent<ARDepthSource>();
        Undo.RegisterCreatedObjectUndo(go, "Create ARDepthSource");

        // Try to find AR components
        var occlusion = Object.FindFirstObjectByType<AROcclusionManager>();
        if (occlusion != null)
        {
            var so = new SerializedObject(source);
            so.FindProperty("_occlusion").objectReferenceValue = occlusion;
            so.ApplyModifiedProperties();
        }

        Debug.Log("[VFXPipelineMasterSetup] Created ARDepthSource");
        Selection.activeGameObject = go;
        return source;
    }

    [MenuItem("H3M/VFX Pipeline Master/Pipeline Components/Add VFXARBinder to All VFX", false, 101)]
    public static int AddVFXARBinderToAllVFX()
    {
        var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
        int added = 0;

        foreach (var vfx in allVFX)
        {
            var binder = vfx.GetComponent<VFXARBinder>();
            if (binder == null)
            {
                binder = Undo.AddComponent<VFXARBinder>(vfx.gameObject);
                added++;
            }
            // Always auto-detect bindings
            binder.AutoDetectBindings();
        }

        Debug.Log($"[VFXPipelineMasterSetup] Added VFXARBinder to {added} VFX, auto-detected bindings for {allVFX.Length}");
        return added;
    }

    [MenuItem("H3M/VFX Pipeline Master/Pipeline Components/Auto-Detect All Bindings", false, 104)]
    public static void AutoDetectAllBindings()
    {
        var binders = Object.FindObjectsByType<VFXARBinder>(FindObjectsSortMode.None);
        foreach (var binder in binders)
        {
            binder.AutoDetectBindings();
        }
        Debug.Log($"[VFXPipelineMasterSetup] Auto-detected bindings for {binders.Length} VFX");
    }

    [MenuItem("H3M/VFX Pipeline Master/Pipeline Components/Remove All Legacy Components", false, 105)]
    public static void RemoveAllLegacyComponents()
    {
        int removed = 0;

        // Remove VFXBinderManager
        var binderManagers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(m => m.GetType().Name == "VFXBinderManager").ToArray();
        foreach (var m in binderManagers)
        {
            Debug.Log($"[VFXPipelineMasterSetup] Removing VFXBinderManager from {m.gameObject.name}");
            Undo.DestroyObjectImmediate(m);
            removed++;
        }

        // Remove VFXARDataBinder
        var dataBinders = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(m => m.GetType().Name == "VFXARDataBinder").ToArray();
        foreach (var b in dataBinders)
        {
            Debug.Log($"[VFXPipelineMasterSetup] Removing VFXARDataBinder from {b.gameObject.name}");
            Undo.DestroyObjectImmediate(b);
            removed++;
        }

        // Remove empty VFXPropertyBinder
        var propertyBinders = Object.FindObjectsByType<UnityEngine.VFX.Utility.VFXPropertyBinder>(FindObjectsSortMode.None);
        foreach (var binder in propertyBinders)
        {
            var bindings = binder.GetPropertyBinders();
            if (bindings == null || bindings.Count() == 0)
            {
                Debug.Log($"[VFXPipelineMasterSetup] Removing empty VFXPropertyBinder from {binder.gameObject.name}");
                Undo.DestroyObjectImmediate(binder);
                removed++;
            }
        }

        // Remove PeopleOcclusionVFXManager
        var peopleManagers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(m => m.GetType().Name == "PeopleOcclusionVFXManager").ToArray();
        foreach (var p in peopleManagers)
        {
            Debug.Log($"[VFXPipelineMasterSetup] Removing PeopleOcclusionVFXManager from {p.gameObject.name}");
            Undo.DestroyObjectImmediate(p);
            removed++;
        }

        Debug.Log($"[VFXPipelineMasterSetup] Removed {removed} legacy components");
        EditorUtility.DisplayDialog("Legacy Components Removed",
            $"Removed {removed} legacy components.\n\n" +
            "Components removed:\n" +
            "- VFXBinderManager\n" +
            "- VFXARDataBinder\n" +
            "- VFXPropertyBinder (empty)\n" +
            "- PeopleOcclusionVFXManager",
            "OK");
    }

    [MenuItem("H3M/VFX Pipeline Master/Pipeline Components/Add VFXARBinder to Selected", false, 102)]
    public static void AddVFXARBinderToSelected()
    {
        int added = 0;
        foreach (var go in Selection.gameObjects)
        {
            var vfx = go.GetComponent<VisualEffect>();
            if (vfx != null && go.GetComponent<VFXARBinder>() == null)
            {
                Undo.AddComponent<VFXARBinder>(go);
                added++;
            }
        }

        if (added > 0)
            Debug.Log($"[VFXPipelineMasterSetup] Added VFXARBinder to {added} selected VFX");
        else
            Debug.LogWarning("[VFXPipelineMasterSetup] No VFX selected or all already have binders");
    }

    [MenuItem("H3M/VFX Pipeline Master/Pipeline Components/Create AudioBridge", false, 103)]
    public static void CreateAudioBridge()
    {
        var existing = Object.FindFirstObjectByType<AudioBridge>();
        if (existing != null)
        {
            Debug.Log($"[VFXPipelineMasterSetup] AudioBridge already exists on {existing.gameObject.name}");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var go = new GameObject("AudioBridge");
        go.AddComponent<AudioBridge>();
        Undo.RegisterCreatedObjectUndo(go, "Create AudioBridge");

        Debug.Log("[VFXPipelineMasterSetup] Created AudioBridge");
        Selection.activeGameObject = go;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LEGACY MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    [MenuItem("H3M/VFX Pipeline Master/Legacy Management/Mark All Legacy (Disable)", false, 200)]
    public static int DisableLegacyComponents()
    {
        int count = DisableLegacyComponentsInternal();
        EditorUtility.DisplayDialog("Legacy Components Disabled",
            $"Disabled {count} legacy components.\n\n" +
            $"Components disabled:\n" +
            $"- VFXBinderManager\n" +
            $"- VFXARDataBinder\n" +
            $"- PeopleOcclusionVFXManager\n" +
            $"- OptimizedARVFXBridge",
            "OK");
        return count;
    }

    static int DisableLegacyComponentsInternal()
    {
        int disabled = 0;

        // Disable VFXBinderManager
        var managers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(m => m.GetType().Name == "VFXBinderManager");
        foreach (var m in managers)
        {
            if (m.enabled)
            {
                Undo.RecordObject(m, "Disable Legacy");
                m.enabled = false;
                disabled++;
            }
        }

        // Disable VFXARDataBinder
        var dataBinders = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(m => m.GetType().Name == "VFXARDataBinder");
        foreach (var b in dataBinders)
        {
            if (b.enabled)
            {
                Undo.RecordObject(b, "Disable Legacy");
                b.enabled = false;
                disabled++;
            }
        }

        // Disable PeopleOcclusionVFXManager
        var peopleManagers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(m => m.GetType().Name == "PeopleOcclusionVFXManager");
        foreach (var p in peopleManagers)
        {
            if (p.enabled)
            {
                Undo.RecordObject(p, "Disable Legacy");
                p.enabled = false;
                disabled++;
            }
        }

        // Disable OptimizedARVFXBridge
        var bridges = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(m => m.GetType().Name == "OptimizedARVFXBridge");
        foreach (var b in bridges)
        {
            if (b.enabled)
            {
                Undo.RecordObject(b, "Disable Legacy");
                b.enabled = false;
                disabled++;
            }
        }

        if (disabled > 0)
            Debug.Log($"[VFXPipelineMasterSetup] Disabled {disabled} legacy components");

        return disabled;
    }

    [MenuItem("H3M/VFX Pipeline Master/Legacy Management/Restore Legacy (Re-enable)", false, 201)]
    public static void RestoreLegacyComponents()
    {
        int enabled = 0;

        var legacyTypes = new[] { "VFXBinderManager", "VFXARDataBinder", "PeopleOcclusionVFXManager", "OptimizedARVFXBridge" };

        foreach (var typeName in legacyTypes)
        {
            var components = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(m => m.GetType().Name == typeName && !m.enabled);

            foreach (var c in components)
            {
                Undo.RecordObject(c, "Restore Legacy");
                c.enabled = true;
                enabled++;
            }
        }

        Debug.Log($"[VFXPipelineMasterSetup] Re-enabled {enabled} legacy components");
        EditorUtility.DisplayDialog("Legacy Components Restored",
            $"Re-enabled {enabled} legacy components.",
            "OK");
    }

    [MenuItem("H3M/VFX Pipeline Master/Legacy Management/Delete All Legacy Components", false, 202)]
    public static void DeleteLegacyComponents()
    {
        if (!EditorUtility.DisplayDialog("Delete Legacy Components",
            "This will DELETE all legacy pipeline components:\n\n" +
            "- VFXBinderManager\n" +
            "- VFXARDataBinder\n" +
            "- PeopleOcclusionVFXManager\n" +
            "- OptimizedARVFXBridge\n\n" +
            "This cannot be undone. Continue?",
            "Delete", "Cancel"))
        {
            return;
        }

        int deleted = 0;
        var legacyTypes = new[] { "VFXBinderManager", "VFXARDataBinder", "PeopleOcclusionVFXManager", "OptimizedARVFXBridge" };

        foreach (var typeName in legacyTypes)
        {
            var components = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(m => m.GetType().Name == typeName)
                .ToArray();

            foreach (var c in components)
            {
                Undo.DestroyObjectImmediate(c);
                deleted++;
            }
        }

        Debug.Log($"[VFXPipelineMasterSetup] Deleted {deleted} legacy components");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TESTING TOOLS
    // ═══════════════════════════════════════════════════════════════════════

    [MenuItem("H3M/VFX Pipeline Master/Testing/Add Pipeline Dashboard", false, 300)]
    public static void AddPipelineDashboard()
    {
        var existing = Object.FindFirstObjectByType<VFXPipelineDashboard>();
        if (existing != null)
        {
            Debug.Log("[VFXPipelineMasterSetup] Dashboard already exists");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var go = new GameObject("VFXPipelineDashboard");
        go.AddComponent<VFXPipelineDashboard>();
        Undo.RegisterCreatedObjectUndo(go, "Create Dashboard");

        Debug.Log("[VFXPipelineMasterSetup] Created VFXPipelineDashboard (press Tab to toggle)");
        Selection.activeGameObject = go;
    }

    [MenuItem("H3M/VFX Pipeline Master/Testing/Add Test Harness", false, 301)]
    public static void AddTestHarness()
    {
        var existing = Object.FindFirstObjectByType<VFXTestHarness>();
        if (existing != null)
        {
            Debug.Log("[VFXPipelineMasterSetup] TestHarness already exists");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var go = new GameObject("VFXTestHarness");
        go.AddComponent<VFXTestHarness>();
        Undo.RegisterCreatedObjectUndo(go, "Create TestHarness");

        Debug.Log("[VFXPipelineMasterSetup] Created VFXTestHarness (1-9/Space/C/A shortcuts)");
        Selection.activeGameObject = go;
    }

    [MenuItem("H3M/VFX Pipeline Master/Testing/Validate All Bindings", false, 302)]
    public static void ValidateAllBindings()
    {
        var source = Object.FindFirstObjectByType<ARDepthSource>();
        var binders = Object.FindObjectsByType<VFXARBinder>(FindObjectsSortMode.None);
        var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

        var issues = new List<string>();

        // Check source
        if (source == null)
            issues.Add("ARDepthSource: MISSING");
        else
            issues.Add($"ARDepthSource: OK");

        // Check binders
        int vfxWithBinder = 0;
        int vfxWithoutBinder = 0;
        foreach (var vfx in allVFX)
        {
            if (vfx.GetComponent<VFXARBinder>() != null)
                vfxWithBinder++;
            else
                vfxWithoutBinder++;
        }

        issues.Add($"VFX with VFXARBinder: {vfxWithBinder}");
        if (vfxWithoutBinder > 0)
            issues.Add($"VFX without VFXARBinder: {vfxWithoutBinder} (WARNING)");

        // Check legacy
        var legacyCount = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Count(m => m.enabled && (
                m.GetType().Name == "VFXBinderManager" ||
                m.GetType().Name == "VFXARDataBinder" ||
                m.GetType().Name == "PeopleOcclusionVFXManager"));

        if (legacyCount > 0)
            issues.Add($"Active legacy components: {legacyCount} (WARNING - consider disabling)");

        // Check debug tools
        var dashboard = Object.FindFirstObjectByType<VFXPipelineDashboard>();
        var harness = Object.FindFirstObjectByType<VFXTestHarness>();
        issues.Add($"Dashboard: {(dashboard != null ? "OK" : "Missing")}");
        issues.Add($"TestHarness: {(harness != null ? "OK" : "Missing")}");

        EditorUtility.DisplayDialog("VFX Pipeline Validation",
            string.Join("\n", issues),
            "OK");

        Debug.Log("[VFXPipelineMasterSetup] Validation:\n" + string.Join("\n", issues));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // VFX LIBRARY
    // ═══════════════════════════════════════════════════════════════════════

    [MenuItem("H3M/VFX Pipeline Master/VFX Library/Create VFXLibraryManager", false, 400)]
    public static void CreateVFXLibraryManager()
    {
        var existing = Object.FindFirstObjectByType<VFXLibraryManager>();
        if (existing != null)
        {
            Debug.Log($"[VFXPipelineMasterSetup] VFXLibraryManager already exists on {existing.gameObject.name}");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var go = new GameObject("VFXLibraryManager");
        var manager = go.AddComponent<VFXLibraryManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create VFXLibraryManager");

        Debug.Log("[VFXPipelineMasterSetup] Created VFXLibraryManager - use context menu to populate");
        Selection.activeGameObject = go;
    }

    [MenuItem("H3M/VFX Pipeline Master/VFX Library/Setup VFXLibraryManager Pipeline", false, 401)]
    public static void SetupVFXLibraryPipeline()
    {
        var manager = Object.FindFirstObjectByType<VFXLibraryManager>();
        if (manager == null)
        {
            // Create new
            var go = new GameObject("VFXLibraryManager");
            manager = go.AddComponent<VFXLibraryManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create VFXLibraryManager");
        }

        manager.SetupCompletePipeline();
        Selection.activeGameObject = manager.gameObject;

        EditorUtility.DisplayDialog("VFX Library Pipeline Setup",
            $"VFXLibraryManager pipeline setup complete.\n\n" +
            $"Total VFX: {manager.TotalCount}\n" +
            $"Pipeline Ready: {manager.IsPipelineReady}\n\n" +
            "All VFX now have VFXARBinder with auto-detected bindings.",
            "OK");
    }

    [MenuItem("H3M/VFX Pipeline Master/VFX Library/Populate from Resources", false, 402)]
    public static void PopulateVFXFromResources()
    {
        var manager = Object.FindFirstObjectByType<VFXLibraryManager>();
        if (manager == null)
        {
            Debug.LogWarning("[VFXPipelineMasterSetup] VFXLibraryManager not found. Create one first.");
            return;
        }

        if (Application.isPlaying)
        {
            manager.PopulateLibraryRuntime();
        }
        else
        {
            // Call editor populate via reflection or direct method
            var method = manager.GetType().GetMethod("PopulateLibraryEditor",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            method?.Invoke(manager, null);
        }

        Debug.Log($"[VFXPipelineMasterSetup] Populated VFX library with {manager.TotalCount} VFX");
    }

    [MenuItem("H3M/VFX Pipeline Master/VFX Library/List All VFX in Scene", false, 410)]
    public static void ListAllVFXInScene()
    {
        var allVFX = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

        var byCategory = new Dictionary<string, List<string>>();
        foreach (var vfx in allVFX)
        {
            string category = InferCategory(vfx.name);
            if (!byCategory.ContainsKey(category))
                byCategory[category] = new List<string>();

            var binder = vfx.GetComponent<VFXARBinder>();
            string status = binder != null ? "bound" : "no-binder";
            byCategory[category].Add($"{vfx.name} [{status}]");
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"VFX in Scene: {allVFX.Length} total\n");

        foreach (var cat in byCategory.OrderBy(kv => kv.Key))
        {
            sb.AppendLine($"[{cat.Key}] ({cat.Value.Count}):");
            foreach (var name in cat.Value.OrderBy(n => n))
                sb.AppendLine($"  - {name}");
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("VFX in Scene", sb.ToString(), "OK");
    }

    static string InferCategory(string vfxName)
    {
        string lower = vfxName.ToLower();

        if (lower.Contains("people") || lower.Contains("human") || lower.Contains("body"))
            return "People";
        if (lower.Contains("hand") || lower.Contains("joint") || lower.Contains("keypoint"))
            return "Hands";
        if (lower.Contains("audio") || lower.Contains("sound") || lower.Contains("wave"))
            return "Audio";
        if (lower.Contains("env") || lower.Contains("mesh") || lower.Contains("world"))
            return "Environment";
        if (lower.Contains("face"))
            return "Face";
        if (lower.Contains("rcam")) return "Rcam";
        if (lower.Contains("metavido")) return "Metavido";
        if (lower.Contains("nncam")) return "NNCam";

        return "Other";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PREFAB
    // ═══════════════════════════════════════════════════════════════════════

    [MenuItem("H3M/VFX Pipeline Master/Create Master Prefab", false, 500)]
    public static void CreateMasterPrefab()
    {
        // Create root
        var root = new GameObject("VFXPipelineMaster");
        Undo.RegisterCreatedObjectUndo(root, "Create VFXPipelineMaster");

        // Add ARDepthSource
        var sourceGO = new GameObject("ARDepthSource");
        sourceGO.transform.SetParent(root.transform);
        sourceGO.AddComponent<ARDepthSource>();

        // Add AudioBridge
        var audioGO = new GameObject("AudioBridge");
        audioGO.transform.SetParent(root.transform);
        audioGO.AddComponent<AudioBridge>();

        // Add Dashboard
        var dashGO = new GameObject("PipelineDashboard");
        dashGO.transform.SetParent(root.transform);
        dashGO.AddComponent<VFXPipelineDashboard>();

        // Add TestHarness
        var harnessGO = new GameObject("TestHarness");
        harnessGO.transform.SetParent(root.transform);
        harnessGO.AddComponent<VFXTestHarness>();

        // Save as prefab
        string path = "Assets/H3M/Prefabs";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets/H3M", "Prefabs");
        }

        string prefabPath = $"{path}/VFXPipelineMaster.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        Debug.Log($"[VFXPipelineMasterSetup] Created prefab at {prefabPath}");
        EditorUtility.DisplayDialog("Prefab Created",
            $"VFXPipelineMaster prefab created at:\n{prefabPath}\n\n" +
            $"Drag into any scene for instant pipeline setup.",
            "OK");

        Selection.activeGameObject = root;
    }
}
