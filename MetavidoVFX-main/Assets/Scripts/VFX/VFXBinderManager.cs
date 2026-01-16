// VFX Binder Manager - Unified data binding for all VFX types
// Handles People, Face, Hands, Environment, Audio data sources

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;

#if BODYPIX_AVAILABLE
using MetavidoVFX.Segmentation;
#endif

namespace MetavidoVFX.VFX
{
    /// <summary>
    /// Manages data bindings for all VFX in scene.
    /// Automatically connects AR data, audio, and tracking to VFX properties.
    /// </summary>
    public class VFXBinderManager : MonoBehaviour
    {
        [Header("AR Data Sources")]
        [SerializeField] private AROcclusionManager occlusionManager;
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private ARCameraBackground cameraBackground;
        [SerializeField] private Camera arCamera;

        [Header("Audio Source")]
        [SerializeField] private Audio.EnhancedAudioProcessor audioProcessor;
        private AudioProcessor legacyAudioProcessor;  // Fallback for EchoVision compatibility

        [Header("Hand Tracking")]
        [SerializeField] private HandTracking.HandVFXController handController;

        [Header("Body Segmentation")]
#if BODYPIX_AVAILABLE
        [SerializeField] private BodyPartSegmenter bodySegmenter;
#endif
        [SerializeField] private bool useBodySegmentation = true;
        [Tooltip("Compute separate position maps for each body segment (face, arms, hands, legs)")]
        [SerializeField] private bool computeSegmentedPositionMaps = true;

        [Header("Settings")]
        [Tooltip("Master toggle - disable all VFX bindings for debugging")]
        [SerializeField] private bool disableAllBindings = false;
        [Tooltip("Test only this VFX (leave null to bind all)")]
        [SerializeField] private VisualEffect isolatedTestVFX = null;
        [SerializeField] private bool autoFindSources = true;
        [SerializeField] private bool bindOnAwake = true;
        [SerializeField] private Vector2 depthRange = new Vector2(0.1f, 10f);
        [SerializeField] private bool computePositionMap = true;

