// VFXARDataBinder - Binds AR depth, stencil, color, position data to VFX
// Attach to any VFX GameObject along with VFXPropertyBinder
// Works with runtime-spawned VFX - no registration needed
// Includes depth rotation and aspect fixes for Metavido-style VFX

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEngine.XR.ARFoundation;

namespace MetavidoVFX.VFX.Binders
{
    [VFXBinder("AR/AR Data")]
    public class VFXARDataBinder : VFXBinderBase
    {
        [Header("Data Sources (auto-found if null)")]
        public AROcclusionManager occlusionManager;
        public ARCameraBackground cameraBackground;
        public Camera arCamera;
        [Tooltip("Optional: Use ARCameraTextureProvider for color (original H3M approach)")]
        public Metavido.ARCameraTextureProvider colorProvider;

        [Header("Binding Options")]
        public bool bindDepthMap = true;
        public bool bindStencilMap = true;
        public bool bindColorMap = true;
        public bool bindPositionMap = true;
        public bool bindCameraMatrices = true;

        [Header("Depth Processing (ARKit)")]
        [Tooltip("Enable for Metavido VFX that sample DepthMap directly with UV→position conversion")]
        public bool rotateDepthTexture = true;
        [Tooltip("Mask depth with stencil for human-only particles")]
        public bool maskDepthWithStencil = true;

        [Header("Property Names")]
        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty depthMapProperty = "DepthMap";
        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty stencilMapProperty = "StencilMap";
        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty colorMapProperty = "ColorMap";
        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty positionMapProperty = "PositionMap";
        [VFXPropertyBinding("UnityEngine.Matrix4x4")]
        public ExposedProperty inverseViewProperty = "InverseView";
        [VFXPropertyBinding("UnityEngine.Matrix4x4")]
        public ExposedProperty inverseProjectionProperty = "InverseProjection";
        [VFXPropertyBinding("UnityEngine.Vector4")]
        public ExposedProperty rayParamsProperty = "RayParams";
        [VFXPropertyBinding("UnityEngine.Vector2")]
        public ExposedProperty depthRangeProperty = "DepthRange";

        [Header("Settings")]
        public Vector2 depthRange = new Vector2(0.1f, 10f);
        [Tooltip("Update textures every N frames (1=every frame, 2=every other frame)")]
        [Range(1, 4)]
        public int updateInterval = 1;

        [Header("Debug")]
        [Tooltip("Enable verbose logging (disable to reduce console spam)")]
        public bool verboseLogging = false;

        // One-time log tracking
        private bool _loggedInit;
        private bool _loggedColorRT;
        private bool _loggedPositionRT;
        private bool _loggedVelocityRT;
        private bool _loggedMaskedDepthRT;

        // Cached data from VFXBinderManager
        private static VFXBinderManager _sharedManager;

        // Rotation resources
        private Material _rotateUVMaterial;
        private RenderTexture _rotatedDepthRT;
        private RenderTexture _rotatedStencilRT;
        private Texture _lastRawDepth;
        private Texture _lastRawStencil;

        // Stencil masking resources
        private ComputeShader _maskDepthCompute;
        private RenderTexture _maskedDepthRT;
        private int _maskDepthKernel = -1;

        // Color capture (blit through ARCameraBackground material for YCbCr→RGB)
        private RenderTexture _colorRT;

        // PositionMap compute (depth → world positions) - matches VFXBinderManager
        private ComputeShader _depthToWorldCompute;
        private RenderTexture _positionMapRT;
        private int _depthToWorldKernel = -1;

        // VelocityMap compute (frame-to-frame motion)
        private RenderTexture _velocityMapRT;
        private RenderTexture _previousPositionMapRT;
        private int _velocityKernel = -1;

        protected override void Awake()
        {
            base.Awake();
            FindDataSources();

            // Load mask depth compute shader
            _maskDepthCompute = Resources.Load<ComputeShader>("MaskDepthWithStencil");
            if (_maskDepthCompute != null)
            {
                try { _maskDepthKernel = _maskDepthCompute.FindKernel("MaskDepth"); }
                catch { _maskDepthKernel = -1; }
            }

            // Load DepthToWorld compute shader (matches VFXBinderManager)
            if (bindPositionMap)
            {
                _depthToWorldCompute = Resources.Load<ComputeShader>("DepthToWorld");
                if (_depthToWorldCompute != null)
                {
                    try { _depthToWorldKernel = _depthToWorldCompute.FindKernel("DepthToWorld"); }
                    catch { _depthToWorldKernel = -1; }
                    try { _velocityKernel = _depthToWorldCompute.FindKernel("CalculateVelocity"); }
                    catch { _velocityKernel = -1; }
                    if (verboseLogging && !_loggedInit) { Debug.Log($"[VFXARDataBinder] DepthToWorld compute loaded: posKernel={_depthToWorldKernel} velKernel={_velocityKernel}"); _loggedInit = true; }
                }
                else
                {
                    Debug.LogWarning("[VFXARDataBinder] DepthToWorld compute shader not found in Resources");
                }
            }
        }

