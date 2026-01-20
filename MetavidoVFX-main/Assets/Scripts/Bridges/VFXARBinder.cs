using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using H3M.Core;
using MetavidoVFX.VFX;

/// <summary>
/// Lightweight binder - reads from ARDepthSource, binds to one VFX.
/// NO compute dispatch. Just SetTexture() calls.
/// Like Keijiro's VFXRcamBinder but even simpler.
/// Uses ExposedProperty for proper VFX Graph property resolution.
///
/// NEW (spec-007): Integrates with VFXCategory for mode-aware binding.
/// SetMode() allows runtime switching between People, Environment, Face, Hands, Audio modes.
/// </summary>
[RequireComponent(typeof(VisualEffect))]
public class VFXARBinder : MonoBehaviour
{
    VisualEffect _vfx;
    VFXCategory _category;

    [Header("Source")]
    [Tooltip("ARDepthSource to read from. If null, uses ARDepthSource.Instance.")]
    [SerializeField] ARDepthSource _source;

    [Header("Binding Toggles")]
    [Tooltip("Bind depth texture from AR")]
    [SerializeField] bool _bindDepthMap = true;
    [Tooltip("Bind human stencil mask")]
    [SerializeField] bool _bindStencilMap = true;
    [Tooltip("Bind computed world positions")]
    [SerializeField] bool _bindPositionMap = true;
    [Tooltip("Bind camera color texture")]
    [SerializeField] bool _bindColorMap = true;
    [Tooltip("Bind velocity texture (requires ARDepthSource velocity enabled)")]
    [SerializeField] bool _bindVelocityMap = false;
    [Tooltip("Bind ray parameters for depth reconstruction")]
    [SerializeField] bool _bindRayParams = true;
    [Tooltip("Bind inverse view matrix")]
    [SerializeField] bool _bindInverseView = true;
    [Tooltip("Bind depth range (near/far)")]
    [SerializeField] bool _bindDepthRange = true;
    [Tooltip("Bind inverse projection matrix")]
    [SerializeField] bool _bindInverseProj = false;

    [Header("Depth Range")]
    [Tooltip("Minimum depth in meters")]
    [SerializeField] float _depthMin = 0.1f;
    [Tooltip("Maximum depth in meters")]
    [SerializeField] float _depthMax = 10f;

    [Header("Throttle / Intensity")]
    [Tooltip("Bind throttle value to VFX")]
    [SerializeField] bool _bindThrottle = false;
    [Tooltip("Throttle value (0-1) to control VFX intensity")]
    [Range(0f, 1f)]
    [SerializeField] float _throttle = 1f;
    [Tooltip("Property name for throttle (common: Throttle, Intensity, Scale)")]
    [SerializeField] string _throttleProperty = "Throttle";

    [Header("Audio (Optional)")]
    [Tooltip("Bind audio data from AudioBridge")]
    [SerializeField] bool _bindAudio = false;
    [Tooltip("Property name for audio volume")]
    [SerializeField] string _audioVolumeProperty = "AudioVolume";
    [Tooltip("Property name for audio bands (Vector4: bass, mid, treble, unused)")]
    [SerializeField] string _audioBandsProperty = "AudioBands";

    [Header("Hologram (Mini-Me)")]
    [Tooltip("Bind anchor position for hologram placement")]
    [SerializeField] bool _bindAnchorPos = false;
    [Tooltip("Bind hologram scale for mini-me effect")]
    [SerializeField] bool _bindHologramScale = false;
    [Tooltip("Transform to use as anchor point (e.g., HologramAnchor)")]
    [SerializeField] Transform _anchorTransform;
    [Tooltip("Scale factor for hologram (1.0 = life-size, 0.1 = 10% scale)")]
    [Range(0.01f, 2f)]
    [SerializeField] float _hologramScale = 0.15f;
    [Tooltip("If true, directly transforms the VFX GameObject instead of using properties (works without VFX modification)")]
    [SerializeField] bool _useTransformMode = true;

    [Header("Property Names (customize if VFX uses different names)")]
    [VFXPropertyBinding("UnityEngine.Texture2D")]
    public ExposedProperty depthMapProperty = "DepthMap";
    [VFXPropertyBinding("UnityEngine.Texture2D")]
    public ExposedProperty stencilMapProperty = "StencilMap";
    [VFXPropertyBinding("UnityEngine.Texture2D")]
    public ExposedProperty positionMapProperty = "PositionMap";
    [VFXPropertyBinding("UnityEngine.Texture2D")]
    public ExposedProperty colorMapProperty = "ColorMap";
    [VFXPropertyBinding("UnityEngine.Texture2D")]
    public ExposedProperty velocityMapProperty = "VelocityMap";
    [VFXPropertyBinding("UnityEngine.Vector4")]
    public ExposedProperty rayParamsProperty = "RayParams";
    [VFXPropertyBinding("UnityEngine.Matrix4x4")]
    public ExposedProperty inverseViewProperty = "InverseView";
    [VFXPropertyBinding("UnityEngine.Matrix4x4")]
    public ExposedProperty inverseProjProperty = "InverseProjection";
    [VFXPropertyBinding("UnityEngine.Vector2")]
    public ExposedProperty depthRangeProperty = "DepthRange";

