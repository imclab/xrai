// VFXAudioDataBinder - Binds audio frequency band data to VFX
// Uses EnhancedAudioProcessor for FFT analysis

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

        [Header("Modulation")]
        [Range(0f, 2f)]
        public float volumeMultiplier = 1f;
        [Range(0f, 2f)]
        public float bandMultiplier = 1f;

        protected override void Awake()
        {
            base.Awake();
            if (audioProcessor == null)
                audioProcessor = FindFirstObjectByType<Audio.EnhancedAudioProcessor>();
        }

        public override bool IsValid(VisualEffect component)
        {
            return audioProcessor != null && (
                component.HasFloat(volumeProperty) ||
                component.HasFloat(bassProperty) ||
                component.HasFloat(midProperty) ||
                component.HasFloat(trebleProperty)
            );
        }

        public override void UpdateBinding(VisualEffect component)
        {
            if (audioProcessor == null) return;

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

        public override string ToString()
        {
            return $"Audio Data : {volumeProperty}, {bassProperty}, {midProperty}, {trebleProperty}";
        }
    }
}
