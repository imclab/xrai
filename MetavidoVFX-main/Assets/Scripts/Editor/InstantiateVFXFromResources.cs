using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Editor utility to instantiate VFX from Resources folder with VFXARBinder
/// </summary>
public static class InstantiateVFXFromResources
{
    [MenuItem("H3M/VFX Pipeline Master/Instantiate VFX from Resources", false, 50)]
    public static void InstantiateAllVFX()
    {
        // Find or create parent container
        var container = GameObject.Find("VFX_Container");
        if (container == null)
        {
            container = new GameObject("VFX_Container");
            Undo.RegisterCreatedObjectUndo(container, "Create VFX Container");
        }

        int created = 0;

        // Load all VFX from Resources/VFX folder
        var vfxAssets = Resources.LoadAll<VisualEffectAsset>("VFX");

        foreach (var asset in vfxAssets)
        {
            // Check if this VFX is already in scene
            bool exists = false;
            foreach (var existingVFX in Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None))
            {
                if (existingVFX.visualEffectAsset == asset)
                {
                    exists = true;
                    break;
                }
            }

            if (exists) continue;

            // Create GameObject with VisualEffect
            var go = new GameObject(asset.name);
            go.transform.SetParent(container.transform);
            var vfx = go.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = asset;

            // Add VFXARBinder
            var binder = go.AddComponent<VFXARBinder>();
            binder.AutoDetectBindings();

            // Start disabled (user can enable as needed)
            go.SetActive(false);

            Undo.RegisterCreatedObjectUndo(go, $"Create {asset.name}");
            created++;

            Debug.Log($"[InstantiateVFX] Created: {asset.name}");
        }

        Debug.Log($"[InstantiateVFX] Created {created} VFX GameObjects. Total VFX assets found: {vfxAssets.Length}");

        if (created > 0)
        {
            EditorUtility.DisplayDialog("VFX Instantiated",
                $"Created {created} VFX GameObjects in VFX_Container.\n\n" +
                "All are disabled by default.\n" +
                "Enable the ones you want to use.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("No New VFX",
                $"Found {vfxAssets.Length} VFX assets in Resources/VFX.\n" +
                "All are already in scene.",
                "OK");
        }
    }

    [MenuItem("H3M/VFX Pipeline Master/List VFX in Resources", false, 51)]
    public static void ListVFXInResources()
    {
        var vfxAssets = Resources.LoadAll<VisualEffectAsset>("VFX");
        Debug.Log($"=== VFX in Resources/VFX ({vfxAssets.Length} total) ===");
        foreach (var asset in vfxAssets)
        {
            Debug.Log($"  - {asset.name}");
        }
    }
}
