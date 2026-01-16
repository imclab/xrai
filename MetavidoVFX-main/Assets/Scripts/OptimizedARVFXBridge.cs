using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Lightweight AR → VFX bridge with optional GPU depth-to-world processing and adaptive resolution.
/// Safe defaults: forwards occlusion textures + camera matrices; compute shader is optional.
/// </summary>
[RequireComponent(typeof(AROcclusionManager))]
[RequireComponent(typeof(ARCameraManager))]
public sealed class OptimizedARVFXBridge : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private VisualEffect vfx;
    [SerializeField] private ComputeShader depthProcessor; // Optional. Kernel name: "DepthToWorld" (_Depth, _Stencil, _PositionRT, _InvVP)

    [Header("Performance")]
    [SerializeField] private bool adaptiveResolution = true;
    [SerializeField] private Vector2Int baseResolution = new Vector2Int(512, 512);
    [SerializeField] private int targetFPS = 60;

    [Header("Texture Property Names")]
    [SerializeField] private string depthTextureProperty = "DepthMap";
    [SerializeField] private string stencilTextureProperty = "StencilMap";
    [SerializeField] private string positionTextureProperty = "PositionMap";

    [Header("Camera Property Names")]
    [SerializeField] private string invViewMatrixProperty = "InverseView";
    [SerializeField] private string invProjMatrixProperty = "InverseProj";
    [SerializeField] private string depthRangeProperty = "DepthRange";

    // Components
    private AROcclusionManager occlusionManager;
    private ARCameraManager cameraManager;

    // GPU resources
    private RenderTexture positionRT;

    // Compute
    private int depthKernel = -1;
    private uint threadSizeX = 32;
    private uint threadSizeY = 32;
    private float smoothedDelta;

    private void Awake()
    {
        occlusionManager = GetComponent<AROcclusionManager>();
        cameraManager = GetComponent<ARCameraManager>();

        if (depthProcessor != null)
        {
            if (depthProcessor.HasKernel("DepthToWorld"))
            {
                depthKernel = depthProcessor.FindKernel("DepthToWorld");
                // Query actual thread group sizes from compute shader (avoids hardcoded values)
                uint z;
                depthProcessor.GetKernelThreadGroupSizes(depthKernel, out threadSizeX, out threadSizeY, out z);
            }
            else
            {
                Debug.LogWarning("OptimizedARVFXBridge: Compute shader missing 'DepthToWorld' kernel. Compute path disabled.");
                depthProcessor = null;
            }
        }

        QualitySettings.vSyncCount = 0; // avoid editor/device vsync throttling during perf testing
        Application.targetFrameRate = targetFPS;
    }

    private void OnEnable()
    {
        AllocatePositionRT(baseResolution);
    }

    private void OnDisable()
    {
        ReleaseRT(ref positionRT);
    }

    private void Update()
    {
        // Adaptive resolution based on FPS (simple hysteresis)
        if (adaptiveResolution)
        {
            smoothedDelta += (Time.unscaledDeltaTime - smoothedDelta) * 0.1f;
            float fps = 1f / Mathf.Max(smoothedDelta, 0.0001f);
            var target = baseResolution;
            if (fps < targetFPS * 0.8f) target = new Vector2Int(Mathf.Max(256, baseResolution.x / 2), Mathf.Max(256, baseResolution.y / 2));
            else if (fps > targetFPS * 0.95f) target = baseResolution;
            AllocatePositionRT(target);
        }

        var depthTex = occlusionManager.environmentDepthTexture;
        if (depthTex == null)
            depthTex = occlusionManager.humanDepthTexture;

        var stencilTex = occlusionManager.humanStencilTexture;

        if (vfx == null)
            return;

        // Forward raw depth/stencil
        if (depthTex != null && vfx.HasTexture(depthTextureProperty))
            vfx.SetTexture(depthTextureProperty, depthTex);
        if (stencilTex != null && vfx.HasTexture(stencilTextureProperty))
            vfx.SetTexture(stencilTextureProperty, stencilTex);

        // Compute depth-to-world if shader provided
        if (depthProcessor != null && depthTex != null && positionRT != null)
        {
            var cam = cameraManager.TryGetComponent(out Camera camComp) ? camComp : null;
            if (cam != null)
            {
                var invVP = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;
                depthProcessor.SetMatrix("_InvVP", invVP);
            }

            depthProcessor.SetTexture(depthKernel, "_Depth", depthTex);
            if (stencilTex != null)
                depthProcessor.SetTexture(depthKernel, "_Stencil", stencilTex);
            depthProcessor.SetInt("_UseStencil", stencilTex != null ? 1 : 0);
            depthProcessor.SetTexture(depthKernel, "_PositionRT", positionRT);

            // Use dynamically queried thread group sizes (matches compute shader [numthreads])
            int tgX = Mathf.CeilToInt(positionRT.width / (float)threadSizeX);
            int tgY = Mathf.CeilToInt(positionRT.height / (float)threadSizeY);
            depthProcessor.Dispatch(depthKernel, tgX, tgY, 1);

            if (vfx.HasTexture(positionTextureProperty))
                vfx.SetTexture(positionTextureProperty, positionRT);
        }

        // Camera matrices/range
        if (cameraManager.TryGetComponent(out Camera camera))
        {
            if (vfx.HasMatrix4x4(invViewMatrixProperty))
                vfx.SetMatrix4x4(invViewMatrixProperty, camera.cameraToWorldMatrix);
            if (vfx.HasMatrix4x4(invProjMatrixProperty))
                vfx.SetMatrix4x4(invProjMatrixProperty, camera.projectionMatrix.inverse);
            if (vfx.HasVector2(depthRangeProperty))
                vfx.SetVector2(depthRangeProperty, new Vector2(0.1f, 10f));
        }
    }

    private void AllocatePositionRT(Vector2Int size)
    {
        if (size.x <= 0 || size.y <= 0) size = baseResolution;
        bool needsNew = positionRT == null || positionRT.width != size.x || positionRT.height != size.y;
        if (!needsNew) return;
        ReleaseRT(ref positionRT);
        positionRT = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        positionRT.Create();
    }

    private void ReleaseRT(ref RenderTexture rt)
    {
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
            rt = null;
        }
    }
}
