// BodyPixHandTrackingProvider - Last ML fallback using BodyPix wrist keypoints (spec-012)
// Priority 40 - lowest ML fallback (wrist-only, no finger tracking)
// Reference: TRACKING_SYSTEMS_DEEP_DIVE.md - BodyPix is for segmentation, not hand tracking

using System;
using UnityEngine;
using MetavidoVFX.Segmentation;

namespace MetavidoVFX.HandTracking.Providers
{
    /// <summary>
    /// BodyPix-based hand tracking fallback.
    /// Uses wrist keypoints from body segmentation (limited accuracy).
    /// </summary>
    [HandTrackingProvider("bodypix", priority: 40)]
    public class BodyPixHandTrackingProvider : IHandTrackingProvider
    {
        public string Id => "bodypix";
        public int Priority => 40;

        bool _initialized;
        bool[] _handTracked = new bool[2];
        Vector3[] _wristPositions = new Vector3[2];
        float _minConfidence = 0.5f;

#if BODYPIX_AVAILABLE
        BodyPartSegmenter _segmenter;
#endif

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

        public event Action<Hand> OnHandTrackingGained;
        public event Action<Hand> OnHandTrackingLost;
        public event Action<Hand, GestureType> OnGestureStart;
        public event Action<Hand, GestureType> OnGestureEnd;

        public void Initialize()
        {
#if BODYPIX_AVAILABLE
            _segmenter = UnityEngine.Object.FindFirstObjectByType<BodyPartSegmenter>();
#endif
            _initialized = true;
        }

        public void Update()
        {
#if BODYPIX_AVAILABLE
            if (_segmenter == null || !_segmenter.IsReady) return;

            // BodyPix keypoints: 9 = Left Wrist, 10 = Right Wrist
            UpdateWrist(Hand.Left, 0, 9);  // Left wrist keypoint
            UpdateWrist(Hand.Right, 1, 10); // Right wrist keypoint
#endif
        }

#if BODYPIX_AVAILABLE
        void UpdateWrist(Hand hand, int index, int keypointIndex)
        {
            bool wasTracked = _handTracked[index];
            float score = _segmenter.GetKeypointScore(keypointIndex);

            _handTracked[index] = score >= _minConfidence;

            if (!wasTracked && _handTracked[index])
                OnHandTrackingGained?.Invoke(hand);
            else if (wasTracked && !_handTracked[index])
                OnHandTrackingLost?.Invoke(hand);

            if (_handTracked[index])
                _wristPositions[index] = _segmenter.GetKeypointPosition(keypointIndex);
        }
#endif

        public void Shutdown() { }
        public void Dispose() { }

        public bool IsHandTracked(Hand hand) => _handTracked[(int)hand];

        public Vector3 GetJointPosition(Hand hand, HandJointID joint)
        {
            // BodyPix only provides wrist - return wrist for all queries
            return _wristPositions[(int)hand];
        }

        public Quaternion GetJointRotation(Hand hand, HandJointID joint)
        {
            // No rotation data from BodyPix
            return Quaternion.identity;
        }

        public float GetJointRadius(Hand hand, HandJointID joint) => 0.02f;

        // No gesture detection from BodyPix wrist-only tracking
        public bool IsGestureActive(Hand hand, GestureType gesture) => false;
        public float GetPinchStrength(Hand hand) => 0f;
        public float GetGrabStrength(Hand hand) => 0f;

        public void SetMinConfidence(float confidence) => _minConfidence = confidence;
    }
}
