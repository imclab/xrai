using UnityEngine;
using UnityEngine.VFX;
using MetavidoVFX.VFX;
#if BODYPIX_AVAILABLE
using MetavidoVFX.Segmentation;
#endif

/// <summary>
/// Maximally Flexible VFX Binder: Supports all common VFX properties with auto-detection.
/// Resolves aliases ONCE in Awake, then uses fast int IDs in Update.
/// Eliminates string lookups and redundant checks during runtime.
///
/// Supported Property Categories:
/// - AR Textures: DepthMap, StencilMap, PositionMap, ColorMap, VelocityMap, MaskTexture, SDF
/// - Camera: RayParams, InverseView, InverseProjection, DepthRange, FocusDistance
/// - Camera Tracking: ReferencePosition, ReferenceVelocity, CameraPosition
/// - Parameters: Throttle, Spawn/SpawnRate, HueShift, Brightness, Alpha, DepthOffset, Size, Color
/// - Dimensions: MapWidth, MapHeight, Resolution, TextureWidth
/// - Hand Tracking: HandPosition, HandVelocity, Interactable, InteractableRadius
/// - ML: KeypointBuffer, KeypointBuffer2 (from BodyPartSegmenter)
/// - Audio: AudioVolume, AudioBands (from global shader properties)
/// - Transform: AnchorPos, HologramScale
/// </summary>
[RequireComponent(typeof(VisualEffect))]
public class VFXARBinder : MonoBehaviour
{
    VisualEffect _vfx;

    // === Core Texture IDs (0 means property not found) ===
    int _idDepth, _idStencil, _idPosition, _idColor, _idVelocity;
    int _idMaskTexture, _idSDF;

    // === Camera Param IDs ===
    int _idRayParams, _idInvView, _idInvProj, _idDepthRange, _idFocusDistance;

    // === Camera Tracking IDs ===
    int _idRefPosition, _idRefVelocity, _idCameraPosition;

    // === Scalar Parameter IDs ===
    int _idThrottle, _idSpawn, _idSize;
    int _idHueShift, _idBrightness, _idAlpha, _idSpawnRate, _idDepthOffset;
    int _idColorTint;  // For "Color" or "Base Color" properties

    // === Dimension IDs ===
    int _idMapWidth, _idMapHeight, _idResolution, _idTextureWidth;

    // === Hand Tracking IDs ===
    int _idHandPosition, _idHandVelocity, _idInteractable, _idInteractableRadius;

    // === Audio IDs ===
    int _idAudioVol, _idAudioBands;

    // === ML/Keypoint IDs ===
    int _idKeypointBuffer, _idKeypointBuffer2;

    // === Settings ===
    [Header("Data Sources")]
    [Tooltip("ARDepthSource to read from. If null, uses ARDepthSource.Instance.")]
    [SerializeField] ARDepthSource _source;

    [Tooltip("Optional: Transform to track for hand position binding")]
    [SerializeField] Transform _handTransform;

    [Tooltip("Optional: SDF texture for SDF-based VFX")]
    [SerializeField] Texture _sdfTexture;

    #if BODYPIX_AVAILABLE
    [Tooltip("Optional: BodyPartSegmenter for KeypointBuffer binding")]
    [SerializeField] BodyPartSegmenter _bodyPartSegmenter;
    #endif

    [Header("Core Parameters")]
    [Tooltip("Global intensity multiplier")]
    [Range(0f, 1f)]
    [SerializeField] float _throttle = 1f;

    [Tooltip("Spawn rate multiplier (0-10)")]
    [Range(0f, 10f)]
    [SerializeField] float _spawn = 1f;

    [Tooltip("Size multiplier")]
    [Range(0.01f, 10f)]
    [SerializeField] float _size = 1f;

    [Tooltip("Depth range for reconstruction")]
    [SerializeField] Vector2 _depthRange = new Vector2(0.1f, 10f);

    [Tooltip("Focus distance for DOF effects")]
    [SerializeField] float _focusDistance = 2f;

    [Header("Color & Appearance")]
    [Tooltip("Hue shift for color effects (0-360)")]
    [Range(0f, 360f)]
    [SerializeField] float _hueShift = 0f;

    [Tooltip("Brightness multiplier")]
    [Range(0f, 2f)]
    [SerializeField] float _brightness = 1f;

    [Tooltip("Alpha/opacity multiplier")]
    [Range(0f, 1f)]
    [SerializeField] float _alpha = 1f;