        [Header("VFX Parameters")]
        [Tooltip("Global VFX intensity/throttle (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultThrottle = 1f;

        [Header("Physics (Optional)")]
        [Tooltip("Enable velocity-driven input binding to all VFX")]
        [SerializeField] private bool enableVelocityBinding = true;
        [Tooltip("Velocity scale multiplier")]
        [Range(0.1f, 10f)]
        [SerializeField] private float velocityScale = 1f;
        [Tooltip("Enable gravity binding to all VFX")]
        [SerializeField] private bool enableGravityBinding = false;
        [Tooltip("Gravity strength (Y-axis, negative = down)")]
        [Range(-20f, 20f)]
        [SerializeField] private float gravityStrength = -9.81f;

        // Tracked VFX
        private List<VFXCategory> _categorizedVFX = new List<VFXCategory>();
        private List<VisualEffect> _uncategorizedVFX = new List<VisualEffect>();

        // Cached arrays for zero-allocation iteration
        private VFXCategory[] _categorizedArray;
        private VisualEffect[] _uncategorizedArray;
        private bool _vfxListDirty = true;

        // Depth rotation (ARKit depth orientation fix)
        [Header("Depth Processing")]
        [Tooltip("Rotate depth/stencil textures to match camera orientation")]
        [SerializeField] private bool rotateDepthTexture = true;
        [Tooltip("Mask depth with stencil for human-only particles (disable for full scene)")]
        [SerializeField] private bool maskDepthWithStencil = true;

        // Cached data
        private Texture _lastDepthTexture;
        private Texture _lastStencilTexture;
        private Texture _lastColorTexture;
        private RenderTexture _rotatedDepthRT;
        private RenderTexture _rotatedStencilRT;
        private Material _rotateUVMaterial;
        private Matrix4x4 _inverseViewMatrix;
        private Matrix4x4 _inverseProjectionMatrix;
        private Vector4 _rayParams;  // Camera projection: (0, 0, tan(fov/2)*aspect, tan(fov/2))

        // Color capture
        private RenderTexture _colorRT;

        // PositionMap compute (depth → world positions)
        private ComputeShader _depthToWorldCompute;
        private RenderTexture _positionMapRT;
        private int _depthToWorldKernel = -1;

        // VelocityMap compute (frame-to-frame motion)
        private RenderTexture _velocityMapRT;
        private RenderTexture _previousPositionMapRT;
        private int _velocityKernel = -1;

        // Depth Hue Encoder (raw depth → Metavido hue-encoded RGB)
        private ComputeShader _depthHueEncoderCompute;
        private RenderTexture _hueDepthRT;
        private int _depthHueEncoderKernel = -1;

        // Masked Depth (depth * stencil for human-only)
        private ComputeShader _maskDepthCompute;
        private RenderTexture _maskedDepthRT;
        private int _maskDepthKernel = -1;

        // Segmented PositionMap compute (24-part body segmentation)
        private ComputeShader _segmentedDepthToWorldCompute;
        private int _segmentedKernel = -1;
        private RenderTexture _bodyPositionMapRT;
        private RenderTexture _armsPositionMapRT;
        private RenderTexture _handsPositionMapRT;
        private RenderTexture _legsPositionMapRT;
        private RenderTexture _facePositionMapRT;

        // Debug
        [Header("Debug - This Manager")]
        [SerializeField] private bool verboseLogging = false;
        [SerializeField] private float logInterval = 3f;

        [Header("Debug - Suppress Other Systems")]
        [SerializeField] private bool suppressARKitBinder = true;
        [SerializeField] private bool suppressPeopleVFX = true;
        [SerializeField] private bool suppressHologram = true;
        [SerializeField] private bool suppressHandTracking = true;
        [SerializeField] private bool suppressAudio = true;
        [SerializeField] private bool suppressMeshVFX = true;
        [SerializeField] private bool suppressBodySegmenter = true;
        [SerializeField] private bool suppressDepthDebug = true;
        [SerializeField] private bool suppressUI = true;

        // Static accessors for other systems
        public static bool SuppressARKitBinderLogs => _instance != null && _instance.suppressARKitBinder;
        public static bool SuppressPeopleVFXLogs => _instance != null && _instance.suppressPeopleVFX;
        public static bool SuppressHologramLogs => _instance != null && _instance.suppressHologram;
        public static bool SuppressHandTrackingLogs => _instance != null && _instance.suppressHandTracking;
        public static bool SuppressAudioLogs => _instance != null && _instance.suppressAudio;
        public static bool SuppressMeshVFXLogs => _instance != null && _instance.suppressMeshVFX;
        public static bool SuppressBodySegmenterLogs => _instance != null && _instance.suppressBodySegmenter;
        public static bool SuppressDepthDebugLogs => _instance != null && _instance.suppressDepthDebug;
        public static bool SuppressUILogs => _instance != null && _instance.suppressUI;
        private static VFXBinderManager _instance;

        private float _lastLogTime;
        private int _boundCount;
        private bool _firstDepthReceived = false;
        private bool _firstStencilReceived = false;
        private bool _firstColorReceived = false;
        private float _startTime;
        private int _frameCount = 0;
        private int _enabledVFXCount = 0;

        // Physics tracking
        private Vector3 _lastCameraPosition;
        private Vector3 _cameraVelocity;
        private Vector3 _smoothedVelocity;
        private float _cameraSpeed;
        private Vector3 _gravityVector;

        // VFX parameters
        private float _throttle = 1f;  // Global intensity control (0-1)

        // Warning spam prevention - only warn once per VFX
        private HashSet<int> _warnedVFXNoDepth = new HashSet<int>();
        private HashSet<int> _warnedVFXNoColor = new HashSet<int>();
        private HashSet<int> _warnedVFXNullDepth = new HashSet<int>();
        private bool _warnedDepthNull = false;

        void Awake()
        {
            _instance = this;
            _startTime = Time.realtimeSinceStartup;
            _throttle = defaultThrottle;

            if (autoFindSources) FindDataSources();
            if (bindOnAwake) RefreshVFXList();

            // Load compute shaders silently
            if (computePositionMap)
            {
                _depthToWorldCompute = Resources.Load<ComputeShader>("DepthToWorld");
                if (_depthToWorldCompute != null)
                {
                    try { _depthToWorldKernel = _depthToWorldCompute.FindKernel("DepthToWorld"); }
                    catch { _depthToWorldKernel = -1; }
                    try { _velocityKernel = _depthToWorldCompute.FindKernel("CalculateVelocity"); }
                    catch { _velocityKernel = -1; }
                }
            }

            _depthHueEncoderCompute = Resources.Load<ComputeShader>("DepthHueEncoder");
            if (_depthHueEncoderCompute != null)
                try { _depthHueEncoderKernel = _depthHueEncoderCompute.FindKernel("EncodeDepthToHue"); }
                catch { _depthHueEncoderKernel = -1; }

            // Load mask depth compute (for human-only depth)
            _maskDepthCompute = Resources.Load<ComputeShader>("MaskDepthWithStencil");
            if (_maskDepthCompute != null)
                try { _maskDepthKernel = _maskDepthCompute.FindKernel("MaskDepth"); }
                catch { _maskDepthKernel = -1; }

            if (computeSegmentedPositionMaps)
            {
                _segmentedDepthToWorldCompute = Resources.Load<ComputeShader>("SegmentedDepthToWorld");
                if (_segmentedDepthToWorldCompute != null)
                    try { _segmentedKernel = _segmentedDepthToWorldCompute.FindKernel("SegmentedDepthToWorld"); }
                    catch { _segmentedKernel = -1; }
            }

            // Single startup log - use explicit null check for Unity serialized fields
            string isolatedName = (isolatedTestVFX != null) ? isolatedTestVFX.name : "none";
            Debug.Log($"[VFXBinderManager] Ready | VFX={_uncategorizedVFX.Count} Isolated={isolatedName}");
        }

        void Start()
        {
            // Startup logging removed - use verboseLogging for debug info
        }

        void OnDestroy()
        {
            if (_colorRT != null)
            {
                _colorRT.Release();
                Destroy(_colorRT);
                _colorRT = null;
            }
            if (_positionMapRT != null)
            {
                _positionMapRT.Release();
                Destroy(_positionMapRT);
                _positionMapRT = null;
            }
            if (_velocityMapRT != null)
            {
                _velocityMapRT.Release();
                Destroy(_velocityMapRT);
                _velocityMapRT = null;
            }
            if (_previousPositionMapRT != null)
            {
                _previousPositionMapRT.Release();
                Destroy(_previousPositionMapRT);
                _previousPositionMapRT = null;
            }
            if (_hueDepthRT != null)
            {
                _hueDepthRT.Release();
                Destroy(_hueDepthRT);
                _hueDepthRT = null;
            }

            // Cleanup segmented position maps
            ReleaseSegmentedRTs();
        }

        void ReleaseSegmentedRTs()
        {
            if (_bodyPositionMapRT != null) { _bodyPositionMapRT.Release(); Destroy(_bodyPositionMapRT); _bodyPositionMapRT = null; }
            if (_armsPositionMapRT != null) { _armsPositionMapRT.Release(); Destroy(_armsPositionMapRT); _armsPositionMapRT = null; }
            if (_handsPositionMapRT != null) { _handsPositionMapRT.Release(); Destroy(_handsPositionMapRT); _handsPositionMapRT = null; }
            if (_legsPositionMapRT != null) { _legsPositionMapRT.Release(); Destroy(_legsPositionMapRT); _legsPositionMapRT = null; }
            if (_facePositionMapRT != null) { _facePositionMapRT.Release(); Destroy(_facePositionMapRT); _facePositionMapRT = null; }
        }

        void FindDataSources()
        {
            if (occlusionManager == null)
                occlusionManager = FindFirstObjectByType<AROcclusionManager>();

            if (cameraManager == null)
                cameraManager = FindFirstObjectByType<ARCameraManager>();

            if (cameraBackground == null)
                cameraBackground = FindFirstObjectByType<ARCameraBackground>();

            if (arCamera == null)
                arCamera = Camera.main;

            if (audioProcessor == null)
                audioProcessor = FindFirstObjectByType<Audio.EnhancedAudioProcessor>();

            // Fallback: Also look for legacy EchoVision AudioProcessor
            if (audioProcessor == null)
                legacyAudioProcessor = FindFirstObjectByType<AudioProcessor>();

            if (handController == null)
                handController = FindFirstObjectByType<HandTracking.HandVFXController>();

#if BODYPIX_AVAILABLE
            if (useBodySegmentation && bodySegmenter == null)
                bodySegmenter = FindFirstObjectByType<BodyPartSegmenter>();
#endif

            // Debug logging for found sources
            bool hasAudio = audioProcessor != null || legacyAudioProcessor != null;
            string audioType = audioProcessor != null ? "Enhanced" : (legacyAudioProcessor != null ? "Legacy" : "None");
#if BODYPIX_AVAILABLE
            bool hasBodySeg = bodySegmenter != null;
#else
            bool hasBodySeg = false;
#endif
            // Startup log removed - sources logged only on errors
        }

        void Update()
        {
            // Master disable toggle for debugging
            if (disableAllBindings) return;

            _frameCount++;
            UpdateCachedData();
            UpdatePhysicsData();
            BindAllVFX();

            // Track first-time data received
            TrackFirstDataReceived();

            // Periodic status logging
            if (Time.time - _lastLogTime > logInterval)
            {
                _lastLogTime = Time.time;
                LogPeriodicStatus();
            }
        }

        void TrackFirstDataReceived()
        {
            if (!_firstDepthReceived && _lastDepthTexture != null)
            {
                _firstDepthReceived = true;
                float elapsed = Time.realtimeSinceStartup - _startTime;
                Debug.Log($"[VFXBinderManager] ✓ FIRST DEPTH RECEIVED at frame {_frameCount} ({elapsed:F2}s) - {_lastDepthTexture.width}x{_lastDepthTexture.height}");
            }

            if (!_firstStencilReceived && _lastStencilTexture != null)
            {
                _firstStencilReceived = true;
                float elapsed = Time.realtimeSinceStartup - _startTime;
                Debug.Log($"[VFXBinderManager] ✓ FIRST STENCIL RECEIVED at frame {_frameCount} ({elapsed:F2}s) - {_lastStencilTexture.width}x{_lastStencilTexture.height}");
            }

            if (!_firstColorReceived && _lastColorTexture != null)
            {
                _firstColorReceived = true;
                float elapsed = Time.realtimeSinceStartup - _startTime;
                Debug.Log($"[VFXBinderManager] ✓ FIRST COLOR RECEIVED at frame {_frameCount} ({elapsed:F2}s) - {_lastColorTexture.width}x{_lastColorTexture.height}");
            }
        }

        void LogPeriodicStatus()
        {
            // Minimal logging - only essential info
            if (!verboseLogging) return;

            int vfxCount = _uncategorizedArray != null ? _uncategorizedArray.Length : 0;
            Debug.Log($"[VFXBinderManager] Depth={(_lastDepthTexture != null ? "OK" : "NULL")} VFX={vfxCount}");
        }

        void LogSourceDetails()
        {
            Debug.Log($"[VFXBinderManager] ─── Data Sources ───");
            Debug.Log($"[VFXBinderManager] OcclusionManager: {(occlusionManager != null ? occlusionManager.gameObject.name : "NULL")}");
            Debug.Log($"[VFXBinderManager] CameraManager: {(cameraManager != null ? cameraManager.gameObject.name : "NULL")}");
            Debug.Log($"[VFXBinderManager] CameraBackground: {(cameraBackground != null ? cameraBackground.gameObject.name : "NULL")}");
            Debug.Log($"[VFXBinderManager] ARCamera: {(arCamera != null ? arCamera.name : "NULL")}");
            Debug.Log($"[VFXBinderManager] AudioProcessor: {(audioProcessor != null ? "Enhanced" : (legacyAudioProcessor != null ? "Legacy" : "NULL"))}");
            Debug.Log($"[VFXBinderManager] HandController: {(handController != null ? handController.gameObject.name : "NULL")}");

            if (occlusionManager != null)
            {
                Debug.Log($"[VFXBinderManager] Occlusion modes: env={occlusionManager.currentEnvironmentDepthMode}, stencil={occlusionManager.currentHumanStencilMode}");
            }
        }

        void LogVFXList()
        {
            Debug.Log($"[VFXBinderManager] ─── VFX List ({_uncategorizedVFX.Count} total) ───");
            for (int i = 0; i < Mathf.Min(_uncategorizedVFX.Count, 20); i++)
            {
                var vfx = _uncategorizedVFX[i];
                if (vfx != null)
                {
                    string assetName = vfx.visualEffectAsset != null ? vfx.visualEffectAsset.name : "NO ASSET";
                    bool hasDepth = vfx.HasTexture("DepthMap") || vfx.HasTexture("DepthTexture");
                    bool hasPos = vfx.HasTexture("PositionMap");
                    bool hasSpawn = vfx.HasBool("Spawn");
                    Debug.Log($"[VFXBinderManager]   [{i}] {vfx.name} ({assetName}) enabled={vfx.enabled} | Depth={hasDepth} Pos={hasPos} Spawn={hasSpawn}");
                }
            }
            if (_uncategorizedVFX.Count > 20)
            {
                Debug.Log($"[VFXBinderManager]   ... and {_uncategorizedVFX.Count - 20} more");
            }
        }

        void UpdateCachedData()
        {
            // Get AR textures
            if (occlusionManager != null)
            {
                // Use TryGet methods for newer API, suppress deprecation warnings for fallback
                #pragma warning disable CS0618
                // Try new depth API first
                if (!occlusionManager.TryGetEnvironmentDepthTexture(out _lastDepthTexture))
                {
                    _lastDepthTexture = occlusionManager.environmentDepthTexture;
                }
                // Stencil uses direct property (no TryGet method exists)
                _lastStencilTexture = occlusionManager.humanStencilTexture;
                #pragma warning restore CS0618

                // Rotate depth/stencil textures 90° CW for Metavido VFX
                // These VFX sample DepthMap directly with their own UV→position conversion
                // ARKit depth is landscape-oriented, VFX expects portrait
                if (rotateDepthTexture && _lastDepthTexture != null)
                {
                    // Create rotation material if needed
                    if (_rotateUVMaterial == null)
                    {
                        var shader = Shader.Find("Hidden/RotateUV90CW");
                        if (shader != null)
                            _rotateUVMaterial = new Material(shader);
                        else
                            Debug.LogWarning("[VFXBinderManager] RotateUV90CW shader not found");
                    }

                    if (_rotateUVMaterial != null)
                    {
                        // Rotated dimensions (swap width/height for 90° rotation)
                        int rotW = _lastDepthTexture.height;
                        int rotH = _lastDepthTexture.width;

                        // Create/resize rotated depth RT
                        if (_rotatedDepthRT == null || _rotatedDepthRT.width != rotW || _rotatedDepthRT.height != rotH)
                        {
                            if (_rotatedDepthRT != null) _rotatedDepthRT.Release();
                            _rotatedDepthRT = new RenderTexture(rotW, rotH, 0, RenderTextureFormat.RFloat);
                            _rotatedDepthRT.filterMode = FilterMode.Bilinear;
                            _rotatedDepthRT.Create();
                        }

                        // Blit with UV rotation
                        Graphics.Blit(_lastDepthTexture, _rotatedDepthRT, _rotateUVMaterial);
                        _lastDepthTexture = _rotatedDepthRT;

                        // Rotate stencil too if available
                        if (_lastStencilTexture != null)
                        {
                            if (_rotatedStencilRT == null || _rotatedStencilRT.width != rotW || _rotatedStencilRT.height != rotH)
                            {
                                if (_rotatedStencilRT != null) _rotatedStencilRT.Release();
                                _rotatedStencilRT = new RenderTexture(rotW, rotH, 0, RenderTextureFormat.R8);
                                _rotatedStencilRT.filterMode = FilterMode.Point;
                                _rotatedStencilRT.Create();
                            }
                            Graphics.Blit(_lastStencilTexture, _rotatedStencilRT, _rotateUVMaterial);
                            _lastStencilTexture = _rotatedStencilRT;
                        }
                    }
                }
            }

            // Mask depth with stencil for human-only VFX (like pointcloud_depth_people_metavido)
            if (maskDepthWithStencil && _maskDepthKernel >= 0 && _lastDepthTexture != null && _lastStencilTexture != null)
            {
                int w = _lastDepthTexture.width;
                int h = _lastDepthTexture.height;

                // Create/resize masked depth RT
                if (_maskedDepthRT == null || _maskedDepthRT.width != w || _maskedDepthRT.height != h)
                {
                    if (_maskedDepthRT != null) _maskedDepthRT.Release();
                    _maskedDepthRT = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat);
                    _maskedDepthRT.enableRandomWrite = true;
                    _maskedDepthRT.filterMode = FilterMode.Point;
                    _maskedDepthRT.Create();
                }

                // Run mask compute
                _maskDepthCompute.SetTexture(_maskDepthKernel, "_Depth", _lastDepthTexture);
                _maskDepthCompute.SetTexture(_maskDepthKernel, "_Stencil", _lastStencilTexture);
                _maskDepthCompute.SetTexture(_maskDepthKernel, "_MaskedDepthRT", _maskedDepthRT);
                _maskDepthCompute.SetFloat("_StencilThreshold", 0.5f);

                int groupsX = Mathf.CeilToInt(w / 32f);
                int groupsY = Mathf.CeilToInt(h / 32f);
                _maskDepthCompute.Dispatch(_maskDepthKernel, groupsX, groupsY, 1);

                // Use masked depth for VFX binding (human-only pixels)
                _lastDepthTexture = _maskedDepthRT;
            }

            // Get camera texture via ARCameraBackground blit
            if (cameraBackground != null && cameraBackground.material != null)
            {
                // Clamp color RT size to avoid exceeding GPU texture limits (2048 on some devices)
                // Full screen resolution isn't needed for VFX color sampling
                const int maxColorRTSize = 1920;
                int colorWidth = Mathf.Min(Screen.width, maxColorRTSize);
                int colorHeight = Mathf.Min(Screen.height, maxColorRTSize);

                // Create/resize color RT if needed
                if (_colorRT == null || _colorRT.width != colorWidth || _colorRT.height != colorHeight)
                {
                    if (_colorRT != null) _colorRT.Release();
                    _colorRT = new RenderTexture(colorWidth, colorHeight, 0, RenderTextureFormat.ARGB32);
                    _colorRT.Create();
                    Debug.Log($"[VFXBinderManager] Created ColorRT: {colorWidth}x{colorHeight} (screen: {Screen.width}x{Screen.height})");
                }
                // Blit AR background (YCbCr→RGB conversion via material)
                Graphics.Blit(null, _colorRT, cameraBackground.material);
                _lastColorTexture = _colorRT;
            }
            else if (verboseLogging && Time.frameCount % 300 == 0)
            {
                Debug.LogWarning($"[VFXBinderManager] ColorMap not available: cameraBackground={cameraBackground != null}, material={cameraBackground?.material != null}");
            }

            // Calculate inverse view matrix, inverse projection, and ray parameters
            if (arCamera != null)
            {
                // InverseView: Use TRS exactly like Keijiro's Metavido RenderUtils.cs
                // Source: Library/PackageCache/jp.keijiro.metavido/Decoder/Scripts/RenderUtils.cs:22
                _inverseViewMatrix = Matrix4x4.TRS(
                    arCamera.transform.position,
                    arCamera.transform.rotation,
                    Vector3.one);

                // InverseProjection: Must account for depth texture rotation
                // When depth is rotated 90° CW, UV space changes: (u,v) → (v, 1-u)
                // We need to build a projection matrix that matches the rotated UVs
                if (rotateDepthTexture && _lastDepthTexture != null)
                {
                    // Build a projection matrix for the rotated depth texture
                    // Original projection maps camera space to clip space
                    // For rotated depth, we need to swap X/Y and account for aspect change
                    float depthAspect = (float)_lastDepthTexture.width / _lastDepthTexture.height;
                    float rotFovV = arCamera.fieldOfView;
                    float fovH = 2f * Mathf.Atan(Mathf.Tan(rotFovV * 0.5f * Mathf.Deg2Rad) * depthAspect) * Mathf.Rad2Deg;

                    // Create projection for rotated depth (swap near/far aspect)
                    Matrix4x4 rotatedProj = Matrix4x4.Perspective(fovH, 1f / depthAspect, arCamera.nearClipPlane, arCamera.farClipPlane);

                    // Apply UV rotation: 90° CW means portrait (u,v) maps to landscape (v, 1-u)
                    // In NDC: portrait (x,y) → landscape (y, -x)
                    // Since (A*B)^-1 = B^-1 * A^-1, we set uvRotation such that its inverse gives [0,1;-1,0]
                    // So uvRotation = [0,-1;1,0] (which inverts to [0,1;-1,0])
                    Matrix4x4 uvRotation = Matrix4x4.identity;
                    uvRotation.m00 = 0;  uvRotation.m01 = -1;  // x' = -y (inverts to: x' = y)
                    uvRotation.m10 = 1;  uvRotation.m11 = 0;   // y' = x  (inverts to: y' = -x)

                    _inverseProjectionMatrix = (uvRotation * rotatedProj).inverse;
                }
                else
                {
                    _inverseProjectionMatrix = arCamera.projectionMatrix.inverse;
                }

                // RayParams: (centerShift.x, centerShift.y, tan(fov/2) * aspect, tan(fov/2))
                // Source: Library/PackageCache/jp.keijiro.metavido/Decoder/Scripts/RenderUtils.cs:10-15
                // For live AR, use actual camera aspect (not Metavido's hardcoded 16:9)
                var proj = arCamera.projectionMatrix;
                float centerShiftX = proj.m02;  // Principal point offset X (usually 0 or small)
                float centerShiftY = proj.m12;  // Principal point offset Y (usually 0 or small)

                // Keijiro uses fieldOfView/2 which is in RADIANS in metadata, but Unity's camera.fieldOfView is DEGREES
                float fovV = arCamera.fieldOfView * Mathf.Deg2Rad;
                float tanV = Mathf.Tan(fovV * 0.5f);

                // Use depth texture aspect when rotated (depth FOV differs from camera FOV)
                float tanH;
                if (rotateDepthTexture && _lastDepthTexture != null)
                {
                    float depthAspect = (float)_lastDepthTexture.width / _lastDepthTexture.height;
                    tanH = tanV * depthAspect;
                    _rayParams = new Vector4(centerShiftX, centerShiftY, -tanH, tanV);
                }
                else
                {
                    tanH = tanV * arCamera.aspect;
                    _rayParams = new Vector4(centerShiftX, centerShiftY, tanH, tanV);
                }

                // Debug: Log RayParams every 3 seconds
                if (verboseLogging && Time.frameCount % 180 == 0)
                {
                    Debug.Log($"[VFXBinderManager] RayParams: ({_rayParams.x:F3},{_rayParams.y:F3},{_rayParams.z:F3},{_rayParams.w:F3}) | rot={rotateDepthTexture} fov={arCamera.fieldOfView:F1}° aspect={arCamera.aspect:F3}");
                }
            }

            // Compute PositionMap (depth → world positions)
            // Note: _lastDepthTexture is already rotated by blit above, so use its dimensions directly
            if (computePositionMap && _depthToWorldCompute != null && _depthToWorldKernel >= 0 && _lastDepthTexture != null && arCamera != null)
            {
                int outWidth = _lastDepthTexture.width;
                int outHeight = _lastDepthTexture.height;

                // Create/resize PositionMap RT
                if (_positionMapRT == null || _positionMapRT.width != outWidth || _positionMapRT.height != outHeight)
                {
                    if (_positionMapRT != null) _positionMapRT.Release();
                    _positionMapRT = new RenderTexture(outWidth, outHeight, 0, RenderTextureFormat.ARGBFloat);
                    _positionMapRT.enableRandomWrite = true;
                    _positionMapRT.filterMode = FilterMode.Bilinear;
                    _positionMapRT.wrapMode = TextureWrapMode.Clamp; // Prevent edge sampling artifacts
                    _positionMapRT.Create();
                    Debug.Log($"[VFXBinderManager] Created PositionMap RT: {outWidth}x{outHeight} (depth: {_lastDepthTexture.width}x{_lastDepthTexture.height}, rotated={rotateDepthTexture})");
                }

                // Set compute shader parameters
                var proj = arCamera.projectionMatrix;
                var invVP = (proj * arCamera.transform.worldToLocalMatrix).inverse;

                _depthToWorldCompute.SetMatrix("_InvVP", invVP);
                _depthToWorldCompute.SetMatrix("_ProjectionMatrix", proj);
                _depthToWorldCompute.SetVector("_DepthRange", new Vector4(depthRange.x, depthRange.y, 0.5f, 0));
                _depthToWorldCompute.SetTexture(_depthToWorldKernel, "_Depth", _lastDepthTexture);
                _depthToWorldCompute.SetTexture(_depthToWorldKernel, "_Stencil", _lastStencilTexture != null ? _lastStencilTexture : Texture2D.whiteTexture);
                _depthToWorldCompute.SetTexture(_depthToWorldKernel, "_PositionRT", _positionMapRT);
                _depthToWorldCompute.SetInt("_UseStencil", _lastStencilTexture != null ? 1 : 0);
                // Depth texture is already rotated by blit, no need for compute shader rotation
                _depthToWorldCompute.SetInt("_RotateUV90CW", 0);

                // Dispatch (32x32 thread groups to match DepthToWorld.compute numthreads)
                int groupsX = Mathf.CeilToInt(outWidth / 32.0f);
                int groupsY = Mathf.CeilToInt(outHeight / 32.0f);
                _depthToWorldCompute.Dispatch(_depthToWorldKernel, groupsX, groupsY, 1);

                // Velocity computation (ported from PeopleOcclusionVFXManager)
                if (_velocityKernel >= 0)
                {
                    // Create/resize velocity RTs to match position map
                    if (_velocityMapRT == null || _velocityMapRT.width != outWidth || _velocityMapRT.height != outHeight)
                    {
                        if (_velocityMapRT != null) _velocityMapRT.Release();
                        _velocityMapRT = new RenderTexture(outWidth, outHeight, 0, RenderTextureFormat.ARGBFloat);
                        _velocityMapRT.enableRandomWrite = true;
                        _velocityMapRT.filterMode = FilterMode.Bilinear;
                        _velocityMapRT.wrapMode = TextureWrapMode.Clamp;
                        _velocityMapRT.Create();

                        if (_previousPositionMapRT != null) _previousPositionMapRT.Release();
                        _previousPositionMapRT = new RenderTexture(outWidth, outHeight, 0, RenderTextureFormat.ARGBFloat);
                        _previousPositionMapRT.filterMode = FilterMode.Bilinear;
                        _previousPositionMapRT.wrapMode = TextureWrapMode.Clamp;
                        _previousPositionMapRT.Create();

                        Debug.Log($"[VFXBinderManager] Created VelocityMap RT: {outWidth}x{outHeight}");
                    }

                    // Dispatch velocity kernel (position and previous already bound)
                    _depthToWorldCompute.SetTexture(_velocityKernel, "_PositionRT", _positionMapRT);
                    _depthToWorldCompute.SetTexture(_velocityKernel, "_PreviousPositionRT", _previousPositionMapRT);
                    _depthToWorldCompute.SetTexture(_velocityKernel, "_VelocityRT", _velocityMapRT);
                    _depthToWorldCompute.SetFloat("_DeltaTime", Time.deltaTime);
                    _depthToWorldCompute.Dispatch(_velocityKernel, groupsX, groupsY, 1);

                    // Copy current position to previous for next frame
                    Graphics.Blit(_positionMapRT, _previousPositionMapRT);
                }
            }

            // Compute Hue-Encoded Depth (raw ARKit depth → Metavido RGB hue format)
            // This allows Metavido VFX (BodyParticles, etc.) to decode depth correctly
            if (_depthHueEncoderCompute != null && _depthHueEncoderKernel >= 0 && _lastDepthTexture != null)
            {
                int width = _lastDepthTexture.width;
                int height = _lastDepthTexture.height;

                // Create/resize HueDepth RT
                if (_hueDepthRT == null || _hueDepthRT.width != width || _hueDepthRT.height != height)
                {
                    if (_hueDepthRT != null) _hueDepthRT.Release();
                    _hueDepthRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
                    _hueDepthRT.enableRandomWrite = true;
                    _hueDepthRT.filterMode = FilterMode.Bilinear;
                    _hueDepthRT.wrapMode = TextureWrapMode.Clamp;
                    _hueDepthRT.Create();
                    Debug.Log($"[VFXBinderManager] Created HueDepth RT: {width}x{height}");
                }

                // Set compute shader parameters
                _depthHueEncoderCompute.SetTexture(_depthHueEncoderKernel, "_Depth", _lastDepthTexture);
                _depthHueEncoderCompute.SetTexture(_depthHueEncoderKernel, "_Stencil",
                    _lastStencilTexture != null ? _lastStencilTexture : Texture2D.whiteTexture);
                _depthHueEncoderCompute.SetTexture(_depthHueEncoderKernel, "_HueDepthRT", _hueDepthRT);
                _depthHueEncoderCompute.SetVector("_DepthRange", new Vector4(depthRange.x, depthRange.y, 0.5f, 0));
                _depthHueEncoderCompute.SetInt("_UseStencil", _lastStencilTexture != null ? 1 : 0);

                // Dispatch (32x32 thread groups to match DepthHueEncoder.compute numthreads)
                int groupsX = Mathf.CeilToInt(width / 32.0f);
                int groupsY = Mathf.CeilToInt(height / 32.0f);
                _depthHueEncoderCompute.Dispatch(_depthHueEncoderKernel, groupsX, groupsY, 1);
            }

            // Compute segmented position maps (body parts → separate world position maps)
            DispatchSegmentedCompute();
        }

        /// <summary>
        /// Update physics data (velocity from camera movement, gravity vector)
        /// </summary>
        void UpdatePhysicsData()
        {
            // Track camera velocity
            if (enableVelocityBinding && arCamera != null)
            {
                // Initialize on first frame
                if (_lastCameraPosition == Vector3.zero)
                {
                    _lastCameraPosition = arCamera.transform.position;
                }

                // Calculate camera velocity from position delta
                Vector3 cameraDelta = arCamera.transform.position - _lastCameraPosition;
                _cameraVelocity = cameraDelta / Mathf.Max(Time.deltaTime, 0.001f);
                _lastCameraPosition = arCamera.transform.position;

                // Apply scale
                _cameraVelocity *= velocityScale;

                // Smooth velocity (lerp factor ~0.15 for smooth but responsive)
                _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, _cameraVelocity, 0.15f);
                _cameraSpeed = _smoothedVelocity.magnitude;
            }

            // Calculate gravity vector
            if (enableGravityBinding)
            {
                _gravityVector = new Vector3(0f, gravityStrength, 0f);
            }
        }

