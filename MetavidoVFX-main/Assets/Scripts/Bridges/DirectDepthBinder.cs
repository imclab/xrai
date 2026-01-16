using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;

/// <summary>
/// Zero-compute approach: Pass raw depth to VFX, let shader do conversion.
/// Best for: New VFX where you control the graph design.
/// </summary>
public class DirectDepthBinder : MonoBehaviour
{
    [SerializeField] AROcclusionManager _occlusion;
    [SerializeField] VisualEffect _vfx;
    [SerializeField] Camera _camera;

    void Start()
    {
        _occlusion ??= FindFirstObjectByType<AROcclusionManager>();
        _vfx ??= GetComponent<VisualEffect>();
        _camera ??= Camera.main;
    }

    void LateUpdate()
    {
        var depth = _occlusion?.environmentDepthTexture;
        if (depth == null || _vfx == null) return;

        // Pass RAW depth - VFX Graph converts to world position
        _vfx.SetTexture("DepthMap", depth);
        if (_occlusion.humanStencilTexture)
            _vfx.SetTexture("ColorMap", _occlusion.humanStencilTexture);
        
        _vfx.SetMatrix4x4("InverseView", _camera.cameraToWorldMatrix);
        _vfx.SetVector4("RayParams", CalculateRayParams());
        _vfx.SetVector2("DepthRange", new Vector2(0.1f, 5f));
        _vfx.SetBool("Spawn", true);
    }

    Vector4 CalculateRayParams()
    {
        float fov = _camera.fieldOfView * Mathf.Deg2Rad;
        float h = Mathf.Tan(fov * 0.5f);
        return new Vector4(0, 0, h * _camera.aspect, h);
    }
}