        void FindDataSources()
        {
            // Try to find shared VFXBinderManager first (most efficient)
            if (_sharedManager == null)
                _sharedManager = FindFirstObjectByType<VFXBinderManager>();

            // Fallback to direct AR component lookup
            if (occlusionManager == null)
                occlusionManager = FindFirstObjectByType<AROcclusionManager>();
            if (cameraBackground == null)
                cameraBackground = FindFirstObjectByType<ARCameraBackground>();
            if (arCamera == null)
                arCamera = Camera.main;
        }

        public override bool IsValid(VisualEffect component)
        {
            // Valid if at least one property exists
            return component.HasTexture(depthMapProperty) ||
                   component.HasTexture(stencilMapProperty) ||
                   component.HasTexture(colorMapProperty) ||
                   component.HasTexture(positionMapProperty) ||
                   component.HasMatrix4x4(inverseViewProperty);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            // Use VFXBinderManager's cached textures if available (shared compute)
            if (_sharedManager != null)
            {
                BindFromManager(component);
            }
            else
            {
                BindDirectly(component);
            }
        }

        void BindFromManager(VisualEffect component)
        {
            // Access VFXBinderManager's internal textures via reflection or public getters
            // For now, bind directly since manager handles all VFX anyway
            BindDirectly(component);
        }