        void DispatchSegmentedCompute()
        {
#if BODYPIX_AVAILABLE
            // Only dispatch if we have BodyPix segmentation data
            if (!computeSegmentedPositionMaps || _segmentedDepthToWorldCompute == null || _segmentedKernel < 0)
                return;

            if (_lastDepthTexture == null || arCamera == null)
                return;

            if (bodySegmenter == null || !bodySegmenter.IsReady || bodySegmenter.MaskTexture == null)
                return;

            int width = _lastDepthTexture.width;
            int height = _lastDepthTexture.height;

            // Create/resize all segmented position map RTs
            EnsureSegmentedRTs(width, height);

            // Set compute shader parameters (shared across all outputs)
            var invView = arCamera.cameraToWorldMatrix;
            var invProj = arCamera.projectionMatrix.inverse;

            _segmentedDepthToWorldCompute.SetMatrix("_InverseView", invView);
            _segmentedDepthToWorldCompute.SetMatrix("_InverseProjection", invProj);
            _segmentedDepthToWorldCompute.SetVector("_DepthRange", new Vector4(depthRange.x, depthRange.y, 0, 0));
            _segmentedDepthToWorldCompute.SetInts("_OutputSize", width, height);
            _segmentedDepthToWorldCompute.SetInt("_UseStencil", _lastStencilTexture != null ? 1 : 0);

            // Input textures
            _segmentedDepthToWorldCompute.SetTexture(_segmentedKernel, "_Depth", _lastDepthTexture);
            _segmentedDepthToWorldCompute.SetTexture(_segmentedKernel, "_BodyPartMask", bodySegmenter.MaskTexture);
            _segmentedDepthToWorldCompute.SetTexture(_segmentedKernel, "_Stencil",
                _lastStencilTexture != null ? _lastStencilTexture : Texture2D.whiteTexture);

            // Output textures (6 separate position maps)
            _segmentedDepthToWorldCompute.SetTexture(_segmentedKernel, "_PositionMap", _positionMapRT);
            _segmentedDepthToWorldCompute.SetTexture(_segmentedKernel, "_BodyPositionMap", _bodyPositionMapRT);
            _segmentedDepthToWorldCompute.SetTexture(_segmentedKernel, "_ArmsPositionMap", _armsPositionMapRT);
            _segmentedDepthToWorldCompute.SetTexture(_segmentedKernel, "_HandsPositionMap", _handsPositionMapRT);
            _segmentedDepthToWorldCompute.SetTexture(_segmentedKernel, "_LegsPositionMap", _legsPositionMapRT);
            _segmentedDepthToWorldCompute.SetTexture(_segmentedKernel, "_FacePositionMap", _facePositionMapRT);

            // Dispatch (32x32 thread groups)
            int groupsX = Mathf.CeilToInt(width / 32.0f);
            int groupsY = Mathf.CeilToInt(height / 32.0f);
            _segmentedDepthToWorldCompute.Dispatch(_segmentedKernel, groupsX, groupsY, 1);
#endif
        }

