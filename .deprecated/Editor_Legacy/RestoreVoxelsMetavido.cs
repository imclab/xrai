using UnityEngine;
using UnityEngine.VFX.Utility;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class RestoreVoxelsMetavido : MonoBehaviour
{
    [ContextMenu("Restore Voxels_metavido with VFXARBinder")]
    public static void RestoreWithVFXARBinder()
    {
#if UNITY_EDITOR
        var voxels = GameObject.Find("Voxels_metavido");
        if (voxels == null)
        {
            Debug.LogError("[RestoreVoxelsMetavido] Voxels_metavido not found!");
            return;
        }

        Debug.Log("[RestoreVoxelsMetavido] Restoring Voxels_metavido configuration...");

        // 1. Enable VFXARBinder (Hybrid Bridge)
        var arBinder = voxels.GetComponent<VFXARBinder>();
        if (arBinder == null)
        {
            arBinder = voxels.AddComponent<VFXARBinder>();
            Debug.Log("[RestoreVoxelsMetavido] Added VFXARBinder component");
        }
        arBinder.enabled = true;
        Debug.Log("[RestoreVoxelsMetavido] Enabled VFXARBinder");

        // 2. Enable VFXPropertyBinder if present (used by optional binders)
        var propBinder = voxels.GetComponent<VFXPropertyBinder>();
        if (propBinder != null)
        {
            propBinder.enabled = true;
            Debug.Log("[RestoreVoxelsMetavido] Enabled VFXPropertyBinder");
        }

        var depthSource = ARDepthSource.Instance;
        if (depthSource == null)
        {
            depthSource = Object.FindFirstObjectByType<ARDepthSource>();
        }

        if (depthSource == null)
        {
            Debug.LogWarning("[RestoreVoxelsMetavido] ARDepthSource not found - add via H3M > VFX Pipeline Master > Create ARDepthSource");
        }
        else
        {
            Debug.Log("[RestoreVoxelsMetavido] ARDepthSource detected");
        }

        // 3. Disable legacy VFXARDataBinder if present
        var legacyBinder = voxels.GetComponent<MetavidoVFX.VFX.Binders.VFXARDataBinder>();
        if (legacyBinder != null)
        {
            legacyBinder.enabled = false;
            Debug.Log("[RestoreVoxelsMetavido] Disabled legacy VFXARDataBinder");
        }

        // 4. Disable Fluo.ARVfxBridge if present
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
