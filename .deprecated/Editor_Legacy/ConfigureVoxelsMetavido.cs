using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Configure Voxels_metavido prefab to use ARDepthSource + VFXARBinder
/// </summary>
public class ConfigureVoxelsMetavido : MonoBehaviour
{
    [ContextMenu("Configure Voxels_metavido for ARDepthSource")]
    public static void ConfigureVoxels()
    {
#if UNITY_EDITOR
        var voxels = GameObject.Find("Voxels_metavido");
        if (voxels == null)
        {
            Debug.LogError("[ConfigureVoxelsMetavido] Voxels_metavido not found in scene!");
            return;
        }

        // 1. Enable VFXPropertyBinder (required for VFXARBinder to work)
        var propBinder = voxels.GetComponent<VFXPropertyBinder>();
        if (propBinder != null)
        {
            propBinder.enabled = true;
            Debug.Log("[ConfigureVoxelsMetavido] Enabled VFXPropertyBinder");
        }
        else
        {
            Debug.LogWarning("[ConfigureVoxelsMetavido] VFXPropertyBinder not found!");
        }

        // 2. Enable VFXARBinder (binds from ARDepthSource)
        var arBinder = voxels.GetComponent<VFXARBinder>();
        if (arBinder != null)
        {
            arBinder.enabled = true;
            Debug.Log("[ConfigureVoxelsMetavido] Enabled VFXARBinder");
        }
        else
        {
            Debug.LogError("[ConfigureVoxelsMetavido] VFXARBinder not found!");
        }

        // 3. Disable conflicting binders
        var vfxARDataBinder = voxels.GetComponent<MetavidoVFX.VFX.Binders.VFXARDataBinder>();
        if (vfxARDataBinder != null)
        {
            vfxARDataBinder.enabled = false;
            Debug.Log("[ConfigureVoxelsMetavido] Disabled VFXARDataBinder");
        }

        var fluoBridge = voxels.GetComponent<Fluo.ARVfxBridge>();
        if (fluoBridge != null)
        {
            fluoBridge.enabled = false;
            Debug.Log("[ConfigureVoxelsMetavido] Disabled Fluo.ARVfxBridge");
        }

        // 4. Verify ARDepthSource exists in scene
        if (ARDepthSource.Instance == null)
        {
            Debug.LogError("[ConfigureVoxelsMetavido] ARDepthSource.Instance is null! Make sure ARDepthSource exists in the scene.");
        }
        else
        {
            Debug.Log("[ConfigureVoxelsMetavido] ARDepthSource.Instance is available");
        }

        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(voxels.scene);
        Debug.Log("[ConfigureVoxelsMetavido] Configuration complete!");
#endif
    }
}
