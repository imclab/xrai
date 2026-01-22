// Human Particle VFX - Depth-to-World position mapping for VFX
// Based on HumanParticleEffect-master project
// Converts AR human depth into world-space positions for VFX sampling

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;

namespace XRRAI.VFXBinders
{
    /// <summary>
    /// Maps human depth to world positions for VFX particle effects.
    /// Uses compute shader to convert depth texture into position map.
    /// Works with VFX that sample PositionMap/ColorMap for particle emission.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class HumanParticleVFX : MonoBehaviour
    {
        [Header("AR Sources")]
        [SerializeField] private AROcclusionManager occlusionManager;

        [Header("Compute Shader")]
        [SerializeField] private ComputeShader computeShader;

        [Header("Render Textures - Portrait")]
        [SerializeField] private RenderTexture positionMapPortrait;
        [SerializeField] private RenderTexture colorMapPortrait;

        [Header("Render Textures - Landscape")]
        [SerializeField] private RenderTexture positionMapLandscape;
        [SerializeField] private RenderTexture colorMapLandscape;

        [Header("VFX")]
        [SerializeField] private VisualEffect targetVFX;
        [SerializeField] private bool autoFindVFX = true;

        public RenderTexture CurrentColorMap => _lastDeviceOrientation == DeviceOrientation.Portrait
            ? colorMapPortrait : colorMapLandscape;

        public RenderTexture CurrentPositionMap => _lastDeviceOrientation == DeviceOrientation.Portrait
            ? positionMapPortrait : positionMapLandscape;

        private RenderTexture _tempRenderTexture;
        private Camera _camera;
        private DeviceOrientation _lastDeviceOrientation;
        private uint _threadSizeX, _threadSizeY, _threadSizeZ;
        private int _portraitKernel, _landscapeKernel;
        private Matrix4x4 _viewportInv;
        private bool _initialized;

        // Compute Shader property IDs
        private static readonly int PropertyID_CameraPos = Shader.PropertyToID("cameraPos");
        private static readonly int PropertyID_Converter = Shader.PropertyToID("converter");
        private static readonly int PropertyID_Target = Shader.PropertyToID("target");
        private static readonly int PropertyID_Origin = Shader.PropertyToID("origin");
        private static readonly int PropertyID_IsWide = Shader.PropertyToID("isWide");
        private static readonly int PropertyID_UVFlip = Shader.PropertyToID("uVFlip");
        private static readonly int PropertyID_UVMultiplierPortrait = Shader.PropertyToID("uVMultiplierPortrait");
        private static readonly int PropertyID_UVMultiplierLandScape = Shader.PropertyToID("uVMultiplierLandScape");

        // VFX property IDs
        private static readonly int PropertyID_PositionMap = Shader.PropertyToID("PositionMap");
        private static readonly int PropertyID_ColorMap = Shader.PropertyToID("ColorMap");

        // Safe texture access - AR Foundation getters can throw when AR isn't ready
        Texture TryGetTexture(System.Func<Texture> getter)
        {
            try { return getter?.Invoke(); }
            catch { return null; }
        }

        void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        void Start()
        {
            // Auto-find components if not assigned
            if (occlusionManager == null)
                occlusionManager = FindFirstObjectByType<AROcclusionManager>();

            if (autoFindVFX && targetVFX == null)
                targetVFX = FindFirstObjectByType<VisualEffect>();

            if (computeShader == null)
            {
                computeShader = Resources.Load<ComputeShader>("HumanDepthMapper");
                if (computeShader == null)
                {
                    Debug.LogError("[HumanParticleVFX] HumanDepthMapper compute shader not found!");
                    enabled = false;
                    return;
                }
            }

            InitializeKernels();
        }

        void InitializeKernels()
        {
            _portraitKernel = computeShader.FindKernel("Portrait");
            _landscapeKernel = computeShader.FindKernel("Landscape");

            // Start in portrait
            _lastDeviceOrientation = DeviceOrientation.Portrait;
            computeShader.SetInt(PropertyID_IsWide, 0);

            _initialized = true;
        }

        void Update()
        {
            if (!_initialized || occlusionManager == null) return;

            var humanDepthTexture = TryGetTexture(() => occlusionManager.humanDepthTexture);
            if (humanDepthTexture == null) return;

            // Check for orientation change
            if (_lastDeviceOrientation != Input.deviceOrientation)
            {
                HandleOrientationChange();
            }

            // Process depth if we have valid temp texture
            if (_tempRenderTexture != null)
            {
                ProcessDepth(humanDepthTexture);
            }
            else
            {
                InitSetup(humanDepthTexture);
            }
        }

        void HandleOrientationChange()
        {
            if (Input.deviceOrientation == DeviceOrientation.LandscapeRight)
            {
                computeShader.SetFloat(PropertyID_UVFlip, 0);
                computeShader.SetInt(PropertyID_IsWide, 1);
            }
            else if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft)
            {
                computeShader.SetFloat(PropertyID_UVFlip, 1);
                computeShader.SetInt(PropertyID_IsWide, 1);
            }
            else
            {
                computeShader.SetInt(PropertyID_IsWide, 0);
            }

            _lastDeviceOrientation = Input.deviceOrientation;

            var humanDepth = TryGetTexture(() => occlusionManager.humanDepthTexture);
            if (humanDepth != null)
            {
                InitSetup(humanDepth);
            }
        }

