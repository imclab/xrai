using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;
using UnityEditor;
using System.Linq;
using Metavido.Decoder;
using Metavido.Encoder;

/// <summary>
/// Editor utilities for setting up the unified hologram system.
/// Includes recording (FrameEncoder) and playback (TextureDemuxer/MetadataDecoder).
/// </summary>
public static class HologramSetup
{
    const string HOLOGRAM_VFX_PATH = "Assets/H3M/VFX/hologram_depth_people_metavido.vfx";
    const string METAVIDO_DEFINE = "METAVIDO_HAS_ARFOUNDATION";

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

    #region Metavido Defines

    [MenuItem("H3M/Metavido/Setup Metavido Defines", false, 200)]
    static void SetupMetavidoDefines()
    {
        var target = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

        if (!defines.Contains(METAVIDO_DEFINE))
        {
            defines = string.IsNullOrEmpty(defines) ? METAVIDO_DEFINE : defines + ";" + METAVIDO_DEFINE;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
            Debug.Log($"[HologramSetup] Added {METAVIDO_DEFINE} scripting define. Unity will recompile.");
        }
        else
        {
            Debug.Log($"[HologramSetup] {METAVIDO_DEFINE} already defined.");
        }
    }

    [MenuItem("H3M/Metavido/Verify Metavido Setup", false, 201)]
    static void VerifyMetavidoSetup()
    {
        var target = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        bool hasDefine = defines.Contains(METAVIDO_DEFINE);

        var encoder = Object.FindAnyObjectByType<FrameEncoder>();
        var decoder = Object.FindAnyObjectByType<MetadataDecoder>();
        var demuxer = Object.FindAnyObjectByType<TextureDemuxer>();
        var xrData = Object.FindAnyObjectByType<XRDataProvider>();

        Debug.Log("=== Metavido Setup Verification ===");
        Debug.Log($"METAVIDO_HAS_ARFOUNDATION: {(hasDefine ? "✓ Defined" : "✗ MISSING - Run Setup Metavido Defines")}");
        Debug.Log($"XRDataProvider: {(xrData != null ? $"✓ {xrData.gameObject.name}" : "✗ Missing")}");
        Debug.Log($"FrameEncoder: {(encoder != null ? $"✓ {encoder.gameObject.name}" : "✗ Missing")}");
        Debug.Log($"MetadataDecoder: {(decoder != null ? $"✓ {decoder.gameObject.name}" : "✗ Missing")}");
        Debug.Log($"TextureDemuxer: {(demuxer != null ? $"✓ {demuxer.gameObject.name}" : "✗ Missing")}");
    }

    #endregion

    #region Recording Setup

    [MenuItem("H3M/Metavido/Setup Recording (FrameEncoder)", false, 210)]
    static void SetupRecording()
    {
        // Ensure define is set
        SetupMetavidoDefines();

        // Find AR Camera
        var camMgr = Object.FindAnyObjectByType<ARCameraManager>();
        var occMgr = Object.FindAnyObjectByType<AROcclusionManager>();

        if (camMgr == null || occMgr == null)
        {
            EditorUtility.DisplayDialog("AR Components Missing",
                "ARCameraManager and AROcclusionManager required for Metavido recording.", "OK");
            return;
        }

        // Create recorder GameObject
        var recorderGO = new GameObject("MetavidoRecorder");
        Undo.RegisterCreatedObjectUndo(recorderGO, "Create Metavido Recorder");

        // Add XRDataProvider
        var xrData = recorderGO.AddComponent<XRDataProvider>();
        var xrDataSO = new SerializedObject(xrData);
        xrDataSO.FindProperty("_cameraManager").objectReferenceValue = camMgr;
        xrDataSO.FindProperty("_occlusionManager").objectReferenceValue = occMgr;
        xrDataSO.ApplyModifiedProperties();

        // Add FrameEncoder
        var encoder = recorderGO.AddComponent<FrameEncoder>();
        var encoderSO = new SerializedObject(encoder);
        encoderSO.FindProperty("_xrSource").objectReferenceValue = xrData;
        encoderSO.ApplyModifiedProperties();

        Selection.activeGameObject = recorderGO;
        Debug.Log("[HologramSetup] Created MetavidoRecorder with XRDataProvider + FrameEncoder.\n" +
                  "Next: Add recording UI to start/stop and save to Camera Roll.");
    }

    #endregion

    #region Playback Setup

