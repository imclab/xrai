using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;
using UnityEditor;

/// <summary>
/// Editor utilities for setting up the unified hologram system
/// </summary>
public static class HologramSetup
{
    const string HOLOGRAM_VFX_PATH = "Assets/H3M/VFX/hologram_depth_people_metavido.vfx";

    [MenuItem("H3M/Hologram/Create Complete Hologram (with Placer)", false, 99)]
    static void CreateCompleteHologram()
    {
        // Create parent
        var rig = new GameObject("Hologram");
        Undo.RegisterCreatedObjectUndo(rig, "Create Complete Hologram");

        // Add placer (handles placement + manipulation)
        var placer = rig.AddComponent<HologramPlacer>();

        // Create VFX child
        var vfxGO = new GameObject("HologramVFX");
        vfxGO.transform.SetParent(rig.transform);
        vfxGO.transform.localPosition = Vector3.zero;
        vfxGO.transform.localScale = Vector3.one * 0.15f;

        // Add VisualEffect
        var vfx = vfxGO.AddComponent<VisualEffect>();

        // Try to load hologram VFX asset
        var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(HOLOGRAM_VFX_PATH);
        if (vfxAsset == null)
            vfxAsset = Resources.Load<VisualEffectAsset>("VFX/People/voxels_depth_people_metavido");
        if (vfxAsset != null)
            vfx.visualEffectAsset = vfxAsset;

        // Add VFXARBinder
        var binder = vfxGO.AddComponent<VFXARBinder>();
        binder.AutoDetectBindings();

        // Add HologramController
        var controller = rig.AddComponent<HologramController>();

        // Wire up placer
        var placerSO = new SerializedObject(placer);
        placerSO.FindProperty("_target").objectReferenceValue = vfxGO.transform;

        // Find AR components in scene
        var raycastMgr = Object.FindAnyObjectByType<ARRaycastManager>();
        var planeMgr = Object.FindAnyObjectByType<ARPlaneManager>();
        if (raycastMgr != null)
            placerSO.FindProperty("_raycastManager").objectReferenceValue = raycastMgr;
        if (planeMgr != null)
            placerSO.FindProperty("_planeManager").objectReferenceValue = planeMgr;
        placerSO.ApplyModifiedProperties();

        // Wire up controller
        var controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("_anchor").objectReferenceValue = vfxGO.transform;
        controllerSO.FindProperty("_vfx").objectReferenceValue = vfx;
        controllerSO.FindProperty("_binder").objectReferenceValue = binder;
        controllerSO.ApplyModifiedProperties();

        Selection.activeGameObject = rig;

        Debug.Log("[HologramSetup] Created complete hologram with:\n" +
                  "  - HologramPlacer (tap to place, drag, pinch to scale)\n" +
                  "  - HologramController (Live AR / Metavido modes)\n" +
                  "  - HologramVFX (VisualEffect + VFXARBinder)\n" +
                  "  - Reticle (procedural, shows placement point)");
    }

    [MenuItem("H3M/Hologram/Create Hologram Rig (Clean)", false, 100)]
    static void CreateHologramRig()
    {
        // Create parent
        var rig = new GameObject("Hologram");
        Undo.RegisterCreatedObjectUndo(rig, "Create Hologram Rig");

        // Create anchor child
        var anchor = new GameObject("Anchor");
        anchor.transform.SetParent(rig.transform);
        anchor.transform.localPosition = new Vector3(0, 0, 1); // 1m in front

        // Create VFX child
        var vfxGO = new GameObject("HologramVFX");
        vfxGO.transform.SetParent(rig.transform);
        vfxGO.transform.localPosition = Vector3.zero;
        vfxGO.transform.localScale = Vector3.one * 0.15f; // Mini-me scale

        // Add VisualEffect
        var vfx = vfxGO.AddComponent<VisualEffect>();

        // Try to load default hologram VFX asset
        var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(HOLOGRAM_VFX_PATH);
        if (vfxAsset != null)
        {
            vfx.visualEffectAsset = vfxAsset;
        }
        else
        {
            // Try Resources
            vfxAsset = Resources.Load<VisualEffectAsset>("VFX/People/voxels_depth_people_metavido");
            if (vfxAsset != null)
                vfx.visualEffectAsset = vfxAsset;
        }

        // Add VFXARBinder
        var binder = vfxGO.AddComponent<VFXARBinder>();
        binder.AutoDetectBindings();

        // Add HologramController to parent
        var controller = rig.AddComponent<HologramController>();

        // Wire up references via SerializedObject
        var so = new SerializedObject(controller);
        so.FindProperty("_anchor").objectReferenceValue = anchor.transform;
        so.FindProperty("_vfx").objectReferenceValue = vfx;
        so.FindProperty("_binder").objectReferenceValue = binder;
        so.FindProperty("_scale").floatValue = 0.15f;
        so.ApplyModifiedProperties();

        // Select the new rig
        Selection.activeGameObject = rig;

        Debug.Log("[HologramSetup] Created clean Hologram rig with:\n" +
                  "  - HologramController (mode switching)\n" +
                  "  - Anchor child (placement point)\n" +
                  "  - HologramVFX child (VisualEffect + VFXARBinder)");
    }

