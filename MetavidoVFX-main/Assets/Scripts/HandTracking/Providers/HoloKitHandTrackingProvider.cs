// HoloKitHandTrackingProvider - Wraps HoloKit SDK hand tracking (spec-012)
// Priority 100 - highest priority when HoloKit is available

using System;
using UnityEngine;

#if HOLOKIT_AVAILABLE
using HoloKit.iOS;
#endif

namespace MetavidoVFX.HandTracking.Providers
{
    /// <summary>
    /// HoloKit SDK hand tracking provider.
    /// Uses ARKit hand tracking via HoloKit native plugin (21 joints).
    /// </summary>
    [HandTrackingProvider("holokit", priority: 100)]
    public class HoloKitHandTrackingProvider : IHandTrackingProvider
    {
        public string Id => "holokit";
        public int Priority => 100;

        bool _initialized;
        bool[] _handTracked = new bool[2];
        bool[] _pinching = new bool[2];
        float[] _pinchStrength = new float[2];

        // Pinch thresholds (meters)
        const float PinchStartDist = 0.025f;
        const float PinchEndDist = 0.04f;

#if HOLOKIT_AVAILABLE
        HandTrackingManager _handManager;
        HandGestureRecognitionManager _gestureManager;
#endif

        public bool IsAvailable
        {
            get
            {
#if HOLOKIT_AVAILABLE && !UNITY_EDITOR
                return _initialized && _handManager != null;
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
#if HOLOKIT_AVAILABLE && !UNITY_EDITOR
            _handManager = UnityEngine.Object.FindFirstObjectByType<HandTrackingManager>();
            _gestureManager = UnityEngine.Object.FindFirstObjectByType<HandGestureRecognitionManager>();

            if (_gestureManager != null)
            {
                _gestureManager.OnHandGestureChanged += HandleGestureChanged;
            }
#endif
            _initialized = true;
        }

        public void Update()
        {
#if HOLOKIT_AVAILABLE && !UNITY_EDITOR
            if (_handManager == null) return;

            UpdateHandState(Hand.Left, 0);
            UpdateHandState(Hand.Right, 1);
#endif
        }

#if HOLOKIT_AVAILABLE && !UNITY_EDITOR
        void UpdateHandState(Hand hand, int handIndex)
        {
            bool wasTracked = _handTracked[handIndex];
            bool isTracked = _handManager.HandCount > handIndex;
            _handTracked[handIndex] = isTracked;

            if (isTracked && !wasTracked)
                OnHandTrackingGained?.Invoke(hand);
            else if (!isTracked && wasTracked)
                OnHandTrackingLost?.Invoke(hand);

            if (!isTracked) return;

            // Calculate pinch strength from thumb-index distance
            var thumbTip = _handManager.GetHandJointPosition(handIndex, JointName.ThumbTip);
            var indexTip = _handManager.GetHandJointPosition(handIndex, JointName.IndexTip);
            float dist = Vector3.Distance(thumbTip, indexTip);

            // Normalize: 0 = fully pinched, 1 = not pinching
            _pinchStrength[handIndex] = 1f - Mathf.InverseLerp(PinchStartDist, PinchEndDist, dist);

            // Pinch state with hysteresis
            bool wasPinching = _pinching[handIndex];
            if (!wasPinching && dist < PinchStartDist)
            {
                _pinching[handIndex] = true;
                OnGestureStart?.Invoke(hand, GestureType.Pinch);
            }
            else if (wasPinching && dist > PinchEndDist)
            {
                _pinching[handIndex] = false;
                OnGestureEnd?.Invoke(hand, GestureType.Pinch);
            }
        }

        void HandleGestureChanged(HandGesture gesture)
        {
            // Map HoloKit gestures to our GestureType
            switch (gesture)
            {
                case HandGesture.Five:
                    OnGestureStart?.Invoke(Hand.Left, GestureType.OpenPalm);
                    break;
            }
        }
#endif

        public void Shutdown()
        {
#if HOLOKIT_AVAILABLE && !UNITY_EDITOR
            if (_gestureManager != null)
            {
                _gestureManager.OnHandGestureChanged -= HandleGestureChanged;
            }
            _handManager = null;
            _gestureManager = null;
#endif
            _initialized = false;
        }

        public bool IsHandTracked(Hand hand) => _handTracked[(int)hand];

        public Vector3 GetJointPosition(Hand hand, HandJointID joint)
        {
#if HOLOKIT_AVAILABLE && !UNITY_EDITOR
            if (_handManager == null || !_handTracked[(int)hand])
                return Vector3.zero;

            var holoKitJoint = MapToHoloKitJoint(joint);
            return _handManager.GetHandJointPosition((int)hand, holoKitJoint);
#else
            return Vector3.zero;
#endif
        }

        public Quaternion GetJointRotation(Hand hand, HandJointID joint)
        {
            // HoloKit doesn't provide joint rotations directly
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
        public float GetGrabStrength(Hand hand) => 0f; // Not implemented

        public void Dispose() => Shutdown();

#if HOLOKIT_AVAILABLE
        JointName MapToHoloKitJoint(HandJointID joint)
        {
            return joint switch
            {
                HandJointID.Wrist => JointName.Wrist,
                HandJointID.ThumbTip => JointName.ThumbTip,
                HandJointID.ThumbDistal => JointName.ThumbIP,
                HandJointID.ThumbProximal => JointName.ThumbMP,
                HandJointID.ThumbMetacarpal => JointName.ThumbCMC,
                HandJointID.IndexTip => JointName.IndexTip,
                HandJointID.IndexDistal => JointName.IndexDIP,
                HandJointID.IndexIntermediate => JointName.IndexPIP,
                HandJointID.IndexProximal => JointName.IndexMCP,
                HandJointID.MiddleTip => JointName.MiddleTip,
                HandJointID.MiddleDistal => JointName.MiddleDIP,
                HandJointID.MiddleIntermediate => JointName.MiddlePIP,
                HandJointID.MiddleProximal => JointName.MiddleMCP,
                HandJointID.RingTip => JointName.RingTip,
                HandJointID.RingDistal => JointName.RingDIP,
                HandJointID.RingIntermediate => JointName.RingPIP,
                HandJointID.RingProximal => JointName.RingMCP,
                HandJointID.PinkyTip => JointName.LittleTip,
                HandJointID.PinkyDistal => JointName.LittleDIP,
                HandJointID.PinkyIntermediate => JointName.LittlePIP,
                HandJointID.PinkyProximal => JointName.LittleMCP,
                _ => JointName.Wrist
            };
        }
#endif
    }
}