        void EnsureSegmentedRTs(int width, int height)
        {
            // Check if resize needed
            if (_bodyPositionMapRT != null && _bodyPositionMapRT.width == width && _bodyPositionMapRT.height == height)
                return;

            // Release existing RTs
            ReleaseSegmentedRTs();

            // Create all segmented position map RTs with same format
            _bodyPositionMapRT = CreateSegmentedRT(width, height, "BodyPositionMap");
            _armsPositionMapRT = CreateSegmentedRT(width, height, "ArmsPositionMap");
            _handsPositionMapRT = CreateSegmentedRT(width, height, "HandsPositionMap");
            _legsPositionMapRT = CreateSegmentedRT(width, height, "LegsPositionMap");
            _facePositionMapRT = CreateSegmentedRT(width, height, "FacePositionMap");

            Debug.Log($"[VFXBinderManager] Created segmented position maps: {width}x{height}");
        }

        RenderTexture CreateSegmentedRT(int width, int height, string name)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            rt.name = name;
            rt.enableRandomWrite = true;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.Create();
            return rt;
        }

        void BindAllVFX()
        {
            // Isolated test mode - only bind to specific VFX for debugging
            if (isolatedTestVFX != null)
            {
                if (isolatedTestVFX.enabled)
                    BindVFX(isolatedTestVFX, VFXBindingRequirements.All);
                return;
            }

            // Rebuild cached arrays if list changed
            if (_vfxListDirty)
            {
                _categorizedArray = _categorizedVFX.ToArray();
                _uncategorizedArray = _uncategorizedVFX.ToArray();
                _vfxListDirty = false;
            }

            // Bind categorized VFX (zero allocation - use cached array)
            for (int i = 0; i < _categorizedArray.Length; i++)
            {
                var vfxCategory = _categorizedArray[i];
                if (vfxCategory == null || vfxCategory.VFX == null || !vfxCategory.VFX.enabled)
                    continue;
                BindVFX(vfxCategory.VFX, vfxCategory.Bindings);
            }

            // Bind uncategorized VFX (zero allocation - use cached array)
            for (int i = 0; i < _uncategorizedArray.Length; i++)
            {
                var vfx = _uncategorizedArray[i];
                if (vfx == null || !vfx.enabled)
                    continue;
                BindVFX(vfx, VFXBindingRequirements.All);
            }
        }

