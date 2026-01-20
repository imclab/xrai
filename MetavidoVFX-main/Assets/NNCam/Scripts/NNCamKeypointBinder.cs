// NNCam Keypoint Binder (Fluo-style simplified)
// Binds BodyPartSegmenter data to NNCam2 VFX
// Use NNCamCameraSpace on VFX parent for UV→world positioning

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

#if BODYPIX_AVAILABLE
using MetavidoVFX.Segmentation;
#endif

namespace MetavidoVFX.NNCam
{
    /// <summary>
    /// Simple VFX binder that connects BodyPartSegmenter to NNCam2 VFX.
    /// Like Fluo's ARVfxBridge - just binds data, no complex computation.
    ///
    /// For UV→world positioning, add NNCamCameraSpace to VFX parent.
    /// VFX uses screen-space "Get Keypoint" subgraph which outputs UV positions.
    /// NNCamCameraSpace transform converts UV to world space.
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

        [VFXPropertyBinding("System.Single"), SerializeField]
        ExposedProperty _thresholdProperty = "Threshold";

        [Header("Source")]
#if BODYPIX_AVAILABLE
        [Tooltip("BodyPartSegmenter that provides KeypointBuffer and MaskTexture. Auto-found if null.")]
        public BodyPartSegmenter segmenter;
#endif

        [Header("Settings")]
        [Range(0f, 1f)]
        [Tooltip("VFX intensity (emission)")]
        public float throttle = 1f;

        [Range(0f, 1f)]
        [Tooltip("Keypoint confidence threshold (0.3 works for eyes)")]
        public float threshold = 0.3f;

        [Header("Optional Bindings")]
        public bool bindMask = true;
        public bool bindColorMap = true;
        public bool bindThreshold = true;

        [Header("Debug")]
        public bool verboseLogging = false;

        ARDepthSource _arDepthSource;
        static System.Collections.Generic.HashSet<int> _loggedVFXIds = new System.Collections.Generic.HashSet<int>();

#if BODYPIX_AVAILABLE
        protected override void Awake()
        {
            base.Awake();
            if (segmenter == null)
                segmenter = FindFirstObjectByType<BodyPartSegmenter>();
        }
#endif

        void Start()
        {
            _arDepthSource = ARDepthSource.Instance;
            if (bindColorMap && _arDepthSource != null)
                _arDepthSource.RequestColorMap(true);
        }

        void OnEnable()
        {
            if (bindColorMap)
            {
                if (_arDepthSource == null)
                    _arDepthSource = ARDepthSource.Instance;
                if (_arDepthSource != null)
                    _arDepthSource.RequestColorMap(true);
            }
        }

        void OnDisable()
        {
            if (bindColorMap && _arDepthSource != null)
                _arDepthSource.RequestColorMap(false);
        }

        public override bool IsValid(VisualEffect component)
        {
#if BODYPIX_AVAILABLE
            bool hasKeypointBuffer = component.HasGraphicsBuffer(_keypointProperty);
            bool keypointValid = segmenter != null &&
                                 segmenter.IsReady &&
                                 segmenter.KeypointBuffer != null &&
                                 hasKeypointBuffer;

//             if (verboseLogging)
//             {
// //                Debug.Log($"[NNCamKeypointBinder] IsValid: seg={segmenter != null}, ready={segmenter?.IsReady}, buf={segmenter?.KeypointBuffer != null}, hasVFXBuf={hasKeypointBuffer}");
//             }

            return keypointValid || component.HasFloat(_throttleProperty);
#else
            if (verboseLogging)
                Debug.Log("[NNCamKeypointBinder] BODYPIX_AVAILABLE not defined");
            return component.HasFloat(_throttleProperty);
#endif
        }

        public override void UpdateBinding(VisualEffect component)
        {
#if BODYPIX_AVAILABLE
            // Bind KeypointBuffer - that's the main data
            if (segmenter != null && segmenter.IsReady && segmenter.KeypointBuffer != null)
            {
                if (component.HasGraphicsBuffer(_keypointProperty))
                    component.SetGraphicsBuffer(_keypointProperty, segmenter.KeypointBuffer);

                // Bind MaskTexture if available
                if (bindMask && segmenter.MaskTexture != null && component.HasTexture(_maskProperty))
                    component.SetTexture(_maskProperty, segmenter.MaskTexture);
            }
#endif

            // Bind ColorMap from ARDepthSource
            if (bindColorMap)
            {
                if (_arDepthSource == null)
                    _arDepthSource = ARDepthSource.Instance;

                if (_arDepthSource != null && _arDepthSource.ColorMap != null && component.HasTexture(_colorMapProperty))
                    component.SetTexture(_colorMapProperty, _arDepthSource.ColorMap);
            }

            // Bind Throttle
            if (component.HasFloat(_throttleProperty))
                component.SetFloat(_throttleProperty, throttle);

            // Bind Threshold
            if (bindThreshold && component.HasFloat(_thresholdProperty))
                component.SetFloat(_thresholdProperty, threshold);

            // Log once per VFX
            if (verboseLogging && !_loggedVFXIds.Contains(component.GetInstanceID()))
            {
                Debug.Log($"[NNCamKeypointBinder] Bound to {component.name}");
                _loggedVFXIds.Add(component.GetInstanceID());
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

        public void SetThrottle(float value) => throttle = Mathf.Clamp01(value);
        public void SetThreshold(float value) => threshold = Mathf.Clamp01(value);
    }
}