    [MenuItem("H3M/Hologram/Add Hologram Controller to Selected", false, 101)]
    static void AddControllerToSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a GameObject first.", "OK");
            return;
        }

        var controller = go.GetComponent<HologramController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<HologramController>(go);
        }

        // Auto-wire
        var vfx = go.GetComponentInChildren<VisualEffect>();
        var binder = go.GetComponentInChildren<VFXARBinder>();

        var so = new SerializedObject(controller);
        if (vfx != null) so.FindProperty("_vfx").objectReferenceValue = vfx;
        if (binder != null) so.FindProperty("_binder").objectReferenceValue = binder;
        so.ApplyModifiedProperties();

        Debug.Log($"[HologramSetup] Added HologramController to {go.name}");
    }

    [MenuItem("H3M/Hologram/Configure Selected for Live AR", false, 110)]
    static void ConfigureForLiveAR()
    {
        var go = Selection.activeGameObject;
        if (go == null) return;

        var controller = go.GetComponent<HologramController>();
        if (controller != null)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("_mode").enumValueIndex = 0; // LiveAR
            so.ApplyModifiedProperties();
        }

        var binder = go.GetComponentInChildren<VFXARBinder>();
        if (binder != null)
        {
            binder.enabled = true;
            binder.AutoDetectBindings();
            EditorUtility.SetDirty(binder);
        }

        Debug.Log("[HologramSetup] Configured for Live AR mode");
    }

    [MenuItem("H3M/Hologram/Configure Selected for Metavido", false, 111)]
    static void ConfigureForMetavido()
    {
        var go = Selection.activeGameObject;
        if (go == null) return;

        var controller = go.GetComponent<HologramController>();
        if (controller != null)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("_mode").enumValueIndex = 1; // MetavidoVideo
            so.ApplyModifiedProperties();

            // Add Metavido components if missing
            if (go.GetComponent<UnityEngine.Video.VideoPlayer>() == null)
                Undo.AddComponent<UnityEngine.Video.VideoPlayer>(go);

            // Note: TextureDemuxer and MetadataDecoder need to be added manually
            // as they're in the Metavido.Decoder namespace
        }

        Debug.Log("[HologramSetup] Configured for Metavido playback mode");
    }

    [MenuItem("H3M/Hologram/Save Selected as Prefab", false, 115)]
    static void SaveAsPrefab()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select the Hologram GameObject first.", "OK");
            return;
        }

        // Ensure prefab folder exists
        string folderPath = "Assets/Prefabs/Hologram";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Hologram");
        }

        string prefabPath = $"{folderPath}/{go.name}.prefab";

        // Check if prefab already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Overwrite?",
                $"Prefab '{go.name}' already exists. Overwrite?", "Yes", "No"))
                return;
        }

        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        if (prefab != null)
        {
            Debug.Log($"[HologramSetup] Saved prefab: {prefabPath}");
            EditorGUIUtility.PingObject(prefab);
        }
    }

    [MenuItem("H3M/Hologram/Verify Hologram Setup", false, 120)]
    static void VerifySetup()
    {
        var controllers = Object.FindObjectsByType<HologramController>(FindObjectsSortMode.None);
        var arDepthSource = Object.FindAnyObjectByType<ARDepthSource>();

        Debug.Log("=== Hologram Setup Verification ===");
        Debug.Log($"ARDepthSource: {(arDepthSource != null ? "Found" : "MISSING")}");
        Debug.Log($"HologramControllers: {controllers.Length}");

        foreach (var c in controllers)
        {
            var vfx = c.VFX;
            var binder = vfx?.GetComponent<VFXARBinder>();

            Debug.Log($"\n[{c.gameObject.name}]");
            Debug.Log($"  Mode: {c.Mode}");
            Debug.Log($"  Scale: {c.Scale}");
            Debug.Log($"  Anchor: {(c.Anchor != null ? c.Anchor.name : "null")}");
            Debug.Log($"  VFX: {(vfx != null ? vfx.name : "MISSING")}");
            Debug.Log($"  VFXARBinder: {(binder != null ? "OK" : "MISSING")}");
            Debug.Log($"  IsPlaying: {c.IsPlaying}");
        }
    }
}
