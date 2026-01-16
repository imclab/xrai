using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Metavido;
using MetavidoVFX.VFX;

namespace H3M.Core
{
    public class HologramSource : MonoBehaviour
    {
        void Log(string msg) { if (!VFXBinderManager.SuppressHologramLogs) Debug.Log(msg); }
        void LogWarning(string msg) { if (!VFXBinderManager.SuppressHologramLogs) Debug.LogWarning(msg); }

        [Header("Inputs")]
        [SerializeField] AROcclusionManager _occlusionManager;
        [SerializeField] ARCameraTextureProvider _colorProvider;
        [SerializeField] Camera _arCamera;
        public Camera arCamera => _arCamera;

        [Header("Compute")]
        [SerializeField] ComputeShader _computeShader;

        [Header("Settings")]
        [SerializeField] bool _useStencil = true;
        [SerializeField] Vector2 _depthRange = new Vector2(0.1f, 5.0f);
        [SerializeField] float _stencilThreshold = 0.1f;

        public Vector2 DepthRange => _depthRange;

        [Header("Outputs (ReadOnly)")]
        public RenderTexture PositionMap;
        public Texture ColorTexture => _colorProvider != null ? _colorProvider.Texture : null;
        public Texture StencilTexture { get; private set; }

        int _width;
        int _height;
        int _kernelId = -1;
        bool _kernelSearched;

        void Start()
        {
            if (_arCamera == null) _arCamera = Camera.main;
            if (_occlusionManager == null) _occlusionManager = FindFirstObjectByType<AROcclusionManager>();
            if (_colorProvider == null) _colorProvider = FindFirstObjectByType<ARCameraTextureProvider>();

            if (_computeShader == null) _computeShader = Resources.Load<ComputeShader>("DepthToWorld");

            // Pre-check kernel at startup to avoid per-frame exceptions
            if (_computeShader != null)
            {
                try
                {
                    _kernelId = _computeShader.FindKernel("DepthToWorld");
                }
                catch (System.ArgumentException)
                {
                    _kernelId = -1;
                }
                _kernelSearched = true;

                if (_kernelId < 0)
                {
                    LogWarning("[HologramSource] DepthToWorld kernel not found. Compute shader may not compile in Editor Metal. Will work on device.");
                    enabled = false;
                    return;
                }
            }

            // Explicitly request high-quality segmentation and depth for hand tracking
            if (_occlusionManager != null)
            {
                _occlusionManager.requestedHumanStencilMode = HumanSegmentationStencilMode.Best;
                _occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Best;
                Log($"[HologramSource] Requested Best AR modes for hand tracking. Currently: Depth={_occlusionManager.requestedEnvironmentDepthMode}, Stencil={_occlusionManager.requestedHumanStencilMode}");
            }
        }

        void OnDestroy()
        {
            ReleaseTexture();
        }

        void Update()
        {
            if (_occlusionManager == null || _computeShader == null || _arCamera == null) return;
            if (_occlusionManager.subsystem == null || !_occlusionManager.subsystem.running) return;

            // 1. Get Textures (Safely)
            Texture depthTex = null;
            Texture stencilTex = null;

            try
            {
                depthTex = _occlusionManager.environmentDepthTexture;
                if (_occlusionManager.descriptor?.humanSegmentationStencilImageSupported != UnityEngine.XR.ARSubsystems.Supported.Unsupported)
                {
                    stencilTex = _occlusionManager.humanStencilTexture;
                }
            }
            catch (System.Exception)
            {
                return; // Wait for stabilization
            }

            if (depthTex == null) return;
            StencilTexture = stencilTex;

            // 2. Init Output if Size Changed
            if (_width != depthTex.width || _height != depthTex.height || PositionMap == null)
            {
                _width = depthTex.width;
                _height = depthTex.height;
                InitTexture(_width, _height);
            }

            // Kernel already validated in Start()
            int kernel = _kernelId;

            // Compute Matrices
            var proj = _arCamera.projectionMatrix;
            var worldToLocal = _arCamera.transform.worldToLocalMatrix;
            var invVP = (proj * worldToLocal).inverse;

            _computeShader.SetMatrix("_InvVP", invVP);
            _computeShader.SetMatrix("_ProjectionMatrix", proj);
            _computeShader.SetVector("_DepthRange", new Vector4(_depthRange.x, _depthRange.y, _stencilThreshold, 0));

            _computeShader.SetTexture(kernel, "_Depth", depthTex);
            _computeShader.SetTexture(kernel, "_PositionRT", PositionMap);

            if (_useStencil && stencilTex != null)
            {
                _computeShader.SetTexture(kernel, "_Stencil", stencilTex);
                _computeShader.SetInt("_UseStencil", 1);
            }
            else
            {
                _computeShader.SetInt("_UseStencil", 0);
                _computeShader.SetTexture(kernel, "_Stencil", Texture2D.whiteTexture);
            }

            // Dispatch (32x32 threads per GeneratePositionTexture.compute standard)
            if (kernel >= 0)
            {
                int threadGroupsX = Mathf.CeilToInt(_width / 32.0f);
                int threadGroupsY = Mathf.CeilToInt(_height / 32.0f);
                _computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
            }
        }

        void InitTexture(int w, int h)
        {
            ReleaseTexture();
            PositionMap = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat);
            PositionMap.enableRandomWrite = true;
            PositionMap.filterMode = FilterMode.Bilinear;
            PositionMap.wrapMode = TextureWrapMode.Clamp; // Prevent edge artifacts
            PositionMap.Create();
        }

        void ReleaseTexture()
        {
            if (PositionMap != null)
            {
                PositionMap.Release();
                // Use true to allow destroying runtime-created assets
                DestroyImmediate(PositionMap, true);
                PositionMap = null;
            }
        }

        public Matrix4x4 GetInverseViewMatrix() => _arCamera.cameraToWorldMatrix;

        public Vector4 GetRayParams()
        {
            float fovV = _arCamera.fieldOfView * Mathf.Deg2Rad;
            float h = Mathf.Tan(fovV * 0.5f);
            float w = h * _arCamera.aspect;
            return new Vector4(0, 0, w, h); // xy=offset, zw=scale (Metavido HLSL standard)
        }
    }
}
