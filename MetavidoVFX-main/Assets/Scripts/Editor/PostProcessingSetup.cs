// Post-Processing Setup - Ensures post-processing is properly configured
// Menu: H3M > Post-Processing > Setup Post-Processing

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XRRAI.Editor
{
    public static class PostProcessingSetup
    {
        private const string POSTPROCESS_PROFILE_PATH = "Assets/Misc/Postprocess.asset";

        [MenuItem("H3M/Post-Processing/Setup Post-Processing")]
        public static void SetupPostProcessing()
        {
            Debug.Log("[PostProcess Setup] Setting up post-processing...");

            // 1. Find or create Global Volume
            EnsureGlobalVolume();

            // 2. Enable post-processing on all cameras
            EnableCameraPostProcessing();

            // 3. Verify URP settings
            VerifyURPSettings();

            Debug.Log("[PostProcess Setup] ✓ Complete!");
        }

        [MenuItem("H3M/Post-Processing/Enable Camera Post-Processing")]
        public static void EnableCameraPostProcessing()
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var cam in cameras)
            {
                var additionalData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (additionalData != null)
                {
                    if (!additionalData.renderPostProcessing)
                    {
                        Undo.RecordObject(additionalData, "Enable Post-Processing");
                        additionalData.renderPostProcessing = true;
                        count++;
                        Debug.Log($"[PostProcess Setup] Enabled post-processing on: {cam.name}");
                    }
                }
                else
                {
                    // Add URP camera data component
                    additionalData = Undo.AddComponent<UniversalAdditionalCameraData>(cam.gameObject);
                    additionalData.renderPostProcessing = true;
                    count++;
                    Debug.Log($"[PostProcess Setup] Added + enabled post-processing on: {cam.name}");
                }
            }

            Debug.Log($"[PostProcess Setup] ✓ Enabled post-processing on {count} cameras");
        }

        static void EnsureGlobalVolume()
        {
            // Load the Postprocess profile
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(POSTPROCESS_PROFILE_PATH);
            if (profile == null)
            {
                Debug.LogError($"[PostProcess Setup] Cannot find profile at: {POSTPROCESS_PROFILE_PATH}");
                return;
            }

            // Find existing Global Volume
            var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            Volume globalVolume = null;

            foreach (var vol in volumes)
            {
                if (vol.isGlobal)
                {
                    globalVolume = vol;
                    break;
                }
            }

            if (globalVolume != null)
            {
                // Update existing volume to use our profile
                if (globalVolume.sharedProfile != profile)
                {
                    Undo.RecordObject(globalVolume, "Update Volume Profile");
                    globalVolume.sharedProfile = profile;
                    Debug.Log("[PostProcess Setup] Updated existing Global Volume with Postprocess profile");
                }
                else
                {
                    Debug.Log("[PostProcess Setup] ✓ Global Volume already using correct profile");
                }
            }
            else
            {
                // Create new Global Volume
                var volumeObj = new GameObject("Global Volume");
                globalVolume = volumeObj.AddComponent<Volume>();
                globalVolume.isGlobal = true;
                globalVolume.sharedProfile = profile;
                globalVolume.priority = 100;
                Undo.RegisterCreatedObjectUndo(volumeObj, "Create Global Volume");
                Debug.Log("[PostProcess Setup] ✓ Created new Global Volume with Postprocess profile");
            }
        }

        static void VerifyURPSettings()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                Debug.LogWarning("[PostProcess Setup] No URP asset found in Graphics Settings");
                return;
            }

            Debug.Log($"[PostProcess Setup] ✓ URP Asset: {urpAsset.name}");

            // Check HDR
            bool hdrEnabled = urpAsset.supportsHDR;
            Debug.Log($"[PostProcess Setup] HDR: {(hdrEnabled ? "✓ Enabled" : "✗ Disabled")}");
        }

        [MenuItem("H3M/Post-Processing/Validate Setup")]
        public static void ValidateSetup()
        {
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("   Post-Processing Validation");
            Debug.Log("═══════════════════════════════════════════════════════════");

            // Check Global Volume
            var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            Volume globalVolume = null;
            foreach (var vol in volumes)
            {
                if (vol.isGlobal && vol.gameObject.activeInHierarchy)
                {
                    globalVolume = vol;
                    break;
                }
            }

            if (globalVolume != null)
            {
                Debug.Log($"  [✓] Global Volume: {globalVolume.gameObject.name}");
                if (globalVolume.sharedProfile != null)
                {
                    Debug.Log($"      Profile: {globalVolume.sharedProfile.name}");

                    // Check effects
                    if (globalVolume.sharedProfile.TryGet<Bloom>(out var bloom))
                        Debug.Log($"      Bloom: intensity={bloom.intensity.value}, threshold={bloom.threshold.value}");
                    if (globalVolume.sharedProfile.TryGet<Tonemapping>(out var tonemap))
                        Debug.Log($"      Tonemapping: mode={tonemap.mode.value}");
                    if (globalVolume.sharedProfile.TryGet<Vignette>(out var vignette))
                        Debug.Log($"      Vignette: intensity={vignette.intensity.value}");
                    if (globalVolume.sharedProfile.TryGet<ChromaticAberration>(out var ca))
                        Debug.Log($"      ChromaticAberration: intensity={ca.intensity.value}");
                }
                else
                {
                    Debug.LogWarning("  [✗] Global Volume has no profile assigned!");
                }
            }
            else
            {
                Debug.LogWarning("  [✗] No active Global Volume found!");
            }

            // Check cameras
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            int ppEnabled = 0, ppDisabled = 0;

            foreach (var cam in cameras)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data != null && data.renderPostProcessing)
                    ppEnabled++;
                else
                    ppDisabled++;
            }

            Debug.Log($"  Cameras with post-processing enabled: {ppEnabled}");
            if (ppDisabled > 0)
                Debug.LogWarning($"  Cameras with post-processing DISABLED: {ppDisabled}");

            Debug.Log("═══════════════════════════════════════════════════════════");
        }

        [MenuItem("H3M/Post-Processing/Copy Effects from Postprocess to DefaultVolume")]
        public static void CopyEffectsToDefaultVolume()
        {
            var srcProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(POSTPROCESS_PROFILE_PATH);
            var dstProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/URP/DefaultVolume.asset");

            if (srcProfile == null || dstProfile == null)
            {
                Debug.LogError("[PostProcess Setup] Cannot find source or destination profile");
                return;
            }

            // Copy Bloom
            if (srcProfile.TryGet<Bloom>(out var srcBloom) && dstProfile.TryGet<Bloom>(out var dstBloom))
            {
                dstBloom.threshold.Override(srcBloom.threshold.value);
                dstBloom.intensity.Override(srcBloom.intensity.value);
                dstBloom.scatter.Override(srcBloom.scatter.value);
                dstBloom.maxIterations.Override(srcBloom.maxIterations.value);
            }

            // Copy Tonemapping
            if (srcProfile.TryGet<Tonemapping>(out var srcTone) && dstProfile.TryGet<Tonemapping>(out var dstTone))
            {
                dstTone.mode.Override(srcTone.mode.value);
            }

            // Copy Vignette
            if (srcProfile.TryGet<Vignette>(out var srcVig) && dstProfile.TryGet<Vignette>(out var dstVig))
            {
                dstVig.intensity.Override(srcVig.intensity.value);
            }

            // Copy ChromaticAberration
            if (srcProfile.TryGet<ChromaticAberration>(out var srcCA) && dstProfile.TryGet<ChromaticAberration>(out var dstCA))
            {
                dstCA.intensity.Override(srcCA.intensity.value);
            }

            // Copy ColorAdjustments
            if (srcProfile.TryGet<ColorAdjustments>(out var srcColor) && dstProfile.TryGet<ColorAdjustments>(out var dstColor))
            {
                dstColor.saturation.Override(srcColor.saturation.value);
                dstColor.contrast.Override(srcColor.contrast.value);
            }

            // Copy LensDistortion
            if (srcProfile.TryGet<LensDistortion>(out var srcLens) && dstProfile.TryGet<LensDistortion>(out var dstLens))
            {
                dstLens.intensity.Override(srcLens.intensity.value);
            }

            EditorUtility.SetDirty(dstProfile);
            AssetDatabase.SaveAssets();

            Debug.Log("[PostProcess Setup] ✓ Copied effects from Postprocess to DefaultVolume");
        }
    }
}
