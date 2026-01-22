// ┌────────────────────────────────────────────────────────────────────────────┐
// │ DEPRECATED - Use VFXARBinder + ARDepthSource instead                       │
// │                                                                            │
// │ This 972-line binder runs compute EVERY FRAME per-VFX (O(N) dispatches).   │
// │ The new Hybrid Bridge Pattern uses:                                        │
// │   • ARDepthSource (singleton) - ONE compute dispatch for ALL VFX           │
// │   • VFXARBinder (per-VFX) - lightweight SetTexture() calls only            │
// │                                                                            │
// │ Migration: Remove this component + VFXPropertyBinder, add VFXARBinder      │
// │ Setup: H3M > VFX Pipeline Master > Setup Complete Pipeline                 │
// │ Date deprecated: 2026-01-16                                                │
// └────────────────────────────────────────────────────────────────────────────┘

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEngine.XR.ARFoundation;
using MetavidoVFX.Audio;

namespace MetavidoVFX.VFX.Binders
{
    [VFXBinder("AR/AR Data (DEPRECATED)")]
    [System.Obsolete("Use VFXARBinder + ARDepthSource instead. This binder runs compute per-VFX (O(N)), the new system runs O(1) compute.")]
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

        [Header("Throttle Binding (Optional)")]
        [Tooltip("Enable throttle property binding - controls overall VFX intensity")]
        public bool bindThrottle = false;
        [Tooltip("Throttle value (0-1) - scales particle count, size, or emission")]
        [Range(0f, 1f)]
        public float throttleValue = 1f;

        [Header("Normal Map Binding (Optional)")]
        [Tooltip("Enable normal map binding - provides surface orientation data")]
        public bool bindNormalMap = false;
        [Tooltip("Custom normal map texture (computed from depth if null)")]
        public Texture normalMapOverride;

        [Header("Velocity Binding (Optional)")]
        [Tooltip("Enable velocity-driven VFX binding - camera movement affects particles")]
        public bool bindVelocity = false;
        [Tooltip("Velocity scale multiplier")]
        [Range(0.1f, 10f)]
        public float velocityScale = 1f;
        [Tooltip("Smooth velocity changes (0 = instant, 1 = very smooth)")]
        [Range(0f, 0.99f)]
        public float velocitySmoothing = 0.5f;

        [Header("Gravity/Physics Binding (Optional)")]
        [Tooltip("Enable gravity/physics binding")]
        public bool bindGravity = false;
        [Tooltip("Gravity strength (Y-axis, negative = down)")]
        [Range(-20f, 20f)]
        public float gravityStrength = -9.81f;

        [Header("Audio Binding (Optional)")]
        [Tooltip("Enable audio frequency band binding - useful for audio-reactive VFX")]
        public bool bindAudio = false;
        [Tooltip("Audio processor (auto-found if null)")]
        public EnhancedAudioProcessor audioProcessor;
        [Range(0f, 2f)]
        public float audioVolumeMultiplier = 1f;
        [Range(0f, 2f)]
        public float audioBandMultiplier = 1f;

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

        [Header("Throttle Property Names")]
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty throttleProperty = "Throttle";

        [Header("Normal Map Property Names")]
        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty normalMapProperty = "NormalMap";

        [Header("Velocity Property Names")]
        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty velocityProperty = "Velocity";
        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty referenceVelocityProperty = "ReferenceVelocity";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty speedProperty = "Speed";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty cameraSpeedProperty = "CameraSpeed";

        [Header("Gravity Property Names")]
        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty gravityProperty = "Gravity";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty gravityStrengthProperty = "GravityStrength";

        [Header("Audio Property Names")]
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty audioVolumeProperty = "AudioVolume";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty audioBassProperty = "AudioBass";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty audioMidProperty = "AudioMid";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty audioTrebleProperty = "AudioTreble";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty audioSubBassProperty = "AudioSubBass";

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