    [MenuItem("H3M/Metavido/Setup Playback on Selected", false, 220)]
    static void SetupPlaybackOnSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("No Selection",
                "Select the Hologram GameObject with HologramController.", "OK");
            return;
        }

        var controller = go.GetComponent<HologramController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<HologramController>(go);
        }

        // Add VideoPlayer
        var videoPlayer = go.GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = Undo.AddComponent<VideoPlayer>(go);
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.APIOnly;
        }

        // Add MetadataDecoder
        var decoder = go.GetComponent<MetadataDecoder>();
        if (decoder == null)
        {
            decoder = Undo.AddComponent<MetadataDecoder>(go);
        }

        // Add TextureDemuxer
        var demuxer = go.GetComponent<TextureDemuxer>();
        if (demuxer == null)
        {
            demuxer = Undo.AddComponent<TextureDemuxer>(go);
        }

        // Wire up HologramController
        var so = new SerializedObject(controller);
        so.FindProperty("_videoPlayer").objectReferenceValue = videoPlayer;
        so.FindProperty("_metadataDecoder").objectReferenceValue = decoder;
        so.FindProperty("_demuxer").objectReferenceValue = demuxer;
        so.FindProperty("_mode").enumValueIndex = 1; // MetavidoVideo
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);

        Debug.Log($"[HologramSetup] Added playback components to {go.name}:\n" +
                  "  - VideoPlayer (renderMode=APIOnly)\n" +
                  "  - MetadataDecoder\n" +
                  "  - TextureDemuxer\n" +
                  "Use HologramController.PlayVideo(path) to start playback.");
    }

    [MenuItem("H3M/Metavido/Create Playback Hologram", false, 221)]
    static void CreatePlaybackHologram()
    {
        // Create rig
        var rig = new GameObject("PlaybackHologram");
        Undo.RegisterCreatedObjectUndo(rig, "Create Playback Hologram");

        // Create VFX child
        var vfxGO = new GameObject("HologramVFX");
        vfxGO.transform.SetParent(rig.transform);
        vfxGO.transform.localScale = Vector3.one * 0.15f;

        // Add VisualEffect
        var vfx = vfxGO.AddComponent<VisualEffect>();
        var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(HOLOGRAM_VFX_PATH);
        if (vfxAsset == null)
            vfxAsset = Resources.Load<VisualEffectAsset>("VFX/People/voxels_depth_people_metavido");
        if (vfxAsset != null)
            vfx.visualEffectAsset = vfxAsset;

        // Add VFXARBinder (disabled - we'll use manual binding in HologramController)
        var binder = vfxGO.AddComponent<VFXARBinder>();
        binder.enabled = false;

        // Add HologramController
        var controller = rig.AddComponent<HologramController>();

        // Add playback components
        var videoPlayer = rig.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;

        var decoder = rig.AddComponent<MetadataDecoder>();
        var demuxer = rig.AddComponent<TextureDemuxer>();

        // Wire up
        var so = new SerializedObject(controller);
        so.FindProperty("_anchor").objectReferenceValue = vfxGO.transform;
        so.FindProperty("_vfx").objectReferenceValue = vfx;
        so.FindProperty("_binder").objectReferenceValue = binder;
        so.FindProperty("_videoPlayer").objectReferenceValue = videoPlayer;
        so.FindProperty("_metadataDecoder").objectReferenceValue = decoder;
        so.FindProperty("_demuxer").objectReferenceValue = demuxer;
        so.FindProperty("_mode").enumValueIndex = 1; // MetavidoVideo
        so.ApplyModifiedProperties();

        // Add HologramAnchor for placement
        var anchor = rig.AddComponent<H3M.Core.HologramAnchor>();
        var anchorSO = new SerializedObject(anchor);
        anchorSO.FindProperty("_hologramRoot").objectReferenceValue = vfxGO.transform;
        anchorSO.ApplyModifiedProperties();

        Selection.activeGameObject = rig;
        Debug.Log("[HologramSetup] Created PlaybackHologram with:\n" +
                  "  - HologramController (Metavido mode)\n" +
                  "  - VideoPlayer + MetadataDecoder + TextureDemuxer\n" +
                  "  - HologramAnchor (tap to place, pinch to scale)\n" +
                  "  - HologramVFX\n" +
                  "Drag a .metavido video to VideoPlayer or use PlayVideo(path).");
    }

    #endregion
}
