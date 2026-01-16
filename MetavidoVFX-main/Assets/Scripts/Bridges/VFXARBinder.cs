using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using H3M.Core;

/// <summary>
/// Lightweight binder - reads from ARDepthSource, binds to one VFX.
/// NO compute dispatch. Just SetTexture() calls.
/// Like Keijiro's VFXRcamBinder but even simpler.
/// Uses ExposedProperty for proper VFX Graph property resolution.
/// </summary>
[RequireComponent(typeof(VisualEffect))]
public class VFXARBinder : MonoBehaviour
{
    VisualEffect _vfx;

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

    [Header("Debug")]
    [SerializeField] bool _verboseLogging = false;

    // Status for Dashboard (public read-only)
    public bool IsBound { get; private set; }
    public int BoundCount { get; private set; }
    public ARDepthSource Source => _source != null ? _source : ARDepthSource.Instance;
    public float Throttle { get => _throttle; set => _throttle = Mathf.Clamp01(value); }
    public Vector2 DepthRange { get => new Vector2(_depthMin, _depthMax); set { _depthMin = value.x; _depthMax = value.y; } }

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

    void Awake() => _vfx = GetComponent<VisualEffect>();

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
    /// Auto-detect which bindings this VFX needs based on exposed properties
    /// </summary>
    [ContextMenu("Auto-Detect Bindings")]
    public void AutoDetectBindings()
    {
        var vfx = GetComponent<VisualEffect>();

        _bindDepthMap = vfx.HasTexture(depthMapProperty);
        _bindStencilMap = vfx.HasTexture(stencilMapProperty);
        _bindPositionMap = vfx.HasTexture(positionMapProperty);
        _bindColorMap = vfx.HasTexture(colorMapProperty);
        _bindVelocityMap = vfx.HasTexture(velocityMapProperty);
        _bindRayParams = vfx.HasVector4(rayParamsProperty);
        _bindInverseView = vfx.HasMatrix4x4(inverseViewProperty);
        _bindInverseProj = vfx.HasMatrix4x4(inverseProjProperty);
        _bindDepthRange = vfx.HasVector2(depthRangeProperty);
        _bindThrottle = vfx.HasFloat(_throttleProperty) || vfx.HasFloat("Intensity") || vfx.HasFloat("Scale");
        _bindAudio = vfx.HasFloat(_audioVolumeProperty) || vfx.HasVector4(_audioBandsProperty);
        _bindAnchorPos = vfx.HasVector3(anchorPosProperty);
        _bindHologramScale = vfx.HasFloat(hologramScaleProperty);

        Debug.Log($"[VFXARBinder] Auto-detected bindings for {vfx.name}:\n" +
                  $"  DepthMap={_bindDepthMap}, StencilMap={_bindStencilMap}, PositionMap={_bindPositionMap}\n" +
                  $"  ColorMap={_bindColorMap}, VelocityMap={_bindVelocityMap}\n" +
                  $"  RayParams={_bindRayParams}, InverseView={_bindInverseView}, InverseProj={_bindInverseProj}\n" +
                  $"  DepthRange={_bindDepthRange}, Throttle={_bindThrottle}, Audio={_bindAudio}\n" +
                  $"  AnchorPos={_bindAnchorPos}, HologramScale={_bindHologramScale}");

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
}