        // Camera velocity tracking (for velocity binding)
        private Vector3 _lastCameraPosition;
        private Vector3 _smoothedVelocity;
        private float _smoothedSpeed;
        private bool _velocityInitialized;

        // Normal map compute
        private ComputeShader _normalMapCompute;
        private RenderTexture _normalMapRT;
        private int _normalMapKernel = -1;

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

            // Load NormalMap compute shader (if available)
            if (bindNormalMap)
            {
                _normalMapCompute = Resources.Load<ComputeShader>("DepthToNormal");
                if (_normalMapCompute != null)
                {
                    try { _normalMapKernel = _normalMapCompute.FindKernel("DepthToNormal"); }
                    catch { _normalMapKernel = -1; }
                }
                // Note: NormalMap compute is optional - can use override texture instead
            }

            // Initialize velocity tracking
            if (bindVelocity && arCamera != null)
            {
                _lastCameraPosition = arCamera.transform.position;
                _velocityInitialized = true;
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

            // Find audio processor if audio binding enabled
            if (bindAudio && audioProcessor == null)
                audioProcessor = FindFirstObjectByType<EnhancedAudioProcessor>();
        }

        public override bool IsValid(VisualEffect component)
        {
            // Valid if at least one AR or optional property exists
            bool hasARProperty = component.HasTexture(depthMapProperty) ||
                   component.HasTexture(stencilMapProperty) ||
                   component.HasTexture(colorMapProperty) ||
                   component.HasTexture(positionMapProperty) ||
                   component.HasMatrix4x4(inverseViewProperty);

            bool hasThrottleProperty = bindThrottle && component.HasFloat(throttleProperty);

            bool hasNormalMapProperty = bindNormalMap && component.HasTexture(normalMapProperty);

            bool hasVelocityProperty = bindVelocity && (
                   component.HasVector3(velocityProperty) ||
                   component.HasVector3(referenceVelocityProperty) ||
                   component.HasFloat(speedProperty) ||
                   component.HasFloat(cameraSpeedProperty));

            bool hasGravityProperty = bindGravity && (
                   component.HasVector3(gravityProperty) ||
                   component.HasFloat(gravityStrengthProperty));

            bool hasAudioProperty = bindAudio && (
                   component.HasFloat(audioVolumeProperty) ||
                   component.HasFloat(audioBassProperty) ||
                   component.HasFloat(audioMidProperty) ||
                   component.HasFloat(audioTrebleProperty));

            return hasARProperty || hasThrottleProperty || hasNormalMapProperty ||
                   hasVelocityProperty || hasGravityProperty || hasAudioProperty;
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

            // ColorMap binding: Skip custom binding, let VFXPropertyBinder handle it via registered binders
            // The ARCameraTextureProvider component on AR Camera already handles YCbCr→RGB conversion
            // and provides proper texture updates via Metavido.ARCameraTextureProvider.Texture property

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

            // Throttle Binding (optional - controls VFX intensity)
            if (bindThrottle)
            {
                BindThrottle(component);
            }

            // Normal Map Binding (optional - provides surface orientation)
            if (bindNormalMap)
            {
                BindNormalMap(component, depthTex);
            }

            // Velocity Binding (optional - camera movement affects particles)
            if (bindVelocity)
            {
                BindVelocity(component);
            }

            // Gravity Binding (optional - physics simulation)
            if (bindGravity)
            {
                BindGravity(component);
            }

            // Audio Binding (optional - for audio-reactive VFX)
            if (bindAudio && audioProcessor != null)
            {
                BindAudio(component);
            }
        }

