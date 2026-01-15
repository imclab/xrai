// VFX Binder Manager - Unified data binding for all VFX types
// Handles People, Face, Hands, Environment, Audio data sources

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;

namespace MetavidoVFX.VFX
{
    /// <summary>
    /// Manages data bindings for all VFX in scene.
    /// Automatically connects AR data, audio, and tracking to VFX properties.
    /// </summary>
    public class VFXBinderManager : MonoBehaviour
    {
        [Header("AR Data Sources")]
        [SerializeField] private AROcclusionManager occlusionManager;
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private ARCameraBackground cameraBackground;
        [SerializeField] private Camera arCamera;

        [Header("Audio Source")]
        [SerializeField] private Audio.EnhancedAudioProcessor audioProcessor;
        private AudioProcessor legacyAudioProcessor;  // Fallback for EchoVision compatibility

        [Header("Hand Tracking")]
        [SerializeField] private HandTracking.HandVFXController handController;

        [Header("Settings")]
        [SerializeField] private bool autoFindSources = true;
        [SerializeField] private bool bindOnAwake = true;
        [SerializeField] private Vector2 depthRange = new Vector2(0.1f, 10f);
        [SerializeField] private bool computePositionMap = true;

        // Tracked VFX
        private List<VFXCategory> _categorizedVFX = new List<VFXCategory>();
        private List<VisualEffect> _uncategorizedVFX = new List<VisualEffect>();

        // Cached data
        private Texture _lastDepthTexture;
        private Texture _lastStencilTexture;
        private Texture _lastColorTexture;
        private Matrix4x4 _inverseViewMatrix;
        private Matrix4x4 _inverseProjectionMatrix;
        private Vector4 _rayParams;  // Camera projection: (0, 0, tan(fov/2)*aspect, tan(fov/2))

        // Color capture
        private RenderTexture _colorRT;

        // PositionMap compute (depth → world positions)
        private ComputeShader _depthToWorldCompute;
        private RenderTexture _positionMapRT;
        private int _depthToWorldKernel = -1;

        // Debug
        private float _lastLogTime;
        private int _boundCount;

        void Awake()
        {
            if (autoFindSources)
            {
                FindDataSources();
            }

            if (bindOnAwake)
            {
                RefreshVFXList();
            }

            // Load DepthToWorld compute shader for PositionMap
            if (computePositionMap)
            {
                _depthToWorldCompute = Resources.Load<ComputeShader>("DepthToWorld");
                if (_depthToWorldCompute != null)
                {
                    try
                    {
                        _depthToWorldKernel = _depthToWorldCompute.FindKernel("DepthToWorld");
                        Debug.Log("[VFXBinderManager] ✓ DepthToWorld compute shader loaded");
                    }
                    catch (System.ArgumentException)
                    {
                        _depthToWorldKernel = -1;
                        Debug.LogWarning("[VFXBinderManager] DepthToWorld kernel not found - PositionMap disabled");
                    }
                }
                else
                {
                    Debug.LogWarning("[VFXBinderManager] DepthToWorld.compute not found in Resources - PositionMap disabled");
                }
            }
        }

        void OnDestroy()
        {
            if (_colorRT != null)
            {
                _colorRT.Release();
                Destroy(_colorRT);
                _colorRT = null;
            }
            if (_positionMapRT != null)
            {
                _positionMapRT.Release();
                Destroy(_positionMapRT);
                _positionMapRT = null;
            }
        }

