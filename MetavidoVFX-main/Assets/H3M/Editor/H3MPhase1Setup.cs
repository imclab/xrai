#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using Unity.XR.CoreUtils;
using H3M.Core;

namespace H3M.Editor
{
    public static class H3MPhase1Setup
    {
        [MenuItem("H3M/Phase 1 Hologram/Validate Scene Setup")]
        public static void ValidateSceneSetup()
        {
            Debug.Log("[H3M Phase1] Validating scene setup...");

            // Check AR Session
            var arSession = Object.FindFirstObjectByType<ARSession>();
            Log("AR Session", arSession != null);

            // Check XR Origin (formerly AR Session Origin)
            var xrOrigin = Object.FindFirstObjectByType<XROrigin>();
            Log("XR Origin", xrOrigin != null);

            // Check AR Camera Manager
            var arCameraManager = Object.FindFirstObjectByType<ARCameraManager>();
            Log("AR Camera Manager", arCameraManager != null);

            // Check AR Occlusion Manager
            var arOcclusionManager = Object.FindFirstObjectByType<AROcclusionManager>();
            Log("AR Occlusion Manager", arOcclusionManager != null);

            if (arOcclusionManager != null)
            {
                Debug.Log($"  - Environment Depth Mode: {arOcclusionManager.requestedEnvironmentDepthMode}");
                Debug.Log($"  - Human Stencil Mode: {arOcclusionManager.requestedHumanStencilMode}");
                Debug.Log($"  - Human Depth Mode: {arOcclusionManager.requestedHumanDepthMode}");
            }

            // Check HologramSource
            var hologramSource = Object.FindFirstObjectByType<HologramSource>();
            Log("Hologram Source", hologramSource != null);

            // Check HologramRenderer
            var hologramRenderer = Object.FindFirstObjectByType<HologramRenderer>();
            Log("Hologram Renderer", hologramRenderer != null);

            // Check VFX
            var vfx = Object.FindFirstObjectByType<VisualEffect>();
            Log("Visual Effect", vfx != null);
            if (vfx != null)
            {
                Debug.Log($"  - VFX Asset: {(vfx.visualEffectAsset != null ? vfx.visualEffectAsset.name : "MISSING")}");
            }

            Debug.Log("[H3M Phase1] Validation complete.");
        }

        [MenuItem("H3M/Phase 1 Hologram/Configure for LiDAR")]
        public static void ConfigureForLiDAR()
        {
            var arOcclusionManager = Object.FindFirstObjectByType<AROcclusionManager>();
            if (arOcclusionManager == null)
            {
                Debug.LogError("[H3M Phase1] No AROcclusionManager found!");
                return;
            }

            arOcclusionManager.requestedEnvironmentDepthMode = UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Best;
            Debug.Log("[H3M Phase1] Configured for LiDAR environment depth (Best mode)");
            EditorUtility.SetDirty(arOcclusionManager);
        }

        [MenuItem("H3M/Phase 1 Hologram/Open H3M_Mirror_MVP Scene")]
        public static void OpenH3MScene()
        {
            string scenePath = "Assets/Scenes/H3M_Mirror_MVP.unity";
            if (System.IO.File.Exists(Application.dataPath.Replace("Assets", "") + scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log("[H3M Phase1] Opened H3M_Mirror_MVP scene");
            }
            else
            {
                Debug.LogError("[H3M Phase1] Scene not found: " + scenePath);
            }
        }

        [MenuItem("H3M/Phase 1 Hologram/Open Rcam4 Scene")]
        public static void OpenRcam4Scene()
        {
            string scenePath = "Assets/Scenes/Rcam4Scene.unity";
            if (System.IO.File.Exists(Application.dataPath.Replace("Assets", "") + scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log("[H3M Phase1] Opened Rcam4 scene");
            }
            else
            {
                Debug.LogError("[H3M Phase1] Scene not found: " + scenePath);
            }
        }

        [MenuItem("H3M/Phase 1 Hologram/Build iOS")]
        public static void BuildIOS()
        {
            HologramBuilder.BuildIOS();
        }

        static void Log(string component, bool found)
        {
            string status = found ? "✓" : "✗ MISSING";
            Debug.Log($"  [{status}] {component}");
        }
    }
}
#endif
// Force reimport Mon Jan 12 17:46:55 EST 2026
