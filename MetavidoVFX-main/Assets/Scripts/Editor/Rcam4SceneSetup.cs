using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using Rcam4;
using Metavido;

public class Rcam4SceneSetup
{
    [MenuItem("Metavido/Setup Rcam4 Scene")]
    public static void SetupScene()
    {
        // 0. Create New Scene
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);

        // 1. Create/Find AR Session
        var sessionGo = GameObject.Find("AR Session");
        if (sessionGo == null)
        {
            sessionGo = new GameObject("AR Session");
            sessionGo.AddComponent<ARSession>();
            sessionGo.AddComponent<ARInputManager>();
        }

        // 2. Create/Find XR Origin
        var originGo = GameObject.Find("XR Origin");
        if (originGo == null)
        {
            originGo = new GameObject("XR Origin");
            originGo.AddComponent<Unity.XR.CoreUtils.XROrigin>();
        }

        // 3. Setup Camera
        var cameraGo = GameObject.Find("Main Camera");
        if (cameraGo == null)
        {
            cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(originGo.transform);
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
        }

        // AR Components on Camera
        if (!cameraGo.GetComponent<ARCameraManager>()) cameraGo.AddComponent<ARCameraManager>();
        if (!cameraGo.GetComponent<ARCameraBackground>()) cameraGo.AddComponent<ARCameraBackground>();

        var occlusionManager = cameraGo.GetComponent<AROcclusionManager>();
        if (!occlusionManager) occlusionManager = cameraGo.AddComponent<AROcclusionManager>();

        // Configure for LiDAR
        occlusionManager.requestedEnvironmentDepthMode = UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Best;
        occlusionManager.requestedHumanDepthMode = UnityEngine.XR.ARSubsystems.HumanSegmentationDepthMode.Best;
        occlusionManager.requestedOcclusionPreferenceMode = UnityEngine.XR.ARSubsystems.OcclusionPreferenceMode.NoOcclusion;

        // 4. Add ARCameraTextureProvider
        var provider = cameraGo.GetComponent<ARCameraTextureProvider>();
        if (!provider) provider = cameraGo.AddComponent<ARCameraTextureProvider>();

        // 5. Setup VFX
        var vfxGo = GameObject.Find("Rcam4 VFX");
        if (vfxGo == null)
        {
            vfxGo = new GameObject("Rcam4 VFX");
            vfxGo.transform.position = Vector3.zero;
            vfxGo.transform.rotation = Quaternion.identity;
        }

        var vfx = vfxGo.GetComponent<VisualEffect>();
        if (!vfx) vfx = vfxGo.AddComponent<VisualEffect>();

        // Assign VFX Asset
        var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/Rcam4/Rcam4.vfx");
        if (vfxAsset != null)
        {
            vfx.visualEffectAsset = vfxAsset;
        }
        else
        {
            Debug.LogWarning("Rcam4.vfx not found at Assets/VFX/Rcam4/Rcam4.vfx");
        }

        // 6. Add H3MLiDARCapture to Camera
        var capture = cameraGo.GetComponent<H3MLiDARCapture>();
        if (!capture) capture = cameraGo.AddComponent<H3MLiDARCapture>();

        // Wire up references
        using (var so = new SerializedObject(capture))
        {
            so.FindProperty("_occlusionManager").objectReferenceValue = occlusionManager;
            so.FindProperty("_colorProvider").objectReferenceValue = provider;
            so.FindProperty("_vfx").objectReferenceValue = vfx;
            so.ApplyModifiedProperties();
        }

        // 6b. Add Debug Cube
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "DebugCube";
        cube.transform.position = new Vector3(0, 0, 1f); // 1m forward
        cube.transform.localScale = Vector3.one * 0.1f; // 10cm size
        cube.transform.SetParent(cameraGo.transform); // Attached to camera for visibility

        Debug.Log("Rcam4 Scene Setup Complete!");

        // 7. Save Scene
        string scenePath = "Assets/Scenes/Rcam4Scene.unity";
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"Scene saved to {scenePath}");
    }
}
