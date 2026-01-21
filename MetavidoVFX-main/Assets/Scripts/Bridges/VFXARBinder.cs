using UnityEngine;
using UnityEngine.VFX;
using MetavidoVFX.VFX;

/// <summary>
/// Optimized Binder: Resolves aliases ONCE in Awake, then uses fast int IDs in Update.
/// Eliminates string lookups and redundant checks during runtime.
/// </summary>
[RequireComponent(typeof(VisualEffect))]
public class VFXARBinder : MonoBehaviour
{
    VisualEffect _vfx;

    // Cached Property IDs (0 means property not found)
    int _idDepth, _idStencil, _idPosition, _idColor, _idVelocity;
    int _idRayParams, _idInvView, _idInvProj, _idDepthRange;
    int _idThrottle, _idAudioVol, _idAudioBands;
    // Extended binding IDs (spec-007)
    int _idHueShift, _idBrightness, _idAlpha, _idSpawnRate, _idDepthOffset;
    int _idMapWidth, _idMapHeight;

    // Settings
    [Tooltip("ARDepthSource to read from. If null, uses ARDepthSource.Instance.")]
    [SerializeField] ARDepthSource _source;

    [Tooltip("Global intensity multiplier")]
    [Range(0f, 1f)]
    [SerializeField] float _throttle = 1f;

    [Tooltip("Depth range for reconstruction")]
    [SerializeField] Vector2 _depthRange = new Vector2(0.1f, 10f);

    [Header("Extended Bindings (spec-007)")]
    [Tooltip("Hue shift for color effects (0-360)")]
    [Range(0f, 360f)]
    [SerializeField] float _hueShift = 0f;

    [Tooltip("Brightness multiplier")]
    [Range(0f, 2f)]
    [SerializeField] float _brightness = 1f;

    [Tooltip("Alpha/opacity multiplier")]
    [Range(0f, 1f)]
    [SerializeField] float _alpha = 1f;

    [Tooltip("Spawn rate multiplier")]
    [Range(0f, 10f)]
    [SerializeField] float _spawnRate = 1f;

    [Tooltip("Depth offset adjustment")]
    [Range(-1f, 1f)]
    [SerializeField] float _depthOffset = 0f;

    // Aliases for compatibility (Rcam, Metavido, H3M)
    static readonly string[] DepthAliases = { "DepthMap", "DepthTexture", "_Depth" };
    static readonly string[] StencilAliases = { "StencilMap", "HumanStencil", "_Stencil" };
    static readonly string[] PosAliases = { "PositionMap", "Position", "WorldPosition" };
    static readonly string[] ColorAliases = { "ColorMap", "ColorTexture", "_MainTex" };
    static readonly string[] VelAliases = { "VelocityMap", "Velocity", "MotionVector" };
    static readonly string[] RayAliases = { "RayParams", "CameraParams", "RayParamsMatrix" };
    static readonly string[] InvViewAliases = { "InverseView", "InvView", "InverseViewMatrix" };
    static readonly string[] InvProjAliases = { "InverseProjection", "InvProj", "InverseProjectionMatrix" };
    static readonly string[] RangeAliases = { "DepthRange", "ClipRange" };
    static readonly string[] ThrottleAliases = { "Throttle", "Intensity", "Scale" };

    // Extended bindings (spec-007-vfx-multi-mode)
    static readonly string[] HueShiftAliases = { "HueShift", "Hue" };
    static readonly string[] BrightnessAliases = { "Brightness", "Exposure" };
    static readonly string[] AlphaAliases = { "Alpha", "Opacity", "Alpha Scale" };
    static readonly string[] SpawnRateAliases = { "SpawnRate", "Spawn Rate", "Spawn rate" };
    static readonly string[] DepthOffsetAliases = { "DepthOffset", "Depth Offset" };
    static readonly string[] MapDimensionAliases = { "MapWidth", "MapHeight", "Resolution" };

