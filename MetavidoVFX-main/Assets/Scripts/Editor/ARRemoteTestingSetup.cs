#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace XRRAI.Editor
{
    /// <summary>
    /// Setup and configuration helper for AR Foundation Remote testing.
    /// Provides menu commands to quickly configure optimal testing settings.
    /// </summary>
    public static class ARRemoteTestingSetup
    {
        [MenuItem("H3M/Testing/AR Remote/Setup Optimal Testing Config")]
        public static void SetupOptimalConfig()
        {
            Debug.Log("[AR Remote Testing] Setting up optimal configuration...");

            // 1. Enable continuous play mode
            ContinuousPlayMode.EnableContinuousPlayMode();

            // 2. Set run in background
            Application.runInBackground = true;
            PlayerSettings.runInBackground = true;

            // 3. Disable pause on focus loss
            EditorPrefs.SetBool("PauseOnFocusLost", false);

            // 4. Set target frame rate for smooth testing
            Application.targetFrameRate = 60;

            // 5. Configure quality settings for testing
            QualitySettings.vSyncCount = 0; // Disable VSync for more responsive testing

            Debug.Log("[AR Remote Testing] Configuration complete:");
            Debug.Log("  - Continuous Play Mode: Enabled");
            Debug.Log("  - Run In Background: true");
            Debug.Log("  - Pause On Focus Lost: false");
            Debug.Log("  - Target Frame Rate: 60");
            Debug.Log("  - VSync: Disabled");
            Debug.Log("");
            Debug.Log("To connect AR Foundation Remote:");
            Debug.Log("  1. Build companion app to device (Assets/Plugins/ARFoundationRemoteInstaller/Scenes/Installer)");
            Debug.Log("  2. Open AR Foundation Remote window (Window > AR Foundation Remote > Connection)");
            Debug.Log("  3. Ensure device and Editor are on same network");
            Debug.Log("  4. Enter device IP in Connection window");
            Debug.Log("  5. Press Play in Editor");
        }

        [MenuItem("H3M/Testing/AR Remote/Open AR Remote Connection Window")]
        public static void OpenARRemoteWindow()
        {
            // Try to open the AR Foundation Remote connection window
            var windowType = System.Type.GetType("ARFoundationRemote.Runtime.Sender, ARFoundationRemote.Runtime");
            if (windowType != null)
            {
                EditorWindow.GetWindow(windowType);
            }
            else
            {
                // Try alternative approach
                EditorApplication.ExecuteMenuItem("Window/AR Foundation Remote/Connection");
            }
        }

        [MenuItem("H3M/Testing/AR Remote/Quick Start Guide")]
        public static void ShowQuickStartGuide()
        {
            Debug.Log("=== AR Foundation Remote Quick Start ===");
            Debug.Log("");
            Debug.Log("FIRST TIME SETUP:");
            Debug.Log("1. Open Assets/Plugins/ARFoundationRemoteInstaller/Scenes/Installer.unity");
            Debug.Log("2. Build and run to your iOS device");
            Debug.Log("3. Note the IP address shown on device screen");
            Debug.Log("");
            Debug.Log("EACH TESTING SESSION:");
            Debug.Log("1. Launch companion app on device");
            Debug.Log("2. Window > AR Foundation Remote > Connection");
            Debug.Log("3. Enter device IP address");
            Debug.Log("4. Click 'Connect'");
            Debug.Log("5. Press Play in Editor");
            Debug.Log("");
            Debug.Log("TROUBLESHOOTING:");
            Debug.Log("- Ensure device and Mac are on same WiFi network");
            Debug.Log("- Check firewall allows connection on port 8080");
            Debug.Log("- Try restarting companion app if connection fails");
            Debug.Log("");
            Debug.Log("RUN: H3M > Testing > AR Remote > Setup Optimal Testing Config");
        }

        [MenuItem("H3M/Testing/AR Remote/Add Test Prefabs to Scene")]
        public static void AddTestPrefabs()
        {
            // Add ARDepthSource if not present
            var depthSource = Object.FindFirstObjectByType<ARDepthSource>();
            if (depthSource == null)
            {
                var go = new GameObject("ARDepthSource");
                go.AddComponent<ARDepthSource>();
                Debug.Log("[AR Remote Testing] Added ARDepthSource");
            }

            // Add VFXPipelineDashboard if not present
            var dashboard = Object.FindFirstObjectByType<VFXPipelineDashboard>();
            if (dashboard == null)
            {
                var go = new GameObject("VFX_PipelineDashboard");
                go.AddComponent<VFXPipelineDashboard>();
                Debug.Log("[AR Remote Testing] Added VFXPipelineDashboard");
            }

            // Add Property Inspector if not present
            var inspector = Object.FindFirstObjectByType<Debugging.VFXPropertyInspector>();
            if (inspector == null)
            {
                var go = new GameObject("VFX_PropertyInspector");
                go.AddComponent<Debugging.VFXPropertyInspector>();
                Debug.Log("[AR Remote Testing] Added VFXPropertyInspector");
            }

            Debug.Log("[AR Remote Testing] Test prefabs ready. Press Tab for Dashboard, F1 for Property Inspector.");
        }

        [MenuItem("H3M/Testing/AR Remote/Verify Scene Setup")]
        public static void VerifySceneSetup()
        {
            Debug.Log("[AR Remote Testing] Verifying scene setup...");

            int issues = 0;

            // Check AR Session
            var arSession = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
            if (arSession == null)
            {
                Debug.LogWarning("  [!] Missing AR Session");
                issues++;
            }
            else
            {
                Debug.Log("  [OK] AR Session found");
            }

            // Check XR Origin
            var xrOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogWarning("  [!] Missing XR Origin");
                issues++;
            }
            else
            {
                Debug.Log("  [OK] XR Origin found");
            }

            // Check AR Camera Manager
            var cameraManager = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>();
            if (cameraManager == null)
            {
                Debug.LogWarning("  [!] Missing AR Camera Manager");
                issues++;
            }
            else
            {
                Debug.Log("  [OK] AR Camera Manager found");
            }

            // Check AR Occlusion Manager
            var occlusionManager = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.AROcclusionManager>();
            if (occlusionManager == null)
            {
                Debug.LogWarning("  [!] Missing AR Occlusion Manager - depth won't work");
                issues++;
            }
            else
            {
                Debug.Log($"  [OK] AR Occlusion Manager (Depth: {occlusionManager.requestedEnvironmentDepthMode})");
            }

            // Check ARDepthSource
            var depthSource = Object.FindFirstObjectByType<ARDepthSource>();
            if (depthSource == null)
            {
                Debug.LogWarning("  [!] Missing ARDepthSource - VFX won't receive AR data");
                issues++;
            }
            else
            {
                Debug.Log("  [OK] ARDepthSource found");
            }

            // Check VFX
            var vfxCount = Object.FindObjectsByType<UnityEngine.VFX.VisualEffect>(FindObjectsSortMode.None).Length;
            Debug.Log($"  [INFO] {vfxCount} VFX in scene");

            // Check HiFi Hologram
            var hifiControllers = Object.FindObjectsByType<XRRAI.Hologram.HiFiHologramController>(FindObjectsSortMode.None);
            Debug.Log($"  [INFO] {hifiControllers.Length} HiFi Hologram Controllers");

            if (issues == 0)
            {
                Debug.Log("[AR Remote Testing] Scene setup OK!");
            }
            else
            {
                Debug.LogWarning($"[AR Remote Testing] Found {issues} issues - scene may not work correctly");
            }
        }
    }
}
#endif
