using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

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

    [Header("Debug")]
    [SerializeField] bool _verboseLogging = false;

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
    [VFXPropertyBinding("UnityEngine.Vector2")]
    public ExposedProperty depthRangeProperty = "DepthRange";

    // Status for Dashboard
    public bool IsBound { get; private set; }
    public int BoundCount { get; private set; }

    void Awake() => _vfx = GetComponent<VisualEffect>();

    void LateUpdate()
    {
        var source = ARDepthSource.Instance;
        if (source == null || !source.IsReady)
        {
            IsBound = false;
            BoundCount = 0;
            return;
        }

        int boundCount = 0;

        // Bind textures using ExposedProperty (proper VFX Graph property resolution)
        if (_vfx.HasTexture(depthMapProperty) && source.DepthMap != null)
        {
            _vfx.SetTexture(depthMapProperty, source.DepthMap);
            boundCount++;
        }
        if (_vfx.HasTexture(stencilMapProperty) && source.StencilMap != null)
        {
            _vfx.SetTexture(stencilMapProperty, source.StencilMap);
            boundCount++;
        }
        if (_vfx.HasTexture(positionMapProperty) && source.PositionMap != null)
        {
            _vfx.SetTexture(positionMapProperty, source.PositionMap);
            boundCount++;
        }
        if (_vfx.HasTexture(colorMapProperty) && source.ColorMap != null)
        {
            _vfx.SetTexture(colorMapProperty, source.ColorMap);
            boundCount++;
        }
        if (_vfx.HasTexture(velocityMapProperty) && source.VelocityMap != null)
        {
            _vfx.SetTexture(velocityMapProperty, source.VelocityMap);
            boundCount++;
        }

        // Vectors/Matrices - also bind explicitly
        if (_vfx.HasVector4(rayParamsProperty))
        {
            _vfx.SetVector4(rayParamsProperty, source.RayParams);
            boundCount++;
        }
        if (_vfx.HasMatrix4x4(inverseViewProperty))
        {
            _vfx.SetMatrix4x4(inverseViewProperty, source.InverseView);
            boundCount++;
        }
        if (_vfx.HasVector2(depthRangeProperty))
        {
            _vfx.SetVector2(depthRangeProperty, new Vector2(0.1f, 10f));
            boundCount++;
        }

        BoundCount = boundCount;
        IsBound = boundCount > 0;

        if (_verboseLogging && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[VFXARBinder] {_vfx.name}: Bound {boundCount} properties. IsBound={IsBound}");
        }
    }
    
    [ContextMenu("Debug Binder")]
    void DebugBinder()
    {
        var vfx = GetComponent<VisualEffect>();
        var source = ARDepthSource.Instance;
        Debug.Log($"=== VFXARBinder Debug: {vfx.name} ===");
        Debug.Log($"VFX Asset: {vfx.visualEffectAsset?.name}");
        Debug.Log($"Has DepthMap '{depthMapProperty}': {vfx.HasTexture(depthMapProperty)}");
        Debug.Log($"Has StencilMap '{stencilMapProperty}': {vfx.HasTexture(stencilMapProperty)}");
        Debug.Log($"Has PositionMap '{positionMapProperty}': {vfx.HasTexture(positionMapProperty)}");
        Debug.Log($"Has ColorMap '{colorMapProperty}': {vfx.HasTexture(colorMapProperty)}");
        Debug.Log($"Has VelocityMap '{velocityMapProperty}': {vfx.HasTexture(velocityMapProperty)}");
        Debug.Log($"Has RayParams '{rayParamsProperty}': {vfx.HasVector4(rayParamsProperty)}");
        Debug.Log($"Has InverseView '{inverseViewProperty}': {vfx.HasMatrix4x4(inverseViewProperty)}");
        Debug.Log($"Has DepthRange '{depthRangeProperty}': {vfx.HasVector2(depthRangeProperty)}");
        Debug.Log($"Source available: {source != null}");
        Debug.Log($"Source.IsReady: {source?.IsReady}");
        if (source != null)
        {
            Debug.Log($"Source.DepthMap: {source.DepthMap} ({source.DepthMap?.width}x{source.DepthMap?.height})");
            Debug.Log($"Source.ColorMap: {source.ColorMap} ({source.ColorMap?.width}x{source.ColorMap?.height})");
            Debug.Log($"Source.PositionMap: {source.PositionMap} ({source.PositionMap?.width}x{source.PositionMap?.height})");
        }
    }

    [ContextMenu("Enable Verbose Logging")]
    void EnableVerboseLogging()
    {
        _verboseLogging = true;
        Debug.Log($"[VFXARBinder] Verbose logging enabled for {gameObject.name}");
    }
}