        void FindDataSources()
        {
            if (occlusionManager == null)
                occlusionManager = FindFirstObjectByType<AROcclusionManager>();

            if (cameraManager == null)
                cameraManager = FindFirstObjectByType<ARCameraManager>();

            if (cameraBackground == null)
                cameraBackground = FindFirstObjectByType<ARCameraBackground>();

            if (arCamera == null)
                arCamera = Camera.main;

            if (audioProcessor == null)
                audioProcessor = FindFirstObjectByType<Audio.EnhancedAudioProcessor>();

            // Fallback: Also look for legacy EchoVision AudioProcessor
            if (audioProcessor == null)
                legacyAudioProcessor = FindFirstObjectByType<AudioProcessor>();

            if (handController == null)
                handController = FindFirstObjectByType<HandTracking.HandVFXController>();

            // Debug logging for found sources
            bool hasAudio = audioProcessor != null || legacyAudioProcessor != null;
            string audioType = audioProcessor != null ? "Enhanced" : (legacyAudioProcessor != null ? "Legacy" : "None");
            Debug.Log($"[VFXBinderManager] Sources found: " +
                $"OcclusionMgr={occlusionManager != null}, " +
                $"CameraMgr={cameraManager != null}, " +
                $"CameraBG={cameraBackground != null}, " +
                $"Camera={arCamera != null}, " +
                $"Audio={hasAudio} ({audioType}), " +
                $"Hand={handController != null}");
        }

        void Update()
        {
            UpdateCachedData();
            BindAllVFX();

            // Periodic status logging
            if (Time.time - _lastLogTime > 3f)
            {
                _lastLogTime = Time.time;
                string depthInfo = _lastDepthTexture != null ? $"{_lastDepthTexture.width}x{_lastDepthTexture.height}" : "NULL";
                string stencilInfo = _lastStencilTexture != null ? $"{_lastStencilTexture.width}x{_lastStencilTexture.height}" : "NULL";
                string colorInfo = _lastColorTexture != null ? $"{_lastColorTexture.width}x{_lastColorTexture.height}" : "NULL";
                string posInfo = _positionMapRT != null ? $"{_positionMapRT.width}x{_positionMapRT.height}" : "NULL";
                Debug.Log($"[VFXBinderManager] Binding {_uncategorizedVFX.Count} VFX | Depth: {depthInfo} | Stencil: {stencilInfo} | Color: {colorInfo} | PositionMap: {posInfo}");
            }
        }

        void UpdateCachedData()
        {
            // Get AR textures
            if (occlusionManager != null)
            {
                // Use TryGet methods for newer API, suppress deprecation warnings for fallback
                #pragma warning disable CS0618
                // Try new depth API first
                if (!occlusionManager.TryGetEnvironmentDepthTexture(out _lastDepthTexture))
                {
                    _lastDepthTexture = occlusionManager.environmentDepthTexture;
                }
                // Stencil uses direct property (no TryGet method exists)
                _lastStencilTexture = occlusionManager.humanStencilTexture;
                #pragma warning restore CS0618
            }

            // Get camera texture via ARCameraBackground blit
            if (cameraBackground != null && cameraBackground.material != null)
            {
                // Create/resize color RT if needed
                if (_colorRT == null || _colorRT.width != Screen.width || _colorRT.height != Screen.height)
                {
                    if (_colorRT != null) _colorRT.Release();
                    _colorRT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                    _colorRT.Create();
                }
                // Blit AR background (YCbCr→RGB conversion via material)
                Graphics.Blit(null, _colorRT, cameraBackground.material);
                _lastColorTexture = _colorRT;
            }

            // Calculate inverse view matrix, inverse projection, and ray parameters
            if (arCamera != null)
            {
                _inverseViewMatrix = arCamera.cameraToWorldMatrix;
                _inverseProjectionMatrix = arCamera.projectionMatrix.inverse;

                // RayParams: (offsetX, offsetY, tan(fov/2)*aspect, tan(fov/2))
                // Used by VFX to convert UV+depth to 3D ray direction
                float fovV = arCamera.fieldOfView * Mathf.Deg2Rad;
                float tanV = Mathf.Tan(fovV * 0.5f);
                float tanH = tanV * arCamera.aspect;
                _rayParams = new Vector4(0f, 0f, tanH, tanV);
            }

            // Compute PositionMap (depth → world positions)
            if (computePositionMap && _depthToWorldCompute != null && _depthToWorldKernel >= 0 && _lastDepthTexture != null && arCamera != null)
            {
                int width = _lastDepthTexture.width;
                int height = _lastDepthTexture.height;

                // Create/resize PositionMap RT
                if (_positionMapRT == null || _positionMapRT.width != width || _positionMapRT.height != height)
                {
                    if (_positionMapRT != null) _positionMapRT.Release();
                    _positionMapRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
                    _positionMapRT.enableRandomWrite = true;
                    _positionMapRT.filterMode = FilterMode.Bilinear;
                    _positionMapRT.Create();
                    Debug.Log($"[VFXBinderManager] Created PositionMap RT: {width}x{height}");
                }

                // Set compute shader parameters
                var proj = arCamera.projectionMatrix;
                var invVP = (proj * arCamera.transform.worldToLocalMatrix).inverse;

                _depthToWorldCompute.SetMatrix("_InvVP", invVP);
                _depthToWorldCompute.SetMatrix("_ProjectionMatrix", proj);
                _depthToWorldCompute.SetVector("_DepthRange", new Vector4(depthRange.x, depthRange.y, 0.5f, 0));
                _depthToWorldCompute.SetTexture(_depthToWorldKernel, "_Depth", _lastDepthTexture);
                _depthToWorldCompute.SetTexture(_depthToWorldKernel, "_Stencil", _lastStencilTexture != null ? _lastStencilTexture : Texture2D.whiteTexture);
                _depthToWorldCompute.SetTexture(_depthToWorldKernel, "_PositionRT", _positionMapRT);
                _depthToWorldCompute.SetInt("_UseStencil", _lastStencilTexture != null ? 1 : 0);

                // Dispatch (assume 8x8 thread groups)
                int groupsX = Mathf.CeilToInt(width / 8.0f);
                int groupsY = Mathf.CeilToInt(height / 8.0f);
                _depthToWorldCompute.Dispatch(_depthToWorldKernel, groupsX, groupsY, 1);
            }
        }