    [VFXPropertyBinding("UnityEngine.Vector3")]
    public ExposedProperty anchorPosProperty = "AnchorPos";
    [VFXPropertyBinding("System.Single")]
    public ExposedProperty hologramScaleProperty = "HologramScale";

    [Header("Auto-Binding")]
    [Tooltip("Automatically detect and enable bindings based on VFX properties")]
    [SerializeField] bool _autoBindOnStart = true;

    [Header("Mode System (spec-007)")]
    [Tooltip("Use VFXCategory bindings instead of manual toggles")]
    [SerializeField] bool _useCategoryBindings = true;
    [Tooltip("Current VFX mode (read-only, use SetMode() to change)")]
    [SerializeField] VFXCategoryType _currentMode = VFXCategoryType.People;

    // Property aliases for cross-project compatibility (Rcam2, Rcam3, Rcam4, Akvfx, H3M)
    // Standard name is first, aliases follow
    static readonly string[] DepthMapAliases = { "DepthMap", "Depth", "DepthTexture", "_Depth" };
    static readonly string[] StencilMapAliases = { "StencilMap", "Stencil", "HumanStencil", "StencilTexture" };
    static readonly string[] PositionMapAliases = { "PositionMap", "Position", "WorldPosition", "WorldPos" };
    static readonly string[] ColorMapAliases = { "ColorMap", "Color", "ColorTexture", "CameraColor", "MainTex" };
    static readonly string[] VelocityMapAliases = { "VelocityMap", "Velocity", "Motion", "MotionVector" };
    static readonly string[] RayParamsAliases = { "RayParams", "RayParameters", "CameraParams" };
    static readonly string[] InverseViewAliases = { "InverseView", "InvView", "CameraToWorld", "ViewInverse" };
    static readonly string[] InverseProjAliases = { "InverseProjection", "RayParamsMatrix", "InvProj", "ProjectionInverse" };
    static readonly string[] DepthRangeAliases = { "DepthRange", "DepthClip", "NearFar", "ClipRange" };
    static readonly string[] ThrottleAliases = { "Throttle", "Intensity", "Scale", "Amount", "Strength" };

    // Resolved property names (set during AutoDetectBindings)
    string _resolvedDepthMap;
    string _resolvedStencilMap;
    string _resolvedPositionMap;
    string _resolvedColorMap;
    string _resolvedVelocityMap;
    string _resolvedRayParams;
    string _resolvedInverseView;
    string _resolvedInverseProj;
    string _resolvedDepthRange;
    string _resolvedThrottle;

    [Header("Debug")]
    [SerializeField] bool _verboseLogging = false;

    // Status for Dashboard (public read-only)
    public bool IsBound { get; private set; }
    public int BoundCount { get; private set; }
    public ARDepthSource Source => _source != null ? _source : ARDepthSource.Instance;
    public float Throttle { get => _throttle; set => _throttle = Mathf.Clamp01(value); }
    public Vector2 DepthRange { get => new Vector2(_depthMin, _depthMax); set { _depthMin = value.x; _depthMax = value.y; } }

    // Mode system (spec-007)
    public VFXCategoryType CurrentMode => _currentMode;
    public VFXCategory Category => _category;
    public bool UseCategoryBindings { get => _useCategoryBindings; set => _useCategoryBindings = value; }

