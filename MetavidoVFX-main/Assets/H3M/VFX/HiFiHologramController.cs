// HiFiHologramController.cs - High-fidelity hologram VFX controller
// Ensures proper RGB color sampling from video texture for lifelike rendering
// Based on Record3D and Metavido point cloud patterns
//
// Key principles:
// 1. Sample actual RGB color from ColorMap texture at particle UV
// 2. High particle count (50K-200K) for dense point cloud
// 3. Small particle size (2-5mm) for crisp detail
// 4. No color tinting - pure video color for realism

using System;
using UnityEngine;
using UnityEngine.VFX;

namespace XRRAI.Hologram
{
    /// <summary>
    /// Quality presets for hologram rendering.
    /// Higher quality = more particles, slower performance.
    /// </summary>
    public enum HologramQuality
    {
        /// <summary>10K particles, 5mm size - Mobile/stress testing</summary>
        Low,
        /// <summary>50K particles, 3mm size - Balanced</summary>
        Medium,
        /// <summary>100K particles, 2mm size - High quality</summary>
        High,
        /// <summary>200K particles, 1.5mm size - Maximum fidelity (Quest 3/PC)</summary>
        Ultra
    }

    /// <summary>
    /// Controller for high-fidelity hologram VFX that samples actual RGB colors.
    /// Attach to a GameObject with VisualEffect component.
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    public class HiFiHologramController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Quality Settings")]
        [SerializeField] private HologramQuality _quality = HologramQuality.High;
        [SerializeField] private bool _autoAdjustQuality = true;
        [SerializeField] private int _targetFPS = 60;

        [Header("Appearance")]
        [SerializeField] private float _particleSizeMultiplier = 1f;
        [SerializeField] private float _colorSaturation = 1f;
        [SerializeField] private float _colorBrightness = 1f;
        [SerializeField] private bool _enableDepthFade = true;
        [SerializeField] private Vector2 _depthFadeRange = new Vector2(0.2f, 5f);

        [Header("Input Textures")]
        [SerializeField] private Texture2D _colorMap;
        [SerializeField] private Texture2D _depthMap;
        [SerializeField] private Texture2D _stencilMap;

        [Header("Advanced")]
        [SerializeField] private bool _enableVelocityStreaks = false;
        [SerializeField] private float _velocityStrength = 0.1f;
        [SerializeField] private bool _enableSSAO = false;
        [SerializeField] private bool _useGaussianSplat = false;

        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;

        #endregion

        #region Private Fields

        private VisualEffect _vfx;
        private float _lastFPSCheck;
        private int _frameCount;
        private float _currentFPS;

        // Quality presets
        private static readonly (int particles, float size)[] QualityPresets = new[]
        {
            (10000, 0.005f),    // Low
            (50000, 0.003f),    // Medium
            (100000, 0.002f),   // High
            (200000, 0.0015f)   // Ultra
        };

        // VFX property IDs
        private static class PropertyID
        {
            public static readonly int ColorMap = Shader.PropertyToID("ColorMap");
            public static readonly int DepthMap = Shader.PropertyToID("DepthMap");
            public static readonly int StencilMap = Shader.PropertyToID("StencilMap");
            public static readonly int PositionMap = Shader.PropertyToID("PositionMap");
            public static readonly int ParticleCount = Shader.PropertyToID("ParticleCount");
            public static readonly int ParticleSize = Shader.PropertyToID("ParticleSize");
            public static readonly int ColorSaturation = Shader.PropertyToID("ColorSaturation");
            public static readonly int ColorBrightness = Shader.PropertyToID("ColorBrightness");
            public static readonly int DepthFadeNear = Shader.PropertyToID("DepthFadeNear");
            public static readonly int DepthFadeFar = Shader.PropertyToID("DepthFadeFar");
            public static readonly int VelocityStrength = Shader.PropertyToID("VelocityStrength");
            public static readonly int RayParams = Shader.PropertyToID("RayParams");
            public static readonly int InverseView = Shader.PropertyToID("InverseView");
            public static readonly int DepthRange = Shader.PropertyToID("DepthRange");
        }