        void BindVFX(VisualEffect vfx, VFXBindingRequirements requirements)
        {
            if (verboseLogging && _frameCount % 180 == 1)
            {
                Debug.Log($"[VFXBinderManager] BindVFX: '{vfx.name}' | Depth={_lastDepthTexture != null} Stencil={_lastStencilTexture != null} Color={_lastColorTexture != null}");
            }

            // Depth Map - Metavido VFX expect RAW FLOAT depth (demuxer pre-decodes)
            // The TextureDemuxer.cs decodes hue→float BEFORE sending to VFX
            // For live AR, we provide raw depth directly (already float from ARKit)
            bool depthRequired = (requirements & VFXBindingRequirements.DepthMap) != 0;
            bool hasDepthTex = _lastDepthTexture != null;
            if (depthRequired && hasDepthTex)
            {
                bool boundDepth = false;
                if (vfx.HasTexture("DepthMap"))
                {
                    vfx.SetTexture("DepthMap", _lastDepthTexture);
                    boundDepth = true;
                }
                if (vfx.HasTexture("DepthTexture"))
                {
                    vfx.SetTexture("DepthTexture", _lastDepthTexture);
                    boundDepth = true;
                }
                // Warn once if VFX has no depth property
                int vfxId = vfx.GetInstanceID();
                if (verboseLogging && !boundDepth && !_warnedVFXNoDepth.Contains(vfxId))
                {
                    _warnedVFXNoDepth.Add(vfxId);
                    Debug.LogWarning($"[VFXBinderManager] VFX '{vfx.name}' has no DepthMap or DepthTexture property (this warning shows once)");
                }
            }
            else if (depthRequired && !hasDepthTex)
            {
                // Only warn once when depth texture is null
                if (!_warnedDepthNull)
                {
                    _warnedDepthNull = true;
                    Debug.LogWarning($"[VFXBinderManager] AR depth not available yet - VFX depth binding delayed");
                }
            }

            // Color Map
            bool colorRequired = (requirements & VFXBindingRequirements.ColorMap) != 0;
            bool hasColorTex = _lastColorTexture != null;
            if (colorRequired && hasColorTex)
            {
                bool boundColor = false;
                if (vfx.HasTexture("ColorMap"))
                {
                    vfx.SetTexture("ColorMap", _lastColorTexture);
                    boundColor = true;
                }
                if (vfx.HasTexture("ColorTexture"))
                {
                    vfx.SetTexture("ColorTexture", _lastColorTexture);
                    boundColor = true;
                }
                // H3M/PeopleVFX style naming (with space)
                if (vfx.HasTexture("Color Map"))
                {
                    vfx.SetTexture("Color Map", _lastColorTexture);
                    boundColor = true;
                }
                // Warn once if VFX has no color property
                int vfxId = vfx.GetInstanceID();
                if (verboseLogging && !boundColor && !_warnedVFXNoColor.Contains(vfxId))
                {
                    _warnedVFXNoColor.Add(vfxId);
                    Debug.LogWarning($"[VFXBinderManager] VFX '{vfx.name}' has no ColorMap property (this warning shows once)");
                }
            }

            // Stencil Map
            if ((requirements & VFXBindingRequirements.StencilMap) != 0 && _lastStencilTexture != null)
            {
                if (vfx.HasTexture("StencilMap"))
                    vfx.SetTexture("StencilMap", _lastStencilTexture);
                if (vfx.HasTexture("HumanStencil"))
                    vfx.SetTexture("HumanStencil", _lastStencilTexture);
                if (vfx.HasTexture("Stencil Map"))
                    vfx.SetTexture("Stencil Map", _lastStencilTexture);
            }

            // Position Map (computed world-space positions)
            if (_positionMapRT != null)
            {
                if (vfx.HasTexture("PositionMap"))
                    vfx.SetTexture("PositionMap", _positionMapRT);
                if (vfx.HasTexture("Position Map"))
                    vfx.SetTexture("Position Map", _positionMapRT);

                // Map dimensions for Akvfx-style VFX
                if (vfx.HasInt("MapWidth"))
                    vfx.SetInt("MapWidth", _positionMapRT.width);
                if (vfx.HasInt("MapHeight"))
                    vfx.SetInt("MapHeight", _positionMapRT.height);
            }

            // Velocity Map (frame-to-frame motion - ported from PeopleVFX)
            if (_velocityMapRT != null)
            {
                if (vfx.HasTexture("VelocityMap"))
                    vfx.SetTexture("VelocityMap", _velocityMapRT);
                // H3M/PeopleVFX style naming (with space)
                if (vfx.HasTexture("Velocity Map"))
                    vfx.SetTexture("Velocity Map", _velocityMapRT);
            }

            // Body Part Segmentation (24-part BodyPix mask + segmented position maps)
            BindBodySegmentation(vfx);

            // Camera matrices and projection parameters
            if (arCamera != null)
            {
                bool boundInvView = false, boundRayParams = false;

                if (vfx.HasMatrix4x4("InverseView"))
                {
                    vfx.SetMatrix4x4("InverseView", _inverseViewMatrix);
                    boundInvView = true;
                }
                if (vfx.HasMatrix4x4("InverseViewMatrix"))
                {
                    vfx.SetMatrix4x4("InverseViewMatrix", _inverseViewMatrix);
                    boundInvView = true;
                }
                if (vfx.HasMatrix4x4("InverseProjection"))
                    vfx.SetMatrix4x4("InverseProjection", _inverseProjectionMatrix);

                if (vfx.HasVector2("DepthRange"))
                    vfx.SetVector2("DepthRange", depthRange);

                // RayParams: Required for Metavido/Rcam VFX to convert UV+depth to 3D positions
                if (vfx.HasVector4("RayParams"))
                {
                    vfx.SetVector4("RayParams", _rayParams);
                    boundRayParams = true;
                }
                if (vfx.HasVector4("ProjectionVector"))
                {
                    vfx.SetVector4("ProjectionVector", _rayParams);
                    boundRayParams = true;
                }

                if (verboseLogging && _frameCount % 180 == 1)
                {
                    Debug.Log($"[VFXBinderManager] Camera params for '{vfx.name}': InvView={boundInvView} RayParams={boundRayParams} | Ray=({_rayParams.x:F3},{_rayParams.y:F3},{_rayParams.z:F3},{_rayParams.w:F3})");
                }
            }

            // Audio bindings
            if ((requirements & VFXBindingRequirements.Audio) != 0)
            {
                if (audioProcessor != null)
                {
                    // Use enhanced audio processor with full frequency bands
                    audioProcessor.PushToVFX(vfx);
                }
                else if (legacyAudioProcessor != null)
                {
                    // Fallback to legacy EchoVision AudioProcessor (volume/pitch only)
                    if (vfx.HasFloat("AudioVolume"))
                        vfx.SetFloat("AudioVolume", legacyAudioProcessor.AudioVolume);
                    if (vfx.HasFloat("AudioPitch"))
                        vfx.SetFloat("AudioPitch", legacyAudioProcessor.AudioPitch);
                }
            }

            // Physics bindings (velocity-driven input and gravity)
            BindPhysics(vfx);

            // VFX-specific parameters (Throttle, HueShift, Spawn, etc.)
            BindVFXParameters(vfx);

            // Hand tracking bindings are handled by HandVFXController directly
        }