        void BindDirectly(VisualEffect component)
        {
            // Re-find sources if references are missing or stale
            if (occlusionManager == null || !occlusionManager.enabled)
                occlusionManager = FindFirstObjectByType<AROcclusionManager>();
            if (cameraBackground == null || !cameraBackground.enabled)
                cameraBackground = FindFirstObjectByType<ARCameraBackground>();
            if (arCamera == null)
                arCamera = Camera.main;

            Texture depthTex = null;
            Texture stencilTex = null;

            // Get raw depth texture
            if (bindDepthMap && occlusionManager != null)
            {
                #pragma warning disable CS0618
                occlusionManager.TryGetEnvironmentDepthTexture(out depthTex);
                if (depthTex == null)
                    depthTex = occlusionManager.environmentDepthTexture;
                #pragma warning restore CS0618
            }

            // Get raw stencil texture
            if (bindStencilMap && occlusionManager != null)
            {
                #pragma warning disable CS0618
                stencilTex = occlusionManager.humanStencilTexture;
                #pragma warning restore CS0618
            }

            // Debug: log texture status once per second (only if verbose)
            if (verboseLogging && Time.frameCount % 60 == 0)
            {
                bool bgEnabled = cameraBackground != null && cameraBackground.enabled;
                bool bgMatReady = false;
                try { bgMatReady = cameraBackground?.material != null; } catch { }
                Debug.Log($"[VFXARDataBinder] depth={depthTex != null} stencil={stencilTex != null} color=(bg={cameraBackground != null} en={bgEnabled} mat={bgMatReady}) occMgr={occlusionManager != null} vfx={component.name}");
            }

            // Apply rotation if needed (ARKit depth is landscape, VFX expects portrait)
            if (rotateDepthTexture && depthTex != null)
            {
                depthTex = RotateTexture(depthTex, ref _rotatedDepthRT, ref _lastRawDepth);
            }
            if (rotateDepthTexture && stencilTex != null)
            {
                stencilTex = RotateTexture(stencilTex, ref _rotatedStencilRT, ref _lastRawStencil);
            }

            // Mask depth with stencil for human-only particles
            if (maskDepthWithStencil && _maskDepthKernel >= 0 && depthTex != null && stencilTex != null)
            {
                depthTex = MaskDepthWithStencil(depthTex, stencilTex);
            }

            // Bind Depth Map - try multiple property names (matches VFXBinderManager)
            if (depthTex != null)
            {
                if (component.HasTexture(depthMapProperty))
                    component.SetTexture(depthMapProperty, depthTex);
                if (component.HasTexture("DepthTexture"))
                    component.SetTexture("DepthTexture", depthTex);
            }

            // Bind Stencil Map - try multiple property names
            if (stencilTex != null)
            {
                if (component.HasTexture(stencilMapProperty))
                    component.SetTexture(stencilMapProperty, stencilTex);
                if (component.HasTexture("HumanStencil"))
                    component.SetTexture("HumanStencil", stencilTex);
                if (component.HasTexture("Stencil Map"))
                    component.SetTexture("Stencil Map", stencilTex);
            }

            // Bind Color Map - blit through ARCameraBackground material for YCbCr→RGB conversion
            // Check multiple property names (matches VFXBinderManager)
            bool hasColorProp = bindColorMap && (
                component.HasTexture(colorMapProperty) ||
                component.HasTexture("ColorTexture") ||
                component.HasTexture("Color Map"));

            if (hasColorProp)
            {
                try
                {
                    if (cameraBackground != null && cameraBackground.enabled && cameraBackground.material != null)
                    {
                        // Match VFXBinderManager approach: blit null through ARCameraBackground material
                        // This renders the AR background shader which converts YCbCr to RGB
                        const int maxColorRTSize = 1920; // Mobile-friendly size
                        int colorWidth = Mathf.Min(Screen.width, maxColorRTSize);
                        int colorHeight = Mathf.Min(Screen.height, maxColorRTSize);

                        // Create/resize color RT if needed
                        if (_colorRT == null || _colorRT.width != colorWidth || _colorRT.height != colorHeight)
                        {
                            if (_colorRT != null) _colorRT.Release();
                            _colorRT = new RenderTexture(colorWidth, colorHeight, 0, RenderTextureFormat.ARGB32);
                            _colorRT.Create();
                            if (verboseLogging && !_loggedColorRT) { Debug.Log($"[VFXARDataBinder] Created ColorRT: {colorWidth}x{colorHeight}"); _loggedColorRT = true; }
                        }

                        // Blit AR background (YCbCr→RGB conversion via material) - same as VFXBinderManager
                        if (Time.frameCount % updateInterval == 0)
                        {
                            Graphics.Blit(null, _colorRT, cameraBackground.material);
                            if (verboseLogging && Time.frameCount % 60 == 0)
                                Debug.Log($"[VFXARDataBinder] ColorMap BLITTED to {_colorRT.width}x{_colorRT.height}");
                        }

                        // Bind to all possible property names
                        bool boundColor = false;
                        if (component.HasTexture(colorMapProperty))
                        {
                            component.SetTexture(colorMapProperty, _colorRT);
                            boundColor = true;
                        }
                        if (component.HasTexture("ColorTexture"))
                        {
                            component.SetTexture("ColorTexture", _colorRT);
                            boundColor = true;
                        }
                        if (component.HasTexture("Color Map"))
                        {
                            component.SetTexture("Color Map", _colorRT);
                            boundColor = true;
                        }

                        if (verboseLogging && Time.frameCount % 180 == 0)
                            Debug.Log($"[VFXARDataBinder] ColorMap bound={boundColor} to '{component.name}' ({_colorRT.width}x{_colorRT.height})");
                    }
                    else if (Time.frameCount % 180 == 0)
                    {
                        Debug.LogWarning($"[VFXARDataBinder] ColorMap not available: bg={cameraBackground != null} enabled={cameraBackground?.enabled} mat={cameraBackground?.material != null}");
                    }
                }
                catch (System.NullReferenceException)
                {
                    // ARCameraBackground.material can throw internally before AR session is ready
                }
            }

            // Compute PositionMap (depth → world positions) - only if VFX needs it
            // Auto-detect: check if VFX has PositionMap, VelocityMap, or MapWidth/Height properties
            bool vfxNeedsPositionMap = bindPositionMap && (
                component.HasTexture(positionMapProperty) ||
                component.HasTexture("Position Map") ||
                component.HasTexture("VelocityMap") ||
                component.HasTexture("Velocity Map") ||
                component.HasInt("MapWidth") ||
                component.HasInt("MapHeight"));

            // Lazy init: load compute shader only if needed (handles domain reload)
            if (vfxNeedsPositionMap && _depthToWorldCompute == null)
            {
                _depthToWorldCompute = Resources.Load<ComputeShader>("DepthToWorld");
                if (_depthToWorldCompute != null)
                {
                    try { _depthToWorldKernel = _depthToWorldCompute.FindKernel("DepthToWorld"); }
                    catch { _depthToWorldKernel = -1; }
                    try { _velocityKernel = _depthToWorldCompute.FindKernel("CalculateVelocity"); }
                    catch { _velocityKernel = -1; }
                    if (verboseLogging) Debug.Log($"[VFXARDataBinder] DepthToWorld compute loaded for '{component.name}': posKernel={_depthToWorldKernel} velKernel={_velocityKernel}");
                }
                else
                {
                    Debug.LogWarning("[VFXARDataBinder] DepthToWorld compute shader not found in Resources");
                }
            }

            if (vfxNeedsPositionMap && _depthToWorldCompute != null && _depthToWorldKernel >= 0 && depthTex != null && arCamera != null)
            {
                int outWidth = depthTex.width;
                int outHeight = depthTex.height;

                // Create/resize PositionMap RT
                if (_positionMapRT == null || _positionMapRT.width != outWidth || _positionMapRT.height != outHeight)
                {
                    if (_positionMapRT != null) _positionMapRT.Release();
                    _positionMapRT = new RenderTexture(outWidth, outHeight, 0, RenderTextureFormat.ARGBFloat);
                    _positionMapRT.enableRandomWrite = true;
                    _positionMapRT.filterMode = FilterMode.Bilinear;
                    _positionMapRT.wrapMode = TextureWrapMode.Clamp;
                    _positionMapRT.Create();
                    if (verboseLogging && !_loggedPositionRT) { Debug.Log($"[VFXARDataBinder] Created PositionMap RT: {outWidth}x{outHeight}"); _loggedPositionRT = true; }
                }

                // Dispatch compute shader (respects updateInterval)
                if (Time.frameCount % updateInterval == 0)
                {
                    var proj = arCamera.projectionMatrix;
                    var invVP = (proj * arCamera.transform.worldToLocalMatrix).inverse;

                    _depthToWorldCompute.SetMatrix("_InvVP", invVP);
                    _depthToWorldCompute.SetMatrix("_ProjectionMatrix", proj);
                    _depthToWorldCompute.SetVector("_DepthRange", new Vector4(depthRange.x, depthRange.y, 0.5f, 0));
                    _depthToWorldCompute.SetTexture(_depthToWorldKernel, "_Depth", depthTex);
                    _depthToWorldCompute.SetTexture(_depthToWorldKernel, "_Stencil", stencilTex != null ? stencilTex : Texture2D.whiteTexture);
                    _depthToWorldCompute.SetTexture(_depthToWorldKernel, "_PositionRT", _positionMapRT);
                    _depthToWorldCompute.SetInt("_UseStencil", stencilTex != null ? 1 : 0);
                    _depthToWorldCompute.SetInt("_RotateUV90CW", 0); // Already rotated by blit

                    int groupsX = Mathf.CeilToInt(outWidth / 32f);
                    int groupsY = Mathf.CeilToInt(outHeight / 32f);
                    _depthToWorldCompute.Dispatch(_depthToWorldKernel, groupsX, groupsY, 1);

                    // VelocityMap computation
                    if (_velocityKernel >= 0)
                    {
                        // Create/resize velocity RTs
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
                            if (verboseLogging && !_loggedVelocityRT) { Debug.Log($"[VFXARDataBinder] Created VelocityMap RT: {outWidth}x{outHeight}"); _loggedVelocityRT = true; }
                        }

                        _depthToWorldCompute.SetTexture(_velocityKernel, "_PositionRT", _positionMapRT);
                        _depthToWorldCompute.SetTexture(_velocityKernel, "_PreviousPositionRT", _previousPositionMapRT);
                        _depthToWorldCompute.SetTexture(_velocityKernel, "_VelocityRT", _velocityMapRT);
                        _depthToWorldCompute.SetFloat("_DeltaTime", Time.deltaTime);
                        _depthToWorldCompute.Dispatch(_velocityKernel, groupsX, groupsY, 1);

                        // Copy current to previous for next frame
                        Graphics.Blit(_positionMapRT, _previousPositionMapRT);
                    }

                    if (verboseLogging && Time.frameCount % 180 == 0)
                        Debug.Log($"[VFXARDataBinder] PositionMap dispatched: {outWidth}x{outHeight} groups={groupsX}x{groupsY}");
                }

                // Bind PositionMap
                if (component.HasTexture(positionMapProperty))
                    component.SetTexture(positionMapProperty, _positionMapRT);
                if (component.HasTexture("Position Map"))
                    component.SetTexture("Position Map", _positionMapRT);

                // Bind VelocityMap
                if (_velocityMapRT != null)
                {
                    if (component.HasTexture("VelocityMap"))
                        component.SetTexture("VelocityMap", _velocityMapRT);
                    if (component.HasTexture("Velocity Map"))
                        component.SetTexture("Velocity Map", _velocityMapRT);
                }

                // Bind map dimensions for Akvfx-style VFX
                if (component.HasInt("MapWidth"))
                    component.SetInt("MapWidth", _positionMapRT.width);
                if (component.HasInt("MapHeight"))
                    component.SetInt("MapHeight", _positionMapRT.height);
            }
            else if (verboseLogging && bindPositionMap && !vfxNeedsPositionMap && Time.frameCount % 180 == 0)
            {
                // VFX doesn't need PositionMap - uses DepthMap + RayParams directly (more efficient)
                Debug.Log($"[VFXARDataBinder] '{component.name}' skipping PositionMap compute (not needed)");
            }

