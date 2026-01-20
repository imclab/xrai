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
    /// - PositionMap: World positions for UV→3D conversion (from ARDepthSource)
    /// - Throttle: VFX intensity control (0-1)
    /// - Threshold: Keypoint confidence filter (default 0.3, lower = more lenient)
    ///
    /// To convert keypoint UV to world position in VFX Graph:
    /// 1. Get keypoint UV from KeypointBuffer (xy)
    /// 2. Sample PositionMap at UV to get world position
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

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        ExposedProperty _positionMapProperty = "PositionMap";

        [VFXPropertyBinding("GraphicsBuffer"), SerializeField]
        ExposedProperty _keypointWorldPositionsProperty = "KeypointWorldPositions";

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

        [Header("Threshold Settings")]
        [Range(0f, 1f)]
        [Tooltip("Keypoint confidence threshold. Lower = more lenient detection (eyes need ~0.3)")]
        public float threshold = 0.3f;

        [Tooltip("Bind Threshold if VFX has the property (filters keypoints by confidence)")]
        public bool bindThreshold = true;

        [Header("Optional Bindings")]
        [Tooltip("Bind MaskTexture if VFX has the property")]
        public bool bindMask = true;

        [Tooltip("Bind ColorMap from ARDepthSource if VFX has the property")]
        public bool bindColorMap = true;

        [Tooltip("Bind PositionMap from ARDepthSource for UV→world position sampling")]
        public bool bindPositionMap = true;

        [Tooltip("Compute and bind KeypointWorldPositions buffer (world positions from keypoint UVs + PositionMap)")]
        public bool bindKeypointWorldPositions = true;

        [Header("Debug")]
        public bool verboseLogging = false;

        // Keypoint world positions buffer (17 Vector4s: xyz=world pos, w=score)
        const int KEYPOINT_COUNT = 17;
        GraphicsBuffer _keypointWorldPositionsBuffer;
        Vector4[] _keypointWorldPositionsData;
        Vector4[] _keypointUVData;
        Texture2D _positionMapReadback;
        RenderTexture _lastPositionMap;

        // Cached references
        ARDepthSource _arDepthSource;

        // Log spam prevention - track which VFX we've already logged for
        static System.Collections.Generic.HashSet<int> _loggedVFXIds = new System.Collections.Generic.HashSet<int>();

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

            // Request ColorMap allocation if we need it (demand-driven system)
            if (bindColorMap && _arDepthSource != null)
            {
                _arDepthSource.RequestColorMap(true);
            }

            // Initialize KeypointWorldPositions buffer
            if (bindKeypointWorldPositions)
            {
                InitializeKeypointWorldPositionsBuffer();
            }
        }

        void InitializeKeypointWorldPositionsBuffer()
        {
            if (_keypointWorldPositionsBuffer == null)
            {
                _keypointWorldPositionsBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.Structured,
                    KEYPOINT_COUNT,
                    sizeof(float) * 4 // Vector4: xyz=position, w=score
                );
                _keypointWorldPositionsData = new Vector4[KEYPOINT_COUNT];
                _keypointUVData = new Vector4[KEYPOINT_COUNT];
            }
        }

        void OnDestroy()
        {
            // Release KeypointWorldPositions buffer
            _keypointWorldPositionsBuffer?.Release();
            _keypointWorldPositionsBuffer = null;

            // Release readback texture
            if (_positionMapReadback != null)
            {
                Destroy(_positionMapReadback);
                _positionMapReadback = null;
            }
        }

        void OnEnable()
        {
            // Re-request ColorMap when re-enabled
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
            // Release ColorMap request when disabled
            if (bindColorMap && _arDepthSource != null)
            {
                _arDepthSource.RequestColorMap(false);
            }
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

            // Bind PositionMap for UV→world position sampling
            if (bindPositionMap && _arDepthSource != null && _arDepthSource.PositionMap != null)
            {
                if (component.HasTexture(_positionMapProperty))
                {
                    component.SetTexture(_positionMapProperty, _arDepthSource.PositionMap);
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

            // Bind Threshold (keypoint confidence filter)
            if (bindThreshold && component.HasFloat(_thresholdProperty))
            {
                component.SetFloat(_thresholdProperty, threshold);
            }

            // Log binding ONCE per VFX (not every 120 frames)
            if (verboseLogging && !_loggedVFXIds.Contains(component.GetInstanceID()))
            {
                Debug.Log($"[NNCamKeypointBinder] Bound data to {component.name}");
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

        /// <summary>
        /// Set throttle value at runtime
        /// </summary>
        public void SetThrottle(float value)
        {
            throttle = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Set threshold value at runtime (keypoint confidence filter)
        /// Lower values = more lenient detection (useful for eyes/face keypoints)
        /// </summary>
        public void SetThreshold(float value)
        {
            threshold = Mathf.Clamp01(value);
        }
    }
}
