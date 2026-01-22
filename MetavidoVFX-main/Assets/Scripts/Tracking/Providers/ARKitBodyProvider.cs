// ARKitBodyProvider - Native 91-joint body tracking via AR Foundation (spec-008)
// PRIORITY: P0 - Use native tracking BEFORE third-party ML inference
// iOS Only: Requires A12+ chip, iOS 13+, rear camera
// ~2ms latency, significantly enhanced by LiDAR

using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MetavidoVFX.Tracking.Providers
{
    /// <summary>
    /// ARKit native body tracking provider (91 joints).
    /// Uses ARHumanBodyManager for world-space 3D skeleton.
    ///
    /// Key advantages over ML-based tracking:
    /// - ~2ms vs ~5-15ms inference time
    /// - Better depth accuracy with LiDAR
    /// - No GPU inference load
    /// - 91 joints vs 17 (BodyPix) or 33 (MediaPipe)
    ///
    /// Limitations:
    /// - iOS only (A12+ chip required)
    /// - Rear camera only
    /// - Single person only
    /// </summary>
    [TrackingProvider("arkit-body", priority: 90)]
    public class ARKitBodyProvider : ITrackingProvider
    {
        public string Id => "arkit-body";
        public int Priority => 90;  // High priority - native tracking
        public Platform SupportedPlatforms => Platform.iOS | Platform.VisionPro;
        public TrackingCap Capabilities => TrackingCap.Body | TrackingCap.Keypoints;

        private ARHumanBodyManager _bodyManager;
        private BodyPoseData _cachedData;
        private bool _initialized;
        private bool _wasTracking;

        // 91 joint positions (ARKit full skeleton)
        private Vector3[] _jointPositions = new Vector3[91];
        private float[] _jointConfidences = new float[91];

        public bool IsAvailable
        {
            get
            {
#if UNITY_IOS || UNITY_EDITOR
                if (!_initialized) return false;
                if (_bodyManager == null) return false;

                // Check subsystem availability
                var subsystem = _bodyManager.subsystem;
                return subsystem != null && subsystem.running;
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
            _bodyManager = UnityEngine.Object.FindAnyObjectByType<ARHumanBodyManager>();

            if (_bodyManager == null)
            {
                Debug.LogWarning("[ARKitBodyProvider] ARHumanBodyManager not found in scene. " +
                    "Add it to enable native body tracking.");
            }
            else
            {
                _bodyManager.humanBodiesChanged += OnBodiesChanged;
                Debug.Log("[ARKitBodyProvider] Initialized - 91-joint native tracking enabled");
            }

            _initialized = true;
        }

        public void Update()
        {
            if (!IsAvailable) return;

            // Data is updated via event callback
            // Check for tracking state changes
            bool isTracking = _cachedData.IsTracked;

            if (isTracking && !_wasTracking)
            {
                OnTrackingFound?.Invoke();
            }
            else if (!isTracking && _wasTracking)
            {
                OnTrackingLost?.Invoke();
            }

            _wasTracking = isTracking;
        }

        private void OnBodiesChanged(ARHumanBodiesChangedEventArgs args)
        {
            // Process first detected body (single person only)
            if (args.added.Count > 0)
            {
                ProcessBody(args.added[0]);
            }
            else if (args.updated.Count > 0)
            {
                ProcessBody(args.updated[0]);
            }
            else if (args.removed.Count > 0)
            {
                _cachedData.IsTracked = false;
            }
        }

        private void ProcessBody(ARHumanBody body)
        {
            if (body == null || body.joints == null) return;

            var joints = body.joints;
            int jointCount = Mathf.Min(joints.Length, 91);

            // Convert XRHumanBodyJoint to Vector3 positions
            for (int i = 0; i < jointCount; i++)
            {
                var joint = joints[i];

                // Transform to world space
                _jointPositions[i] = body.transform.TransformPoint(joint.localPose.position);

                // Convert tracked state to confidence (1.0 if tracked, 0.0 if not)
                _jointConfidences[i] = joint.tracked ? 1f : 0f;
            }

            // Map to standard 17-keypoint format for compatibility
            _cachedData.Keypoints = MapTo17Keypoints(_jointPositions, jointCount);
            _cachedData.Confidences = MapTo17Confidences(_jointConfidences, jointCount);
            _cachedData.IsTracked = true;
            _cachedData.Timestamp = Time.time;
        }

        /// <summary>
        /// Maps 91 ARKit joints to COCO 17-keypoint format for VFX compatibility.
        /// </summary>
        private Vector3[] MapTo17Keypoints(Vector3[] joints, int count)
        {
            var keypoints = new Vector3[17];

            // ARKit joint indices (approximate mapping to COCO)
            // See: https://developer.apple.com/documentation/arkit/arskeletonjointnamehead
            if (count >= 91)
            {
                keypoints[BodyPoseData.Nose] = joints[51];           // head_joint → nose
                keypoints[BodyPoseData.LeftEye] = joints[52];        // left_eye_joint
                keypoints[BodyPoseData.RightEye] = joints[58];       // right_eye_joint
                keypoints[BodyPoseData.LeftEar] = joints[53];        // left_eyeball_joint (approx)
                keypoints[BodyPoseData.RightEar] = joints[59];       // right_eyeball_joint (approx)
                keypoints[BodyPoseData.LeftShoulder] = joints[11];   // left_shoulder_1_joint
                keypoints[BodyPoseData.RightShoulder] = joints[63];  // right_shoulder_1_joint
                keypoints[BodyPoseData.LeftElbow] = joints[12];      // left_arm_joint
                keypoints[BodyPoseData.RightElbow] = joints[64];     // right_arm_joint
                keypoints[BodyPoseData.LeftWrist] = joints[13];      // left_forearm_joint
                keypoints[BodyPoseData.RightWrist] = joints[65];     // right_forearm_joint
                keypoints[BodyPoseData.LeftHip] = joints[2];         // left_upLeg_joint
                keypoints[BodyPoseData.RightHip] = joints[7];        // right_upLeg_joint
                keypoints[BodyPoseData.LeftKnee] = joints[3];        // left_leg_joint
                keypoints[BodyPoseData.RightKnee] = joints[8];       // right_leg_joint
                keypoints[BodyPoseData.LeftAnkle] = joints[4];       // left_foot_joint
                keypoints[BodyPoseData.RightAnkle] = joints[9];      // right_foot_joint
            }

            return keypoints;
        }

        private float[] MapTo17Confidences(float[] confidences, int count)
        {
            var result = new float[17];

            if (count >= 91)
            {
                result[BodyPoseData.Nose] = confidences[51];
                result[BodyPoseData.LeftEye] = confidences[52];
                result[BodyPoseData.RightEye] = confidences[58];
                result[BodyPoseData.LeftEar] = confidences[53];
                result[BodyPoseData.RightEar] = confidences[59];
                result[BodyPoseData.LeftShoulder] = confidences[11];
                result[BodyPoseData.RightShoulder] = confidences[63];
                result[BodyPoseData.LeftElbow] = confidences[12];
                result[BodyPoseData.RightElbow] = confidences[64];
                result[BodyPoseData.LeftWrist] = confidences[13];
                result[BodyPoseData.RightWrist] = confidences[65];
                result[BodyPoseData.LeftHip] = confidences[2];
                result[BodyPoseData.RightHip] = confidences[7];
                result[BodyPoseData.LeftKnee] = confidences[3];
                result[BodyPoseData.RightKnee] = confidences[8];
                result[BodyPoseData.LeftAnkle] = confidences[4];
                result[BodyPoseData.RightAnkle] = confidences[9];
            }

            return result;
        }

        public void Shutdown()
        {
            if (_bodyManager != null)
            {
                _bodyManager.humanBodiesChanged -= OnBodiesChanged;
            }
            _initialized = false;
        }

        public bool TryGetData<T>(out T data) where T : struct, ITrackingData
        {
            if (typeof(T) == typeof(BodyPoseData) && _cachedData.IsTracked)
            {
                data = (T)(object)_cachedData;
                return true;
            }
            data = default;
            return false;
        }

        /// <summary>
        /// Get raw 91-joint skeleton data (ARKit-specific).
        /// </summary>
        public Vector3[] GetFullSkeleton()
        {
            return _jointPositions;
        }

        /// <summary>
        /// Get confidence values for all 91 joints.
        /// </summary>
        public float[] GetFullConfidences()
        {
            return _jointConfidences;
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