        void BindAllVFX()
        {
            // Bind categorized VFX
            foreach (var vfxCategory in _categorizedVFX)
            {
                if (vfxCategory == null || !vfxCategory.VFX.enabled)
                    continue;

                BindVFX(vfxCategory.VFX, vfxCategory.Bindings);
            }

            // Bind uncategorized VFX (assume full bindings)
            foreach (var vfx in _uncategorizedVFX)
            {
                if (vfx == null || !vfx.enabled)
                    continue;

                BindVFX(vfx, VFXBindingRequirements.All);
            }
        }

        void BindVFX(VisualEffect vfx, VFXBindingRequirements requirements)
        {
            // Depth Map
            if ((requirements & VFXBindingRequirements.DepthMap) != 0 && _lastDepthTexture != null)
            {
                if (vfx.HasTexture("DepthMap"))
                    vfx.SetTexture("DepthMap", _lastDepthTexture);
                if (vfx.HasTexture("DepthTexture"))
                    vfx.SetTexture("DepthTexture", _lastDepthTexture);
            }

            // Color Map
            if ((requirements & VFXBindingRequirements.ColorMap) != 0 && _lastColorTexture != null)
            {
                if (vfx.HasTexture("ColorMap"))
                    vfx.SetTexture("ColorMap", _lastColorTexture);
                if (vfx.HasTexture("ColorTexture"))
                    vfx.SetTexture("ColorTexture", _lastColorTexture);
            }

            // Stencil Map
            if ((requirements & VFXBindingRequirements.StencilMap) != 0 && _lastStencilTexture != null)
            {
                if (vfx.HasTexture("StencilMap"))
                    vfx.SetTexture("StencilMap", _lastStencilTexture);
                if (vfx.HasTexture("HumanStencil"))
                    vfx.SetTexture("HumanStencil", _lastStencilTexture);
                if (vfx.HasTexture("Stencil Map"))
                    vfx.SetTexture("Stencil Map", _lastStencilTexture);
            }

            // Position Map (computed world-space positions)
            if (_positionMapRT != null)
            {
                if (vfx.HasTexture("PositionMap"))
                    vfx.SetTexture("PositionMap", _positionMapRT);
                if (vfx.HasTexture("Position Map"))
                    vfx.SetTexture("Position Map", _positionMapRT);
            }

            // Camera matrices and projection parameters
            if (arCamera != null)
            {
                if (vfx.HasMatrix4x4("InverseView"))
                    vfx.SetMatrix4x4("InverseView", _inverseViewMatrix);
                if (vfx.HasMatrix4x4("InverseViewMatrix"))
                    vfx.SetMatrix4x4("InverseViewMatrix", _inverseViewMatrix);
                if (vfx.HasMatrix4x4("InverseProjection"))
                    vfx.SetMatrix4x4("InverseProjection", _inverseProjectionMatrix);

                if (vfx.HasVector2("DepthRange"))
                    vfx.SetVector2("DepthRange", depthRange);

                // RayParams: Required for Metavido/Rcam VFX to convert UV+depth to 3D positions
                if (vfx.HasVector4("RayParams"))
                    vfx.SetVector4("RayParams", _rayParams);
                if (vfx.HasVector4("ProjectionVector"))
                    vfx.SetVector4("ProjectionVector", _rayParams);
            }

            // Audio bindings
            if ((requirements & VFXBindingRequirements.Audio) != 0)
            {
                if (audioProcessor != null)
                {
                    // Use enhanced audio processor with full frequency bands
                    audioProcessor.PushToVFX(vfx);
                }
                else if (legacyAudioProcessor != null)
                {
                    // Fallback to legacy EchoVision AudioProcessor (volume/pitch only)
                    if (vfx.HasFloat("AudioVolume"))
                        vfx.SetFloat("AudioVolume", legacyAudioProcessor.AudioVolume);
                    if (vfx.HasFloat("AudioPitch"))
                        vfx.SetFloat("AudioPitch", legacyAudioProcessor.AudioPitch);
                }
            }

            // Hand tracking bindings are handled by HandVFXController directly
        }

