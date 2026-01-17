using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;
using UnityEditor;

/// <summary>
/// Auto-creates Hologram prefab on Unity load if it doesn't exist
/// </summary>
[InitializeOnLoad]
public static class HologramAutoSetup
{
    const string PREFAB_PATH = "Assets/Prefabs/Hologram/Hologram.prefab";
    const string HOLOGRAM_VFX_PATH = "Assets/H3M/VFX/hologram_depth_people_metavido.vfx";

    static HologramAutoSetup()
    {
        // Delay execution to after Unity finishes loading
        EditorApplication.delayCall += CheckAndCreatePrefab;
    }

    static void CheckAndCreatePrefab()
    {
        // Only run once
        EditorApplication.delayCall -= CheckAndCreatePrefab;

        // Check if prefab already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
        {
            Debug.Log("[HologramAutoSetup] Hologram prefab already exists");
            return;
        }

        // Create prefab
        CreateHologramPrefab();
    }

    [MenuItem("H3M/Hologram/Force Create Prefab", false, 116)]
    static void ForceCreatePrefab()
    {
        CreateHologramPrefab();
    }

    static void CreateHologramPrefab()
    {
        Debug.Log("[HologramAutoSetup] Creating Hologram prefab...");

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Hologram"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Hologram");

        // Create root
        var root = new GameObject("Hologram");

        // Add HologramPlacer
        var placer = root.AddComponent<HologramPlacer>();

        // Add HologramController
        var controller = root.AddComponent<HologramController>();

        // Create VFX child
        var vfxGO = new GameObject("HologramVFX");
        vfxGO.transform.SetParent(root.transform);
        vfxGO.transform.localPosition = Vector3.zero;
        vfxGO.transform.localScale = Vector3.one * 0.15f;

        // Add VisualEffect
        var vfx = vfxGO.AddComponent<VisualEffect>();

        // Try to load VFX asset
        var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(HOLOGRAM_VFX_PATH);
        string[] guids;
        if (vfxAsset == null)
        {
            // Try alternative paths
            guids = AssetDatabase.FindAssets("t:VisualEffectAsset hologram");
            if (guids.Length > 0)
                vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        if (vfxAsset == null)
        {
            // Fall back to voxels
            guids = AssetDatabase.FindAssets("t:VisualEffectAsset voxels_depth_people");
            if (guids.Length > 0)
                vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        if (vfxAsset != null)
            vfx.visualEffectAsset = vfxAsset;

        // Add VFXARBinder
        var binder = vfxGO.AddComponent<VFXARBinder>();

        // Wire up references using SerializedObject
        var placerSO = new SerializedObject(placer);
        placerSO.FindProperty("_target").objectReferenceValue = vfxGO.transform;
        placerSO.ApplyModifiedPropertiesWithoutUndo();

        var controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("_anchor").objectReferenceValue = vfxGO.transform;
        controllerSO.FindProperty("_vfx").objectReferenceValue = vfx;
        controllerSO.FindProperty("_binder").objectReferenceValue = binder;
        controllerSO.ApplyModifiedPropertiesWithoutUndo();

        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);

        // Cleanup temp object
        Object.DestroyImmediate(root);

        if (prefab != null)
        {
            Debug.Log($"[HologramAutoSetup] Created prefab: {PREFAB_PATH}");
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;
        }
        else
        {
            Debug.LogError("[HologramAutoSetup] Failed to create prefab");
        }
    }
}