        void ProcessDepth(Texture humanDepthTexture)
        {
            computeShader.SetVector(PropertyID_CameraPos, _camera.transform.position);
            computeShader.SetMatrix(PropertyID_Converter, GetConverter());

            if (_lastDeviceOrientation == DeviceOrientation.Portrait)
            {
                computeShader.SetTexture(_portraitKernel, PropertyID_Origin, humanDepthTexture);
                // Use CeilToInt to ensure all pixels are processed (fixes edge artifacts)
                computeShader.Dispatch(_portraitKernel,
                    Mathf.CeilToInt(Screen.width / (float)_threadSizeX),
                    Mathf.CeilToInt(Screen.height / (float)_threadSizeY),
                    (int)_threadSizeZ);
                Graphics.CopyTexture(_tempRenderTexture, positionMapPortrait);
            }
            else
            {
                computeShader.SetTexture(_landscapeKernel, PropertyID_Origin, humanDepthTexture);
                // Use CeilToInt to ensure all pixels are processed (fixes edge artifacts)
                computeShader.Dispatch(_landscapeKernel,
                    Mathf.CeilToInt(Screen.width / (float)_threadSizeX),
                    Mathf.CeilToInt(Screen.height / (float)_threadSizeY),
                    (int)_threadSizeZ);
                Graphics.CopyTexture(_tempRenderTexture, positionMapLandscape);
            }
        }