        /// <summary>
        /// Refresh list of VFX to bind
        /// </summary>
        public void RefreshVFXList()
        {
            _categorizedVFX.Clear();
            _uncategorizedVFX.Clear();

            var allVFX = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

            foreach (var vfx in allVFX)
            {
                var category = vfx.GetComponent<VFXCategory>();
                if (category != null)
                {
                    _categorizedVFX.Add(category);
                }
                else
                {
                    _uncategorizedVFX.Add(vfx);
                }
            }

            Debug.Log($"[VFXBinderManager] Found {_categorizedVFX.Count} categorized, {_uncategorizedVFX.Count} uncategorized VFX");
        }

        /// <summary>
        /// Get all VFX of a specific category
        /// </summary>
        public List<VFXCategory> GetVFXByCategory(VFXCategoryType category)
        {
            var result = new List<VFXCategory>();
            foreach (var vfx in _categorizedVFX)
            {
                if (vfx.Category == category)
                    result.Add(vfx);
            }
            return result;
        }

        /// <summary>
        /// Enable only VFX of a specific category
        /// </summary>
        public void EnableCategory(VFXCategoryType category)
        {
            foreach (var vfx in _categorizedVFX)
            {
                bool shouldEnable = vfx.Category == category;
                vfx.VFX.enabled = shouldEnable;

                if (vfx.VFX.HasBool("Spawn"))
                {
                    vfx.VFX.SetBool("Spawn", shouldEnable);
                }
            }

            Debug.Log($"[VFXBinderManager] Enabled category: {category}");
        }

        /// <summary>
        /// Enable only VFX with specific performance tier or lower
        /// </summary>
        public void FilterByPerformance(int maxTier)
        {
            foreach (var vfx in _categorizedVFX)
            {
                bool shouldEnable = vfx.PerformanceTier <= maxTier;
                vfx.VFX.enabled = shouldEnable;
            }

            Debug.Log($"[VFXBinderManager] Filtered to performance tier <= {maxTier}");
        }
    }
}
