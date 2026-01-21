#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using System.IO;

namespace MetavidoVFX.H3M.Editor
{
    /// <summary>
    /// Editor utilities to create and configure HiFi Hologram VFX assets.
    /// </summary>
    public static class HiFiHologramVFXSetup
    {
        private const string TemplatePath = "Assets/VFX/People/particles_depth_people_metavido.vfx";
        private const string OutputPath = "Assets/VFX/People/hifi_hologram_people.vfx";
        private const string ResourcesOutputPath = "Assets/Resources/VFX/People/hifi_hologram_people.vfx";

        [MenuItem("H3M/HiFi Hologram/Create HiFi Hologram VFX")]
        public static void CreateHiFiHologramVFX()
        {
            // Check if already exists
            if (File.Exists(Application.dataPath.Replace("Assets", "") + OutputPath))
            {
                Debug.Log($"[HiFi Hologram] VFX already exists at {OutputPath}");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(OutputPath);
                return;
            }

            // Check template exists
            if (!File.Exists(Application.dataPath.Replace("Assets", "") + TemplatePath))
            {
                Debug.LogError($"[HiFi Hologram] Template not found at {TemplatePath}");
                return;
            }

            // Ensure output directory exists
            string outputDir = Path.GetDirectoryName(OutputPath);
            if (!AssetDatabase.IsValidFolder(outputDir))
            {
                Directory.CreateDirectory(Application.dataPath.Replace("Assets", "") + outputDir);
                AssetDatabase.Refresh();
            }

            // Duplicate the template
            if (AssetDatabase.CopyAsset(TemplatePath, OutputPath))
            {
                AssetDatabase.Refresh();
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(OutputPath);

                if (vfxAsset != null)
                {
                    Debug.Log($"[HiFi Hologram] Created HiFi Hologram VFX at {OutputPath}");
                    Debug.Log("[HiFi Hologram] Required modifications:");
                    Debug.Log("  1. Open in VFX Graph editor");
                    Debug.Log("  2. Add 'ParticleCount' (UInt) exposed property - default 100000");
                    Debug.Log("  3. Add 'ParticleSize' (Float) exposed property - default 0.002");
                    Debug.Log("  4. Add 'ColorSaturation' (Float) exposed property - default 1.0");
                    Debug.Log("  5. Add 'ColorBrightness' (Float) exposed property - default 1.0");
                    Debug.Log("  6. Ensure Initialize samples ColorMap at particle UV");
                    Debug.Log("  7. Ensure particles are small billboards (2mm)");

                    Selection.activeObject = vfxAsset;
                    EditorGUIUtility.PingObject(vfxAsset);
                }
            }
            else
            {
                Debug.LogError("[HiFi Hologram] Failed to copy template VFX");
            }
        }

        [MenuItem("H3M/HiFi Hologram/Add HiFiHologramController to Selected")]
        public static void AddHiFiControllerToSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("[HiFi Hologram] No GameObject selected");
                return;
            }

            var vfx = selected.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                Debug.LogError("[HiFi Hologram] Selected object has no VisualEffect component");
                return;
            }

            // Add HiFiHologramController if not present
            var controller = selected.GetComponent<VFX.HiFiHologramController>();
            if (controller == null)
            {
                controller = selected.AddComponent<VFX.HiFiHologramController>();
                Debug.Log("[HiFi Hologram] Added HiFiHologramController");
            }

            // Add VFXARBinder if not present
            var binder = selected.GetComponent<global::VFXARBinder>();
            if (binder == null)
            {
                binder = selected.AddComponent<global::VFXARBinder>();
                Debug.Log("[HiFi Hologram] Added VFXARBinder");
            }

            EditorUtility.SetDirty(selected);
            Debug.Log("[HiFi Hologram] Setup complete! VFX is ready for HiFi rendering.");
        }

        [MenuItem("H3M/HiFi Hologram/Setup Complete HiFi Hologram Rig")]
        public static void SetupCompleteRig()
        {
            // Create parent object
            var rigGO = new GameObject("HiFi_Hologram_Rig");

            // Create VFX child
            var vfxGO = new GameObject("HiFi_VFX");
            vfxGO.transform.SetParent(rigGO.transform);

            // Add VisualEffect component
            var vfx = vfxGO.AddComponent<VisualEffect>();

            // Try to load the HiFi VFX asset
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(OutputPath);
            if (vfxAsset != null)
            {
                vfx.visualEffectAsset = vfxAsset;
            }
            else
            {
                // Fallback to template
                vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(TemplatePath);
                if (vfxAsset != null)
                {
                    vfx.visualEffectAsset = vfxAsset;
                    Debug.LogWarning("[HiFi Hologram] Using template VFX. Run 'Create HiFi Hologram VFX' first.");
                }
            }

            // Add components
            vfxGO.AddComponent<VFX.HiFiHologramController>();
            vfxGO.AddComponent<global::VFXARBinder>();

            Selection.activeGameObject = rigGO;
            Debug.Log("[HiFi Hologram] Created HiFi Hologram Rig. Components:");
            Debug.Log("  - VisualEffect");
            Debug.Log("  - HiFiHologramController");
            Debug.Log("  - VFXARBinder");
        }

        [MenuItem("H3M/HiFi Hologram/Verify HiFi Setup")]
        public static void VerifySetup()
        {
            Debug.Log("[HiFi Hologram] Verifying setup...");

            // Check VFX asset
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(OutputPath);
            Log("HiFi VFX Asset", vfxAsset != null, vfxAsset != null ? OutputPath : "Run 'Create HiFi Hologram VFX'");

            // Check ARDepthSource
            var depthSource = Object.FindFirstObjectByType<ARDepthSource>();
            Log("ARDepthSource", depthSource != null, depthSource != null ? "Found" : "Run 'H3M > VFX Pipeline Master > Setup Complete Pipeline'");

            // Check HiFiHologramController
            var controller = Object.FindFirstObjectByType<VFX.HiFiHologramController>();
            Log("HiFiHologramController", controller != null, controller != null ? "Found" : "Add to VFX GameObject");

            // Check VFXARBinder
            var binder = Object.FindFirstObjectByType<global::VFXARBinder>();
            Log("VFXARBinder", binder != null, binder != null ? "Found" : "Add to VFX GameObject");

            Debug.Log("[HiFi Hologram] Verification complete.");
        }

        private static void Log(string component, bool found, string details)
        {
            string status = found ? "\u2713" : "\u2717 MISSING";
            Debug.Log($"  [{status}] {component}: {details}");
        }
    }
}
#endif
