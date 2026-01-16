using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.VFX;
using System.Linq;
using Metavido;


public class ARKitMetavidoSceneSetup
{
    [MenuItem("Metavido/Setup ARKit Scene")]
    public static void SetupScene()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ARKitMetavido";

        // 1. AR Session
        GameObject sessionGO = new GameObject("AR Session");
        sessionGO.AddComponent<ARSession>();
        sessionGO.AddComponent<ARInputManager>();

        // 2. XR Origin (AR Session Origin)
        GameObject xrOriginGO = new GameObject("XR Origin");
        var xrOrigin = xrOriginGO.AddComponent<Unity.XR.CoreUtils.XROrigin>();

        // Camera Offset
        GameObject cameraOffsetGO = new GameObject("Camera Offset");
        cameraOffsetGO.transform.SetParent(xrOriginGO.transform, false);
        xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;

        // Main Camera
        GameObject cameraGO = new GameObject("Main Camera");
        cameraGO.transform.SetParent(cameraOffsetGO.transform, false);
        cameraGO.tag = "MainCamera";
        var camera = cameraGO.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20f;

        xrOrigin.Camera = camera;

        // AR Components on Camera
        cameraGO.AddComponent<ARCameraManager>();
        cameraGO.AddComponent<ARCameraBackground>();
        var occlusionManager = cameraGO.AddComponent<AROcclusionManager>();
        occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Best;
        occlusionManager.requestedHumanDepthMode = HumanSegmentationDepthMode.Best;
        occlusionManager.requestedOcclusionPreferenceMode = OcclusionPreferenceMode.NoOcclusion;

        // Texture Provider
        var textureProvider = cameraGO.AddComponent<ARCameraTextureProvider>();

        // Optimized AR → VFX bridge (optional compute shader if found)
        var bridge = cameraGO.AddComponent<OptimizedARVFXBridge>();
        var bridgeSO = new SerializedObject(bridge);
        bridgeSO.FindProperty("adaptiveResolution").boolValue = true;
        bridgeSO.FindProperty("targetFPS").intValue = 60;
        bridgeSO.FindProperty("baseResolution").vector2IntValue = new Vector2Int(512, 512);

        // 3. Metavido VFX
        GameObject vfxGO = new GameObject("Metavido VFX");
        var vfx = vfxGO.AddComponent<VisualEffect>();

        // Load VFX Asset (Assuming it's at a standard path or we find it)
        string[] vfxGuids = AssetDatabase.FindAssets("t:VisualEffectAsset Particles");
        if (vfxGuids.Length > 0)
        {
            string vfxPath = AssetDatabase.GUIDToAssetPath(vfxGuids[0]);
            vfx.visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
        }
        else
        {
            Debug.LogWarning("Particles.vfx not found. Please assign it manually.");
        }

        // 4. Binder & Controller
        var binder = vfxGO.AddComponent<ARKitMetavidoBinder>();
        // We need to assign private fields via SerializedObject since they are [SerializeField] private
        SerializedObject binderSO = new SerializedObject(binder);
        binderSO.FindProperty("_occlusionManager").objectReferenceValue = occlusionManager;
        binderSO.FindProperty("_textureProvider").objectReferenceValue = textureProvider;
        binderSO.FindProperty("_camera").objectReferenceValue = camera;
        binderSO.ApplyModifiedProperties();

        var controller = vfxGO.AddComponent<ARKitMetavidoController>();
        // Controller auto-finds references in Start(), so no need to assign manually if in same scene

        // Link bridge to VFX and optional compute shader (DepthToWorld.compute if present)
        bridgeSO.FindProperty("vfx").objectReferenceValue = vfx;
        string[] computeGuids = AssetDatabase.FindAssets("DepthToWorld t:ComputeShader");
        if (computeGuids.Length > 0)
        {
            string computePath = AssetDatabase.GUIDToAssetPath(computeGuids[0]);
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(computePath);
            bridgeSO.FindProperty("depthProcessor").objectReferenceValue = cs;
        }
        bridgeSO.ApplyModifiedProperties();


        // Try to find BodyPix model - DISABLED for MVP
        /*
        string[] modelGuids = AssetDatabase.FindAssets("t:ModelAsset bodypix"); // Heuristic
        if (modelGuids.Length > 0)
        {
            var model = AssetDatabase.LoadAssetAtPath<ModelAsset>(AssetDatabase.GUIDToAssetPath(modelGuids[0]));
            SerializedObject trackerSO = new SerializedObject(bodyTracker);
            trackerSO.FindProperty("_bodyPixModel").objectReferenceValue = model;
            trackerSO.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("BodyPix model not found. Please assign it to BodyTracker on Main Camera.");
        }
        */

        // Save Scene
        string scenePath = "Assets/Scenes/ARKitMetavido.unity";
        if (!System.IO.Directory.Exists("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"ARKit Metavido Scene created at {scenePath}");
    }
}