    [Tooltip("Color tint")]
    [SerializeField] Color _colorTint = Color.white;

    [Tooltip("Depth offset adjustment")]
    [Range(-1f, 1f)]
    [SerializeField] float _depthOffset = 0f;

    [Header("Interaction")]
    [Tooltip("Interactable position for interaction-based VFX")]
    [SerializeField] Vector3 _interactablePos;

    [Tooltip("Interactable radius")]
    [Range(0f, 2f)]
    [SerializeField] float _interactableRadius = 0.1f;

    // Legacy alias for backward compatibility
    float _spawnRate { get => _spawn; set => _spawn = value; }

    // === Comprehensive Aliases for Cross-Project Compatibility ===
    // (Rcam, Metavido, H3M, NNCam, Akvfx, SdfVfx, Fluo)

    // Texture Aliases
    static readonly string[] DepthAliases = { "DepthMap", "DepthTexture", "_Depth", "Depth" };
    static readonly string[] StencilAliases = { "StencilMap", "HumanStencil", "_Stencil", "Stencil" };
    static readonly string[] PosAliases = { "PositionMap", "Position", "WorldPosition", "Positions" };
    static readonly string[] ColorAliases = { "ColorMap", "ColorTexture", "_MainTex", "MainTex", "Background" };
    static readonly string[] VelAliases = { "VelocityMap", "Velocity", "MotionVector", "Motion" };
    static readonly string[] MaskAliases = { "MaskTexture", "Mask", "BodyMask", "SegmentationMask" };
    static readonly string[] SDFAliases = { "SDF", "SDFTexture", "SignedDistanceField" };

    // Camera Param Aliases
    static readonly string[] RayAliases = { "RayParams", "CameraParams", "RayParamsMatrix" };
    static readonly string[] InvViewAliases = { "InverseView", "InvView", "InverseViewMatrix" };
    static readonly string[] InvProjAliases = { "InverseProjection", "InvProj", "InverseProjectionMatrix" };
    static readonly string[] RangeAliases = { "DepthRange", "ClipRange", "NearFar" };
    static readonly string[] FocusAliases = { "FocusDistance", "Focus", "FocalDistance" };

    // Camera Tracking Aliases
    static readonly string[] RefPosAliases = { "ReferencePosition", "CameraPosition", "ViewPosition" };
    static readonly string[] RefVelAliases = { "ReferenceVelocity", "CameraVelocity", "ViewVelocity" };

    // Scalar Parameter Aliases
    static readonly string[] ThrottleAliases = { "Throttle", "Intensity", "Scale", "Amount" };
    static readonly string[] SpawnAliases = { "Spawn", "SpawnRate", "Spawn Rate", "Spawn rate" };
    static readonly string[] SizeAliases = { "Size", "PointSize", "ParticleSize", "Scale" };
    static readonly string[] HueShiftAliases = { "HueShift", "Hue" };
    static readonly string[] BrightnessAliases = { "Brightness", "Exposure", "Luminance" };
    static readonly string[] AlphaAliases = { "Alpha", "Opacity", "Alpha Scale" };
    static readonly string[] DepthOffsetAliases = { "DepthOffset", "Depth Offset" };
    static readonly string[] ColorTintAliases = { "Color", "Base Color", "Tint", "MainColor" };

    // Dimension Aliases
    static readonly string[] WidthAliases = { "MapWidth", "TextureWidth", "Width" };
    static readonly string[] HeightAliases = { "MapHeight", "TextureHeight", "Height" };
    static readonly string[] ResolutionAliases = { "Resolution", "Dimensions", "Size" };

    // Hand Tracking Aliases
    static readonly string[] HandPosAliases = { "Hand Position", "HandPosition", "HandPos" };
    static readonly string[] HandVelAliases = { "Hand Velocity", "HandVelocity", "HandVel" };
    static readonly string[] InteractAliases = { "Interactable", "InteractPosition", "PointerPosition" };
    static readonly string[] InteractRadiusAliases = { "InteractableRadius", "InteractionRadius", "TouchRadius" };

    void Awake()
    {
        _vfx = GetComponent<VisualEffect>();

        // Auto-detect and enable all bindings on startup
        AutoDetectBindings();
    }

