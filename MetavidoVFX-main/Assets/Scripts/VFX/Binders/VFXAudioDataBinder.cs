// VFXAudioDataBinder - Binds audio frequency band data to VFX
// Supports both AudioBridge (simple, with beat detection) and EnhancedAudioProcessor (advanced, with pitch)
// Updated for spec-007: Beat detection support

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

        [Header("Modulation")]
        [Range(0f, 2f)]
        public float volumeMultiplier = 1f;
        [Range(0f, 2f)]
        public float bandMultiplier = 1f;
        [Range(0f, 2f)]
        public float beatMultiplier = 1f;

        // Cache AudioBridge reference for beat detection
        AudioBridge _audioBridge;

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
            // Valid if either EnhancedAudioProcessor or AudioBridge is available
            bool hasAudioSource = audioProcessor != null || _audioBridge != null;

            bool hasAnyProperty =
                component.HasFloat(volumeProperty) ||
                component.HasFloat(bassProperty) ||
                component.HasFloat(midProperty) ||
                component.HasFloat(trebleProperty) ||
                component.HasFloat(beatPulseProperty) ||
                component.HasFloat(beatIntensityProperty);

            return hasAudioSource && hasAnyProperty;
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
        }

        public override string ToString()
        {
            string source = audioProcessor != null ? "Enhanced" : (_audioBridge != null ? "Bridge" : "None");
            return $"Audio Data [{source}]: {volumeProperty}, {bassProperty}, Beat: {bindBeatDetection}";
        }
    }
}
