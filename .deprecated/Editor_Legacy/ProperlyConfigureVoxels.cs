using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class ProperlyConfigureVoxels : MonoBehaviour
{
    [ContextMenu("Properly Configure Voxels_metavido")]
    public static void Configure()
    {
#if UNITY_EDITOR
        var voxels = GameObject.Find("Voxels_metavido");
        if (voxels == null)
        {
            Debug.LogError("[ProperlyConfigureVoxels] Voxels_metavido not found!");
            return;
        }

        // 1. Get the VFXPropertyBinder
        var propBinder = voxels.GetComponent<VFXPropertyBinder>();
        if (propBinder == null)
        {
            Debug.LogError("[ProperlyConfigureVoxels] VFXPropertyBinder not found!");
            return;
        }

        // 2. Enable it
        propBinder.enabled = true;
        Debug.Log("[ProperlyConfigureVoxels] VFXPropertyBinder enabled");

        // 3. Get VFXARBinder
        var arBinder = voxels.GetComponent<VFXARBinder>();
        if (arBinder == null)
        {
            Debug.LogError("[ProperlyConfigureVoxels] VFXARBinder not found!");
            return;
        }

        // 4. Enable it
        arBinder.enabled = true;
        Debug.Log("[ProperlyConfigureVoxels] VFXARBinder enabled");

        // 5. Set the VFXPropertyBinder's m_Bindings to include VFXARBinder
        // This is what makes the binding work!
        SerializedObject so = new SerializedObject(propBinder);
        SerializedProperty bindings = so.FindProperty("m_Bindings");
        
        if (bindings != null && bindings.arraySize == 0)
        {
            bindings.arraySize = 1;
            SerializedProperty binding = bindings.GetArrayElementAtIndex(0);
            binding.objectReferenceValue = arBinder;
            so.ApplyModifiedProperties();
            Debug.Log("[ProperlyConfigureVoxels] Added VFXARBinder to VFXPropertyBinder.m_Bindings");
        }
        else if (bindings != null && bindings.arraySize > 0)
        {
            SerializedProperty binding = bindings.GetArrayElementAtIndex(0);
            if (binding.objectReferenceValue != arBinder)
            {
                binding.objectReferenceValue = arBinder;
                so.ApplyModifiedProperties();
                Debug.Log("[ProperlyConfigureVoxels] Updated VFXPropertyBinder.m_Bindings[0] to VFXARBinder");
            }
        }

        // 6. Disable conflicting binders
        var vfxARDataBinder = voxels.GetComponent<MetavidoVFX.VFX.Binders.VFXARDataBinder>();
        if (vfxARDataBinder != null)
        {
            vfxARDataBinder.enabled = false;
            Debug.Log("[ProperlyConfigureVoxels] Disabled VFXARDataBinder");
        }

        var fluoBridge = voxels.GetComponent<Fluo.ARVfxBridge>();
        if (fluoBridge != null)
        {
            fluoBridge.enabled = false;
            Debug.Log("[ProperlyConfigureVoxels] Disabled Fluo.ARVfxBridge");
        }

        EditorSceneManager.MarkSceneDirty(voxels.scene);
        Debug.Log("[ProperlyConfigureVoxels] ✓ Configuration complete!");
#endif
    }
}
