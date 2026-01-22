// SentisTrackingProvider - 24-part body segmentation via BodyPixSentis (spec-008)
// PRIORITY: P0 for segmentation ONLY - Use ARFoundation for pose/hands/face
// Provides: 24-part body segmentation (beyond binary stencil)
// ~5ms inference on iOS, works with any camera

using System;
using UnityEngine;

#if BODYPIX_AVAILABLE
using MetavidoVFX.Segmentation;
#endif

namespace XRRAI.ARTracking
{
    /// <summary>
    /// BodyPixSentis tracking provider for 24-part body segmentation.
    ///
    /// KEY INSIGHT: Only use this for segmentation. For body pose (17kp), prefer:
    /// - ARKitBodyProvider (91 joints, native, ~2ms)
    /// - ARKitPoseProvider (17 joints via Vision, ~3ms)
    ///
    /// 24-part segmentation enables:
    /// - Face-only VFX
    /// - Hands-only VFX
    /// - Torso-specific effects
    /// - Limb-based particles
    ///
    /// Part indices (0-23):
    /// 0-1: Face (L/R)
    /// 2-9: Arms (upper/lower, front/back)
    /// 10-11: Hands
    /// 12-13: Torso (front/back)
    /// 14-21: Legs (upper/lower, front/back)
    /// 22-23: Feet
    /// 255: Background
    /// </summary>
    [TrackingProvider("sentis-bodypix", priority: 50)]
    public class SentisTrackingProvider : ITrackingProvider
    {
        public string Id => "sentis-bodypix";
        public int Priority => 50;  // Lower than native tracking
        public Platform SupportedPlatforms => Platform.iOS | Platform.Android | Platform.Editor;
        public TrackingCap Capabilities => TrackingCap.Segmentation | TrackingCap.Keypoints;

#if BODYPIX_AVAILABLE
        private BodyPartSegmenter _segmenter;
#endif
        private SegmentationData _cachedData;
        private BodyPoseData _cachedPoseData;
        private bool _initialized;
        private bool _wasTracking;

        public bool IsAvailable
        {
            get
            {
#if BODYPIX_AVAILABLE
                return _initialized && _segmenter != null && _segmenter.IsReady;
#else
                return false;
#endif
            }
        }

        public event Action<TrackingCap> OnCapabilitiesChanged;
        public event Action OnTrackingLost;
        public event Action OnTrackingFound;

        public void Initialize()
        {
#if BODYPIX_AVAILABLE
            _segmenter = UnityEngine.Object.FindAnyObjectByType<BodyPartSegmenter>();

            if (_segmenter == null)
            {
                Debug.LogWarning("[SentisTrackingProvider] BodyPartSegmenter not found in scene. " +
                    "Add it to enable 24-part body segmentation.");
            }
            else
            {
                Debug.Log("[SentisTrackingProvider] Initialized - 24-part segmentation enabled");
            }
#else
            Debug.LogWarning("[SentisTrackingProvider] BODYPIX_AVAILABLE not defined. " +
                "Run H3M > Body Segmentation > Setup BodyPix Defines");
#endif
            _initialized = true;
        }

        public void Update()
        {
            if (!IsAvailable) return;

#if BODYPIX_AVAILABLE
            // Update segmentation data
            bool isValid = _segmenter.MaskTexture != null;

            if (isValid)
            {
                _cachedData.MaskTexture = _segmenter.MaskTexture;
                _cachedData.KeypointBuffer = _segmenter.KeypointBuffer;
                _cachedData.PartCount = 24;
                _cachedData.IsValid = true;
                _cachedData.Timestamp = Time.time;

                // Also update pose data from keypoints
                UpdatePoseFromKeypoints();

                if (!_wasTracking)
                {
                    OnTrackingFound?.Invoke();
                }
            }
            else if (_wasTracking)
            {
                _cachedData.IsValid = false;
                OnTrackingLost?.Invoke();
            }

            _wasTracking = isValid;
#endif
        }

#if BODYPIX_AVAILABLE
        private void UpdatePoseFromKeypoints()
        {
            // Get 17 keypoints from BodyPartSegmenter
            var keypoints = new Vector3[17];
            var confidences = new float[17];

            for (int i = 0; i < 17; i++)
            {
                keypoints[i] = _segmenter.GetKeypointPosition(i);
                confidences[i] = _segmenter.GetKeypointScore(i);
            }

            _cachedPoseData.Keypoints = keypoints;
            _cachedPoseData.Confidences = confidences;
            _cachedPoseData.IsTracked = confidences[0] > 0.3f; // Nose confidence threshold
            _cachedPoseData.Timestamp = Time.time;
        }
#endif

        public void Shutdown()
        {
            _initialized = false;
        }

        public bool TryGetData<T>(out T data) where T : struct, ITrackingData
        {
            if (typeof(T) == typeof(SegmentationData) && _cachedData.IsValid)
            {
                data = (T)(object)_cachedData;
                return true;
            }

            if (typeof(T) == typeof(BodyPoseData) && _cachedPoseData.IsTracked)
            {
                data = (T)(object)_cachedPoseData;
                return true;
            }

            data = default;
            return false;
        }

        /// <summary>
        /// Get the raw mask texture for direct VFX binding.
        /// R channel contains part index (0-23, 255 = background).
        /// </summary>
        public Texture GetMaskTexture()
        {
            return _cachedData.MaskTexture;
        }

        /// <summary>
        /// Get the keypoint buffer for GPU-based VFX.
        /// Contains 17 keypoint positions as float3.
        /// </summary>
        public GraphicsBuffer GetKeypointBuffer()
        {
            return _cachedData.KeypointBuffer;
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