    void LateUpdate()
    {
        // Lazy load source if needed
        if (_source == null) _source = ARDepthSource.Instance;
        if (_source == null || _vfx == null) return;

        // Use try-catch blocks to handle property binding errors gracefully
        // This prevents errors when VFX assets change or have different property types
        try
        {
            // 1. Textures - verify with HasTexture before setting
            if (_idDepth != 0 && _bindDepthMapOverride && _source.DepthMap && _vfx.HasTexture(_idDepth))
                _vfx.SetTexture(_idDepth, _source.DepthMap);
            if (_idStencil != 0 && _bindStencilMapOverride && _source.StencilMap && _vfx.HasTexture(_idStencil))
                _vfx.SetTexture(_idStencil, _source.StencilMap);
            if (_idPosition != 0 && _bindPositionMapOverride && _source.PositionMap && _vfx.HasTexture(_idPosition))
                _vfx.SetTexture(_idPosition, _source.PositionMap);
            if (_idColor != 0 && _bindColorMapOverride && _source.ColorMap && _vfx.HasTexture(_idColor))
                _vfx.SetTexture(_idColor, _source.ColorMap);
            if (_idVelocity != 0 && _bindVelocityMapOverride && _source.VelocityMap && _vfx.HasTexture(_idVelocity))
                _vfx.SetTexture(_idVelocity, _source.VelocityMap);

            // 2. Camera Params - verify types before setting
            if (_idRayParams != 0 && _bindRayParamsOverride && _vfx.HasVector4(_idRayParams))
                _vfx.SetVector4(_idRayParams, _source.RayParams);
            if (_idInvView != 0 && _bindInverseViewOverride && _vfx.HasMatrix4x4(_idInvView))
                _vfx.SetMatrix4x4(_idInvView, _source.InverseView);
            if (_idInvProj != 0 && Camera.main != null && _vfx.HasMatrix4x4(_idInvProj))
                _vfx.SetMatrix4x4(_idInvProj, Camera.main.projectionMatrix.inverse);
            if (_idDepthRange != 0 && _bindDepthRangeOverride && _vfx.HasVector2(_idDepthRange))
                _vfx.SetVector2(_idDepthRange, _depthRange);

            // 3. Parameters
            if (_idThrottle != 0 && _bindThrottleOverride && _vfx.HasFloat(_idThrottle))
                _vfx.SetFloat(_idThrottle, _throttle);

            // 4. Audio (Read from Globals set by AudioBridge)
            if (_bindAudioOverride)
            {
                if (_idAudioVol != 0 && _vfx.HasFloat(_idAudioVol))
                    _vfx.SetFloat(_idAudioVol, Shader.GetGlobalFloat("_AudioVolume"));
                if (_idAudioBands != 0 && _vfx.HasVector4(_idAudioBands))
                    _vfx.SetVector4(_idAudioBands, Shader.GetGlobalVector("_AudioBands"));
            }

            // 5. Extended bindings (spec-007)
            if (_idHueShift != 0 && _vfx.HasFloat(_idHueShift)) _vfx.SetFloat(_idHueShift, _hueShift);
            if (_idBrightness != 0 && _vfx.HasFloat(_idBrightness)) _vfx.SetFloat(_idBrightness, _brightness);
            if (_idAlpha != 0 && _vfx.HasFloat(_idAlpha)) _vfx.SetFloat(_idAlpha, _alpha);
            if (_idSpawnRate != 0 && _vfx.HasFloat(_idSpawnRate)) _vfx.SetFloat(_idSpawnRate, _spawnRate);
            if (_idDepthOffset != 0 && _vfx.HasFloat(_idDepthOffset)) _vfx.SetFloat(_idDepthOffset, _depthOffset);

            // 6. Map dimensions (auto-derived from PositionMap)
            if (_source.PositionMap != null)
            {
                if (_idMapWidth != 0 && _vfx.HasFloat(_idMapWidth)) _vfx.SetFloat(_idMapWidth, _source.PositionMap.width);
                if (_idMapHeight != 0 && _vfx.HasFloat(_idMapHeight)) _vfx.SetFloat(_idMapHeight, _source.PositionMap.height);
            }
        }
        catch (System.Exception)
        {
            // Silently ignore binding errors - re-detect bindings on next frame
            // This handles cases where VFX asset changed or properties were removed
        }

        // 7. Transform mode bindings (for anchored holograms)
        if (_useTransformMode)
        {
            if (_bindAnchorPos && _anchorTransform != null)
            {
                _anchorPos = _anchorTransform.position;
                if (_vfx.HasVector3("AnchorPos")) _vfx.SetVector3("AnchorPos", _anchorPos);
            }
            if (_bindHologramScale && _vfx.HasFloat("HologramScale"))
            {
                _vfx.SetFloat("HologramScale", _hologramScale);
            }
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

    // Type-specific property finders (avoids type mismatch errors)
    int FindTexturePropertyID(string[] aliases)
    {
        foreach (var name in aliases)
        {
            if (_vfx.HasTexture(name))
            {
                return Shader.PropertyToID(name);
            }
        }
        return 0;
    }

    int FindVector4PropertyID(string[] aliases)
    {
        foreach (var name in aliases)
        {
            if (_vfx.HasVector4(name))
            {
                return Shader.PropertyToID(name);
            }
        }
        return 0;
    }

    int FindVector2PropertyID(string[] aliases)
    {
        foreach (var name in aliases)
        {
            if (_vfx.HasVector2(name))
            {
                return Shader.PropertyToID(name);
            }
        }
        return 0;
    }

    int FindFloatPropertyID(string[] aliases)
    {
        foreach (var name in aliases)
        {
            if (_vfx.HasFloat(name))
            {
                return Shader.PropertyToID(name);
            }
        }
        return 0;
    }

    int FindMatrix4x4PropertyID(string[] aliases)
    {
        foreach (var name in aliases)
        {
            if (_vfx.HasMatrix4x4(name))
            {
                return Shader.PropertyToID(name);
            }
        }
        return 0;
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
    // Auto-enables bindings for all detected properties
    // Uses type-specific finders to avoid "Value type incorrect" errors
    public void AutoDetectBindings()
    {
        if (_vfx == null) _vfx = GetComponent<VisualEffect>();
        if (_vfx == null) return;

        // Texture bindings (type-safe)
        _idDepth = FindTexturePropertyID(DepthAliases);
        _bindDepthMapOverride = _idDepth != 0;

        _idStencil = FindTexturePropertyID(StencilAliases);
        _bindStencilMapOverride = _idStencil != 0;

        _idPosition = FindTexturePropertyID(PosAliases);
        _bindPositionMapOverride = _idPosition != 0;

        _idColor = FindTexturePropertyID(ColorAliases);
        _bindColorMapOverride = _idColor != 0;

        _idVelocity = FindTexturePropertyID(VelAliases);
        _bindVelocityMapOverride = _idVelocity != 0;

        // Vector4 bindings (type-safe)
        _idRayParams = FindVector4PropertyID(RayAliases);
        _bindRayParamsOverride = _idRayParams != 0;

        // Matrix4x4 bindings (type-safe)
        _idInvView = FindMatrix4x4PropertyID(InvViewAliases);
        _bindInverseViewOverride = _idInvView != 0;

        _idInvProj = FindMatrix4x4PropertyID(InvProjAliases);

        // Vector2 bindings (stored as Vector4 in VFX Graph)
        _idDepthRange = FindVector2PropertyID(RangeAliases);
        _bindDepthRangeOverride = _idDepthRange != 0;

        // Float bindings (type-safe)
        _idThrottle = FindFloatPropertyID(ThrottleAliases);
        _bindThrottleOverride = _idThrottle != 0;

        _idAudioVol = _vfx.HasFloat("AudioVolume") ? Shader.PropertyToID("AudioVolume") : 0;
        _idAudioBands = _vfx.HasVector4("AudioBands") ? Shader.PropertyToID("AudioBands") : 0;
        _bindAudioOverride = _idAudioVol != 0 || _idAudioBands != 0;

        // Extended bindings (spec-007) - all floats
        _idHueShift = FindFloatPropertyID(HueShiftAliases);
        _idBrightness = FindFloatPropertyID(BrightnessAliases);
        _idAlpha = FindFloatPropertyID(AlphaAliases);
        _idSpawnRate = FindFloatPropertyID(SpawnAliases);
        _idDepthOffset = FindFloatPropertyID(DepthOffsetAliases);
        _idMapWidth = _vfx.HasFloat("MapWidth") ? Shader.PropertyToID("MapWidth") : 0;
        _idMapHeight = _vfx.HasFloat("MapHeight") ? Shader.PropertyToID("MapHeight") : 0;

        // Transform mode bindings (check for hologram properties)
        bool hasAnchor = _vfx.HasVector3("AnchorPos");
        bool hasScale = _vfx.HasFloat("HologramScale");
        _bindAnchorPos = hasAnchor;
        _bindHologramScale = hasScale;
        _useTransformMode = hasAnchor || hasScale;
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
