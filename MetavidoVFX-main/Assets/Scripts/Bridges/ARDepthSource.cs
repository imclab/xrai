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

    [Header("Depth Rotation (iOS Portrait)")]
    [Tooltip("Rotate depth/stencil 90° CW for iOS portrait mode. Required for Metavido-style VFX.")]
    [SerializeField] bool _rotateDepthTexture = true;

    [Header("Editor Testing")]
    [Tooltip("Use mock textures in Editor when no AR data available. Allows testing VFXARBinder without device.")]
    [SerializeField] bool _useMockDataInEditor = true;
    [SerializeField] Vector2Int _mockResolution = new Vector2Int(256, 192);

    [Header("Color Capture")]
    [SerializeField] Vector2Int _colorResolution = new Vector2Int(1920, 1080);
    [SerializeField] bool _verboseLogging = false;

    // Mock textures for Editor testing
    Texture2D _mockDepthMap;
    Texture2D _mockStencilMap;
    RenderTexture _mockPositionMap;
    bool _usingMockData;

    // Rotation resources
    Material _rotateMaterial;
    RenderTexture _rotatedDepthRT;
    RenderTexture _rotatedStencilRT;

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
    public bool UsingMockData => _usingMockData;
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

        // Create mock textures for Editor testing
        #if UNITY_EDITOR
        if (_useMockDataInEditor)
        {
            CreateMockTextures();
        }
        #endif

        if (_verboseLogging)
            Debug.Log($"[ARDepthSource] Initialized. ColorProvider={_colorProvider != null}, MockData={_useMockDataInEditor}");
    }

    #if UNITY_EDITOR
    void CreateMockTextures()
    {
        int w = _mockResolution.x;
        int h = _mockResolution.y;

        // Mock depth map - gradient from near to far (simulates depth)
        _mockDepthMap = new Texture2D(w, h, TextureFormat.RFloat, false);
        Color[] depthPixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Create a circular depth pattern (closer in center)
                float nx = (x / (float)w) * 2f - 1f;
                float ny = (y / (float)h) * 2f - 1f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float depth = Mathf.Lerp(0.5f, 3f, dist); // 0.5m to 3m
                depthPixels[y * w + x] = new Color(depth, 0, 0, 1);
            }
        }
        _mockDepthMap.SetPixels(depthPixels);
        _mockDepthMap.Apply();

        // Mock stencil map - center blob is "human"
        _mockStencilMap = new Texture2D(w, h, TextureFormat.R8, false);
        Color[] stencilPixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x / (float)w) * 2f - 1f;
                float ny = (y / (float)h) * 2f - 1f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float stencil = dist < 0.6f ? 1f : 0f; // Human in center circle
                stencilPixels[y * w + x] = new Color(stencil, stencil, stencil, 1);
            }
        }
        _mockStencilMap.SetPixels(stencilPixels);
        _mockStencilMap.Apply();

        // Mock position map - create as RenderTexture for compute shader output
        _mockPositionMap = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat);
        _mockPositionMap.enableRandomWrite = true;
        _mockPositionMap.Create();

        // Fill color map with gradient for visual feedback
        RenderTexture.active = ColorMap;
        GL.Clear(true, true, new Color(0.2f, 0.4f, 0.6f, 1f)); // Blue-ish gradient
        RenderTexture.active = null;

        Debug.Log($"[ARDepthSource] Mock textures created: {w}x{h}");
    }

    void UseMockData()
    {
        _usingMockData = true;
        float startTime = Time.realtimeSinceStartup;

        // Use mock textures
        DepthMap = _mockDepthMap;
        StencilMap = _mockStencilMap;

        // Create/resize position map if needed
        if (PositionMap == null || PositionMap.width != _mockResolution.x)
        {
            if (PositionMap != null) PositionMap.Release();
            PositionMap = new RenderTexture(_mockResolution.x, _mockResolution.y, 0, RenderTextureFormat.ARGBFloat);
            PositionMap.enableRandomWrite = true;
            PositionMap.Create();

            if (_enableVelocity)
            {
                if (_prevPositionMap != null) _prevPositionMap.Release();
                _prevPositionMap = new RenderTexture(_mockResolution.x, _mockResolution.y, 0, RenderTextureFormat.ARGBFloat);
                _prevPositionMap.Create();

                if (_velocityMap != null) _velocityMap.Release();
                _velocityMap = new RenderTexture(_mockResolution.x, _mockResolution.y, 0, RenderTextureFormat.ARGBFloat);
                _velocityMap.enableRandomWrite = true;
                _velocityMap.Create();
            }
        }

        // Compute position map from mock depth
        if (_depthToWorld != null && _arCamera != null)
        {
            var proj = _arCamera.projectionMatrix;
            float centerShiftX = proj.m02;
            float centerShiftY = proj.m12;

            float fov = _arCamera.fieldOfView * Mathf.Deg2Rad;
            float tanV = Mathf.Tan(fov * 0.5f);
            float tanH = tanV * _arCamera.aspect;

            _depthToWorld.SetTexture(_kernel, "_Depth", _mockDepthMap);
            _depthToWorld.SetTexture(_kernel, "_PositionRT", PositionMap);
            _depthToWorld.SetMatrix("_InvVP", (_arCamera.projectionMatrix * _arCamera.worldToCameraMatrix).inverse);

            int groupsX = Mathf.CeilToInt(_mockResolution.x / 32f);
            int groupsY = Mathf.CeilToInt(_mockResolution.y / 32f);
            _depthToWorld.Dispatch(_kernel, groupsX, groupsY, 1);

            // Velocity calculation
            if (_enableVelocity && _prevPositionMap != null && _velocityMap != null)
            {
                _depthToWorld.SetTexture(_velocityKernel, "_PositionRT", PositionMap);
                _depthToWorld.SetTexture(_velocityKernel, "_PreviousPositionRT", _prevPositionMap);
                _depthToWorld.SetTexture(_velocityKernel, "_VelocityRT", _velocityMap);
                _depthToWorld.SetFloat("_DeltaTime", Time.deltaTime);
                _depthToWorld.Dispatch(_velocityKernel, groupsX, groupsY, 1);
                Graphics.Blit(PositionMap, _prevPositionMap);
            }

            RayParams = new Vector4(centerShiftX, centerShiftY, tanH, tanV);
            InverseView = _arCamera.cameraToWorldMatrix;

            Shader.SetGlobalVector("_ARRayParams", RayParams);
            Shader.SetGlobalMatrix("_ARInverseView", InverseView);
        }

        LastComputeTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f;

        if (_verboseLogging && Time.frameCount % 60 == 0)
            Debug.Log($"[ARDepthSource] Using MOCK data: {_mockResolution.x}x{_mockResolution.y}, ComputeTime={LastComputeTimeMs:F2}ms");
    }
    #endif

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

    /// <summary>
    /// Rotates a texture 90° CW using the RotateUV90CW shader (swaps width/height)
    /// </summary>
    Texture RotateTexture(Texture source, ref RenderTexture rotatedRT)
    {
        if (source == null) return null;

        // Initialize rotation material
        if (_rotateMaterial == null)
        {
            var shader = Shader.Find("Hidden/RotateUV90CW");
            if (shader == null)
            {
                if (_verboseLogging)
                    Debug.LogWarning("[ARDepthSource] RotateUV90CW shader not found, using unrotated texture");
                return source;
            }
            _rotateMaterial = new Material(shader);
        }

        // Rotated dimensions (swap width/height)
        int rotW = source.height;
        int rotH = source.width;

        // Create or resize RT
        if (rotatedRT == null || rotatedRT.width != rotW || rotatedRT.height != rotH)
        {
            if (rotatedRT != null) rotatedRT.Release();
            rotatedRT = new RenderTexture(rotW, rotH, 0, RenderTextureFormat.RFloat);
            rotatedRT.filterMode = FilterMode.Point;
            rotatedRT.Create();
        }

        Graphics.Blit(source, rotatedRT, _rotateMaterial);
        return rotatedRT;
    }

    void LateUpdate()
    {
        // Prefer human depth for body VFX, fall back to environment depth
        Texture depth = null;
        if (_preferHumanDepth)
            depth = _occlusion?.humanDepthTexture ?? _occlusion?.environmentDepthTexture;
        else
            depth = _occlusion?.environmentDepthTexture ?? _occlusion?.humanDepthTexture;

        Texture stencil = _occlusion?.humanStencilTexture;

        #if UNITY_EDITOR
        // Use mock data in Editor when no AR data available
        if (depth == null && _useMockDataInEditor && _mockDepthMap != null)
        {
            UseMockData();
            return;
        }
        #endif

        if (depth == null)
        {
            _usingMockData = false;
            // Throttle warning to once per second (not every frame)
            if (_verboseLogging && Time.frameCount % 60 == 0)
                Debug.LogWarning($"[ARDepthSource] No depth available. HumanDepth={_occlusion?.humanDepthTexture != null}, EnvDepth={_occlusion?.environmentDepthTexture != null}");
            return;
        }

        _usingMockData = false;

        // Apply rotation if enabled (ARKit depth is landscape, VFX expects portrait)
        if (_rotateDepthTexture)
        {
            depth = RotateTexture(depth, ref _rotatedDepthRT);
            if (stencil != null)
                stencil = RotateTexture(stencil, ref _rotatedStencilRT);
        }

        if (_verboseLogging && Time.frameCount % 60 == 0)
            Debug.Log($"[ARDepthSource] Using depth: {depth.width}x{depth.height} (rotated={_rotateDepthTexture})");

        float startTime = Time.realtimeSinceStartup;

        // Resize if needed (use rotated dimensions)
        if (PositionMap == null || PositionMap.width != depth.width || PositionMap.height != depth.height)
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

        // Compute RayParams - extract principal point offset from projection matrix
        var proj = _arCamera.projectionMatrix;
        float centerShiftX = proj.m02;
        float centerShiftY = proj.m12;

        float fov = _arCamera.fieldOfView * Mathf.Deg2Rad;
        float tanV = Mathf.Tan(fov * 0.5f);
        float tanH;

        if (_rotateDepthTexture)
        {
            // Use depth texture aspect (after rotation) and negate tanH for correct orientation
            float depthAspect = (float)depth.width / depth.height;
            tanH = tanV * depthAspect;
            RayParams = new Vector4(centerShiftX, centerShiftY, -tanH, tanV);
        }
        else
        {
            tanH = tanV * _arCamera.aspect;
            RayParams = new Vector4(centerShiftX, centerShiftY, tanH, tanV);
        }

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
        StencilMap = stencil ?? Texture2D.whiteTexture;
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

        // Rotation cleanup
        if (_rotatedDepthRT != null) _rotatedDepthRT.Release();
        if (_rotatedStencilRT != null) _rotatedStencilRT.Release();
        if (_rotateMaterial != null)
        {
            if (Application.isPlaying) Destroy(_rotateMaterial);
            else DestroyImmediate(_rotateMaterial);
        }

        #if UNITY_EDITOR
        if (_mockDepthMap != null) Destroy(_mockDepthMap);
        if (_mockStencilMap != null) Destroy(_mockStencilMap);
        if (_mockPositionMap != null) _mockPositionMap.Release();
        #endif
    }
    
    [ContextMenu("Debug Source")]
    void DebugSource()
    {
        Debug.Log($"=== ARDepthSource Debug ===");
        Debug.Log($"IsReady: {IsReady}");
        Debug.Log($"UsingMockData: {_usingMockData}");
        Debug.Log($"PreferHumanDepth: {_preferHumanDepth}");
        Debug.Log($"RotateDepthTexture: {_rotateDepthTexture}");
        Debug.Log($"HumanDepthTexture: {_occlusion?.humanDepthTexture} ({_occlusion?.humanDepthTexture?.width}x{_occlusion?.humanDepthTexture?.height})");
        Debug.Log($"EnvironmentDepthTexture: {_occlusion?.environmentDepthTexture} ({_occlusion?.environmentDepthTexture?.width}x{_occlusion?.environmentDepthTexture?.height})");
        Debug.Log($"DepthMap (selected): {DepthMap} ({DepthMap?.width}x{DepthMap?.height})");
        Debug.Log($"StencilMap: {StencilMap} ({StencilMap?.width}x{StencilMap?.height})");
        Debug.Log($"PositionMap: {PositionMap} ({PositionMap?.width}x{PositionMap?.height})");
        Debug.Log($"VelocityMap: {VelocityMap} ({VelocityMap?.width}x{VelocityMap?.height})");
        Debug.Log($"ColorMap: {ColorMap} ({ColorMap?.width}x{ColorMap?.height})");
        Debug.Log($"RayParams: {RayParams}");
        Debug.Log($"InverseView: {InverseView}");
        Debug.Log($"ComputeTime: {LastComputeTimeMs:F2}ms");
        #if UNITY_EDITOR
        Debug.Log($"MockDataEnabled: {_useMockDataInEditor}");
        Debug.Log($"MockDepthMap: {_mockDepthMap} ({_mockDepthMap?.width}x{_mockDepthMap?.height})");
        #endif
    }

    [ContextMenu("Enable Verbose Logging")]
    void EnableVerboseLogging()
    {
        _verboseLogging = true;
        Debug.Log("[ARDepthSource] Verbose logging enabled");
    }
}