        /// <summary>
        /// Bind VFX-specific parameters that many Rcam/Metavido VFX expect
        /// </summary>
        void BindVFXParameters(VisualEffect vfx)
        {
            // Throttle/Intensity - controls overall VFX intensity (default 1.0 for full effect)
            if (vfx.HasFloat("Throttle"))
                vfx.SetFloat("Throttle", _throttle);
            if (vfx.HasFloat("Intensity"))
                vfx.SetFloat("Intensity", _throttle);

            // Spawn - enables particle spawning (true when VFX is enabled)
            if (vfx.HasBool("Spawn"))
                vfx.SetBool("Spawn", true);

            // FocusDistance - depth at which effect is strongest (default mid-range)
            float focusDist = (depthRange.x + depthRange.y) * 0.5f;
            if (vfx.HasFloat("FocusDistance"))
                vfx.SetFloat("FocusDistance", focusDist);

            // Dimensions - texture dimensions for UV calculations
            if (_lastDepthTexture != null)
            {
                if (vfx.HasVector2("Dimensions"))
                    vfx.SetVector2("Dimensions", new Vector2(_lastDepthTexture.width, _lastDepthTexture.height));
            }

            // ReferencePosition - camera position for warp effects
            if (arCamera != null)
            {
                if (vfx.HasVector3("ReferencePosition"))
                    vfx.SetVector3("ReferencePosition", arCamera.transform.position);
            }
        }

