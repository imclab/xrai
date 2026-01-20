// VFXAudioDataBinder - Binds audio frequency band data to VFX
// Supports both AudioBridge (simple, with beat detection) and EnhancedAudioProcessor (advanced, with pitch)
// Updated for spec-007: Beat detection support
// Auto-binds AudioDataTexture for VFX without exposed float properties

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using MetavidoVFX.Audio;

namespace MetavidoVFX.VFX.Binders
{
    [VFXBinder("Audio/Audio Data")]
    public class VFXAudioDataBinder : VFXBinderBase
    {
        [Header("Audio Source (auto-found if null)")]
        [Tooltip("EnhancedAudioProcessor for pitch detection. Optional - uses AudioBridge if null.")]
        public EnhancedAudioProcessor audioProcessor;

        [Header("Property Names")]
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty volumeProperty = "AudioVolume";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty pitchProperty = "AudioPitch";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty bassProperty = "AudioBass";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty midProperty = "AudioMid";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty trebleProperty = "AudioTreble";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty subBassProperty = "AudioSubBass";

        [Header("Beat Detection (spec-007)")]
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty beatPulseProperty = "BeatPulse";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty beatIntensityProperty = "BeatIntensity";
        [Tooltip("Bind beat detection properties from AudioBridge")]
        public bool bindBeatDetection = true;

        [Header("Audio Data Texture (Auto-Fallback)")]
        [Tooltip("Automatically bind AudioDataTexture for VFX without exposed audio properties")]
        public bool autoBindAudioTexture = true;
        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty audioDataTextureProperty = "AudioDataTexture";

        [Header("Modulation")]
        [Range(0f, 2f)]
        public float volumeMultiplier = 1f;
        [Range(0f, 2f)]
        public float bandMultiplier = 1f;
        [Range(0f, 2f)]
        public float beatMultiplier = 1f;

        // Cache AudioBridge reference for beat detection
        AudioBridge _audioBridge;

        // Track if we're using texture fallback
        bool _usingTextureFallback;

        protected override void Awake()
        {
            base.Awake();
            // Try to find EnhancedAudioProcessor first (for pitch), then AudioBridge (for beat)
            if (audioProcessor == null)
                audioProcessor = FindFirstObjectByType<Audio.EnhancedAudioProcessor>();

            _audioBridge = AudioBridge.Instance;
            if (_audioBridge == null)
                _audioBridge = FindFirstObjectByType<AudioBridge>();
        }

        public override bool IsValid(VisualEffect component)
        {
            // Valid if AudioBridge is available - it sets global shader properties
            // that any VFX can read via Custom HLSL (no exposed properties needed)
            bool hasAudioSource = audioProcessor != null || _audioBridge != null;

            // Check for any bindable properties (optional - globals work without these)
            bool hasAnyFloatProperty =
                component.HasFloat(volumeProperty) ||
                component.HasFloat(bassProperty) ||
                component.HasFloat(midProperty) ||
                component.HasFloat(trebleProperty) ||
                component.HasFloat(beatPulseProperty) ||
                component.HasFloat(beatIntensityProperty);

            // Check for texture property (auto-fallback)
            bool hasTextureProperty = autoBindAudioTexture && component.HasTexture(audioDataTextureProperty);

            // Track if we're using texture fallback
            _usingTextureFallback = !hasAnyFloatProperty && hasTextureProperty;

            // ALWAYS valid if we have an audio source - globals are set for Custom HLSL access
            // Even without exposed properties, VFX can use #include "Assets/Shaders/ARGlobals.hlsl"
            return hasAudioSource;
        }

        public override void UpdateBinding(VisualEffect component)
        {
            // Use EnhancedAudioProcessor if available, else fall back to AudioBridge
            if (audioProcessor != null)
            {
                // EnhancedAudioProcessor bindings (includes pitch)
                if (component.HasFloat(volumeProperty))
                    component.SetFloat(volumeProperty, audioProcessor.AudioVolume * volumeMultiplier);

                if (component.HasFloat(pitchProperty))
                    component.SetFloat(pitchProperty, audioProcessor.AudioPitch);

                if (component.HasFloat(bassProperty))
                    component.SetFloat(bassProperty, audioProcessor.AudioBass * bandMultiplier);

                if (component.HasFloat(midProperty))
                    component.SetFloat(midProperty, audioProcessor.AudioMid * bandMultiplier);

                if (component.HasFloat(trebleProperty))
                    component.SetFloat(trebleProperty, audioProcessor.AudioTreble * bandMultiplier);

                if (component.HasFloat(subBassProperty))
                    component.SetFloat(subBassProperty, audioProcessor.AudioSubBass * bandMultiplier);
            }
            else if (_audioBridge != null)
            {
                // AudioBridge bindings (simpler, no pitch)
                if (component.HasFloat(volumeProperty))
                    component.SetFloat(volumeProperty, _audioBridge.Volume * volumeMultiplier);

                if (component.HasFloat(bassProperty))
                    component.SetFloat(bassProperty, _audioBridge.Bass * bandMultiplier);

                if (component.HasFloat(midProperty))
                    component.SetFloat(midProperty, _audioBridge.Mids * bandMultiplier);

                if (component.HasFloat(trebleProperty))
                    component.SetFloat(trebleProperty, _audioBridge.Treble * bandMultiplier);

                if (component.HasFloat(subBassProperty))
                    component.SetFloat(subBassProperty, _audioBridge.SubBass * bandMultiplier);
            }

            // Beat detection from AudioBridge (spec-007)
            if (bindBeatDetection && _audioBridge != null)
            {
                if (component.HasFloat(beatPulseProperty))
                    component.SetFloat(beatPulseProperty, _audioBridge.BeatPulse * beatMultiplier);

                if (component.HasFloat(beatIntensityProperty))
                    component.SetFloat(beatIntensityProperty, _audioBridge.BeatIntensity * beatMultiplier);
            }

            // Auto-bind AudioDataTexture for VFX without exposed float properties
            if (autoBindAudioTexture && _audioBridge != null && _audioBridge.AudioDataTexture != null)
            {
                if (component.HasTexture(audioDataTextureProperty))
                {
                    component.SetTexture(audioDataTextureProperty, _audioBridge.AudioDataTexture);
                }
            }
        }

        public override string ToString()
        {
            string source = audioProcessor != null ? "Enhanced" : (_audioBridge != null ? "Bridge" : "None");
            string mode = _usingTextureFallback ? " (Texture)" : "";
            return $"Audio Data [{source}{mode}]: {volumeProperty}, {bassProperty}, Beat: {bindBeatDetection}";
        }

        /// <summary>
        /// Returns true if this binder is using AudioDataTexture fallback instead of float properties.
        /// </summary>
        public bool IsUsingTextureFallback => _usingTextureFallback;
    }
}
