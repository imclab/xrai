using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;

namespace H3M.Core
{
    /// <summary>
    /// Minimal ARKit Human Depth → VFX binding (20 lines of core logic)
    /// Based on YoHana19/HumanParticleEffect pattern
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    public class SimpleHumanHologram : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] AROcclusionManager occlusionManager;
        [SerializeField] Camera arCamera;

        [Header("Options")]
        [SerializeField] bool useHumanDepth = true;  // Person segmentation
        [SerializeField] bool useLiDARDepth = true;  // Full environment (fallback)

        VisualEffect vfx;

        void Start()
        {
            vfx = GetComponent<VisualEffect>();
            if (arCamera == null) arCamera = Camera.main;
        }

        void Update()
        {
            if (vfx == null || occlusionManager == null) return;

            // Get depth texture - prefer human (segmented) over environment (full scene)
            Texture depth = null;
            if (useHumanDepth) depth = occlusionManager.humanDepthTexture;
            if (depth == null && useLiDARDepth) depth = occlusionManager.environmentDepthTexture;

            if (depth == null) return;

            // === CORE BINDING (20 lines) ===

            // 1. Depth texture
            vfx.SetTexture("DepthMap", depth);

            // 2. Camera matrix for world reconstruction
            vfx.SetMatrix4x4("InverseView", arCamera.cameraToWorldMatrix);

            // 3. Ray params for unprojection (xy=offset, zw=scale)
            float fov = arCamera.fieldOfView * Mathf.Deg2Rad;
            float h = Mathf.Tan(fov * 0.5f);
            float w = h * arCamera.aspect;
            vfx.SetVector4("RayParams", new Vector4(0, 0, w, h));

            // 4. Depth range
            vfx.SetVector2("DepthRange", new Vector2(0.1f, 5f));

            // 5. Enable spawning
            vfx.SetBool("Spawn", true);

            // === END CORE BINDING ===
        }

        void OnGUI()
        {
            // Simple debug overlay
            var depth = occlusionManager?.humanDepthTexture ?? occlusionManager?.environmentDepthTexture;
            string status = depth != null ? $"Depth: {depth.width}x{depth.height}" : "No depth";
            string particles = vfx != null ? $"Particles: {vfx.aliveParticleCount}" : "No VFX";

            GUI.Label(new Rect(10, 10, 300, 25), $"[H3M] {status} | {particles}");
        }
    }
}
