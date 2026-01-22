using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.IO;

/// <summary>
/// Creates optimized HiFi Hologram VFX that uses PositionMap + ColorMap.
/// No redundant depth→position computation - uses precomputed positions from ARDepthSource.
/// </summary>
public static class HiFiHologramVFXCreator
{
    const string OutputPath = "Assets/VFX/People/hifi_hologram_optimized.vfx";
    const string TemplatePath = "Assets/VFX/Buddha/points_mesh_buddha.vfx";

    [MenuItem("H3M/HiFi Hologram/Create Optimized HiFi Hologram VFX", false, 100)]
    public static void CreateHiFiHologramVFX()
    {
        // Find template VFX that uses PositionMap
        var template = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(TemplatePath);
        if (template == null)
        {
            // Try alternate templates
            string[] alternates = {
                "Assets/VFX/Buddha/particles_mesh_buddha.vfx",
                "Assets/VFX/pointcloud_depth_people_metavido.vfx"
            };

            foreach (var alt in alternates)
            {
                template = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(alt);
                if (template != null) break;
            }
        }

        if (template == null)
        {
            EditorUtility.DisplayDialog("HiFi Hologram VFX",
                "Could not find template VFX.\n\n" +
                "Please manually create a VFX with these properties:\n" +
                "• PositionMap (Texture2D)\n" +
                "• ColorMap (Texture2D)\n" +
                "• Throttle (float)\n" +
                "• MapWidth, MapHeight (float)\n\n" +
                "See HiFiHologramVFX.hlsl for sampling functions.",
                "OK");
            return;
        }

        // Ensure output directory exists
        string dir = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Copy template to new location
        if (File.Exists(OutputPath))
        {
            if (!EditorUtility.DisplayDialog("HiFi Hologram VFX",
                $"VFX already exists at {OutputPath}.\n\nOverwrite?",
                "Yes", "No"))
            {
                return;
            }
        }

        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(template), OutputPath);
        AssetDatabase.Refresh();

        var newVFX = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(OutputPath);
        if (newVFX != null)
        {
            // Select the new asset
            Selection.activeObject = newVFX;
            EditorGUIUtility.PingObject(newVFX);

            ShowSetupInstructions();
        }
    }

    [MenuItem("H3M/HiFi Hologram/Show VFX Setup Instructions", false, 101)]
    static void ShowSetupInstructions()
    {
        EditorUtility.DisplayDialog("HiFi Hologram VFX - Setup Instructions",
            "OPTIMIZED HIFI HOLOGRAM VFX\n\n" +
            "This VFX uses PositionMap (precomputed) + ColorMap for RGB color.\n" +
            "No redundant depth→position computation.\n\n" +
            "REQUIRED EXPOSED PROPERTIES:\n" +
            "• PositionMap (Texture2D) - World positions from ARDepthSource\n" +
            "• ColorMap (Texture2D) - Camera RGB from ARDepthSource\n" +
            "• MapWidth (float) - Texture width\n" +
            "• MapHeight (float) - Texture height\n" +
            "• Throttle (float) - Intensity control\n\n" +
            "INITIALIZE PARTICLE:\n" +
            "1. Add Custom HLSL block\n" +
            "2. Include: Assets/Shaders/HiFiHologramVFX.hlsl\n" +
            "3. Use SamplePositionMap(PositionMap, uv) for position\n" +
            "4. Use SampleColorMap(ColorMap, uv) for color\n\n" +
            "VFXARBINDER:\n" +
            "VFXARBinder will auto-detect and bind:\n" +
            "• PositionMap ← ARDepthSource.PositionMap\n" +
            "• ColorMap ← ARDepthSource.ColorMap\n" +
            "• MapWidth/MapHeight ← from PositionMap dimensions",
            "OK");
    }

    [MenuItem("H3M/HiFi Hologram/Add to Hologram Prefab", false, 102)]
    public static void AddToHologramPrefab()
    {
        // Find Hologram/HologramVFX
        var hologramRoot = GameObject.Find("Hologram");
        if (hologramRoot == null)
        {
            EditorUtility.DisplayDialog("HiFi Hologram",
                "Cannot find 'Hologram' GameObject in scene.\n\n" +
                "Please ensure the Hologram prefab is in the scene.",
                "OK");
            return;
        }

        var hologramVFXTransform = hologramRoot.transform.Find("HologramVFX");
        if (hologramVFXTransform == null)
        {
            EditorUtility.DisplayDialog("HiFi Hologram",
                "Cannot find 'Hologram/HologramVFX' in scene.\n\n" +
                "Please ensure the Hologram prefab has a HologramVFX child.",
                "OK");
            return;
        }

        var vfx = hologramVFXTransform.GetComponent<VisualEffect>();
        if (vfx == null)
        {
            vfx = hologramVFXTransform.gameObject.AddComponent<VisualEffect>();
        }

        // Try to find optimized VFX, fall back to Metavido
        var optimizedVFX = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(OutputPath);
        if (optimizedVFX == null)
        {
            optimizedVFX = FindMetavidoVFX();
        }

        if (optimizedVFX != null)
        {
            vfx.visualEffectAsset = optimizedVFX;
            EditorUtility.SetDirty(vfx);
            Debug.Log($"[HiFiHologram] Assigned {optimizedVFX.name} to HologramVFX");

            // Ensure VFXARBinder is set up
            var binder = vfx.GetComponent<VFXARBinder>();
            if (binder == null)
            {
                binder = vfx.gameObject.AddComponent<VFXARBinder>();
            }

            // Enable essential bindings
            var so = new SerializedObject(binder);
            SetBool(so, "_bindPositionMapOverride", true);
            SetBool(so, "_bindColorMapOverride", true);
            SetBool(so, "_bindDepthMapOverride", true);
            SetBool(so, "_bindRayParamsOverride", true);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(binder);

            EditorUtility.DisplayDialog("HiFi Hologram",
                $"Assigned {optimizedVFX.name} to HologramVFX.\n\n" +
                "VFXARBinder configured with:\n" +
                "• PositionMap binding\n" +
                "• ColorMap binding\n" +
                "• DepthMap binding\n" +
                "• RayParams binding\n\n" +
                "Test with AR Remote or on device.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("HiFi Hologram",
                "No HiFi Hologram VFX found.\n\n" +
                "Run 'Create Optimized HiFi Hologram VFX' first.",
                "OK");
        }
    }

    static void SetBool(SerializedObject so, string propName, bool value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null) prop.boolValue = value;
    }

    static VisualEffectAsset FindMetavidoVFX()
    {
        string[] guids = AssetDatabase.FindAssets("pointcloud_depth_people_metavido t:VisualEffectAsset");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains(" 1"))
            {
                return AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            }
        }
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }
}
