// AROcclusionDebugSetup - Editor utilities for AR occlusion debugging

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace MetavidoVFX.Debugging.Editor
{
    public static class AROcclusionDebugSetup
    {
        [MenuItem("H3M/Debug/Add Occlusion Debug Controller")]
        public static void AddOcclusionDebugController()
        {
            // Check if already exists
            var existing = Object.FindFirstObjectByType<AROcclusionDebugController>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("[AROcclusionDebug] Controller already exists in scene.");
                return;
            }

            // Create new GameObject with controller
            var go = new GameObject("AROcclusionDebugController");
            var controller = go.AddComponent<AROcclusionDebugController>();
            Undo.RegisterCreatedObjectUndo(go, "Add AR Occlusion Debug Controller");

            Selection.activeGameObject = go;
            Debug.Log("[AROcclusionDebug] Created AR Occlusion Debug Controller. Keyboard shortcuts: O=Mesh, P=Depth, F=VFX Foreground");
        }

        [MenuItem("H3M/Debug/Disable All Occlusion (Quick)")]
        public static void DisableAllOcclusion()
        {
            var controller = Object.FindFirstObjectByType<AROcclusionDebugController>();
            if (controller == null)
            {
                AddOcclusionDebugController();
                controller = Object.FindFirstObjectByType<AROcclusionDebugController>();
            }

            controller?.DisableAllOcclusion();
            Debug.Log("[AROcclusionDebug] All occlusion disabled for VFX debugging.");
        }

        [MenuItem("H3M/Debug/Enable All Occlusion (Quick)")]
        public static void EnableAllOcclusion()
        {
            var controller = Object.FindFirstObjectByType<AROcclusionDebugController>();
            if (controller != null)
            {
                controller.EnableAllOcclusion();
                Debug.Log("[AROcclusionDebug] All occlusion re-enabled.");
            }
        }

        [MenuItem("H3M/Debug/Toggle Mesh Occlusion")]
        public static void ToggleMeshOcclusion()
        {
            // Try to find ARMeshManager directly
            var meshManager = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARMeshManager>();
            if (meshManager != null)
            {
                meshManager.enabled = !meshManager.enabled;

                // Also toggle mesh renderers
                foreach (var filter in meshManager.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (renderer != null)
                        renderer.enabled = meshManager.enabled;
                }

                string status = meshManager.enabled ? "ENABLED" : "DISABLED";
                Debug.Log($"[AROcclusionDebug] Mesh Occlusion: {status}");
            }
            else
            {
                Debug.LogWarning("[AROcclusionDebug] No ARMeshManager found in scene.");
            }
        }

        [MenuItem("H3M/Debug/Toggle Depth Occlusion")]
        public static void ToggleDepthOcclusion()
        {
            var occlusionManager = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.AROcclusionManager>();
            if (occlusionManager != null)
            {
                // Toggle between NoOcclusion and PreferEnvironmentOcclusion
                bool isDisabled = occlusionManager.requestedOcclusionPreferenceMode ==
                    UnityEngine.XR.ARSubsystems.OcclusionPreferenceMode.NoOcclusion;

                if (isDisabled)
                {
                    occlusionManager.requestedOcclusionPreferenceMode =
                        UnityEngine.XR.ARSubsystems.OcclusionPreferenceMode.PreferEnvironmentOcclusion;
                    occlusionManager.requestedEnvironmentDepthMode =
                        UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Best;
                    Debug.Log("[AROcclusionDebug] Depth Occlusion: ENABLED");
                }
                else
                {
                    occlusionManager.requestedOcclusionPreferenceMode =
                        UnityEngine.XR.ARSubsystems.OcclusionPreferenceMode.NoOcclusion;
                    Debug.Log("[AROcclusionDebug] Depth Occlusion: DISABLED");
                }
            }
            else
            {
                Debug.LogWarning("[AROcclusionDebug] No AROcclusionManager found in scene.");
            }
        }
    }
}
#endif
