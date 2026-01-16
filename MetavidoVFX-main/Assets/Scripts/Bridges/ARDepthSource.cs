using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Rendering;

/// <summary>
/// Single compute dispatch, holds textures for binders.
/// Does NOT set globals for textures (VFX Graph can't read them).
/// </summary>
[DefaultExecutionOrder(-100)]
public class ARDepthSource : MonoBehaviour
{
    public static ARDepthSource Instance { get; private set; }

    [SerializeField] AROcclusionManager _occlusion;
    [SerializeField] ARCameraManager _cameraManager;
    [SerializeField] ComputeShader _depthToWorld;
    [SerializeField] Camera _arCamera;

    [Header("Depth Source")]
    [Tooltip("Prefer human depth (body segmentation) over environment depth (LiDAR). Best for people VFX.")]
    [SerializeField] bool _preferHumanDepth = true;

    [Header("Color Capture")]
    [SerializeField] Vector2Int _colorResolution = new Vector2Int(1920, 1080);
    [SerializeField] bool _verboseLogging = false;

    // Cached camera textures from frameReceived
    Texture _lastCameraTexture;
    Texture _lastCbCrTexture;
    bool _colorCaptured;

    // Optional: Use ARCameraTextureProvider for better color (Keijiro's H3M approach)
    [Header("Color Source (Optional)")]
    [Tooltip("Use ARCameraTextureProvider for proper YCbCr→RGB conversion. Best for iOS.")]
    [SerializeField] Metavido.ARCameraTextureProvider _colorProvider;

    // PUBLIC - Binders read these
    public Texture DepthMap { get; private set; }
    public Texture StencilMap { get; private set; }
    public RenderTexture PositionMap { get; private set; }
    public RenderTexture ColorMap { get; private set; }
    public Vector4 RayParams { get; private set; }
    public Matrix4x4 InverseView { get; private set; }

    // Status for Dashboard
    public bool IsReady => DepthMap != null && PositionMap != null;
    public float LastComputeTimeMs { get; private set; }

    // Velocity support
    [SerializeField] bool _enableVelocity = true;
    RenderTexture _prevPositionMap;
    RenderTexture _velocityMap;
    public RenderTexture VelocityMap => _velocityMap;

    int _kernel;
    int _velocityKernel;

    void Awake() => Instance = this;

    void Start()
    {
        _occlusion ??= FindFirstObjectByType<AROcclusionManager>();
        _cameraManager ??= FindFirstObjectByType<ARCameraManager>();
        _arCamera ??= Camera.main;
        _depthToWorld ??= Resources.Load<ComputeShader>("DepthToWorld");
        _colorProvider ??= FindFirstObjectByType<Metavido.ARCameraTextureProvider>();

        if (_depthToWorld != null)
        {
            _kernel = _depthToWorld.FindKernel("DepthToWorld");
            _velocityKernel = _depthToWorld.FindKernel("CalculateVelocity");
        }

        // Create color capture RenderTexture (fallback if no colorProvider)
        ColorMap = new RenderTexture(_colorResolution.x, _colorResolution.y, 0, RenderTextureFormat.ARGB32);
        ColorMap.Create();

        if (_verboseLogging)
            Debug.Log($"[ARDepthSource] Initialized. ColorProvider={_colorProvider != null}");
    }

    void OnEnable()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived += OnCameraFrameReceived;

        // URP: Use RenderPipelineManager to capture after camera renders
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived -= OnCameraFrameReceived;

        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // Only capture from AR camera
        if (camera != _arCamera || ColorMap == null) return;
        if (_colorCaptured) return; // Already captured this frame

        // Priority 1: Use ARCameraTextureProvider if available (best - proper YCbCr→RGB)
        if (_colorProvider != null && _colorProvider.Texture != null)
        {
            var providerTex = _colorProvider.Texture;
            if (providerTex.width > 0 && providerTex.height > 0)
            {
                Graphics.Blit(providerTex, ColorMap);
                _colorCaptured = true;
                if (_verboseLogging && Time.frameCount % 60 == 0)
                    Debug.Log($"[ARDepthSource] Color from ARCameraTextureProvider: {providerTex.width}x{providerTex.height}");
                return;
            }
        }

