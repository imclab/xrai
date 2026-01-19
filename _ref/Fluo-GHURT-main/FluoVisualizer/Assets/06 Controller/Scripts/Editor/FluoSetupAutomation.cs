using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using UnityEngine.Rendering.Universal;
using Unity.XR.CoreUtils;
using System.Reflection;
using System.Linq;

#if XR_MANAGEMENT_4_0_OR_NEWER
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
#endif

namespace Fluo {

public static class FluoSetupAutomation
{
    [MenuItem("Fluo/Automate Setup")]
    public static void Automate()
    {
        SetupXR();
        SetupRenderer();
        SetupScene();
        Debug.Log("Fluo Setup Automation Complete!");
    }

    static void SetupXR()
    {
        #if XR_MANAGEMENT_4_0_OR_NEWER
        var settings = XRGeneralSettingsPerBuildTarget.GetSettingsForBuildTarget(BuildTargetGroup.iOS);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            // This is complex to setup via script without side effects, 
            // but we can at least warn the user.
            Debug.LogWarning("XR Settings for iOS not found. Please enable ARKit in Project Settings > XR Plug-in Management.");
        }
        #endif
    }

    static void SetupRenderer()
    {
        var rendererDataPath = "Assets/04 Visualizer/Settings/VisualRenderer.asset";
        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererDataPath);

        if (rendererData == null)
        {
            Debug.LogError($"Could not find UniversalRendererData at {rendererDataPath}");
            return;
        }

        // Add AR Background Renderer Feature if missing
        bool hasARBackground = rendererData.rendererFeatures.Any(f => f is ARBackgroundRendererFeature);
        if (!hasARBackground)
        {
            var arFeature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
            arFeature.name = "AR Background Renderer Feature";
            AssetDatabase.AddObjectToAsset(arFeature, rendererData);
            rendererData.rendererFeatures.Add(arFeature);
            EditorUtility.SetDirty(rendererData);
            Debug.Log("Added AR Background Renderer Feature to " + rendererData.name);
        }
    }

    static void SetupScene()
    {
        // 1. Setup AR Origin / Session
        var origin = Object.FindFirstObjectByType<XROrigin>();
        if (origin == null)
        {
            var originGo = new GameObject("XR Origin (AR)");
            origin = originGo.AddComponent<XROrigin>();
            var cameraGo = GameObject.FindWithTag("MainCamera");
            if (cameraGo != null)
            {
                cameraGo.transform.SetParent(originGo.transform);
                origin.Camera = cameraGo.GetComponent<Camera>();
            }
        }

        var session = Object.FindFirstObjectByType<ARSession>();
        if (session == null)
        {
            var sessionGo = new GameObject("AR Session");
            sessionGo.AddComponent<ARSession>();
            sessionGo.AddComponent<ARInputManager>();
        }

        // 2. Setup Camera components
        var cam = origin.Camera;
        if (cam != null)
        {
            if (cam.GetComponent<ARCameraManager>() == null) cam.gameObject.AddComponent<ARCameraManager>();
            if (cam.GetComponent<ARCameraBackground>() == null) cam.gameObject.AddComponent<ARCameraBackground>();
            if (cam.GetComponent<AROcclusionManager>() == null) cam.gameObject.AddComponent<AROcclusionManager>();
        }

        // 3. Setup VFX Bridge
        var vfx = Object.FindFirstObjectByType<VisualEffect>();
        if (vfx != null)
        {
            var bridge = vfx.GetComponent<ARVfxBridge>();
            if (bridge == null) bridge = vfx.gameObject.AddComponent<ARVfxBridge>();
            
            var cameraManager = cam.GetComponent<ARCameraManager>();
            var occlusionManager = cam.GetComponent<AROcclusionManager>();
            
            Undo.RecordObject(bridge, "Setup VFX Bridge");
            
            var so = new SerializedObject(bridge);
            so.FindProperty("_cameraManager").objectReferenceValue = cameraManager;
            so.FindProperty("_occlusionManager").objectReferenceValue = occlusionManager;
            so.FindProperty("_targetVfx").objectReferenceValue = vfx;

            var multiplexTarget = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/01 Input/Targets/VideoInPrefiltered.renderTexture");
            var blurTarget = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/01 Input/Targets/VideoInBlurred.renderTexture");
            var multiplexShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/01 Input/Prefilter/Prefilter.shader");

            so.FindProperty("_multiplexTarget").objectReferenceValue = multiplexTarget;
            so.FindProperty("_blurTarget").objectReferenceValue = blurTarget;
            so.FindProperty("_multiplexShader").objectReferenceValue = multiplexShader;

            so.ApplyModifiedProperties();
        }

        // 4. Setup Control UI Bridge
        var uiDoc = Object.FindFirstObjectByType<UIDocument>();
        if (uiDoc != null)
        {
            var bridge = uiDoc.GetComponent<ControlUIBridge>();
            if (bridge == null) bridge = uiDoc.gameObject.AddComponent<ControlUIBridge>();
            
            var so = new SerializedObject(bridge);
            so.FindProperty("_uiDocument").objectReferenceValue = uiDoc;
            var metadataRec = Object.FindFirstObjectByType<MetadataReceiver>();
            if (metadataRec != null) so.FindProperty("_metadataReceiver").objectReferenceValue = metadataRec;
            so.ApplyModifiedProperties();
        }

        // 5. Disable legacy logic
        var prefilter = Object.FindFirstObjectByType<Prefilter>();
        if (prefilter != null) prefilter.enabled = false;

        var metadataReceiver = Object.FindFirstObjectByType<MetadataReceiver>();
        if (metadataReceiver != null) metadataReceiver.enabled = false;

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}

}
