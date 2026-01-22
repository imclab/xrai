// XRHandsTrackingProvider - Wraps AR Foundation XR Hands (spec-012)
// Priority 80 - fallback when HoloKit unavailable

using System;
using UnityEngine;

#if UNITY_XR_HANDS
using UnityEngine.XR.Hands;
#endif

namespace MetavidoVFX.HandTracking.Providers
{
    /// <summary>
    /// XR Hands subsystem provider (26 joints).
    /// Cross-platform hand tracking via AR Foundation.
    /// </summary>
    [HandTrackingProvider("xrhands", priority: 80)]
    public class XRHandsTrackingProvider : IHandTrackingProvider
    {
        public string Id => "xrhands";
        public int Priority => 80;

        bool _initialized;
        bool[] _handTracked = new bool[2];
        bool[] _pinching = new bool[2];
        float[] _pinchStrength = new float[2];
        Vector3[] _wristPositions = new Vector3[2];
        Vector3[] _thumbTipPositions = new Vector3[2];
        Vector3[] _indexTipPositions = new Vector3[2];

        const float PinchStartDist = 0.025f;
        const float PinchEndDist = 0.04f;

#if UNITY_XR_HANDS
        XRHandSubsystem _subsystem;
#endif

        public bool IsAvailable
        {
            get
            {
#if UNITY_XR_HANDS
                return _initialized && _subsystem != null && _subsystem.running;
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
#if UNITY_XR_HANDS
            var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
            {
                _subsystem = subsystems[0];
                _subsystem.updatedHands += OnHandsUpdated;
            }
#endif
            _initialized = true;
        }

        public void Update()
        {
#if UNITY_XR_HANDS
            if (_subsystem == null) return;
            // Hand updates happen via callback
#endif
        }

#if UNITY_XR_HANDS
        void OnHandsUpdated(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
        {
            ProcessHand(subsystem.leftHand, Hand.Left, 0);
            ProcessHand(subsystem.rightHand, Hand.Right, 1);
        }

        void ProcessHand(XRHand xrHand, Hand hand, int index)
        {
            bool wasTracked = _handTracked[index];
            _handTracked[index] = xrHand.isTracked;

            if (!wasTracked && _handTracked[index])
                OnHandTrackingGained?.Invoke(hand);
            else if (wasTracked && !_handTracked[index])
                OnHandTrackingLost?.Invoke(hand);

            if (!xrHand.isTracked) return;

            // Extract key joints
            if (xrHand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wristPose))
                _wristPositions[index] = wristPose.position;

            if (xrHand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose))
                _thumbTipPositions[index] = thumbPose.position;

            if (xrHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
                _indexTipPositions[index] = indexPose.position;

            // Pinch detection
            float pinchDist = Vector3.Distance(_thumbTipPositions[index], _indexTipPositions[index]);
            _pinchStrength[index] = Mathf.InverseLerp(PinchEndDist, PinchStartDist, pinchDist);

            bool wasPinching = _pinching[index];
            if (!_pinching[index] && pinchDist < PinchStartDist)
            {
                _pinching[index] = true;
                OnGestureStart?.Invoke(hand, GestureType.Pinch);
            }
            else if (_pinching[index] && pinchDist > PinchEndDist)
            {
                _pinching[index] = false;
                OnGestureEnd?.Invoke(hand, GestureType.Pinch);
            }
        }
#endif

        public void Shutdown()
        {
#if UNITY_XR_HANDS
            if (_subsystem != null)
                _subsystem.updatedHands -= OnHandsUpdated;
#endif
        }

        public void Dispose() => Shutdown();

        public bool IsHandTracked(Hand hand) => _handTracked[(int)hand];

        public Vector3 GetJointPosition(Hand hand, HandJointID joint)
        {
#if UNITY_XR_HANDS
            if (!IsAvailable || !_handTracked[(int)hand]) return Vector3.zero;

            var xrHand = (int)hand == 0 ? _subsystem.leftHand : _subsystem.rightHand;
            var xrJointId = MapToXRHandJointID(joint);
            if (xrHand.GetJoint(xrJointId).TryGetPose(out Pose pose))
                return pose.position;
#endif
            return Vector3.zero;
        }

        public Quaternion GetJointRotation(Hand hand, HandJointID joint)
        {
#if UNITY_XR_HANDS
            if (!IsAvailable || !_handTracked[(int)hand]) return Quaternion.identity;

            var xrHand = (int)hand == 0 ? _subsystem.leftHand : _subsystem.rightHand;
            var xrJointId = MapToXRHandJointID(joint);
            if (xrHand.GetJoint(xrJointId).TryGetPose(out Pose pose))
                return pose.rotation;
#endif
            return Quaternion.identity;
        }

        public float GetJointRadius(Hand hand, HandJointID joint)
        {
#if UNITY_XR_HANDS
            if (!IsAvailable || !_handTracked[(int)hand]) return 0f;

            var xrHand = (int)hand == 0 ? _subsystem.leftHand : _subsystem.rightHand;
            var xrJointId = MapToXRHandJointID(joint);
            if (xrHand.GetJoint(xrJointId).TryGetRadius(out float radius))
                return radius;
#endif
            return 0.01f;
        }

        public bool IsGestureActive(Hand hand, GestureType gesture)
        {
            if (gesture == GestureType.Pinch)
                return _pinching[(int)hand];
            return false;
        }

        public float GetPinchStrength(Hand hand) => _pinchStrength[(int)hand];
        public float GetGrabStrength(Hand hand) => 0f;

#if UNITY_XR_HANDS
        static XRHandJointID MapToXRHandJointID(HandJointID joint)
        {
            return joint switch
            {
                HandJointID.Wrist => XRHandJointID.Wrist,
                HandJointID.Palm => XRHandJointID.Palm,
                HandJointID.ThumbMetacarpal => XRHandJointID.ThumbMetacarpal,
                HandJointID.ThumbProximal => XRHandJointID.ThumbProximal,
                HandJointID.ThumbDistal => XRHandJointID.ThumbDistal,
                HandJointID.ThumbTip => XRHandJointID.ThumbTip,
                HandJointID.IndexMetacarpal => XRHandJointID.IndexMetacarpal,
                HandJointID.IndexProximal => XRHandJointID.IndexProximal,
                HandJointID.IndexIntermediate => XRHandJointID.IndexIntermediate,
                HandJointID.IndexDistal => XRHandJointID.IndexDistal,
                HandJointID.IndexTip => XRHandJointID.IndexTip,
                HandJointID.MiddleMetacarpal => XRHandJointID.MiddleMetacarpal,
                HandJointID.MiddleProximal => XRHandJointID.MiddleProximal,
                HandJointID.MiddleIntermediate => XRHandJointID.MiddleIntermediate,
                HandJointID.MiddleDistal => XRHandJointID.MiddleDistal,
                HandJointID.MiddleTip => XRHandJointID.MiddleTip,
                HandJointID.RingMetacarpal => XRHandJointID.RingMetacarpal,
                HandJointID.RingProximal => XRHandJointID.RingProximal,
                HandJointID.RingIntermediate => XRHandJointID.RingIntermediate,
                HandJointID.RingDistal => XRHandJointID.RingDistal,
                HandJointID.RingTip => XRHandJointID.RingTip,
                HandJointID.PinkyMetacarpal => XRHandJointID.LittleMetacarpal,
                HandJointID.PinkyProximal => XRHandJointID.LittleProximal,
                HandJointID.PinkyIntermediate => XRHandJointID.LittleIntermediate,
                HandJointID.PinkyDistal => XRHandJointID.LittleDistal,
                HandJointID.PinkyTip => XRHandJointID.LittleTip,
                _ => XRHandJointID.Wrist
            };
        }
#endif
    }
}
