using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;

namespace Fluo {

/// <summary>
/// Connects ARFoundation camera and occlusion textures to a Visual Effect Graph.
/// </summary>
public sealed class ARVfxBridge : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] ARCameraManager _cameraManager;
    [SerializeField] AROcclusionManager _occlusionManager;

    [Header("VFX References")]
    [SerializeField] VisualEffect _targetVfx;

    [Header("VFX Property Names")]
    [SerializeField] string _cameraTextureProperty = "CameraTexture";
    [SerializeField] string _stencilTextureProperty = "StencilTexture";
    [SerializeField] string _depthTextureProperty = "DepthTexture";

    [Header("Legacy Texture Targets (Optional)")]
    [SerializeField] RenderTexture _multiplexTarget;
    [SerializeField] RenderTexture _blurTarget;

    [Header("Shaders")]
    [SerializeField] Shader _multiplexShader;

    Material _multiplexMaterial;

    // Cached property IDs
    int _cameraTexId;
    int _stencilTexId;
    int _depthTexId;

    void OnEnable()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived += OnCameraFrameReceived;

        if (_multiplexShader != null)
            _multiplexMaterial = new Material(_multiplexShader);

        // Cache property IDs
        _cameraTexId = Shader.PropertyToID(_cameraTextureProperty);
        _stencilTexId = Shader.PropertyToID(_stencilTextureProperty);
        _depthTexId = Shader.PropertyToID(_depthTextureProperty);
    }

    void OnDisable()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (_targetVfx == null) return;

        Texture cameraTex = null;

        // 1. Camera Texture (only if VFX has this property)
        if (args.textures.Count > 0)
        {
            cameraTex = args.textures[0];
            if (_targetVfx.HasTexture(_cameraTexId))
                _targetVfx.SetTexture(_cameraTexId, cameraTex);
        }

        // 2. Human Stencil & Depth (only if VFX has these properties)
        if (_occlusionManager != null)
        {
            var stencil = _occlusionManager.humanStencilTexture;
            if (stencil != null)
            {
                if (_targetVfx.HasTexture(_stencilTexId))
                    _targetVfx.SetTexture(_stencilTexId, stencil);

                // Legacy support: Blit to multiplex target if assigned
                if (_multiplexTarget != null && _multiplexMaterial != null && cameraTex != null)
                {
                    _multiplexMaterial.SetTexture("_StencilTex", stencil);
                    Graphics.Blit(cameraTex, _multiplexTarget, _multiplexMaterial);
                }
            }

            var depth = _occlusionManager.humanDepthTexture;
            if (depth != null && _targetVfx.HasTexture(_depthTexId))
                _targetVfx.SetTexture(_depthTexId, depth);
        }

        // Legacy blur support: Simple blit for now
        if (_blurTarget != null && _multiplexTarget != null)
        {
            Graphics.Blit(_multiplexTarget, _blurTarget);
        }
    }
}

} // namespace Fluo
