// VFXAudioDataBinder8Band.cs - 8-band audio binder using UnifiedAudioReactive
// Replaces legacy 4-band VFXAudioDataBinder with full 8-band Reaktion-style support
// Auto-binds to UnifiedAudioReactive singleton - no manual wiring required

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using XRRAI.Audio;

namespace XRRAI.VFXBinders
{
    /// <summary>
    /// VFX binder for 8-band audio reactive effects.
    /// Automatically uses UnifiedAudioReactive singleton - zero configuration.
    /// </summary>
    [VFXBinder("Audio/Audio Data (8-Band)")]
    public class VFXAudioDataBinder8Band : VFXBinderBase
    {
        [Header("8-Band Properties")]
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty band0Property = "AudioBand0"; // Sub-bass
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty band1Property = "AudioBand1"; // Bass
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty band2Property = "AudioBand2"; // Low-mids
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty band3Property = "AudioBand3"; // Mids
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty band4Property = "AudioBand4"; // High-mids
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty band5Property = "AudioBand5"; // Presence
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty band6Property = "AudioBand6"; // Brilliance
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty band7Property = "AudioBand7"; // Air

        [Header("Legacy 4-Band Compatibility")]
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty subBassProperty = "AudioSubBass";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty bassProperty = "AudioBass";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty midProperty = "AudioMid";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty trebleProperty = "AudioTreble";

        [Header("Volume & Peak")]
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty volumeProperty = "AudioVolume";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty peakProperty = "AudioPeak";

        [Header("Beat Detection")]
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty beatPulseProperty = "BeatPulse";
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty beatIntensityProperty = "BeatIntensity";
        public bool bindBeatDetection = true;

        [Header("Audio Texture")]
        [Tooltip("Bind 4x2 audio texture for VFX using HLSL sampling")]
        public bool bindAudioTexture = true;
        [VFXPropertyBinding("UnityEngine.Texture2D")]
        public ExposedProperty audioTextureProperty = "AudioDataTexture";

        [Header("Modulation")]
        [Range(0f, 2f)]
        public float bandMultiplier = 1f;
        [Range(0f, 2f)]
        public float volumeMultiplier = 1f;
        [Range(0f, 2f)]
        public float beatMultiplier = 1f;

        // Cached reference
        UnifiedAudioReactive _audio;

        protected override void Awake()
        {
            base.Awake();
            _audio = UnifiedAudioReactive.Instance;
        }

        public override bool IsValid(VisualEffect component)
        {
            // Try to get audio source
            if (_audio == null)
                _audio = UnifiedAudioReactive.Instance ?? FindFirstObjectByType<UnifiedAudioReactive>();

            if (_audio == null)
            {
                // Fallback: Check for legacy AudioBridge
                var legacyBridge = AudioBridge.Instance;
                if (legacyBridge != null)
                {
                    Debug.LogWarning($"[VFXAudioDataBinder8Band] Using legacy AudioBridge for {component.name}. Consider adding UnifiedAudioReactive.");
                    return true; // Will use global shader props
                }
                return false;
            }

            return true;
        }

        public override void UpdateBinding(VisualEffect component)
        {
            // Re-acquire reference if needed
            if (_audio == null)
                _audio = UnifiedAudioReactive.Instance;

            if (_audio == null) return;

            // 8-band bindings
            if (component.HasFloat(band0Property))
                component.SetFloat(band0Property, _audio.Band0 * bandMultiplier);
            if (component.HasFloat(band1Property))
                component.SetFloat(band1Property, _audio.Band1 * bandMultiplier);
            if (component.HasFloat(band2Property))
                component.SetFloat(band2Property, _audio.Band2 * bandMultiplier);
            if (component.HasFloat(band3Property))
                component.SetFloat(band3Property, _audio.Band3 * bandMultiplier);
            if (component.HasFloat(band4Property))
                component.SetFloat(band4Property, _audio.Band4 * bandMultiplier);
            if (component.HasFloat(band5Property))
                component.SetFloat(band5Property, _audio.Band5 * bandMultiplier);
            if (component.HasFloat(band6Property))
                component.SetFloat(band6Property, _audio.Band6 * bandMultiplier);
            if (component.HasFloat(band7Property))
                component.SetFloat(band7Property, _audio.Band7 * bandMultiplier);

            // Legacy 4-band compatibility
            if (component.HasFloat(subBassProperty))
                component.SetFloat(subBassProperty, _audio.SubBass * bandMultiplier);
            if (component.HasFloat(bassProperty))
                component.SetFloat(bassProperty, _audio.Bass * bandMultiplier);
            if (component.HasFloat(midProperty))
                component.SetFloat(midProperty, _audio.Mids * bandMultiplier);
            if (component.HasFloat(trebleProperty))
                component.SetFloat(trebleProperty, _audio.Treble * bandMultiplier);

            // Volume & peak
            if (component.HasFloat(volumeProperty))
                component.SetFloat(volumeProperty, _audio.Volume * volumeMultiplier);
            if (component.HasFloat(peakProperty))
                component.SetFloat(peakProperty, _audio.Peak * volumeMultiplier);

            // Beat detection
            if (bindBeatDetection)
            {
                if (component.HasFloat(beatPulseProperty))
                    component.SetFloat(beatPulseProperty, _audio.BeatPulse * beatMultiplier);
                if (component.HasFloat(beatIntensityProperty))
                    component.SetFloat(beatIntensityProperty, _audio.BeatIntensity * beatMultiplier);
            }

            // Audio texture
            if (bindAudioTexture && _audio.AudioTexture != null)
            {
                if (component.HasTexture(audioTextureProperty))
                    component.SetTexture(audioTextureProperty, _audio.AudioTexture);
            }
        }

        public override string ToString()
        {
            string src = _audio != null ? "Unified8" : "None";
            return $"Audio 8-Band [{src}]: {band0Property}-{band7Property}, Beat: {bindBeatDetection}";
        }

        /// <summary>
        /// Manually set audio source (useful for testing)
        /// </summary>
        public void SetAudioSource(UnifiedAudioReactive audio)
        {
            _audio = audio;
        }
    }
}
