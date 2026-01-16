// NNCam Keypoint Binder
// Adapts BodyPartSegmenter's KeypointBuffer for NNCam2 VFX
// Works with eyes_any_nncam2.vfx, joints_any_nncam2.vfx, electrify_any_nncam2.vfx, etc.
// Also binds ColorMap, MaskTexture, and Throttle for full NNCam2 compatibility

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

#if BODYPIX_AVAILABLE
using MetavidoVFX.Segmentation;
#endif

namespace MetavidoVFX.NNCam
{
    /// <summary>
    /// VFX Property Binder that connects BodyPartSegmenter's KeypointBuffer
    /// to NNCam2 VFX effects (Eyes, Joints, Electrify, etc.).
    ///
    /// Binds:
    /// - KeypointBuffer: 17 pose keypoints (Vector4 buffer: xy=UV, z=score)
    /// - MaskTexture: 24-part body segmentation mask
    /// - ColorMap: Camera color texture (from ARDepthSource)
    /// - Throttle: VFX intensity control (0-1)
    /// </summary>
    [VFXBinder("NNCam/Keypoint Buffer")]
    public class NNCamKeypointBinder : VFXBinderBase
    {
        [Header("Property Names")]
        [VFXPropertyBinding("GraphicsBuffer"), SerializeField]
        ExposedProperty _keypointProperty = "KeypointBuffer";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        ExposedProperty _maskProperty = "MaskTexture";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        ExposedProperty _colorMapProperty = "ColorMap";

        [VFXPropertyBinding("System.Single"), SerializeField]
        ExposedProperty _throttleProperty = "Throttle";

        [Header("Source")]
        [Tooltip("BodyPartSegmenter that provides KeypointBuffer and MaskTexture. Auto-found if null.")]
#if BODYPIX_AVAILABLE
        public BodyPartSegmenter segmenter;
#endif

        [Header("Throttle Settings")]
        [Range(0f, 1f)]
        [Tooltip("Base throttle value (emission intensity)")]
        public float throttle = 1f;

        [Tooltip("If true, throttle is modulated by audio volume")]
        public bool audioModulatedThrottle = false;

        [Header("Optional Bindings")]
        [Tooltip("Bind MaskTexture if VFX has the property")]
        public bool bindMask = true;

        [Tooltip("Bind ColorMap from ARDepthSource if VFX has the property")]
        public bool bindColorMap = true;

        [Header("Debug")]
        public bool verboseLogging = false;

        // Cached references
        ARDepthSource _arDepthSource;

#if BODYPIX_AVAILABLE
        protected override void Awake()
        {
            base.Awake();
            if (segmenter == null)
            {
                segmenter = FindFirstObjectByType<BodyPartSegmenter>();
            }
        }
#endif

        void Start()
        {
            _arDepthSource = ARDepthSource.Instance;
        }

        public override bool IsValid(VisualEffect component)
        {
#if BODYPIX_AVAILABLE
            // Valid if segmenter is ready OR we have at least throttle to bind
            bool hasKeypointBuffer = component.HasGraphicsBuffer(_keypointProperty);
            bool hasThrottle = component.HasFloat(_throttleProperty);

            bool keypointValid = segmenter != null &&
                                 segmenter.IsReady &&
                                 segmenter.KeypointBuffer != null &&
                                 hasKeypointBuffer;

            // Accept VFX that have KeypointBuffer OR just Throttle
            return keypointValid || hasThrottle;
#else
            // Even without BodyPix, we can bind Throttle
            return component.HasFloat(_throttleProperty);
#endif
        }

        public override void UpdateBinding(VisualEffect component)
        {
#if BODYPIX_AVAILABLE
            // Bind KeypointBuffer
            if (segmenter != null && segmenter.IsReady && segmenter.KeypointBuffer != null)
            {
                if (component.HasGraphicsBuffer(_keypointProperty))
                {
                    component.SetGraphicsBuffer(_keypointProperty, segmenter.KeypointBuffer);
                }

                // Bind MaskTexture
                if (bindMask && segmenter.MaskTexture != null && component.HasTexture(_maskProperty))
                {
                    component.SetTexture(_maskProperty, segmenter.MaskTexture);
                }
            }
#endif

            // Bind ColorMap from ARDepthSource
            if (bindColorMap && _arDepthSource == null)
            {
                _arDepthSource = ARDepthSource.Instance;
            }

            if (bindColorMap && _arDepthSource != null && _arDepthSource.ColorMap != null)
            {
                if (component.HasTexture(_colorMapProperty))
                {
                    component.SetTexture(_colorMapProperty, _arDepthSource.ColorMap);
                }
            }

            // Bind Throttle
            if (component.HasFloat(_throttleProperty))
            {
                float finalThrottle = throttle;

                // Optionally modulate by audio
                if (audioModulatedThrottle && AudioBridge.Instance != null)
                {
                    finalThrottle *= AudioBridge.Instance.Volume;
                }

                component.SetFloat(_throttleProperty, finalThrottle);
            }

            if (verboseLogging && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[NNCamKeypointBinder] Bound data to {component.name}");
            }
        }

        public override string ToString()
        {
#if BODYPIX_AVAILABLE
            string status = segmenter != null && segmenter.IsReady ? "Ready" : "Not Ready";
            return $"NNCam Keypoint : {_keypointProperty} ({status})";
#else
            return $"NNCam Keypoint : Throttle only (BODYPIX_AVAILABLE not defined)";
#endif
        }

        /// <summary>
        /// Set throttle value at runtime
        /// </summary>
        public void SetThrottle(float value)
        {
            throttle = Mathf.Clamp01(value);
        }
    }
}