    // Binding toggle accessors
    public bool BindDepthMap { get => _bindDepthMap; set => _bindDepthMap = value; }
    public bool BindStencilMap { get => _bindStencilMap; set => _bindStencilMap = value; }
    public bool BindPositionMap { get => _bindPositionMap; set => _bindPositionMap = value; }
    public bool BindColorMap { get => _bindColorMap; set => _bindColorMap = value; }
    public bool BindVelocityMap { get => _bindVelocityMap; set => _bindVelocityMap = value; }
    public bool BindRayParams { get => _bindRayParams; set => _bindRayParams = value; }
    public bool BindInverseView { get => _bindInverseView; set => _bindInverseView = value; }
    public bool BindDepthRange { get => _bindDepthRange; set => _bindDepthRange = value; }
    public bool BindThrottle { get => _bindThrottle; set => _bindThrottle = value; }
    public bool BindAudio { get => _bindAudio; set => _bindAudio = value; }
    public bool BindAnchorPos { get => _bindAnchorPos; set => _bindAnchorPos = value; }
    public bool BindHologramScale { get => _bindHologramScale; set => _bindHologramScale = value; }
    public Transform AnchorTransform { get => _anchorTransform; set => _anchorTransform = value; }
    public float HologramScale { get => _hologramScale; set => _hologramScale = Mathf.Clamp(value, 0.01f, 2f); }
    public bool UseTransformMode { get => _useTransformMode; set => _useTransformMode = value; }

    void Awake()
    {
        _vfx = GetComponent<VisualEffect>();
        _category = GetComponent<VFXCategory>();

        // Auto-detect bindings if enabled
        if (_autoBindOnStart)
        {
            AutoDetectBindings();
        }
        // If using category bindings and category exists, sync bindings on start
        else if (_useCategoryBindings && _category != null)
        {
            _currentMode = _category.Category;
            ApplyBindingsFromCategory();
        }
    }

    /// <summary>
    /// Public accessor for auto-bind setting
    /// </summary>
    public bool AutoBindOnStart { get => _autoBindOnStart; set => _autoBindOnStart = value; }

    #if UNITY_EDITOR
    // Track previous value to detect toggle change
    [System.NonSerialized] bool _prevAutoBindValue;

