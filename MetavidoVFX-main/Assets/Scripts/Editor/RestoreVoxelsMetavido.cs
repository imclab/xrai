using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEngine.XR.ARFoundation;
using Metavido;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class RestoreVoxelsMetavido : MonoBehaviour
{
    [ContextMenu("Restore Voxels_metavido with VFXARDataBinder")]
    public static void RestoreWithVFXARDataBinder()
    {
#if UNITY_EDITOR
        var voxels = GameObject.Find("Voxels_metavido");
        if (voxels == null)
        {
            Debug.LogError("[RestoreVoxelsMetavido] Voxels_metavido not found!");
            return;
        }

        Debug.Log("[RestoreVoxelsMetavido] Restoring Voxels_metavido configuration...");

        // 1. Disable VFXARBinder if present
        var arBinder = voxels.GetComponent<VFXARBinder>();
        if (arBinder != null)
        {
            arBinder.enabled = false;
            Debug.Log("[RestoreVoxelsMetavido] Disabled VFXARBinder");
        }

        // 2. Disable VFXPropertyBinder if present (not needed for VFXARDataBinder)
        var propBinder = voxels.GetComponent<VFXPropertyBinder>();
        if (propBinder != null)
        {
            propBinder.enabled = false;
            Debug.Log("[RestoreVoxelsMetavido] Disabled VFXPropertyBinder");
        }

        // 3. Look for or add VFXARDataBinder
        var vfxARDataBinder = voxels.GetComponent<MetavidoVFX.VFX.Binders.VFXARDataBinder>();
        if (vfxARDataBinder == null)
        {
            vfxARDataBinder = voxels.AddComponent<MetavidoVFX.VFX.Binders.VFXARDataBinder>();
            Debug.Log("[RestoreVoxelsMetavido] Added VFXARDataBinder component");
        }

        // 4. Configure VFXARDataBinder
        vfxARDataBinder.enabled = true;
        
        // Set required references
        var arCamera = GameObject.Find("HoloKit Camera Rig/Camera Offset/AR Camera");
        if (arCamera != null)
        {
            var camera = arCamera.GetComponent<Camera>();
            var occlusionMgr = arCamera.GetComponent<AROcclusionManager>();
            var cameraBackground = arCamera.GetComponent<ARCameraBackground>();

            var so = new SerializedObject(vfxARDataBinder);
            
            so.FindProperty("occlusionManager").objectReferenceValue = occlusionMgr;
            so.FindProperty("cameraBackground").objectReferenceValue = cameraBackground;
            so.FindProperty("arCamera").objectReferenceValue = camera;
            so.FindProperty("colorProvider").objectReferenceValue = arCamera.GetComponent<ARCameraTextureProvider>();
            
            so.FindProperty("bindDepthMap").boolValue = true;
            so.FindProperty("bindStencilMap").boolValue = true;
            so.FindProperty("bindColorMap").boolValue = true;
            so.FindProperty("bindPositionMap").boolValue = true;
            so.FindProperty("bindCameraMatrices").boolValue = true;
            so.FindProperty("rotateDepthTexture").boolValue = true;
            so.FindProperty("maskDepthWithStencil").boolValue = true;
            so.FindProperty("verboseLogging").boolValue = true;

            so.ApplyModifiedProperties();
            Debug.Log("[RestoreVoxelsMetavido] Configured VFXARDataBinder with AR components");
        }

        // 5. Disable Fluo.ARVfxBridge if present
        var fluoBridge = voxels.GetComponent<Fluo.ARVfxBridge>();
        if (fluoBridge != null)
        {
            fluoBridge.enabled = false;
            Debug.Log("[RestoreVoxelsMetavido] Disabled Fluo.ARVfxBridge");
        }

        EditorSceneManager.MarkSceneDirty(voxels.scene);
        Debug.Log("[RestoreVoxelsMetavido] ✓ Restoration complete!");
        Debug.Log("[RestoreVoxelsMetavido] If depth still not working:");
        Debug.Log("   1. Check AR Session has Environment Depth enabled");
        Debug.Log("   2. On device: Verify app has camera permissions");
        Debug.Log("   3. Check AR Foundation Remote device connection");
#endif
    }
}
