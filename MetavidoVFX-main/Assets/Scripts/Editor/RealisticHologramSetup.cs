using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;

/// <summary>
/// One-click setup for realistic RGB hologram (Record3D-style point cloud).
/// Configures ARCameraTextureProvider, ARDepthSource, and VFXARBinder for ColorMap.
/// </summary>
public static class RealisticHologramSetup
{
    [MenuItem("H3M/Hologram/Setup Realistic RGB Hologram", false, 100)]
    public static void SetupRealisticHologram()
    {
        Debug.Log("[RealisticHologram] Starting setup...");

        bool success = true;

        // Step 1: Find or create ARCameraTextureProvider
        var colorProvider = SetupARCameraTextureProvider();

        // Step 2: Connect to ARDepthSource
        var depthSource = ConnectToARDepthSource(colorProvider);

        // Step 3: Find hologram VFX and assign Metavido asset
        var hologramVFX = SetupHologramVFX();

        // Step 4: Enable ColorMap binding on VFXARBinder
        if (hologramVFX != null)
        {
            EnableColorMapBinding(hologramVFX);
        }
        else
        {
            success = false;
        }

        if (success)
        {
            Debug.Log("[RealisticHologram] Setup complete! Hologram will show realistic RGB colors.");
            EditorUtility.DisplayDialog("Realistic Hologram Setup",
                "Setup complete!\n\n" +
                "• ARCameraTextureProvider configured\n" +
                "• ColorMap binding enabled\n" +
                "• Using pointcloud_depth_people_metavido.vfx\n\n" +
                "Test with AR Remote or on device.", "OK");
        }
        else
        {
            Debug.LogWarning("[RealisticHologram] Setup incomplete. Check Console for details.");
        }
    }

    static Metavido.ARCameraTextureProvider SetupARCameraTextureProvider()
    {
        // Check if already exists
        var existing = Object.FindFirstObjectByType<Metavido.ARCameraTextureProvider>();
        if (existing != null)
        {
            Debug.Log($"[RealisticHologram] ARCameraTextureProvider already exists on {existing.gameObject.name}");
            return existing;
        }

        // Find AR Camera
        var arCameraManager = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>();
        if (arCameraManager != null)
        {
            var provider = arCameraManager.gameObject.AddComponent<Metavido.ARCameraTextureProvider>();
            Debug.Log($"[RealisticHologram] Added ARCameraTextureProvider to {arCameraManager.gameObject.name}");
            EditorUtility.SetDirty(arCameraManager.gameObject);
            return provider;
        }

        // Fallback: Find main camera
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var provider = mainCamera.gameObject.AddComponent<Metavido.ARCameraTextureProvider>();
            Debug.Log($"[RealisticHologram] Added ARCameraTextureProvider to {mainCamera.gameObject.name}");
            EditorUtility.SetDirty(mainCamera.gameObject);
            return provider;
        }

        Debug.LogWarning("[RealisticHologram] Could not find AR Camera or Main Camera for ARCameraTextureProvider");
        return null;
    }

    static ARDepthSource ConnectToARDepthSource(Metavido.ARCameraTextureProvider colorProvider)
    {
        var depthSource = Object.FindFirstObjectByType<ARDepthSource>();
        if (depthSource == null)
        {
            Debug.LogWarning("[RealisticHologram] ARDepthSource not found. Run 'H3M > VFX Pipeline Master > Setup Complete Pipeline' first.");
            return null;
        }

        if (colorProvider != null)
        {
            // Use SerializedObject to set private field
            var so = new SerializedObject(depthSource);
            var colorProviderProp = so.FindProperty("_colorProvider");
            if (colorProviderProp != null)
            {
                colorProviderProp.objectReferenceValue = colorProvider;
                so.ApplyModifiedProperties();
                Debug.Log("[RealisticHologram] Connected ARCameraTextureProvider to ARDepthSource");
            }
        }

        return depthSource;
    }

    static VisualEffect SetupHologramVFX()
    {
        // Find Hologram/HologramVFX in hierarchy
        var hologramRoot = GameObject.Find("Hologram");
        if (hologramRoot == null)
        {
            Debug.LogWarning("[RealisticHologram] 'Hologram' GameObject not found in scene");
            return null;
        }

        var hologramVFXTransform = hologramRoot.transform.Find("HologramVFX");
        if (hologramVFXTransform == null)
        {
            Debug.LogWarning("[RealisticHologram] 'Hologram/HologramVFX' not found in scene");
            return null;
        }

        var vfx = hologramVFXTransform.GetComponent<VisualEffect>();
        if (vfx == null)
        {
            Debug.LogWarning("[RealisticHologram] VisualEffect component not found on HologramVFX");
            return null;
        }

        // Find and assign Metavido VFX asset
        var metavidoVFX = FindMetavidoVFXAsset();
        if (metavidoVFX != null)
        {
            vfx.visualEffectAsset = metavidoVFX;
            EditorUtility.SetDirty(vfx);
            Debug.Log($"[RealisticHologram] Assigned {metavidoVFX.name} to HologramVFX");
        }
        else
        {
            Debug.LogWarning("[RealisticHologram] Could not find pointcloud_depth_people_metavido VFX asset");
        }

        return vfx;
    }

    static VisualEffectAsset FindMetavidoVFXAsset()
    {
        // Search for the Metavido pointcloud VFX
        string[] guids = AssetDatabase.FindAssets("pointcloud_depth_people_metavido t:VisualEffectAsset");

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Prefer the one without " 1" suffix
            if (!path.Contains(" 1"))
            {
                return AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            }
        }

        // Fallback: return first match
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
        }

        return null;
    }

    static void EnableColorMapBinding(VisualEffect vfx)
    {
        var binder = vfx.GetComponent<VFXARBinder>();
        if (binder == null)
        {
            binder = vfx.gameObject.AddComponent<VFXARBinder>();
            Debug.Log("[RealisticHologram] Added VFXARBinder to HologramVFX");
        }

        // Enable ColorMap binding via SerializedObject
        var so = new SerializedObject(binder);
        var colorMapProp = so.FindProperty("_bindColorMapOverride");
        if (colorMapProp != null)
        {
            colorMapProp.boolValue = true;
            so.ApplyModifiedProperties();
            Debug.Log("[RealisticHologram] Enabled ColorMap binding on VFXARBinder");
        }

        // Also enable other essential bindings
        var depthMapProp = so.FindProperty("_bindDepthMapOverride");
        if (depthMapProp != null) depthMapProp.boolValue = true;

        var rayParamsProp = so.FindProperty("_bindRayParamsOverride");
        if (rayParamsProp != null) rayParamsProp.boolValue = true;

        var invViewProp = so.FindProperty("_bindInverseViewOverride");
        if (invViewProp != null) invViewProp.boolValue = true;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(binder);
    }

    [MenuItem("H3M/Hologram/Setup Realistic RGB Hologram", true)]
    static bool ValidateSetupRealisticHologram()
    {
        // Only enable in Edit mode
        return !Application.isPlaying;
    }
}