        void InitSetup(Texture humanDepthTexture)
        {
            // Release old temp texture
            if (_tempRenderTexture != null)
            {
                _tempRenderTexture.Release();
            }

            if (_lastDeviceOrientation == DeviceOrientation.Portrait)
            {
                if (positionMapPortrait == null)
                {
                    Debug.LogWarning("[HumanParticleVFX] positionMapPortrait not assigned");
                    return;
                }

                _tempRenderTexture = new RenderTexture(
                    positionMapPortrait.width,
                    positionMapPortrait.height,
                    0,
                    positionMapPortrait.format
                ) { enableRandomWrite = true };
                _tempRenderTexture.Create();

                computeShader.SetTexture(_portraitKernel, PropertyID_Target, _tempRenderTexture);
                computeShader.GetKernelThreadGroupSizes(_portraitKernel, out _threadSizeX, out _threadSizeY, out _threadSizeZ);
                computeShader.SetFloat(PropertyID_UVMultiplierPortrait, CalculateUVMultiplierPortrait(humanDepthTexture));

                // Set VFX textures
                if (targetVFX != null)
                {
                    if (targetVFX.HasTexture(PropertyID_PositionMap))
                        targetVFX.SetTexture(PropertyID_PositionMap, positionMapPortrait);
                    if (targetVFX.HasTexture(PropertyID_ColorMap) && colorMapPortrait != null)
                        targetVFX.SetTexture(PropertyID_ColorMap, colorMapPortrait);
                }
            }
            else
            {
                if (positionMapLandscape == null)
                {
                    Debug.LogWarning("[HumanParticleVFX] positionMapLandscape not assigned");
                    return;
                }

                _tempRenderTexture = new RenderTexture(
                    positionMapLandscape.width,
                    positionMapLandscape.height,
                    0,
                    positionMapLandscape.format
                ) { enableRandomWrite = true };
                _tempRenderTexture.Create();

                computeShader.SetTexture(_landscapeKernel, PropertyID_Target, _tempRenderTexture);
                computeShader.GetKernelThreadGroupSizes(_landscapeKernel, out _threadSizeX, out _threadSizeY, out _threadSizeZ);
                computeShader.SetFloat(PropertyID_UVMultiplierLandScape, CalculateUVMultiplierLandScape(humanDepthTexture));

                // Set VFX textures
                if (targetVFX != null)
                {
                    if (targetVFX.HasTexture(PropertyID_PositionMap))
                        targetVFX.SetTexture(PropertyID_PositionMap, positionMapLandscape);
                    if (targetVFX.HasTexture(PropertyID_ColorMap) && colorMapLandscape != null)
                        targetVFX.SetTexture(PropertyID_ColorMap, colorMapLandscape);
                }
            }

            SetViewPortInv();
        }

        float CalculateUVMultiplierLandScape(Texture textureFromAROcclusionManager)
        {
            float screenAspect = (float)Screen.width / Screen.height;
            float cameraTextureAspect = (float)textureFromAROcclusionManager.width / textureFromAROcclusionManager.height;
            return screenAspect / cameraTextureAspect;
        }

        float CalculateUVMultiplierPortrait(Texture textureFromAROcclusionManager)
        {
            float screenAspect = (float)Screen.height / Screen.width;
            float cameraTextureAspect = (float)textureFromAROcclusionManager.width / textureFromAROcclusionManager.height;
            return screenAspect / cameraTextureAspect;
        }

        void SetViewPortInv()
        {
            _viewportInv = Matrix4x4.identity;
            _viewportInv.m00 = _viewportInv.m03 = Screen.width / 2f;
            _viewportInv.m11 = Screen.height / 2f;
            _viewportInv.m13 = Screen.height / 2f;
            _viewportInv.m22 = (_camera.farClipPlane - _camera.nearClipPlane) / 2f;
            _viewportInv.m23 = (_camera.farClipPlane + _camera.nearClipPlane) / 2f;
            _viewportInv = _viewportInv.inverse;
        }

        Matrix4x4 GetConverter()
        {
            Matrix4x4 viewMatInv = _camera.worldToCameraMatrix.inverse;
            Matrix4x4 projMatInv = _camera.projectionMatrix.inverse;
            return viewMatInv * projMatInv * _viewportInv;
        }

        /// <summary>
        /// Push position/color maps to any VFX
        /// </summary>
        public void PushMapsToVFX(VisualEffect vfx)
        {
            if (vfx == null) return;

            var posMap = CurrentPositionMap;
            var colorMap = CurrentColorMap;

            if (vfx.HasTexture(PropertyID_PositionMap) && posMap != null)
                vfx.SetTexture(PropertyID_PositionMap, posMap);

            if (vfx.HasTexture(PropertyID_ColorMap) && colorMap != null)
                vfx.SetTexture(PropertyID_ColorMap, colorMap);
        }

        void OnDestroy()
        {
            // Properly release and destroy temp RenderTexture to avoid memory leak
            if (_tempRenderTexture != null)
            {
                _tempRenderTexture.Release();
                Destroy(_tempRenderTexture);
                _tempRenderTexture = null;
            }
        }
    }
}
