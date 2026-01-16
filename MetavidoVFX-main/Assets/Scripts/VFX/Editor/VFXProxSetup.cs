// VFXProx Setup Editor
// Quick setup for VFXProxBuffer component

using UnityEngine;
using UnityEditor;

namespace MetavidoVFX.VFX.Editor
{
    public static class VFXProxSetup
    {
        [MenuItem("H3M/VFX/Setup VFXProx Buffer", priority = 200)]
        public static void SetupVFXProxBuffer()
        {
            // Check if VFXProxBuffer already exists
            var existing = Object.FindFirstObjectByType<VFXProxBuffer>();
            if (existing != null)
            {
                Debug.Log($"[VFXProxSetup] VFXProxBuffer already exists on '{existing.gameObject.name}'");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create new GameObject with VFXProxBuffer
            var go = new GameObject("VFXProxBuffer");
            var buffer = go.AddComponent<VFXProxBuffer>();

            // Set default extent to cover typical AR scene
            buffer.Extent = new Vector3(4f, 4f, 4f);

            Undo.RegisterCreatedObjectUndo(go, "Create VFXProxBuffer");
            Selection.activeGameObject = go;

            Debug.Log("[VFXProxSetup] Created VFXProxBuffer. This is required for plexus VFX with neighbor lookups.");
        }

        [MenuItem("H3M/VFX/Validate VFXProx Setup", priority = 201)]
        public static void ValidateVFXProxSetup()
        {
            // Check for VFXProxBuffer
            var buffer = Object.FindFirstObjectByType<VFXProxBuffer>();
            if (buffer == null)
            {
                Debug.LogWarning("[VFXProxSetup] No VFXProxBuffer found. Plexus VFX will show 'VFXProx_CountBuffer not set' errors.");
                return;
            }

            // Check for compute shader
            var compute = Resources.Load<ComputeShader>("VFXProxClear");
            if (compute == null)
            {
                Debug.LogWarning("[VFXProxSetup] VFXProxClear.compute not found in Resources folder.");
                return;
            }

            // Check for VFXProxCommon.hlsl
            var hlslPath = "Assets/Shaders/VFXProxCommon.hlsl";
            if (!System.IO.File.Exists(hlslPath))
            {
                Debug.LogWarning($"[VFXProxSetup] {hlslPath} not found. Plexus VFX may not compile.");
                return;
            }

            Debug.Log("[VFXProxSetup] VFXProx setup is valid!");
        }
    }
}
