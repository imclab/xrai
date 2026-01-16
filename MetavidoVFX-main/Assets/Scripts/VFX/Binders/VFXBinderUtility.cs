// VFXBinderUtility - Helper for attaching binders to runtime-spawned VFX
// Usage: VFXBinderUtility.SetupVFX(vfxComponent, VFXBinderPreset.ARWithAudio);

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace MetavidoVFX.VFX.Binders
{
    public enum VFXBinderPreset
    {
        None,           // No bindings
        AROnly,         // DepthMap, StencilMap, ColorMap, PositionMap, Matrices
        AudioOnly,      // Audio frequency bands
        HandOnly,       // Hand position, velocity, pinch
        ARWithAudio,    // AR + Audio
        ARWithHand,     // AR + Hand
        Full            // All bindings
    }

    public static class VFXBinderUtility
    {
        /// <summary>
        /// Setup a VFX component with the specified binding preset.
        /// Adds VFXPropertyBinder and appropriate data binders.
        /// </summary>
        public static VFXPropertyBinder SetupVFX(VisualEffect vfx, VFXBinderPreset preset)
        {
            if (vfx == null) return null;

            // Ensure VFXPropertyBinder exists
            var propertyBinder = vfx.GetComponent<VFXPropertyBinder>();
            if (propertyBinder == null)
            {
                propertyBinder = vfx.gameObject.AddComponent<VFXPropertyBinder>();
            }

            // Add binders based on preset
            switch (preset)
            {
                case VFXBinderPreset.AROnly:
                    AddARBinder(vfx.gameObject);
                    break;

                case VFXBinderPreset.AudioOnly:
                    AddAudioBinder(vfx.gameObject);
                    break;

                case VFXBinderPreset.HandOnly:
                    AddHandBinder(vfx.gameObject);
                    break;

                case VFXBinderPreset.ARWithAudio:
                    AddARBinder(vfx.gameObject);
                    AddAudioBinder(vfx.gameObject);
                    break;

                case VFXBinderPreset.ARWithHand:
                    AddARBinder(vfx.gameObject);
                    AddHandBinder(vfx.gameObject);
                    break;

                case VFXBinderPreset.Full:
                    AddARBinder(vfx.gameObject);
                    AddAudioBinder(vfx.gameObject);
                    AddHandBinder(vfx.gameObject);
                    break;

                case VFXBinderPreset.None:
                default:
                    break;
            }

            return propertyBinder;
        }

        /// <summary>
        /// Setup VFX with custom binder configuration
        /// </summary>
        public static VFXPropertyBinder SetupVFX(VisualEffect vfx, bool ar, bool audio, bool hand)
        {
            if (vfx == null) return null;

            var propertyBinder = vfx.GetComponent<VFXPropertyBinder>();
            if (propertyBinder == null)
            {
                propertyBinder = vfx.gameObject.AddComponent<VFXPropertyBinder>();
            }

            if (ar) AddARBinder(vfx.gameObject);
            if (audio) AddAudioBinder(vfx.gameObject);
            if (hand) AddHandBinder(vfx.gameObject);

            return propertyBinder;
        }

        /// <summary>
        /// Add AR data binder to GameObject
        /// </summary>
        public static VFXARDataBinder AddARBinder(GameObject go)
        {
            var existing = go.GetComponent<VFXARDataBinder>();
            if (existing != null) return existing;
            return go.AddComponent<VFXARDataBinder>();
        }

        /// <summary>
        /// Add Audio data binder to GameObject
        /// </summary>
        public static VFXAudioDataBinder AddAudioBinder(GameObject go)
        {
            var existing = go.GetComponent<VFXAudioDataBinder>();
            if (existing != null) return existing;
            return go.AddComponent<VFXAudioDataBinder>();
        }

        /// <summary>
        /// Add Hand data binder to GameObject
        /// </summary>
        public static Component AddHandBinder(GameObject go, bool leftHand = true)
        {
            // Use type name to avoid compile-order issues
            var binderType = System.Type.GetType("MetavidoVFX.VFX.Binders.VFXHandDataBinder, Assembly-CSharp");
            if (binderType == null) return null;

            var existing = go.GetComponent(binderType);
            if (existing != null) return existing;

            var binder = go.AddComponent(binderType);
            var field = binderType.GetField("useLeftHand");
            if (field != null) field.SetValue(binder, leftHand);
            return binder;
        }

        /// <summary>
        /// Remove all custom binders from a VFX GameObject
        /// </summary>
        public static void ClearBinders(GameObject go)
        {
            var arBinders = go.GetComponents<VFXARDataBinder>();
            var audioBinders = go.GetComponents<VFXAudioDataBinder>();

            foreach (var b in arBinders) Object.Destroy(b);
            foreach (var b in audioBinders) Object.Destroy(b);

            // Use reflection for hand binder to avoid compile-order issues
            var handType = System.Type.GetType("MetavidoVFX.VFX.Binders.VFXHandDataBinder, Assembly-CSharp");
            if (handType != null)
            {
                var handBinders = go.GetComponents(handType);
                foreach (var b in handBinders) Object.Destroy(b);
            }
        }

        /// <summary>
        /// Auto-detect what bindings a VFX needs based on its exposed properties
        /// </summary>
        public static VFXBinderPreset DetectPreset(VisualEffect vfx)
        {
            if (vfx == null) return VFXBinderPreset.None;

            bool needsAR = vfx.HasTexture("DepthMap") || vfx.HasTexture("PositionMap") ||
                           vfx.HasTexture("StencilMap") || vfx.HasMatrix4x4("InverseView");

            bool needsAudio = vfx.HasFloat("AudioVolume") || vfx.HasFloat("AudioBass") ||
                              vfx.HasFloat("AudioMid") || vfx.HasFloat("AudioTreble");

            bool needsHand = vfx.HasVector3("HandPosition") || vfx.HasVector3("HandVelocity") ||
                             vfx.HasFloat("BrushWidth") || vfx.HasBool("IsPinching");

            if (needsAR && needsAudio && needsHand) return VFXBinderPreset.Full;
            if (needsAR && needsAudio) return VFXBinderPreset.ARWithAudio;
            if (needsAR && needsHand) return VFXBinderPreset.ARWithHand;
            if (needsAR) return VFXBinderPreset.AROnly;
            if (needsAudio) return VFXBinderPreset.AudioOnly;
            if (needsHand) return VFXBinderPreset.HandOnly;

            return VFXBinderPreset.None;
        }

        /// <summary>
        /// Setup VFX with auto-detected bindings
        /// </summary>
        public static VFXPropertyBinder SetupVFXAuto(VisualEffect vfx)
        {
            var preset = DetectPreset(vfx);
            return SetupVFX(vfx, preset);
        }
    }
}