        /// <summary>
        /// Bind optional physics data (velocity and gravity) to VFX
        /// </summary>
        void BindPhysics(VisualEffect vfx)
        {
            // Velocity-driven input
            if (enableVelocityBinding)
            {
                // Bind velocity vector to multiple property names
                if (vfx.HasVector3("Velocity"))
                    vfx.SetVector3("Velocity", _smoothedVelocity);
                if (vfx.HasVector3("ReferenceVelocity"))
                    vfx.SetVector3("ReferenceVelocity", _smoothedVelocity);
                if (vfx.HasVector3("Initial Velocity"))
                    vfx.SetVector3("Initial Velocity", _smoothedVelocity);
                if (vfx.HasVector3("CameraVelocity"))
                    vfx.SetVector3("CameraVelocity", _smoothedVelocity);

                // Bind speed (velocity magnitude)
                if (vfx.HasFloat("Speed"))
                    vfx.SetFloat("Speed", _cameraSpeed);
                if (vfx.HasFloat("VelocityMagnitude"))
                    vfx.SetFloat("VelocityMagnitude", _cameraSpeed);
                if (vfx.HasFloat("CameraSpeed"))
                    vfx.SetFloat("CameraSpeed", _cameraSpeed);
            }

            // Gravity binding
            if (enableGravityBinding)
            {
                // Bind gravity vector to multiple property names
                if (vfx.HasVector3("Gravity"))
                    vfx.SetVector3("Gravity", _gravityVector);
                if (vfx.HasVector3("Gravity Vector"))
                    vfx.SetVector3("Gravity Vector", _gravityVector);
                if (vfx.HasVector3("GravityVector"))
                    vfx.SetVector3("GravityVector", _gravityVector);

                // Bind gravity strength as scalar
                if (vfx.HasFloat("GravityStrength"))
                    vfx.SetFloat("GravityStrength", gravityStrength);
                if (vfx.HasFloat("GravityY"))
                    vfx.SetFloat("GravityY", gravityStrength);
            }
        }