    void Awake()
    {
        _vfx = GetComponent<VisualEffect>();

        // Resolve IDs once on startup
        _idDepth = FindPropertyID(DepthAliases);
        _idStencil = FindPropertyID(StencilAliases);
        _idPosition = FindPropertyID(PosAliases);
        _idColor = FindPropertyID(ColorAliases);
        _idVelocity = FindPropertyID(VelAliases);

        _idRayParams = FindPropertyID(RayAliases);
        _idInvView = FindPropertyID(InvViewAliases);
        _idInvProj = FindPropertyID(InvProjAliases);
        _idDepthRange = FindPropertyID(RangeAliases);
        _idThrottle = FindPropertyID(ThrottleAliases);

        // Audio (Optional)
        _idAudioVol = _vfx.HasFloat("AudioVolume") ? Shader.PropertyToID("AudioVolume") : 0;
        _idAudioBands = _vfx.HasVector4("AudioBands") ? Shader.PropertyToID("AudioBands") : 0;

        // Extended bindings (spec-007)
        _idHueShift = FindPropertyID(HueShiftAliases);
        _idBrightness = FindPropertyID(BrightnessAliases);
        _idAlpha = FindPropertyID(AlphaAliases);
        _idSpawnRate = FindPropertyID(SpawnRateAliases);
        _idDepthOffset = FindPropertyID(DepthOffsetAliases);
        _idMapWidth = _vfx.HasFloat("MapWidth") ? Shader.PropertyToID("MapWidth") : 0;
        _idMapHeight = _vfx.HasFloat("MapHeight") ? Shader.PropertyToID("MapHeight") : 0;
    }

    void LateUpdate()
    {
        // Lazy load source if needed
        if (_source == null) _source = ARDepthSource.Instance;
        if (_source == null) return;

        // 1. Textures (Only bind if ID is valid and texture exists)
        if (_idDepth != 0 && _source.DepthMap) _vfx.SetTexture(_idDepth, _source.DepthMap);
        if (_idStencil != 0 && _source.StencilMap) _vfx.SetTexture(_idStencil, _source.StencilMap);
        if (_idPosition != 0 && _source.PositionMap) _vfx.SetTexture(_idPosition, _source.PositionMap);
        if (_idColor != 0 && _source.ColorMap) _vfx.SetTexture(_idColor, _source.ColorMap);
        if (_idVelocity != 0 && _source.VelocityMap) _vfx.SetTexture(_idVelocity, _source.VelocityMap);

        // 2. Camera Params
        if (_idRayParams != 0) _vfx.SetVector4(_idRayParams, _source.RayParams);
        if (_idInvView != 0) _vfx.SetMatrix4x4(_idInvView, _source.InverseView);
        if (_idInvProj != 0 && Camera.main != null) _vfx.SetMatrix4x4(_idInvProj, Camera.main.projectionMatrix.inverse);
        if (_idDepthRange != 0) _vfx.SetVector2(_idDepthRange, _depthRange);

        // 3. Parameters
        if (_idThrottle != 0) _vfx.SetFloat(_idThrottle, _throttle);

        // 4. Audio (Read from Globals set by AudioBridge)
        if (_idAudioVol != 0) _vfx.SetFloat(_idAudioVol, Shader.GetGlobalFloat("_AudioVolume"));
        if (_idAudioBands != 0) _vfx.SetVector4(_idAudioBands, Shader.GetGlobalVector("_AudioBands"));

        // 5. Extended bindings (spec-007)
        if (_idHueShift != 0) _vfx.SetFloat(_idHueShift, _hueShift);
        if (_idBrightness != 0) _vfx.SetFloat(_idBrightness, _brightness);
        if (_idAlpha != 0) _vfx.SetFloat(_idAlpha, _alpha);
        if (_idSpawnRate != 0) _vfx.SetFloat(_idSpawnRate, _spawnRate);
        if (_idDepthOffset != 0) _vfx.SetFloat(_idDepthOffset, _depthOffset);

        // 6. Map dimensions (auto-derived from PositionMap)
        if (_source.PositionMap != null)
        {
            if (_idMapWidth != 0) _vfx.SetFloat(_idMapWidth, _source.PositionMap.width);
            if (_idMapHeight != 0) _vfx.SetFloat(_idMapHeight, _source.PositionMap.height);
        }
    }

