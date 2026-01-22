// BrushData.cs - Brush definition ScriptableObject
// Part of Spec 011: OpenBrush Integration
//
// Simplified brush descriptor adapted from OpenBrush's BrushDescriptor.
// Contains all metadata needed for a brush: material, geometry type, audio reactive params.

using System;
using UnityEngine;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Defines a brush type with all rendering and behavior parameters.
    /// Create via Assets > Create > XRRAI > Brush Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBrush", menuName = "XRRAI/Brush Data")]
    public class BrushData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for save/load compatibility")]
        public string BrushId;

        [Tooltip("Display name in UI")]
        public string DisplayName;

        [Tooltip("Category for UI grouping")]
        public BrushCategory Category = BrushCategory.Basic;

        [Tooltip("Thumbnail for brush selector")]
        public Texture2D Thumbnail;

        [Header("Rendering")]
        [Tooltip("Material used for stroke rendering")]
        public Material Material;

        [Tooltip("How geometry is generated")]
        public BrushGeometryType GeometryType = BrushGeometryType.Flat;

        [Tooltip("Render backfaces for two-sided strokes")]
        public bool RenderBackfaces;

        [Header("Size")]
        [Tooltip("Brush size range (min, max) in meters")]
        public Vector2 SizeRange = new Vector2(0.001f, 0.1f);

        [Tooltip("Default brush size in meters")]
        public float DefaultSize = 0.02f;

        [Tooltip("Size variance for particle/spray brushes")]
        [Range(0f, 1f)]
        public float SizeVariance;

        [Header("Pressure")]
        [Tooltip("Pressure sensitivity range (0=no effect, 1=full range)")]
        public Vector2 PressureRange = new Vector2(0.1f, 1f);

        [Tooltip("Opacity range based on pressure")]
        public Vector2 OpacityRange = new Vector2(0.5f, 1f);

        [Header("Color")]
        [Tooltip("Base opacity multiplier")]
        [Range(0f, 1f)]
        public float BaseOpacity = 1f;

        [Tooltip("Minimum luminance for color picker")]
        [Range(0f, 1f)]
        public float ColorLuminanceMin;

        [Tooltip("Maximum saturation for color picker")]
        [Range(0f, 1f)]
        public float ColorSaturationMax = 1f;

        [Header("Particles (if applicable)")]
        [Tooltip("Particle emission rate")]
        public float ParticleRate = 100f;

        [Tooltip("Particle speed")]
        public float ParticleSpeed = 1f;

        [Tooltip("Random rotation range for particles")]
        public float ParticleRotationRange = 360f;

        [Header("Tube (if applicable)")]
        [Tooltip("Number of sides for tube geometry")]
        [Range(3, 32)]
        public int TubeSides = 8;

        [Tooltip("Minimum segment length in meters")]
        public float MinSegmentLength = 0.002f;

        [Header("Audio Reactive")]
        [Tooltip("Is this brush audio reactive?")]
        public bool IsAudioReactive;

        [Tooltip("Audio reactive parameters")]
        public AudioReactiveParams AudioParams;

        [Header("Advanced")]
        [Tooltip("Texture atlas rows (V direction)")]
        public int TextureAtlasV = 1;

        [Tooltip("Texture tile rate")]
        public float TileRate = 1f;

        [Tooltip("Extra bounds padding in meters")]
        public float BoundsPadding;

        /// <summary>
        /// Get brush size clamped to valid range
        /// </summary>
        public float ClampSize(float size)
        {
            return Mathf.Clamp(size, SizeRange.x, SizeRange.y);
        }

        /// <summary>
        /// Get size adjusted for pressure
        /// </summary>
        public float GetPressuredSize(float baseSize, float pressure01)
        {
            float multiplier = Mathf.Lerp(PressureRange.x, PressureRange.y, pressure01);
            return baseSize * multiplier;
        }

        /// <summary>
        /// Get opacity adjusted for pressure
        /// </summary>
        public float GetPressuredOpacity(float pressure01)
        {
            return Mathf.Lerp(OpacityRange.x, OpacityRange.y, pressure01) * BaseOpacity;
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(BrushId))
                BrushId = Guid.NewGuid().ToString("N").Substring(0, 8);
            if (string.IsNullOrEmpty(DisplayName))
                DisplayName = name;
        }
    }

    /// <summary>
    /// Brush categories for UI organization
    /// </summary>
    public enum BrushCategory
    {
        Basic,
        Artistic,
        Particles,
        AudioReactive,
        Experimental
    }

    /// <summary>
    /// How brush geometry is generated
    /// </summary>
    public enum BrushGeometryType
    {
        /// <summary>Flat ribbon facing camera</summary>
        Flat,

        /// <summary>3D tube with configurable sides</summary>
        Tube,

        /// <summary>Particle system</summary>
        Particles,

        /// <summary>Spray/splatter effect</summary>
        Spray,

        /// <summary>Custom mesh per control point</summary>
        Custom
    }

    /// <summary>
    /// Parameters for audio reactive brushes
    /// </summary>
    [Serializable]
    public class AudioReactiveParams
    {
        [Tooltip("Which audio property drives the effect")]
        public AudioReactiveMode Mode = AudioReactiveMode.RMS;

        [Tooltip("Audio band to use (0-7 for FFT modes)")]
        [Range(0, 7)]
        public int FrequencyBand;

        [Tooltip("Sensitivity multiplier")]
        [Range(0.1f, 10f)]
        public float Sensitivity = 1f;

        [Tooltip("Smoothing factor (higher = smoother)")]
        [Range(0f, 0.99f)]
        public float Smoothing = 0.8f;

        [Header("Size Modulation")]
        [Tooltip("Audio modulates brush size")]
        public bool ModulateSize;
        public Vector2 SizeMultiplierRange = new Vector2(0.5f, 2f);

        [Header("Color Modulation")]
        [Tooltip("Audio modulates color hue")]
        public bool ModulateHue;
        public float HueShiftRange = 0.5f;

        [Header("Emission Modulation")]
        [Tooltip("Audio modulates emission intensity")]
        public bool ModulateEmission;
        public Vector2 EmissionRange = new Vector2(0f, 2f);

        [Header("Particle Rate Modulation")]
        [Tooltip("Audio modulates particle emission rate")]
        public bool ModulateParticleRate;
        public Vector2 ParticleRateMultiplier = new Vector2(0.1f, 3f);
    }

    /// <summary>
    /// How audio input drives the brush effect
    /// </summary>
    public enum AudioReactiveMode
    {
        /// <summary>Root Mean Square (overall volume)</summary>
        RMS,

        /// <summary>Single frequency band</summary>
        FrequencyBand,

        /// <summary>All 8 frequency bands (for gradient effects)</summary>
        FFTSpectrum,

        /// <summary>Peak detection with decay</summary>
        Peak,

        /// <summary>Beat detection</summary>
        Beat
    }
}