            // Camera Matrices
            if (bindCameraMatrices && arCamera != null)
            {
                // Use TRS to match Keijiro's Metavido RenderUtils.cs approach (same as VFXBinderManager)
                var inverseView = Matrix4x4.TRS(
                    arCamera.transform.position,
                    arCamera.transform.rotation,
                    Vector3.one);

                // Bind InverseView to all possible property names
                if (component.HasMatrix4x4(inverseViewProperty))
                    component.SetMatrix4x4(inverseViewProperty, inverseView);
                if (component.HasMatrix4x4("InverseViewMatrix"))
                    component.SetMatrix4x4("InverseViewMatrix", inverseView);

                if (component.HasMatrix4x4(inverseProjectionProperty))
                    component.SetMatrix4x4(inverseProjectionProperty, arCamera.projectionMatrix.inverse);

                if (component.HasVector4(rayParamsProperty))
                {
                    // Extract principal point offset from projection matrix (matches Keijiro's RenderUtils.cs)
                    var proj = arCamera.projectionMatrix;
                    float centerShiftX = proj.m02;
                    float centerShiftY = proj.m12;

                    float fovV = arCamera.fieldOfView * Mathf.Deg2Rad;
                    float tanV = Mathf.Tan(fovV * 0.5f);
                    float tanH;

                    // Use depth texture aspect when rotated (depth FOV differs from camera FOV)
                    Vector4 rayParams;
                    if (rotateDepthTexture && depthTex != null)
                    {
                        float depthAspect = (float)depthTex.width / depthTex.height;
                        tanH = tanV * depthAspect;
                        // Negate tanH for rotated depth to fix horizontal flip
                        rayParams = new Vector4(centerShiftX, centerShiftY, -tanH, tanV);

                        if (verboseLogging && Time.frameCount % 180 == 0)
                            Debug.Log($"[VFXARDataBinder] RayParams={rayParams} fov={arCamera.fieldOfView} depthAspect={depthAspect}");
                    }
                    else
                    {
                        tanH = tanV * arCamera.aspect;
                        rayParams = new Vector4(centerShiftX, centerShiftY, tanH, tanV);
                    }

                    // Bind RayParams to all possible property names
                    component.SetVector4(rayParamsProperty, rayParams);
                    if (component.HasVector4("ProjectionVector"))
                        component.SetVector4("ProjectionVector", rayParams);
                }

                // DepthRange - used by VFX for depth clipping
                if (component.HasVector2(depthRangeProperty))
                {
                    component.SetVector2(depthRangeProperty, depthRange);
                    if (verboseLogging && Time.frameCount % 180 == 0)
                        Debug.Log($"[VFXARDataBinder] DepthRange bound: ({depthRange.x:F2}, {depthRange.y:F2}) to '{depthRangeProperty}'");
                }
                else if (verboseLogging && Time.frameCount % 180 == 0)
                {
                    Debug.Log($"[VFXARDataBinder] VFX '{component.name}' has no '{depthRangeProperty}' property");
                }
            }
            else if (verboseLogging && Time.frameCount % 60 == 0)
            {
                Debug.LogWarning($"[VFXARDataBinder] Camera matrices NOT bound: bindCameraMatrices={bindCameraMatrices} arCamera={arCamera != null}");
            }
        }