    // Helper to find first matching property
    int FindPropertyID(string[] aliases)
    {
        foreach (var name in aliases)
        {
            if (_vfx.HasTexture(name) || _vfx.HasVector4(name) || _vfx.HasMatrix4x4(name) || _vfx.HasFloat(name))
            {
                return Shader.PropertyToID(name);
            }
        }
        return 0; // 0 is safe "null" for PropertyID
    }

    // Public API for external controllers
    public void SetThrottle(float val) => _throttle = val;

    // =========================================================================
    // Extended API (for VFXLibraryManager, VFXModeController, etc.)
    // =========================================================================

    // Binding status
    public bool IsBound => _idDepth != 0 || _idPosition != 0 || _idStencil != 0;
    public int BoundCount => (_idDepth != 0 ? 1 : 0) + (_idStencil != 0 ? 1 : 0) +
                             (_idPosition != 0 ? 1 : 0) + (_idColor != 0 ? 1 : 0) +
                             (_idVelocity != 0 ? 1 : 0);

    // Individual binding toggles (Editor can override, defaults to auto-detected)
    [SerializeField] bool _bindDepthMapOverride, _bindStencilMapOverride, _bindPositionMapOverride;
    [SerializeField] bool _bindColorMapOverride, _bindVelocityMapOverride, _bindRayParamsOverride;
    [SerializeField] bool _bindInverseViewOverride, _bindDepthRangeOverride, _bindThrottleOverride, _bindAudioOverride;

    public bool BindDepthMap { get => _idDepth != 0 && _bindDepthMapOverride; set => _bindDepthMapOverride = value; }
    public bool BindStencilMap { get => _idStencil != 0 && _bindStencilMapOverride; set => _bindStencilMapOverride = value; }
    public bool BindPositionMap { get => _idPosition != 0 && _bindPositionMapOverride; set => _bindPositionMapOverride = value; }
    public bool BindColorMap { get => _idColor != 0 && _bindColorMapOverride; set => _bindColorMapOverride = value; }
    public bool BindVelocityMap { get => _idVelocity != 0 && _bindVelocityMapOverride; set => _bindVelocityMapOverride = value; }
    public bool BindRayParams { get => _idRayParams != 0 && _bindRayParamsOverride; set => _bindRayParamsOverride = value; }
    public bool BindInverseView { get => _idInvView != 0 && _bindInverseViewOverride; set => _bindInverseViewOverride = value; }
    public bool BindDepthRange { get => _idDepthRange != 0 && _bindDepthRangeOverride; set => _bindDepthRangeOverride = value; }
    public bool BindThrottle { get => _idThrottle != 0 && _bindThrottleOverride; set => _bindThrottleOverride = value; }
    public bool BindAudio { get => (_idAudioVol != 0 || _idAudioBands != 0) && _bindAudioOverride; set => _bindAudioOverride = value; }

    // Re-detect bindings (call after VFX asset changes)
    public void AutoDetectBindings()
    {
        if (_vfx == null) _vfx = GetComponent<VisualEffect>();
        if (_vfx == null) return;

        _idDepth = FindPropertyID(DepthAliases);
        _idStencil = FindPropertyID(StencilAliases);
        _idPosition = FindPropertyID(PosAliases);
        _idColor = FindPropertyID(ColorAliases);
        _idVelocity = FindPropertyID(VelAliases);
        _idRayParams = FindPropertyID(RayAliases);
        _idInvView = FindPropertyID(InvViewAliases);
        _idInvProj = FindPropertyID(InvProjAliases);
        _idDepthRange = FindPropertyID(RangeAliases);
        _idThrottle = FindPropertyID(ThrottleAliases);
        _idAudioVol = _vfx.HasFloat("AudioVolume") ? Shader.PropertyToID("AudioVolume") : 0;
        _idAudioBands = _vfx.HasVector4("AudioBands") ? Shader.PropertyToID("AudioBands") : 0;

        // Extended bindings (spec-007)
        _idHueShift = FindPropertyID(HueShiftAliases);
        _idBrightness = FindPropertyID(BrightnessAliases);
        _idAlpha = FindPropertyID(AlphaAliases);
        _idSpawnRate = FindPropertyID(SpawnRateAliases);
        _idDepthOffset = FindPropertyID(DepthOffsetAliases);
        _idMapWidth = _vfx.HasFloat("MapWidth") ? Shader.PropertyToID("MapWidth") : 0;
        _idMapHeight = _vfx.HasFloat("MapHeight") ? Shader.PropertyToID("MapHeight") : 0;
    }