        // Priority 2: Capture from camera's rendered output
        var src = camera.activeTexture;
        if (src != null && src.width > 0 && src.height > 0)
        {
            Graphics.Blit(src, ColorMap);
            _colorCaptured = true;
            if (_verboseLogging && Time.frameCount % 60 == 0)
                Debug.Log($"[ARDepthSource] Color from activeTexture: {src.width}x{src.height}");
            return;
        }

        // Priority 3: Camera's target texture
        if (camera.targetTexture != null && camera.targetTexture.width > 0)
        {
            Graphics.Blit(camera.targetTexture, ColorMap);
            _colorCaptured = true;
            if (_verboseLogging && Time.frameCount % 60 == 0)
                Debug.Log($"[ARDepthSource] Color from targetTexture");
            return;
        }

        // NOTE: Do NOT use frameReceived textures for ColorMap!
        // On iOS, textures[0] is Y (luma only), textures[1] is CbCr (chroma).
        // These require YCbCr→RGB conversion which ARCameraTextureProvider handles.
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        // Cache textures for fallback - on iOS: [0]=Y (luma), [1]=CbCr (chroma)
        if (args.textures.Count > 0)
            _lastCameraTexture = args.textures[0];
        if (args.textures.Count > 1)
            _lastCbCrTexture = args.textures[1];

