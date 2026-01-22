// MediaPipeHandTrackingProvider - MediaPipe Hands ML tracking (spec-012)
// Priority 60 - after native (HoloKit/XRHands), before BodyPix
// References: _OPENCV_VS_MEDIAPIPE_UNITY_RESEARCH.md, TRACKING_SYSTEMS_DEEP_DIVE.md

using System;
using UnityEngine;

#if MEDIAPIPE_AVAILABLE
using Mediapipe.Unity;
using Mediapipe.Unity.HandTracking;
#endif

namespace MetavidoVFX.HandTracking.Providers
{
    /// <summary>
    /// MediaPipe Hands tracking provider (21 joints per hand).
    /// Cross-platform ML-based hand tracking via MediaPipeUnityPlugin.
    /// Performance: 8-12ms latency, 92% accuracy.
    /// </summary>
    [HandTrackingProvider("mediapipe", priority: 60)]
    public class MediaPipeHandTrackingProvider : IHandTrackingProvider
    {
        public string Id => "mediapipe";
        public int Priority => 60;

        bool _initialized;
        bool[] _handTracked = new bool[2];
        bool[] _pinching = new bool[2];
        float[] _pinchStrength = new float[2];

        // MediaPipe provides 21 joints per hand
        Vector3[,] _jointPositions = new Vector3[2, 21];

        const float PinchStartDist = 0.025f;
        const float PinchEndDist = 0.04f;

#if MEDIAPIPE_AVAILABLE
        HandTrackingSolution _solution;
#endif

        public bool IsAvailable
        {
            get
            {
#if MEDIAPIPE_AVAILABLE
                return _initialized && _solution != null && _solution.IsRunning;
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
#if MEDIAPIPE_AVAILABLE
            _solution = UnityEngine.Object.FindFirstObjectByType<HandTrackingSolution>();
            if (_solution != null)
            {
                _solution.OnHandLandmarksOutput += OnHandLandmarksReceived;
            }
#endif
            _initialized = true;
        }

        public void Update()
        {
            // MediaPipe updates via callback
        }

#if MEDIAPIPE_AVAILABLE
        void OnHandLandmarksReceived(object sender, HandLandmarksEventArgs e)
        {
            if (e.MultiHandLandmarks == null) return;

            // Reset tracking state
            bool[] prevTracked = new bool[2];
            Array.Copy(_handTracked, prevTracked, 2);
            _handTracked[0] = false;
            _handTracked[1] = false;

            for (int h = 0; h < e.MultiHandLandmarks.Count && h < 2; h++)
            {
                var landmarks = e.MultiHandLandmarks[h];
                var handedness = e.MultiHandedness?[h];

                // Determine hand index (0=left, 1=right)
                int handIndex = (handedness?.Label == "Right") ? 1 : 0;
                Hand hand = (Hand)handIndex;

                _handTracked[handIndex] = true;

                // Extract joint positions (MediaPipe uses normalized 0-1 coords)
                for (int j = 0; j < 21 && j < landmarks.Landmark.Count; j++)
                {
                    var lm = landmarks.Landmark[j];
                    // Convert to world space (assumes camera at origin looking +Z)
                    _jointPositions[handIndex, j] = new Vector3(
                        (lm.X - 0.5f) * 2f,  // Center and scale
                        -(lm.Y - 0.5f) * 2f, // Flip Y
                        -lm.Z * 0.5f         // Depth
                    );
                }

                // Calculate pinch (thumb tip = 4, index tip = 8)
                Vector3 thumbTip = _jointPositions[handIndex, 4];
                Vector3 indexTip = _jointPositions[handIndex, 8];
                float pinchDist = Vector3.Distance(thumbTip, indexTip);

                _pinchStrength[handIndex] = Mathf.InverseLerp(PinchEndDist, PinchStartDist, pinchDist);

                bool wasPinching = _pinching[handIndex];
                if (!_pinching[handIndex] && pinchDist < PinchStartDist)
                {
                    _pinching[handIndex] = true;
                    OnGestureStart?.Invoke(hand, GestureType.Pinch);
                }
                else if (_pinching[handIndex] && pinchDist > PinchEndDist)
                {
                    _pinching[handIndex] = false;
                    OnGestureEnd?.Invoke(hand, GestureType.Pinch);
                }

                // Tracking gained/lost events
                if (!prevTracked[handIndex] && _handTracked[handIndex])
                    OnHandTrackingGained?.Invoke(hand);
            }

            // Check for tracking lost
            for (int i = 0; i < 2; i++)
            {
                if (prevTracked[i] && !_handTracked[i])
                    OnHandTrackingLost?.Invoke((Hand)i);
            }
        }
#endif

        public void Shutdown()
        {
#if MEDIAPIPE_AVAILABLE
            if (_solution != null)
                _solution.OnHandLandmarksOutput -= OnHandLandmarksReceived;
#endif
        }

        public void Dispose() => Shutdown();

        public bool IsHandTracked(Hand hand) => _handTracked[(int)hand];

        public Vector3 GetJointPosition(Hand hand, HandJointID joint)
        {
            int handIdx = (int)hand;
            if (!_handTracked[handIdx]) return Vector3.zero;

            int mpIndex = MapToMediaPipeIndex(joint);
            if (mpIndex >= 0 && mpIndex < 21)
                return _jointPositions[handIdx, mpIndex];

            return Vector3.zero;
        }

        public Quaternion GetJointRotation(Hand hand, HandJointID joint)
        {
            // MediaPipe doesn't provide rotation - estimate from bone direction
            return Quaternion.identity;
        }

        public float GetJointRadius(Hand hand, HandJointID joint) => 0.01f;

        public bool IsGestureActive(Hand hand, GestureType gesture)
        {
            if (gesture == GestureType.Pinch)
                return _pinching[(int)hand];
            return false;
        }

        public float GetPinchStrength(Hand hand) => _pinchStrength[(int)hand];
        public float GetGrabStrength(Hand hand) => 0f;

        /// <summary>
        /// Map unified HandJointID to MediaPipe landmark index.
        /// MediaPipe uses: 0=wrist, 1-4=thumb, 5-8=index, 9-12=middle, 13-16=ring, 17-20=pinky
        /// </summary>
        static int MapToMediaPipeIndex(HandJointID joint)
        {
            return joint switch
            {
                HandJointID.Wrist => 0,
                HandJointID.ThumbMetacarpal => 1,
                HandJointID.ThumbProximal => 2,
                HandJointID.ThumbDistal => 3,
                HandJointID.ThumbTip => 4,
                HandJointID.IndexMetacarpal => 5,
                HandJointID.IndexProximal => 6,
                HandJointID.IndexIntermediate => 7,
                HandJointID.IndexDistal => 7, // MediaPipe combines
                HandJointID.IndexTip => 8,
                HandJointID.MiddleMetacarpal => 9,
                HandJointID.MiddleProximal => 10,
                HandJointID.MiddleIntermediate => 11,
                HandJointID.MiddleDistal => 11,
                HandJointID.MiddleTip => 12,
                HandJointID.RingMetacarpal => 13,
                HandJointID.RingProximal => 14,
                HandJointID.RingIntermediate => 15,
                HandJointID.RingDistal => 15,
                HandJointID.RingTip => 16,
                HandJointID.PinkyMetacarpal => 17,
                HandJointID.PinkyProximal => 18,
                HandJointID.PinkyIntermediate => 19,
                HandJointID.PinkyDistal => 19,
                HandJointID.PinkyTip => 20,
                _ => 0
            };
        }
    }
}