    // =========================================================================
    // Mode System (for multi-mode VFX - spec 007)
    // =========================================================================

    VFXCategoryType _currentMode = VFXCategoryType.People;
    public VFXCategoryType CurrentMode => _currentMode;

    // Transform mode support (for anchored holograms)
    bool _useTransformMode;
    bool _bindAnchorPos;
    bool _bindHologramScale;
    Vector3 _anchorPos;
    float _hologramScale = 1f;
    [SerializeField] Transform _anchorTransform;

    // Properties for HologramController assignment syntax
    public bool UseTransformMode { get => _useTransformMode; set => _useTransformMode = value; }
    public bool BindAnchorPos { get => _bindAnchorPos; set => _bindAnchorPos = value; }
    public bool BindHologramScale { get => _bindHologramScale; set => _bindHologramScale = value; }
    public Transform AnchorTransform { get => _anchorTransform; set => _anchorTransform = value; }

    // Throttle property (get/set for VFXPipelineAuditor compatibility)
    public float Throttle { get => _throttle; set => _throttle = value; }

    // Value setters (for when binding is enabled)
    public void SetAnchorPos(Vector3 pos) => _anchorPos = pos;
    public void SetHologramScale(float scale) => _hologramScale = scale;

    // Extended binding setters (spec-007)
    public float HueShift { get => _hueShift; set => _hueShift = value; }
    public float Brightness { get => _brightness; set => _brightness = value; }
    public float Alpha { get => _alpha; set => _alpha = value; }
    public float SpawnRate { get => _spawnRate; set => _spawnRate = value; }
    public float DepthOffset { get => _depthOffset; set => _depthOffset = value; }

    // Extended binding status
    public int ExtendedBoundCount => (_idHueShift != 0 ? 1 : 0) + (_idBrightness != 0 ? 1 : 0) +
                                     (_idAlpha != 0 ? 1 : 0) + (_idSpawnRate != 0 ? 1 : 0) +
                                     (_idDepthOffset != 0 ? 1 : 0) + (_idMapWidth != 0 ? 1 : 0);

    public bool SetMode(VFXCategoryType mode) { _currentMode = mode; return true; }
    public bool SupportsMode(VFXCategoryType mode) => true; // All modes supported
    public VFXCategoryType[] GetSupportedModes() => (VFXCategoryType[])System.Enum.GetValues(typeof(VFXCategoryType));

    [ContextMenu("Debug Binder")]
    void DebugBinder()
    {
        Debug.Log($"[VFXARBinder] {_vfx.name} Binding Status:");
        Debug.Log($"  Depth: {(_idDepth != 0 ? "Bound" : "Missing")}");
        Debug.Log($"  Position: {(_idPosition != 0 ? "Bound" : "Missing")}");
        Debug.Log($"  Color: {(_idColor != 0 ? "Bound" : "Missing")}");
        Debug.Log($"  Source: {(_source != null ? "Connected" : "Missing")}");
        Debug.Log($"  Mode: {_currentMode}");
        Debug.Log($"  Extended Bindings: {ExtendedBoundCount} (HueShift:{_idHueShift != 0}, Brightness:{_idBrightness != 0}, Alpha:{_idAlpha != 0})");
    }
}