        /// <summary>
        /// Rotates a texture 90° CW using the RotateUV90CW shader
        /// </summary>
        Texture RotateTexture(Texture source, ref RenderTexture rotatedRT, ref Texture lastRaw)
        {
            if (source == null) return null;

            // Initialize rotation material
            if (_rotateUVMaterial == null)
            {
                var shader = Shader.Find("Hidden/RotateUV90CW");
                if (shader == null)
                {
                    Debug.LogWarning("[VFXARDataBinder] RotateUV90CW shader not found, using unrotated texture");
                    return source;
                }
                _rotateUVMaterial = new Material(shader);
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

            // Blit with optional frame skipping for performance
            // AR textures update contents every frame while keeping same reference
            if (Time.frameCount % updateInterval == 0)
            {
                Graphics.Blit(source, rotatedRT, _rotateUVMaterial);
                lastRaw = source;
            }

            return rotatedRT;
        }

        /// <summary>
        /// Masks depth texture with stencil for human-only particles
        /// </summary>
        Texture MaskDepthWithStencil(Texture depthTex, Texture stencilTex)
        {
            if (depthTex == null || stencilTex == null || _maskDepthCompute == null || _maskDepthKernel < 0)
            {
                if (verboseLogging && Time.frameCount % 60 == 0)
                    Debug.LogWarning($"[VFXARDataBinder] MaskDepth skipped: depth={depthTex != null} stencil={stencilTex != null} compute={_maskDepthCompute != null} kernel={_maskDepthKernel}");
                return depthTex;
            }

            int w = depthTex.width;
            int h = depthTex.height;

            // Create or resize masked depth RT
            if (_maskedDepthRT == null || _maskedDepthRT.width != w || _maskedDepthRT.height != h)
            {
                if (_maskedDepthRT != null) _maskedDepthRT.Release();
                _maskedDepthRT = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat);
                _maskedDepthRT.enableRandomWrite = true;
                _maskedDepthRT.filterMode = FilterMode.Point;
                _maskedDepthRT.Create();
                if (verboseLogging && !_loggedMaskedDepthRT) { Debug.Log($"[VFXARDataBinder] Created masked depth RT: {w}x{h}"); _loggedMaskedDepthRT = true; }
            }

            // Run mask compute shader (respects updateInterval)
            if (Time.frameCount % updateInterval == 0)
            {
                _maskDepthCompute.SetTexture(_maskDepthKernel, "_Depth", depthTex);
                _maskDepthCompute.SetTexture(_maskDepthKernel, "_Stencil", stencilTex);
                _maskDepthCompute.SetTexture(_maskDepthKernel, "_MaskedDepthRT", _maskedDepthRT);
                _maskDepthCompute.SetFloat("_StencilThreshold", 0.5f);

                int groupsX = Mathf.CeilToInt(w / 32f);
                int groupsY = Mathf.CeilToInt(h / 32f);
                _maskDepthCompute.Dispatch(_maskDepthKernel, groupsX, groupsY, 1);

                if (verboseLogging && Time.frameCount % 180 == 0)
                    Debug.Log($"[VFXARDataBinder] Masked depth dispatched: {w}x{h} groups={groupsX}x{groupsY}");
            }

            return _maskedDepthRT;
        }

        void OnDestroy()
        {
            if (_rotatedDepthRT != null)
            {
                _rotatedDepthRT.Release();
                _rotatedDepthRT = null;
            }
            if (_rotatedStencilRT != null)
            {
                _rotatedStencilRT.Release();
                _rotatedStencilRT = null;
            }
            if (_rotateUVMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_rotateUVMaterial);
                else
                    DestroyImmediate(_rotateUVMaterial);
                _rotateUVMaterial = null;
            }
            if (_maskedDepthRT != null)
            {
                _maskedDepthRT.Release();
                _maskedDepthRT = null;
            }
            if (_colorRT != null)
            {
                _colorRT.Release();
                _colorRT = null;
            }
            if (_positionMapRT != null)
            {
                _positionMapRT.Release();
                _positionMapRT = null;
            }
            if (_velocityMapRT != null)
            {
                _velocityMapRT.Release();
                _velocityMapRT = null;
            }
            if (_previousPositionMapRT != null)
            {
                _previousPositionMapRT.Release();
                _previousPositionMapRT = null;
            }
        }

        public override string ToString()
        {
            return $"AR Data : {depthMapProperty}, {stencilMapProperty}, {colorMapProperty}";
        }
    }
}