        #endregion

        #region Properties

        public HologramQuality Quality
        {
            get => _quality;
            set
            {
                _quality = value;
                ApplyQualitySettings();
            }
        }

        public float CurrentFPS => _currentFPS;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _vfx = GetComponent<VisualEffect>();
        }

        private void Start()
        {
            ApplyQualitySettings();
            ApplyAppearanceSettings();
        }

        private void Update()
        {
            // Update input textures if changed
            UpdateInputTextures();

            // FPS tracking for auto quality adjustment
            if (_autoAdjustQuality)
            {
                TrackFPS();
            }

            // Debug info
            if (_showDebugInfo)
            {
                DrawDebugInfo();
            }
        }

        private void OnDestroy()
        {
            // Cleanup is handled by Unity for serialized texture references
            // This method exists to ensure proper lifecycle management
            _vfx = null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the color texture (RGB video frame).
        /// </summary>
        public void SetColorMap(Texture2D colorMap)
        {
            _colorMap = colorMap;
            if (_vfx != null && _vfx.HasTexture(PropertyID.ColorMap))
            {
                _vfx.SetTexture(PropertyID.ColorMap, colorMap);
            }
        }

        /// <summary>
        /// Set the depth texture.
        /// </summary>
        public void SetDepthMap(Texture2D depthMap)
        {
            _depthMap = depthMap;
            if (_vfx != null && _vfx.HasTexture(PropertyID.DepthMap))
            {
                _vfx.SetTexture(PropertyID.DepthMap, depthMap);
            }
        }

        /// <summary>
        /// Set the stencil/mask texture.
        /// </summary>
        public void SetStencilMap(Texture2D stencilMap)
        {
            _stencilMap = stencilMap;
            if (_vfx != null && _vfx.HasTexture(PropertyID.StencilMap))
            {
                _vfx.SetTexture(PropertyID.StencilMap, stencilMap);
            }
        }

        /// <summary>
        /// Set camera matrices for depth-to-world conversion.
        /// </summary>
        public void SetCameraMatrices(Matrix4x4 inverseView, Vector4 rayParams, Vector2 depthRange)
        {
            if (_vfx != null)
            {
                if (_vfx.HasMatrix4x4(PropertyID.InverseView))
                    _vfx.SetMatrix4x4(PropertyID.InverseView, inverseView);
                if (_vfx.HasVector4(PropertyID.RayParams))
                    _vfx.SetVector4(PropertyID.RayParams, rayParams);
                if (_vfx.HasVector2(PropertyID.DepthRange))
                    _vfx.SetVector2(PropertyID.DepthRange, depthRange);
            }
        }

        /// <summary>
        /// Force quality level change (bypasses auto-adjustment).
        /// </summary>
        public void ForceQuality(HologramQuality quality)
        {
            _autoAdjustQuality = false;
            Quality = quality;
        }

        /// <summary>
        /// Enable auto quality adjustment based on FPS.
        /// </summary>
        public void EnableAutoQuality(int targetFPS = 60)
        {
            _targetFPS = targetFPS;
            _autoAdjustQuality = true;
        }

        #endregion

        #region Quality Management

        private void ApplyQualitySettings()
        {
            if (_vfx == null) return;

            var (particleCount, particleSize) = QualityPresets[(int)_quality];

            // Apply particle count if property exists
            if (_vfx.HasUInt(PropertyID.ParticleCount))
            {
                _vfx.SetUInt(PropertyID.ParticleCount, (uint)particleCount);
            }

            // Apply particle size
            if (_vfx.HasFloat(PropertyID.ParticleSize))
            {
                _vfx.SetFloat(PropertyID.ParticleSize, particleSize * _particleSizeMultiplier);
            }

            Debug.Log($"[HiFiHologram] Quality: {_quality} ({particleCount} particles, {particleSize * 1000:F1}mm)");
        }

        private void ApplyAppearanceSettings()
        {
            if (_vfx == null) return;

            // Color adjustments
            if (_vfx.HasFloat(PropertyID.ColorSaturation))
                _vfx.SetFloat(PropertyID.ColorSaturation, _colorSaturation);

            if (_vfx.HasFloat(PropertyID.ColorBrightness))
                _vfx.SetFloat(PropertyID.ColorBrightness, _colorBrightness);

            // Depth fade
            if (_enableDepthFade)
            {
                if (_vfx.HasFloat(PropertyID.DepthFadeNear))
                    _vfx.SetFloat(PropertyID.DepthFadeNear, _depthFadeRange.x);
                if (_vfx.HasFloat(PropertyID.DepthFadeFar))
                    _vfx.SetFloat(PropertyID.DepthFadeFar, _depthFadeRange.y);
            }

            // Velocity streaks
            if (_vfx.HasFloat(PropertyID.VelocityStrength))
            {
                _vfx.SetFloat(PropertyID.VelocityStrength, _enableVelocityStreaks ? _velocityStrength : 0f);
            }
        }

        private void TrackFPS()
        {
            _frameCount++;
            float elapsed = Time.time - _lastFPSCheck;

            if (elapsed >= 1f)
            {
                _currentFPS = _frameCount / elapsed;
                _frameCount = 0;
                _lastFPSCheck = Time.time;

                // Adjust quality if needed
                AdjustQualityIfNeeded();
            }
        }

        private void AdjustQualityIfNeeded()
        {
            // If FPS is too low, reduce quality
            if (_currentFPS < _targetFPS * 0.8f && _quality > HologramQuality.Low)
            {
                Quality = _quality - 1;
                Debug.Log($"[HiFiHologram] Reduced quality to {_quality} (FPS: {_currentFPS:F0})");
            }
            // If FPS is very high, consider increasing quality
            else if (_currentFPS > _targetFPS * 1.2f && _quality < HologramQuality.Ultra)
            {
                Quality = _quality + 1;
                Debug.Log($"[HiFiHologram] Increased quality to {_quality} (FPS: {_currentFPS:F0})");
            }
        }

        #endregion

        #region Input Textures

        private void UpdateInputTextures()
        {
            if (_vfx == null) return;

            // Update textures if they've changed
            if (_colorMap != null && _vfx.HasTexture(PropertyID.ColorMap))
            {
                _vfx.SetTexture(PropertyID.ColorMap, _colorMap);
            }

            if (_depthMap != null && _vfx.HasTexture(PropertyID.DepthMap))
            {
                _vfx.SetTexture(PropertyID.DepthMap, _depthMap);
            }

            if (_stencilMap != null && _vfx.HasTexture(PropertyID.StencilMap))
            {
                _vfx.SetTexture(PropertyID.StencilMap, _stencilMap);
            }
        }

        #endregion

        #region Debug

        private void DrawDebugInfo()
        {
            var (particles, size) = QualityPresets[(int)_quality];
            Debug.Log($"[HiFiHologram] Quality: {_quality}, Particles: {particles}, Size: {size * 1000:F2}mm, FPS: {_currentFPS:F0}");
        }

        private void OnGUI()
        {
            if (!_showDebugInfo) return;

            GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 150));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"HiFi Hologram", GUI.skin.box);
            GUILayout.Label($"Quality: {_quality}");
            GUILayout.Label($"Particles: {QualityPresets[(int)_quality].particles:N0}");
            GUILayout.Label($"Size: {QualityPresets[(int)_quality].size * 1000:F2}mm");
            GUILayout.Label($"FPS: {_currentFPS:F0}");
            GUILayout.Label($"Auto: {(_autoAdjustQuality ? "On" : "Off")}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Apply High Quality")]
        private void ApplyHighQuality() => Quality = HologramQuality.High;

        [ContextMenu("Apply Ultra Quality")]
        private void ApplyUltraQuality() => Quality = HologramQuality.Ultra;

        [ContextMenu("Apply Low Quality")]
        private void ApplyLowQuality() => Quality = HologramQuality.Low;

        [ContextMenu("Toggle Debug Info")]
        private void ToggleDebugInfo() => _showDebugInfo = !_showDebugInfo;
#endif

        #endregion
    }
}