        // Reset capture flag each frame to allow re-capture
        _colorCaptured = false;
    }

    void LateUpdate()
    {
        // Prefer human depth for body VFX, fall back to environment depth
        Texture depth = null;
        if (_preferHumanDepth)
            depth = _occlusion?.humanDepthTexture ?? _occlusion?.environmentDepthTexture;
        else
            depth = _occlusion?.environmentDepthTexture ?? _occlusion?.humanDepthTexture;

        if (depth == null)
        {
            if (_verboseLogging)
                Debug.LogWarning($"[ARDepthSource] No depth available. HumanDepth={_occlusion?.humanDepthTexture != null}, EnvDepth={_occlusion?.environmentDepthTexture != null}");
            return;
        }

        if (_verboseLogging && Time.frameCount % 60 == 0)
            Debug.Log($"[ARDepthSource] Using depth: {depth.width}x{depth.height} (human={_occlusion?.humanDepthTexture == depth})");

        float startTime = Time.realtimeSinceStartup;

        // Resize if needed
        if (PositionMap == null || PositionMap.width != depth.width)
        {
            // Use explicit null check - Unity's ?. doesn't respect destroyed objects
            if (PositionMap != null) PositionMap.Release();
            PositionMap = new RenderTexture(depth.width, depth.height, 0, RenderTextureFormat.ARGBFloat);
            PositionMap.enableRandomWrite = true;
            PositionMap.Create();

            if (_enableVelocity)
            {
                if (_prevPositionMap != null) _prevPositionMap.Release();
                _prevPositionMap = new RenderTexture(depth.width, depth.height, 0, RenderTextureFormat.ARGBFloat);
                _prevPositionMap.Create();

                if (_velocityMap != null) _velocityMap.Release();
                _velocityMap = new RenderTexture(depth.width, depth.height, 0, RenderTextureFormat.ARGBFloat);
                _velocityMap.enableRandomWrite = true;
                _velocityMap.Create();
            }
        }

        // Compute RayParams
        float fov = _arCamera.fieldOfView * Mathf.Deg2Rad;
        float h = Mathf.Tan(fov * 0.5f);
        float w = h * _arCamera.aspect;

        // SINGLE dispatch for ALL VFX
        _depthToWorld.SetTexture(_kernel, "_Depth", depth);
        _depthToWorld.SetTexture(_kernel, "_PositionRT", PositionMap);
        _depthToWorld.SetMatrix("_InvVP", (_arCamera.projectionMatrix * _arCamera.worldToCameraMatrix).inverse);
        
        int groupsX = Mathf.CeilToInt(depth.width / 32f);
        int groupsY = Mathf.CeilToInt(depth.height / 32f);
        _depthToWorld.Dispatch(_kernel, groupsX, groupsY, 1);

        // Velocity calculation
        if (_enableVelocity && _prevPositionMap != null && _velocityMap != null)
        {
            _depthToWorld.SetTexture(_velocityKernel, "_PositionRT", PositionMap);
            _depthToWorld.SetTexture(_velocityKernel, "_PreviousPositionRT", _prevPositionMap);
            _depthToWorld.SetTexture(_velocityKernel, "_VelocityRT", _velocityMap);
            _depthToWorld.SetFloat("_DeltaTime", Time.deltaTime);
            _depthToWorld.Dispatch(_velocityKernel, groupsX, groupsY, 1);

            // Swap buffers
            Graphics.Blit(PositionMap, _prevPositionMap);
        }

        // Cache for binders (NOT global textures - they don't work for VFX!)
        DepthMap = depth;
        StencilMap = _occlusion.humanStencilTexture ?? Texture2D.whiteTexture;
        RayParams = new Vector4(0, 0, w, h);
        InverseView = _arCamera.cameraToWorldMatrix;

        // Vectors/matrices CAN be global (VFX reads via HLSL)
        Shader.SetGlobalVector("_ARRayParams", RayParams);
        Shader.SetGlobalMatrix("_ARInverseView", InverseView);

        // Measure compute time for Dashboard
        LastComputeTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f;
    }

    void OnDestroy()
    {
        if (PositionMap != null) PositionMap.Release();
        if (_prevPositionMap != null) _prevPositionMap.Release();
        if (_velocityMap != null) _velocityMap.Release();
        if (ColorMap != null) ColorMap.Release();
    }
    
    [ContextMenu("Debug Source")]
    void DebugSource()
    {
        Debug.Log($"=== ARDepthSource Debug ===");
        Debug.Log($"PreferHumanDepth: {_preferHumanDepth}");
        Debug.Log($"HumanDepthTexture: {_occlusion?.humanDepthTexture} ({_occlusion?.humanDepthTexture?.width}x{_occlusion?.humanDepthTexture?.height})");
        Debug.Log($"EnvironmentDepthTexture: {_occlusion?.environmentDepthTexture} ({_occlusion?.environmentDepthTexture?.width}x{_occlusion?.environmentDepthTexture?.height})");
        Debug.Log($"DepthMap (selected): {DepthMap} ({DepthMap?.width}x{DepthMap?.height})");
        Debug.Log($"StencilMap: {StencilMap} ({StencilMap?.width}x{StencilMap?.height})");
        Debug.Log($"PositionMap: {PositionMap} ({PositionMap?.width}x{PositionMap?.height})");
        Debug.Log($"ColorMap: {ColorMap} ({ColorMap?.width}x{ColorMap?.height})");
        Debug.Log($"RayParams: {RayParams}");
        Debug.Log($"IsReady: {IsReady}");
        Debug.Log($"ARCameraManager: {_cameraManager != null}");
        Debug.Log($"ARCamera: {_arCamera?.name}");
        Debug.Log($"ARCamera activeTexture: {_arCamera?.activeTexture}");
        Debug.Log($"ARCamera targetTexture: {_arCamera?.targetTexture}");
        Debug.Log($"Last frameReceived texture: {_lastCameraTexture} ({_lastCameraTexture?.width}x{_lastCameraTexture?.height})");
        Debug.Log($"Color captured this frame: {_colorCaptured}");
    }

    [ContextMenu("Enable Verbose Logging")]
    void EnableVerboseLogging()
    {
        _verboseLogging = true;
        Debug.Log("[ARDepthSource] Verbose logging enabled");
    }
}
