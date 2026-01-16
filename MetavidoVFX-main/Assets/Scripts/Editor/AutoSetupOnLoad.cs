// Auto-setup script that runs HoloKit + VFX Gallery setup on Unity load
// This script runs once when Unity Editor loads and sets up everything

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MetavidoVFX.Editor
{
    // DISABLED: Auto-setup was creating duplicate objects in scene
    // Use manual menu: H3M > Run Full Auto-Setup Now
    // [InitializeOnLoad]
    public static class AutoSetupOnLoad
    {
        private const string SETUP_DONE_KEY = "MetavidoVFX_SetupDone_v2";

        // DISABLED: Constructor caused duplicate creation on each Unity session
        // static AutoSetupOnLoad()
        // {
        //     // Only run once per project session
        //     if (SessionState.GetBool(SETUP_DONE_KEY, false))
        //         return;
        //
        //     // Delay execution to ensure Unity is fully loaded
        //     EditorApplication.delayCall += RunAutoSetup;
        // }

        static void RunAutoSetup()
        {
            // Mark as done for this session
            SessionState.SetBool(SETUP_DONE_KEY, true);

            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("   MetavidoVFX Auto-Setup Starting...");
            Debug.Log("═══════════════════════════════════════════════════════════");

            // Check if we're in the right scene
            var currentScene = EditorSceneManager.GetActiveScene();
            Debug.Log($"[AutoSetup] Current scene: {currentScene.name}");

            // Run the setup steps
            try
            {
                // Step 1: Setup Complete HoloKit Rig
                Debug.Log("\n[Step 1/4] Setting up HoloKit Rig...");
                HoloKitHandTrackingSetup.SetupCompleteHoloKitRig();

                // Step 2: Validate Hand Tracking
                Debug.Log("\n[Step 2/4] Validating Hand Tracking Setup...");
                HoloKitHandTrackingSetup.ValidateHandTrackingSetup();

                // Step 3: Setup VFX Gallery
                Debug.Log("\n[Step 3/4] Setting up VFX Gallery System...");
                UI.Editor.VFXGallerySetup.SetupCompleteVFXSystem();

                // Step 4: Final validation
                Debug.Log("\n[Step 4/4] Final Validation...");
                ValidateFullSetup();

                Debug.Log("\n═══════════════════════════════════════════════════════════");
                Debug.Log("   MetavidoVFX Auto-Setup COMPLETE!");
                Debug.Log("═══════════════════════════════════════════════════════════");
                Debug.Log("To re-run setup: H3M > HoloKit > Setup Complete HoloKit Rig");
                Debug.Log("To switch back to original XR: H3M > HoloKit > Re-enable Original XR Rig");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AutoSetup] Error during setup: {e.Message}");
                Debug.LogException(e);
            }
        }

        static void ValidateFullSetup()
        {
            int passed = 0;
            int total = 0;

            // Check AR Session
            total++;
            var arSession = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
            if (arSession != null) { passed++; Debug.Log("  ✓ AR Session"); }
            else Debug.LogWarning("  ✗ AR Session MISSING");

            // Check AR Camera
            total++;
            var arCamera = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>();
            if (arCamera != null) { passed++; Debug.Log("  ✓ AR Camera Manager"); }
            else Debug.LogWarning("  ✗ AR Camera Manager MISSING");

            // Check Occlusion Manager
            total++;
            var occlusion = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.AROcclusionManager>();
            if (occlusion != null) { passed++; Debug.Log("  ✓ AR Occlusion Manager"); }
            else Debug.LogWarning("  ✗ AR Occlusion Manager MISSING");

            // Check VFX Gallery
            total++;
            var gallery = Object.FindFirstObjectByType<UI.VFXGalleryUI>();
            if (gallery != null) { passed++; Debug.Log("  ✓ VFX Gallery UI"); }
            else Debug.LogWarning("  ✗ VFX Gallery UI MISSING");

            // Check VFX assets
            var vfxAssets = Resources.LoadAll<UnityEngine.VFX.VisualEffectAsset>("VFX");
            Debug.Log($"  ✓ {vfxAssets.Length} VFX assets in Resources/VFX");

            Debug.Log($"\n  Result: {passed}/{total} core components validated");
        }

        [MenuItem("H3M/Run Full Auto-Setup Now")]
        public static void RunManualSetup()
        {
            SessionState.SetBool(SETUP_DONE_KEY, false);
            RunAutoSetup();
        }

        [MenuItem("H3M/Reset Auto-Setup Flag")]
        public static void ResetSetupFlag()
        {
            SessionState.SetBool(SETUP_DONE_KEY, false);
            Debug.Log("[AutoSetup] Reset - will run on next Unity restart");
        }
    }
}