    /// <summary>
    /// Called when Inspector values change in Editor.
    /// Triggers auto-binding when toggle is enabled.
    /// </summary>
    void OnValidate()
    {
        // Only run auto-detect when toggle changes from false to true
        if (_autoBindOnStart && !_prevAutoBindValue)
        {
            // Delay to next frame to ensure VFX component is available
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && _autoBindOnStart)
                {
                    AutoDetectBindings();
                }
            };
        }
        _prevAutoBindValue = _autoBindOnStart;
    }

    /// <summary>
    /// Reset is called when component is first added or reset.
    /// Initialize tracking variable.
    /// </summary>
    void Reset()
    {
        _prevAutoBindValue = _autoBindOnStart;
        // Auto-detect on component add
        if (_autoBindOnStart)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    AutoDetectBindings();
                }
            };
        }
    }
    #endif

    void LateUpdate()
    {
        var source = Source;
        if (source == null || !source.IsReady)
        {
            IsBound = false;
            BoundCount = 0;
            return;
        }

        int boundCount = 0;

        // === Textures ===
        if (_bindDepthMap && _vfx.HasTexture(depthMapProperty) && source.DepthMap != null)
        {
            _vfx.SetTexture(depthMapProperty, source.DepthMap);
            boundCount++;
        }
        if (_bindStencilMap && _vfx.HasTexture(stencilMapProperty) && source.StencilMap != null)
        {
            _vfx.SetTexture(stencilMapProperty, source.StencilMap);
            boundCount++;
        }
        if (_bindPositionMap && _vfx.HasTexture(positionMapProperty) && source.PositionMap != null)
        {
            _vfx.SetTexture(positionMapProperty, source.PositionMap);
            boundCount++;
        }
        if (_bindColorMap && _vfx.HasTexture(colorMapProperty) && source.ColorMap != null)
        {
            _vfx.SetTexture(colorMapProperty, source.ColorMap);
            boundCount++;
        }
        if (_bindVelocityMap && _vfx.HasTexture(velocityMapProperty) && source.VelocityMap != null)
        {
            _vfx.SetTexture(velocityMapProperty, source.VelocityMap);
            boundCount++;
        }

        // === Vectors / Matrices ===
        if (_bindRayParams && _vfx.HasVector4(rayParamsProperty))
        {
            _vfx.SetVector4(rayParamsProperty, source.RayParams);
            boundCount++;
        }
        if (_bindInverseView && _vfx.HasMatrix4x4(inverseViewProperty))
        {
            _vfx.SetMatrix4x4(inverseViewProperty, source.InverseView);
            boundCount++;
        }
        if (_bindInverseProj && _vfx.HasMatrix4x4(inverseProjProperty))
        {
            var cam = Camera.main;
            if (cam != null)
            {
                _vfx.SetMatrix4x4(inverseProjProperty, cam.projectionMatrix.inverse);
                boundCount++;
            }
        }
        if (_bindDepthRange && _vfx.HasVector2(depthRangeProperty))
        {
            _vfx.SetVector2(depthRangeProperty, new Vector2(_depthMin, _depthMax));
            boundCount++;
        }

        // === Throttle / Intensity ===
        if (_bindThrottle)
        {
            // Try multiple common property names
            if (_vfx.HasFloat(_throttleProperty))
            {
                _vfx.SetFloat(_throttleProperty, _throttle);
                boundCount++;
            }
            else if (_vfx.HasFloat("Intensity"))
            {
                _vfx.SetFloat("Intensity", _throttle);
                boundCount++;
            }
            else if (_vfx.HasFloat("Scale"))
            {
                _vfx.SetFloat("Scale", _throttle);
                boundCount++;
            }
        }

        // === Audio (from global shader props set by AudioBridge) ===
        if (_bindAudio)
        {
            // AudioBridge sets these as global shader properties
            // We read them and bind explicitly to VFX
            if (_vfx.HasFloat(_audioVolumeProperty))
            {
                float volume = Shader.GetGlobalFloat("_AudioVolume");
                _vfx.SetFloat(_audioVolumeProperty, volume);
                boundCount++;
            }
            if (_vfx.HasVector4(_audioBandsProperty))
            {
                Vector4 bands = Shader.GetGlobalVector("_AudioBands");
                _vfx.SetVector4(_audioBandsProperty, bands);
                boundCount++;
            }
        }

        // === Hologram (Mini-Me) ===
        if (_bindAnchorPos || _bindHologramScale)
        {
            if (_useTransformMode)
            {
                // Direct transform mode - no VFX properties needed
                if (_bindAnchorPos && _anchorTransform != null)
                {
                    transform.position = _anchorTransform.position;
                    boundCount++;
                }
                if (_bindHologramScale)
                {
                    transform.localScale = Vector3.one * _hologramScale;
                    boundCount++;
                }
            }
            else
            {
                // Property binding mode - requires VFX to have AnchorPos/HologramScale properties
                if (_bindAnchorPos && _vfx.HasVector3(anchorPosProperty))
                {
                    Vector3 pos = _anchorTransform != null ? _anchorTransform.position : Vector3.zero;
                    _vfx.SetVector3(anchorPosProperty, pos);
                    boundCount++;
                }
                if (_bindHologramScale && _vfx.HasFloat(hologramScaleProperty))
                {
                    _vfx.SetFloat(hologramScaleProperty, _hologramScale);
                    boundCount++;
                }
            }
        }

        BoundCount = boundCount;
        IsBound = boundCount > 0;

        if (_verboseLogging && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[VFXARBinder] {_vfx.name}: Bound {boundCount} properties. IsBound={IsBound}");
        }
    }

    /// <summary>
    /// Find which alias name the VFX actually uses for a texture property
    /// </summary>
    string ResolveTextureAlias(VisualEffect vfx, string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (vfx.HasTexture(alias))
                return alias;
        }
        return null;
    }

    /// <summary>
    /// Find which alias name the VFX actually uses for a Vector4 property
    /// </summary>
    string ResolveVector4Alias(VisualEffect vfx, string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (vfx.HasVector4(alias))
                return alias;
        }
        return null;
    }

    /// <summary>
    /// Find which alias name the VFX actually uses for a Matrix4x4 property
    /// </summary>
    string ResolveMatrix4x4Alias(VisualEffect vfx, string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (vfx.HasMatrix4x4(alias))
                return alias;
        }
        return null;
    }

    /// <summary>
    /// Find which alias name the VFX actually uses for a Vector2 property
    /// </summary>
    string ResolveVector2Alias(VisualEffect vfx, string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (vfx.HasVector2(alias))
                return alias;
        }
        return null;
    }

    /// <summary>
    /// Find which alias name the VFX actually uses for a float property
    /// </summary>
    string ResolveFloatAlias(VisualEffect vfx, string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (vfx.HasFloat(alias))
                return alias;
        }
        return null;
    }

    /// <summary>
    /// Auto-detect which bindings this VFX needs based on exposed properties.
    /// Uses alias resolution for cross-project compatibility (Rcam2, Rcam3, Rcam4, Akvfx, H3M).
    /// </summary>
    [ContextMenu("Auto-Detect Bindings (with Aliases)")]
    public void AutoDetectBindings()
    {
        var vfx = GetComponent<VisualEffect>();

        // Resolve aliases - find actual property names used by this VFX
        _resolvedDepthMap = ResolveTextureAlias(vfx, DepthMapAliases);
        _resolvedStencilMap = ResolveTextureAlias(vfx, StencilMapAliases);
        _resolvedPositionMap = ResolveTextureAlias(vfx, PositionMapAliases);
        _resolvedColorMap = ResolveTextureAlias(vfx, ColorMapAliases);
        _resolvedVelocityMap = ResolveTextureAlias(vfx, VelocityMapAliases);
        _resolvedRayParams = ResolveVector4Alias(vfx, RayParamsAliases);
        _resolvedInverseView = ResolveMatrix4x4Alias(vfx, InverseViewAliases);
        _resolvedInverseProj = ResolveMatrix4x4Alias(vfx, InverseProjAliases);
        _resolvedDepthRange = ResolveVector2Alias(vfx, DepthRangeAliases);
        _resolvedThrottle = ResolveFloatAlias(vfx, ThrottleAliases);

        // Set binding flags based on resolved properties
        _bindDepthMap = _resolvedDepthMap != null;
        _bindStencilMap = _resolvedStencilMap != null;
        _bindPositionMap = _resolvedPositionMap != null;
        _bindColorMap = _resolvedColorMap != null;
        _bindVelocityMap = _resolvedVelocityMap != null;
        _bindRayParams = _resolvedRayParams != null;
        _bindInverseView = _resolvedInverseView != null;
        _bindInverseProj = _resolvedInverseProj != null;
        _bindDepthRange = _resolvedDepthRange != null;
        _bindThrottle = _resolvedThrottle != null;
        _bindAudio = vfx.HasFloat(_audioVolumeProperty) || vfx.HasVector4(_audioBandsProperty);
        _bindAnchorPos = vfx.HasVector3(anchorPosProperty);
        _bindHologramScale = vfx.HasFloat(hologramScaleProperty);

        // Update exposed property names to match resolved aliases
        if (_resolvedDepthMap != null) depthMapProperty = _resolvedDepthMap;
        if (_resolvedStencilMap != null) stencilMapProperty = _resolvedStencilMap;
        if (_resolvedPositionMap != null) positionMapProperty = _resolvedPositionMap;
        if (_resolvedColorMap != null) colorMapProperty = _resolvedColorMap;
        if (_resolvedVelocityMap != null) velocityMapProperty = _resolvedVelocityMap;
        if (_resolvedRayParams != null) rayParamsProperty = _resolvedRayParams;
        if (_resolvedInverseView != null) inverseViewProperty = _resolvedInverseView;
        if (_resolvedInverseProj != null) inverseProjProperty = _resolvedInverseProj;
        if (_resolvedDepthRange != null) depthRangeProperty = _resolvedDepthRange;
        if (_resolvedThrottle != null) _throttleProperty = _resolvedThrottle;

        Debug.Log($"[VFXARBinder] Auto-detected bindings for {vfx.name} (with alias resolution):\n" +
                  $"  DepthMap={_bindDepthMap} ({_resolvedDepthMap ?? "none"})\n" +
                  $"  StencilMap={_bindStencilMap} ({_resolvedStencilMap ?? "none"})\n" +
                  $"  PositionMap={_bindPositionMap} ({_resolvedPositionMap ?? "none"})\n" +
                  $"  ColorMap={_bindColorMap} ({_resolvedColorMap ?? "none"})\n" +
                  $"  VelocityMap={_bindVelocityMap} ({_resolvedVelocityMap ?? "none"})\n" +
                  $"  RayParams={_bindRayParams} ({_resolvedRayParams ?? "none"})\n" +
                  $"  InverseView={_bindInverseView} ({_resolvedInverseView ?? "none"})\n" +
                  $"  InverseProj={_bindInverseProj} ({_resolvedInverseProj ?? "none"})\n" +
                  $"  DepthRange={_bindDepthRange} ({_resolvedDepthRange ?? "none"})\n" +
                  $"  Throttle={_bindThrottle} ({_resolvedThrottle ?? "none"})\n" +
                  $"  Audio={_bindAudio}, AnchorPos={_bindAnchorPos}, HologramScale={_bindHologramScale}");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    /// <summary>
    /// Enable all bindings
    /// </summary>
    [ContextMenu("Enable All Bindings")]
    public void EnableAllBindings()
    {
        _bindDepthMap = true;
        _bindStencilMap = true;
        _bindPositionMap = true;
        _bindColorMap = true;
        _bindVelocityMap = true;
        _bindRayParams = true;
        _bindInverseView = true;
        _bindInverseProj = true;
        _bindDepthRange = true;
        _bindThrottle = true;
        _bindAudio = true;
        _bindAnchorPos = true;
        _bindHologramScale = true;

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    /// <summary>
    /// Disable all bindings
    /// </summary>
    [ContextMenu("Disable All Bindings")]
    public void DisableAllBindings()
    {
        _bindDepthMap = false;
        _bindStencilMap = false;
        _bindPositionMap = false;
        _bindColorMap = false;
        _bindVelocityMap = false;
        _bindRayParams = false;
        _bindInverseView = false;
        _bindInverseProj = false;
        _bindDepthRange = false;
        _bindThrottle = false;
        _bindAudio = false;
        _bindAnchorPos = false;
        _bindHologramScale = false;

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    [ContextMenu("Debug Binder")]
    void DebugBinder()
    {
        var vfx = GetComponent<VisualEffect>();
        var source = Source;
        Debug.Log($"=== VFXARBinder Debug: {vfx.name} ===");
        Debug.Log($"VFX Asset: {vfx.visualEffectAsset?.name}");
        Debug.Log($"--- Binding Toggles ---");
        Debug.Log($"  DepthMap: {_bindDepthMap} (Has: {vfx.HasTexture(depthMapProperty)})");
        Debug.Log($"  StencilMap: {_bindStencilMap} (Has: {vfx.HasTexture(stencilMapProperty)})");
        Debug.Log($"  PositionMap: {_bindPositionMap} (Has: {vfx.HasTexture(positionMapProperty)})");
        Debug.Log($"  ColorMap: {_bindColorMap} (Has: {vfx.HasTexture(colorMapProperty)})");
        Debug.Log($"  VelocityMap: {_bindVelocityMap} (Has: {vfx.HasTexture(velocityMapProperty)})");
        Debug.Log($"  RayParams: {_bindRayParams} (Has: {vfx.HasVector4(rayParamsProperty)})");
        Debug.Log($"  InverseView: {_bindInverseView} (Has: {vfx.HasMatrix4x4(inverseViewProperty)})");
        Debug.Log($"  InverseProj: {_bindInverseProj} (Has: {vfx.HasMatrix4x4(inverseProjProperty)})");
        Debug.Log($"  DepthRange: {_bindDepthRange} (Has: {vfx.HasVector2(depthRangeProperty)})");
        Debug.Log($"  Throttle: {_bindThrottle} (Value: {_throttle})");
        Debug.Log($"  Audio: {_bindAudio}");
        Debug.Log($"  AnchorPos: {_bindAnchorPos} (Has: {vfx.HasVector3(anchorPosProperty)})");
        Debug.Log($"  HologramScale: {_bindHologramScale} (Has: {vfx.HasFloat(hologramScaleProperty)})");
        Debug.Log($"--- Hologram Status ---");
        Debug.Log($"  UseTransformMode: {_useTransformMode}");
        Debug.Log($"  AnchorTransform: {(_anchorTransform != null ? _anchorTransform.name : "null")}");
        Debug.Log($"  AnchorPos: {(_anchorTransform != null ? _anchorTransform.position.ToString() : "N/A")}");
        Debug.Log($"  HologramScale: {_hologramScale}");
        Debug.Log($"  Current VFX Position: {transform.position}");
        Debug.Log($"  Current VFX Scale: {transform.localScale}");
        Debug.Log($"--- Source Status ---");
        Debug.Log($"  Source: {(source != null ? source.name : "null")}");
        Debug.Log($"  IsReady: {source?.IsReady}");
        Debug.Log($"  UsingMockData: {source?.UsingMockData}");
        if (source != null)
        {
            Debug.Log($"  DepthMap: {source.DepthMap} ({source.DepthMap?.width}x{source.DepthMap?.height})");
            Debug.Log($"  PositionMap: {source.PositionMap} ({source.PositionMap?.width}x{source.PositionMap?.height})");
            Debug.Log($"  ColorMap: {source.ColorMap} ({source.ColorMap?.width}x{source.ColorMap?.height})");
            Debug.Log($"  RayParams: {source.RayParams}");
        }
        Debug.Log($"--- Runtime Status ---");
        Debug.Log($"  IsBound: {IsBound}");
        Debug.Log($"  BoundCount: {BoundCount}");
        Debug.Log($"  DepthRange: {_depthMin}m - {_depthMax}m");
    }

    /// <summary>
    /// Configure as hologram with anchor transform and scale
    /// </summary>
    [ContextMenu("Enable Hologram Mode")]
    public void EnableHologramMode()
    {
        _bindAnchorPos = true;
        _bindHologramScale = true;

        // If no anchor set, try to find one
        if (_anchorTransform == null)
        {
            var anchor = FindAnyObjectByType<HologramAnchor>();
            if (anchor != null)
                _anchorTransform = anchor.transform;
        }

        Debug.Log($"[VFXARBinder] Hologram mode enabled for {gameObject.name}. " +
                  $"Anchor: {(_anchorTransform != null ? _anchorTransform.name : "null")}, Scale: {_hologramScale}");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    /// <summary>
    /// Set hologram transform at runtime
    /// </summary>
    public void SetHologramTransform(Transform anchor, float scale)
    {
        _anchorTransform = anchor;
        _hologramScale = Mathf.Clamp(scale, 0.01f, 2f);
        _bindAnchorPos = true;
        _bindHologramScale = true;
    }

    [ContextMenu("Enable Verbose Logging")]
    void EnableVerboseLogging()
    {
        _verboseLogging = true;
        Debug.Log($"[VFXARBinder] Verbose logging enabled for {gameObject.name}");
    }

    [ContextMenu("Disable Verbose Logging")]
    void DisableVerboseLogging()
    {
        _verboseLogging = false;
        Debug.Log($"[VFXARBinder] Verbose logging disabled for {gameObject.name}");
    }

    #region Mode System (spec-007)

    /// <summary>
    /// Switch VFX mode at runtime. Updates bindings without restarting VFX.
    /// Includes fallback handling if mode is not supported (spec-007 T-012).
    /// </summary>
    /// <param name="mode">New mode (People, Environment, Face, Hands, Audio, Hybrid)</param>
    /// <returns>True if requested mode was applied, false if fallback was used</returns>
    public bool SetMode(VFXCategoryType mode)
    {
        if (_currentMode == mode) return true;

        var previousMode = _currentMode;
        var targetMode = mode;

        // T-012: Check mode compatibility and find fallback if needed
        if (!SupportsMode(mode))
        {
            targetMode = FindFallbackMode(mode);
            Debug.LogWarning($"[VFXARBinder] {_vfx?.name}: Mode '{mode}' not supported, falling back to '{targetMode}'");
        }

        _currentMode = targetMode;

        // Update category if it exists
        if (_category != null)
        {
            _category.SetCategory(targetMode);
        }

        // Apply new bindings
        ApplyBindingsFromMode(targetMode);

        if (_verboseLogging)
        {
            string fallbackInfo = targetMode != mode ? $" (requested: {mode})" : "";
            Debug.Log($"[VFXARBinder] {_vfx.name}: Mode changed {previousMode} → {targetMode}{fallbackInfo}");
        }

        // Notify demand-driven systems
        NotifyModeChange(targetMode);

        return targetMode == mode;
    }

    /// <summary>
    /// Find the best fallback mode when requested mode is not supported (T-012).
    /// Priority: Hybrid → People → Environment → original request
    /// </summary>
    VFXCategoryType FindFallbackMode(VFXCategoryType requestedMode)
    {
        // Priority order for fallback
        VFXCategoryType[] fallbackOrder = {
            VFXCategoryType.Hybrid,      // Universal - always supported
            VFXCategoryType.People,      // Most common
            VFXCategoryType.Environment, // Depth-based
            VFXCategoryType.Audio,       // Audio-reactive
            requestedMode                // Last resort: try original anyway
        };

        foreach (var fallback in fallbackOrder)
        {
            if (SupportsMode(fallback))
                return fallback;
        }

        // Should never reach here since Hybrid always returns true
        return VFXCategoryType.Hybrid;
    }

    /// <summary>
    /// Try to set mode, returning the actual mode that was applied.
    /// Useful for UI to show which mode is actually active.
    /// </summary>
    public VFXCategoryType SetModeWithFallback(VFXCategoryType mode)
    {
        SetMode(mode);
        return _currentMode;
    }

    /// <summary>
    /// Apply binding toggles based on VFXCategory component
    /// </summary>
    [ContextMenu("Apply Bindings From Category")]
    public void ApplyBindingsFromCategory()
    {
        if (_category == null)
        {
            Debug.LogWarning($"[VFXARBinder] {gameObject.name}: No VFXCategory component, using manual toggles");
            return;
        }

        ApplyBindingsFromRequirements(_category.Bindings);
        _currentMode = _category.Category;

        if (_verboseLogging)
        {
            Debug.Log($"[VFXARBinder] {_vfx.name}: Applied bindings from category {_category.Category}");
        }
    }

    /// <summary>
    /// Apply binding toggles based on mode
    /// </summary>
    private void ApplyBindingsFromMode(VFXCategoryType mode)
    {
        var bindings = mode switch
        {
            VFXCategoryType.People => VFXBindingRequirements.DepthMap | VFXBindingRequirements.ColorMap | VFXBindingRequirements.StencilMap,
            VFXCategoryType.Face => VFXBindingRequirements.FaceTracking | VFXBindingRequirements.ColorMap,
            VFXCategoryType.Hands => VFXBindingRequirements.HandTracking | VFXBindingRequirements.ColorMap,
            VFXCategoryType.Environment => VFXBindingRequirements.DepthMap | VFXBindingRequirements.ColorMap, // No stencil
            VFXCategoryType.Audio => VFXBindingRequirements.Audio | VFXBindingRequirements.ColorMap,
            VFXCategoryType.Hybrid => VFXBindingRequirements.All,
            _ => VFXBindingRequirements.DepthMap | VFXBindingRequirements.ColorMap
        };

        ApplyBindingsFromRequirements(bindings);
    }

    /// <summary>
    /// Apply binding toggles based on VFXBindingRequirements flags
    /// </summary>
    private void ApplyBindingsFromRequirements(VFXBindingRequirements bindings)
    {
        // Core AR bindings
        _bindDepthMap = (bindings & VFXBindingRequirements.DepthMap) != 0;
        _bindStencilMap = (bindings & VFXBindingRequirements.StencilMap) != 0;
        _bindColorMap = (bindings & VFXBindingRequirements.ColorMap) != 0;

        // Always bind these if depth is bound (required for depth reconstruction)
        _bindPositionMap = _bindDepthMap;
        _bindRayParams = _bindDepthMap;
        _bindInverseView = _bindDepthMap;
        _bindDepthRange = _bindDepthMap;

        // Optional bindings
        _bindAudio = (bindings & VFXBindingRequirements.Audio) != 0;

        // Hand/Face tracking would need separate binders (HandVFXController, etc.)
        // ARMesh collision needs VFXPhysicsBinder

        if (_verboseLogging)
        {
            Debug.Log($"[VFXARBinder] {_vfx?.name}: Applied bindings: " +
                      $"Depth={_bindDepthMap}, Stencil={_bindStencilMap}, Color={_bindColorMap}, " +
                      $"Position={_bindPositionMap}, Audio={_bindAudio}");
        }
    }

    /// <summary>
    /// Notify demand-driven systems about mode change
    /// </summary>
    private void NotifyModeChange(VFXCategoryType mode)
    {
        var source = Source;
        if (source == null) return;

        // Request ColorMap if mode needs it
        bool needsColorMap = mode != VFXCategoryType.Environment || _bindColorMap;
        source.RequestColorMap(needsColorMap);

        // Request audio system if mode needs it
        if ((mode == VFXCategoryType.Audio || _bindAudio) && AudioBridge.Instance != null)
        {
            // AudioBridge is always-on, but could add demand-driven enable here
        }
    }

    /// <summary>
    /// Check if this VFX supports a specific mode
    /// </summary>
    public bool SupportsMode(VFXCategoryType mode)
    {
        if (_vfx == null) return false;

        // Check if VFX has required properties for the mode
        return mode switch
        {
            VFXCategoryType.People => _vfx.HasTexture(depthMapProperty) || _vfx.HasTexture(positionMapProperty),
            VFXCategoryType.Environment => _vfx.HasTexture(depthMapProperty) || _vfx.HasTexture(positionMapProperty),
            VFXCategoryType.Face => _vfx.HasTexture(colorMapProperty), // Face needs at least color
            VFXCategoryType.Hands => _vfx.HasTexture(positionMapProperty), // Hands need position
            VFXCategoryType.Audio => _vfx.HasFloat("AudioVolume") || _vfx.HasVector4("AudioBands"),
            VFXCategoryType.Hybrid => true, // Hybrid always supported
            _ => true
        };
    }

    /// <summary>
    /// Get list of supported modes for this VFX
    /// </summary>
    public VFXCategoryType[] GetSupportedModes()
    {
        var modes = new System.Collections.Generic.List<VFXCategoryType>();
        foreach (VFXCategoryType mode in System.Enum.GetValues(typeof(VFXCategoryType)))
        {
            if (SupportsMode(mode))
                modes.Add(mode);
        }
        return modes.ToArray();
    }

    [ContextMenu("Debug Mode System")]
    void DebugModeSystem()
    {
        Debug.Log($"=== VFXARBinder Mode System Debug: {_vfx?.name} ===");
        Debug.Log($"Current Mode: {_currentMode}");
        Debug.Log($"Use Category Bindings: {_useCategoryBindings}");
        Debug.Log($"Has VFXCategory: {_category != null}");
        if (_category != null)
        {
            Debug.Log($"  Category: {_category.Category}");
            Debug.Log($"  Bindings: {_category.Bindings}");
        }
        Debug.Log($"Supported Modes: {string.Join(", ", GetSupportedModes())}");
    }

    #endregion
}