        void BindAudio(VisualEffect component)
        {
            // Volume - bind to multiple property names for compatibility
            float volume = audioProcessor.AudioVolume * audioVolumeMultiplier;
            if (component.HasFloat(audioVolumeProperty))
                component.SetFloat(audioVolumeProperty, volume);
            if (component.HasFloat("Volume"))
                component.SetFloat("Volume", volume);

            // Bass
            float bass = audioProcessor.AudioBass * audioBandMultiplier;
            if (component.HasFloat(audioBassProperty))
                component.SetFloat(audioBassProperty, bass);
            if (component.HasFloat("Bass"))
                component.SetFloat("Bass", bass);

            // Mid
            float mid = audioProcessor.AudioMid * audioBandMultiplier;
            if (component.HasFloat(audioMidProperty))
                component.SetFloat(audioMidProperty, mid);
            if (component.HasFloat("Mid"))
                component.SetFloat("Mid", mid);

            // Treble
            float treble = audioProcessor.AudioTreble * audioBandMultiplier;
            if (component.HasFloat(audioTrebleProperty))
                component.SetFloat(audioTrebleProperty, treble);
            if (component.HasFloat("Treble"))
                component.SetFloat("Treble", treble);
            if (component.HasFloat("High"))
                component.SetFloat("High", treble);

            // SubBass
            float subBass = audioProcessor.AudioSubBass * audioBandMultiplier;
            if (component.HasFloat(audioSubBassProperty))
                component.SetFloat(audioSubBassProperty, subBass);
            if (component.HasFloat("SubBass"))
                component.SetFloat("SubBass", subBass);

            if (verboseLogging && Time.frameCount % 180 == 0)
                Debug.Log($"[VFXARDataBinder] Audio bound: vol={volume:F2} bass={bass:F2} mid={mid:F2} treble={treble:F2}");
        }

        void BindThrottle(VisualEffect component)
        {
            // Throttle - bind to multiple property names for compatibility
            if (component.HasFloat(throttleProperty))
                component.SetFloat(throttleProperty, throttleValue);
            if (component.HasFloat("throttle"))
                component.SetFloat("throttle", throttleValue);
            if (component.HasFloat("Intensity"))
                component.SetFloat("Intensity", throttleValue);
            if (component.HasFloat("Scale"))
                component.SetFloat("Scale", throttleValue);
        }