        /// <summary>
        /// Bind body segmentation data (24-part mask + segmented position maps + keypoints)
        /// </summary>
        void BindBodySegmentation(VisualEffect vfx)
        {
#if BODYPIX_AVAILABLE
            // BodyPix 24-part segmentation mask
            if (bodySegmenter != null && bodySegmenter.IsReady && bodySegmenter.MaskTexture != null)
            {
                if (vfx.HasTexture("BodyPartMask"))
                    vfx.SetTexture("BodyPartMask", bodySegmenter.MaskTexture);
                if (vfx.HasTexture("SegmentationMask"))
                    vfx.SetTexture("SegmentationMask", bodySegmenter.MaskTexture);

                // Keypoint buffer (17 pose landmarks)
                if (bodySegmenter.KeypointBuffer != null && vfx.HasGraphicsBuffer("KeypointBuffer"))
                    vfx.SetGraphicsBuffer("KeypointBuffer", bodySegmenter.KeypointBuffer);

                // Also push individual keypoints as Vector3 properties
                bodySegmenter.PushToVFX(vfx);
            }
#endif

            // Segmented position maps (computed by SegmentedDepthToWorld.compute)
            if (_bodyPositionMapRT != null)
            {
                if (vfx.HasTexture("BodyPositionMap"))
                    vfx.SetTexture("BodyPositionMap", _bodyPositionMapRT);
                if (vfx.HasTexture("TorsoPositionMap"))
                    vfx.SetTexture("TorsoPositionMap", _bodyPositionMapRT);
            }

            if (_armsPositionMapRT != null)
            {
                if (vfx.HasTexture("ArmsPositionMap"))
                    vfx.SetTexture("ArmsPositionMap", _armsPositionMapRT);
            }

            if (_handsPositionMapRT != null)
            {
                if (vfx.HasTexture("HandsPositionMap"))
                    vfx.SetTexture("HandsPositionMap", _handsPositionMapRT);
            }

            if (_legsPositionMapRT != null)
            {
                if (vfx.HasTexture("LegsPositionMap"))
                    vfx.SetTexture("LegsPositionMap", _legsPositionMapRT);
            }

            if (_facePositionMapRT != null)
            {
                if (vfx.HasTexture("FacePositionMap"))
                    vfx.SetTexture("FacePositionMap", _facePositionMapRT);
            }
        }

        /// <summary>
        /// Refresh list of VFX to bind. Call this after spawning new VFX.
        /// </summary>
        public void RefreshVFXList()
        {
            _categorizedVFX.Clear();
            _uncategorizedVFX.Clear();

            var allVFX = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

            foreach (var vfx in allVFX)
            {
                // Disable VFXMetavidoBinder to prevent it from overwriting our AR bindings
                // VFXMetavidoBinder is designed for video playback, not live AR
                DisableConflictingBinders(vfx.gameObject);

                var category = vfx.GetComponent<VFXCategory>();
                if (category != null)
                {
                    _categorizedVFX.Add(category);
                }
                else
                {
                    _uncategorizedVFX.Add(vfx);
                }
            }

            // Mark dirty so BindAllVFX rebuilds cached arrays
            _vfxListDirty = true;
        }

        /// <summary>
        /// Disable binders that would conflict with VFXBinderManager's live AR bindings
        /// </summary>
        void DisableConflictingBinders(GameObject go)
        {
            // Find VFXMetavidoBinder by type name (avoids hard dependency on Metavido namespace)
            var propertyBinder = go.GetComponent<UnityEngine.VFX.Utility.VFXPropertyBinder>();
            if (propertyBinder == null) return;

            // Get all binders and disable Metavido ones
            // Copy to array to avoid collection modification during iteration
            var binders = propertyBinder.GetPropertyBinders<UnityEngine.VFX.Utility.VFXBinderBase>();
            var bindersCopy = new List<UnityEngine.VFX.Utility.VFXBinderBase>(binders);
            foreach (var binder in bindersCopy)
            {
                if (binder == null) continue;
                string typeName = binder.GetType().Name;
                if (typeName == "VFXMetavidoBinder")
                {
                    // Disable the component so it doesn't overwrite our bindings
                    var mb = binder as MonoBehaviour;
                    if (mb != null && mb.enabled)
                    {
                        mb.enabled = false;
                        Debug.Log($"[VFXBinderManager] Disabled {typeName} on '{go.name}' (live AR mode)");
                    }
                }
            }
        }

        /// <summary>
        /// Get all VFX of a specific category
        /// </summary>
        public List<VFXCategory> GetVFXByCategory(VFXCategoryType category)
        {
            var result = new List<VFXCategory>();
            foreach (var vfx in _categorizedVFX)
            {
                if (vfx.Category == category)
                    result.Add(vfx);
            }
            return result;
        }

        /// <summary>
        /// Enable only VFX of a specific category
        /// </summary>
        public void EnableCategory(VFXCategoryType category)
        {
            foreach (var vfx in _categorizedVFX)
            {
                bool shouldEnable = vfx.Category == category;
                vfx.VFX.enabled = shouldEnable;

                if (vfx.VFX.HasBool("Spawn"))
                {
                    vfx.VFX.SetBool("Spawn", shouldEnable);
                }
            }

            Debug.Log($"[VFXBinderManager] Enabled category: {category}");
        }

        /// <summary>
        /// Enable only VFX with specific performance tier or lower
        /// </summary>
        public void FilterByPerformance(int maxTier)
        {
            foreach (var vfx in _categorizedVFX)
            {
                bool shouldEnable = vfx.PerformanceTier <= maxTier;
                vfx.VFX.enabled = shouldEnable;
            }

            Debug.Log($"[VFXBinderManager] Filtered to performance tier <= {maxTier}");
        }

        // ========== PHYSICS API ==========

        /// <summary>
        /// Enable or disable velocity-driven input binding to all VFX
        /// </summary>
        public void SetVelocityBindingEnabled(bool enabled)
        {
            enableVelocityBinding = enabled;
            Debug.Log($"[VFXBinderManager] Velocity binding: {(enabled ? "ENABLED" : "DISABLED")}");
        }

        /// <summary>
        /// Set velocity scale multiplier (0.1 to 10)
        /// </summary>
        public void SetVelocityScale(float scale)
        {
            velocityScale = Mathf.Clamp(scale, 0.1f, 10f);
        }

        /// <summary>
        /// Enable or disable gravity binding to all VFX
        /// </summary>
        public void SetGravityBindingEnabled(bool enabled)
        {
            enableGravityBinding = enabled;
            Debug.Log($"[VFXBinderManager] Gravity binding: {(enabled ? "ENABLED" : "DISABLED")}");
        }

        /// <summary>
        /// Set gravity strength (-20 to 20, negative = down)
        /// </summary>
        public void SetGravityStrength(float strength)
        {
            gravityStrength = Mathf.Clamp(strength, -20f, 20f);
            _gravityVector = new Vector3(0f, gravityStrength, 0f);
        }

        /// <summary>
        /// Get current camera velocity (smoothed)
        /// </summary>
        public Vector3 GetCameraVelocity() => _smoothedVelocity;

        /// <summary>
        /// Get current camera speed (velocity magnitude)
        /// </summary>
        public float GetCameraSpeed() => _cameraSpeed;

        /// <summary>
        /// Get current gravity vector
        /// </summary>
        public Vector3 GetGravityVector() => _gravityVector;

        // ========== VFX PARAMETERS API ==========

        /// <summary>
        /// Set global VFX throttle/intensity (0-1)
        /// Controls Throttle and Intensity properties on all VFX
        /// </summary>
        public void SetThrottle(float value)
        {
            _throttle = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Get current throttle value
        /// </summary>
        public float GetThrottle() => _throttle;
    }
}