        void BindNormalMap(VisualEffect component, Texture depthTex)
        {
            // Use override texture if provided
            if (normalMapOverride != null)
            {
                if (component.HasTexture(normalMapProperty))
                    component.SetTexture(normalMapProperty, normalMapOverride);
                if (component.HasTexture("Normal Map"))
                    component.SetTexture("Normal Map", normalMapOverride);
                return;
            }

            // Compute normal map from depth (if compute shader available)
            if (_normalMapCompute != null && _normalMapKernel >= 0 && depthTex != null)
            {
                int w = depthTex.width;
                int h = depthTex.height;

                // Create/resize normal map RT
                if (_normalMapRT == null || _normalMapRT.width != w || _normalMapRT.height != h)
                {
                    if (_normalMapRT != null) _normalMapRT.Release();
                    _normalMapRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat);
                    _normalMapRT.enableRandomWrite = true;
                    _normalMapRT.filterMode = FilterMode.Bilinear;
                    _normalMapRT.Create();
                }

                // Dispatch compute shader
                if (Time.frameCount % updateInterval == 0)
                {
                    _normalMapCompute.SetTexture(_normalMapKernel, "_Depth", depthTex);
                    _normalMapCompute.SetTexture(_normalMapKernel, "_NormalRT", _normalMapRT);

                    int groupsX = Mathf.CeilToInt(w / 8f);
                    int groupsY = Mathf.CeilToInt(h / 8f);
                    _normalMapCompute.Dispatch(_normalMapKernel, groupsX, groupsY, 1);
                }

                // Bind
                if (component.HasTexture(normalMapProperty))
                    component.SetTexture(normalMapProperty, _normalMapRT);
                if (component.HasTexture("Normal Map"))
                    component.SetTexture("Normal Map", _normalMapRT);
            }
        }

        void BindVelocity(VisualEffect component)
        {
            // Initialize velocity tracking if needed
            if (!_velocityInitialized && arCamera != null)
            {
                _lastCameraPosition = arCamera.transform.position;
                _velocityInitialized = true;
            }

            if (!_velocityInitialized || arCamera == null) return;

            // Calculate camera velocity
            Vector3 cameraDelta = arCamera.transform.position - _lastCameraPosition;
            Vector3 rawVelocity = cameraDelta / Time.deltaTime * velocityScale;
            _lastCameraPosition = arCamera.transform.position;

            // Smooth velocity
            float smoothFactor = 1f - velocitySmoothing;
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, rawVelocity, smoothFactor);
            _smoothedSpeed = _smoothedVelocity.magnitude;

            // Bind velocity to all possible property names
            if (component.HasVector3(velocityProperty))
                component.SetVector3(velocityProperty, _smoothedVelocity);
            if (component.HasVector3(referenceVelocityProperty))
                component.SetVector3(referenceVelocityProperty, _smoothedVelocity);
            if (component.HasVector3("Initial Velocity"))
                component.SetVector3("Initial Velocity", _smoothedVelocity);
            if (component.HasVector3("CameraVelocity"))
                component.SetVector3("CameraVelocity", _smoothedVelocity);

            // Bind speed
            if (component.HasFloat(speedProperty))
                component.SetFloat(speedProperty, _smoothedSpeed);
            if (component.HasFloat(cameraSpeedProperty))
                component.SetFloat(cameraSpeedProperty, _smoothedSpeed);
            if (component.HasFloat("VelocityMagnitude"))
                component.SetFloat("VelocityMagnitude", _smoothedSpeed);

            if (verboseLogging && Time.frameCount % 180 == 0)
                Debug.Log($"[VFXARDataBinder] Velocity bound: {_smoothedVelocity:F2} speed={_smoothedSpeed:F2}");
        }

        void BindGravity(VisualEffect component)
        {
            // Calculate gravity vector
            Vector3 gravityVector = new Vector3(0f, gravityStrength, 0f);

            // Bind gravity to all possible property names
            if (component.HasVector3(gravityProperty))
                component.SetVector3(gravityProperty, gravityVector);
            if (component.HasVector3("Gravity Vector"))
                component.SetVector3("Gravity Vector", gravityVector);

            // Bind gravity strength as scalar
            if (component.HasFloat(gravityStrengthProperty))
                component.SetFloat(gravityStrengthProperty, gravityStrength);
            if (component.HasFloat("GravityY"))
                component.SetFloat("GravityY", gravityStrength);

            if (verboseLogging && Time.frameCount % 180 == 0)
                Debug.Log($"[VFXARDataBinder] Gravity bound: {gravityVector:F2} strength={gravityStrength:F2}");
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
            if (_normalMapRT != null)
            {
                _normalMapRT.Release();
                _normalMapRT = null;
            }
        }

        public override string ToString()
        {
            var extras = new System.Collections.Generic.List<string>();
            if (bindThrottle) extras.Add("Throttle");
            if (bindNormalMap) extras.Add("NormalMap");
            if (bindVelocity) extras.Add("Velocity");
            if (bindGravity) extras.Add("Gravity");
            if (bindAudio) extras.Add("Audio");

            string extraStr = extras.Count > 0 ? $" + {string.Join(", ", extras)}" : "";
            return $"AR Data : {depthMapProperty}, {stencilMapProperty}, {colorMapProperty}{extraStr}";
        }

        // ========== PUBLIC API ==========

        /// <summary>
        /// Set throttle value at runtime (0-1)
        /// </summary>
        public void SetThrottle(float value)
        {
            throttleValue = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Set velocity binding enabled state at runtime
        /// </summary>
        public void SetVelocityEnabled(bool enabled)
        {
            bindVelocity = enabled;
        }

        /// <summary>
        /// Set gravity binding enabled state at runtime
        /// </summary>
        public void SetGravityEnabled(bool enabled)
        {
            bindGravity = enabled;
        }

        /// <summary>
        /// Set gravity strength at runtime (-20 to 20)
        /// </summary>
        public void SetGravityStrength(float strength)
        {
            gravityStrength = Mathf.Clamp(strength, -20f, 20f);
        }
    }
}
