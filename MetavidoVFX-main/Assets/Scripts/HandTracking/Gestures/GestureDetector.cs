// GestureDetector - Gesture detection with hysteresis and hold tracking (spec-012 T4.1-T4.3)
// Reusable helper for IHandTrackingProvider implementations

using System;
using UnityEngine;

namespace MetavidoVFX.HandTracking.Gestures
{
    /// <summary>
    /// Detects gestures from hand joint data with configurable thresholds and hysteresis.
    /// Use one instance per hand for proper state tracking.
    /// </summary>
    [Serializable]
    public class GestureDetector
    {
        #region Configuration

        [Header("Pinch Detection")]
        [Tooltip("Distance (m) at which pinch gesture starts")]
        [SerializeField] private float _pinchStartThreshold = 0.02f;

        [Tooltip("Distance (m) at which pinch gesture ends (should be > start for hysteresis)")]
        [SerializeField] private float _pinchEndThreshold = 0.04f;

        [Header("Grab Detection")]
        [Tooltip("Angle (degrees) at which finger is considered curled")]
        [SerializeField] private float _fingerCurlAngle = 60f;

        [Tooltip("Number of fingers that must be curled for grab (excluding thumb)")]
        [SerializeField] private int _grabMinCurledFingers = 4;

        [Header("General")]
        [Tooltip("Minimum time (s) a gesture must be held before triggering OnGestureHold")]
        [SerializeField] private float _holdDuration = 0.5f;

        #endregion

        #region State

        private Hand _hand;
        private bool _isPinching;
        private bool _isGrabbing;
        private bool _isPointing;
        private bool _isOpenPalm;
        private bool _isThumbsUp;

        private float _pinchStartTime;
        private float _grabStartTime;
        private float _pointStartTime;
        private float _openPalmStartTime;
        private float _thumbsUpStartTime;

        private float _currentPinchStrength;
        private float _currentGrabStrength;

        #endregion

        #region Events

        /// <summary>Fired when a gesture starts.</summary>
        public event Action<Hand, GestureType> OnGestureStart;

        /// <summary>Fired when a gesture ends.</summary>
        public event Action<Hand, GestureType> OnGestureEnd;

        /// <summary>Fired continuously while gesture is held (with duration).</summary>
        public event Action<Hand, GestureType, float> OnGestureHold;

        #endregion

        #region Public Properties

        public float PinchStartThreshold
        {
            get => _pinchStartThreshold;
            set => _pinchStartThreshold = Mathf.Max(0.001f, value);
        }

        public float PinchEndThreshold
        {
            get => _pinchEndThreshold;
            set => _pinchEndThreshold = Mathf.Max(_pinchStartThreshold + 0.01f, value);
        }

        public float FingerCurlAngle
        {
            get => _fingerCurlAngle;
            set => _fingerCurlAngle = Mathf.Clamp(value, 10f, 120f);
        }

        public float HoldDuration
        {
            get => _holdDuration;
            set => _holdDuration = Mathf.Max(0.1f, value);
        }

        public float CurrentPinchStrength => _currentPinchStrength;
        public float CurrentGrabStrength => _currentGrabStrength;

        public bool IsPinching => _isPinching;
        public bool IsGrabbing => _isGrabbing;
        public bool IsPointing => _isPointing;
        public bool IsOpenPalm => _isOpenPalm;
        public bool IsThumbsUp => _isThumbsUp;

        #endregion

        #region Constructor

        public GestureDetector(Hand hand)
        {
            _hand = hand;
        }

        /// <summary>
        /// Creates a detector with custom thresholds.
        /// </summary>
        public GestureDetector(Hand hand, float pinchStart, float pinchEnd, float curlAngle = 60f)
        {
            _hand = hand;
            _pinchStartThreshold = pinchStart;
            _pinchEndThreshold = Mathf.Max(pinchStart + 0.01f, pinchEnd);
            _fingerCurlAngle = curlAngle;
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Update gesture detection using an IHandTrackingProvider.
        /// Call this in your provider's Update() method.
        /// </summary>
        public void UpdateFromProvider(IHandTrackingProvider provider)
        {
            if (provider == null || !provider.IsHandTracked(_hand))
            {
                ResetAllGestures();
                return;
            }

            // Get joint positions
            Vector3 thumbTip = provider.GetJointPosition(_hand, HandJointID.ThumbTip);
            Vector3 indexTip = provider.GetJointPosition(_hand, HandJointID.IndexTip);
            Vector3 middleTip = provider.GetJointPosition(_hand, HandJointID.MiddleTip);
            Vector3 ringTip = provider.GetJointPosition(_hand, HandJointID.RingTip);
            Vector3 pinkyTip = provider.GetJointPosition(_hand, HandJointID.PinkyTip);

            Vector3 indexBase = provider.GetJointPosition(_hand, HandJointID.IndexProximal);
            Vector3 middleBase = provider.GetJointPosition(_hand, HandJointID.MiddleProximal);
            Vector3 ringBase = provider.GetJointPosition(_hand, HandJointID.RingProximal);
            Vector3 pinkyBase = provider.GetJointPosition(_hand, HandJointID.PinkyProximal);

            Vector3 wrist = provider.GetJointPosition(_hand, HandJointID.Wrist);

            // Update pinch
            float pinchDist = Vector3.Distance(thumbTip, indexTip);
            UpdatePinch(pinchDist);

            // Calculate finger curl states
            bool indexCurled = IsFingerCurled(indexBase, indexTip, wrist);
            bool middleCurled = IsFingerCurled(middleBase, middleTip, wrist);
            bool ringCurled = IsFingerCurled(ringBase, ringTip, wrist);
            bool pinkyCurled = IsFingerCurled(pinkyBase, pinkyTip, wrist);

            // Check thumb extended
            Vector3 thumbBase = provider.GetJointPosition(_hand, HandJointID.ThumbProximal);
            bool thumbExtended = !IsFingerCurled(thumbBase, thumbTip, wrist);

            // Update grab (all fingers curled)
            int curledCount = (indexCurled ? 1 : 0) + (middleCurled ? 1 : 0) +
                             (ringCurled ? 1 : 0) + (pinkyCurled ? 1 : 0);
            UpdateGrab(curledCount);

            // Update point (index extended, others curled)
            bool indexExtended = !indexCurled;
            UpdatePoint(indexExtended && middleCurled && ringCurled && pinkyCurled);

            // Update open palm (all fingers extended)
            bool allExtended = !indexCurled && !middleCurled && !ringCurled && !pinkyCurled;
            UpdateOpenPalm(allExtended);

            // Update thumbs up (thumb extended, all others curled)
            UpdateThumbsUp(thumbExtended && curledCount >= _grabMinCurledFingers);

            // Fire hold events
            float time = Time.time;
            if (_isPinching && (time - _pinchStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.Pinch, time - _pinchStartTime);
            if (_isGrabbing && (time - _grabStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.Grab, time - _grabStartTime);
            if (_isPointing && (time - _pointStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.Point, time - _pointStartTime);
            if (_isOpenPalm && (time - _openPalmStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.OpenPalm, time - _openPalmStartTime);
            if (_isThumbsUp && (time - _thumbsUpStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.ThumbsUp, time - _thumbsUpStartTime);
        }

        /// <summary>
        /// Update gesture detection from raw joint positions.
        /// Use when you have direct access to joint data.
        /// </summary>
        public void UpdateFromJoints(
            Vector3 thumbTip, Vector3 indexTip, Vector3 middleTip, Vector3 ringTip, Vector3 pinkyTip,
            Vector3 thumbBase, Vector3 indexBase, Vector3 middleBase, Vector3 ringBase, Vector3 pinkyBase,
            Vector3 wrist)
        {
            // Update pinch
            float pinchDist = Vector3.Distance(thumbTip, indexTip);
            UpdatePinch(pinchDist);

            // Calculate finger curl states
            bool indexCurled = IsFingerCurled(indexBase, indexTip, wrist);
            bool middleCurled = IsFingerCurled(middleBase, middleTip, wrist);
            bool ringCurled = IsFingerCurled(ringBase, ringTip, wrist);
            bool pinkyCurled = IsFingerCurled(pinkyBase, pinkyTip, wrist);
            bool thumbExtended = !IsFingerCurled(thumbBase, thumbTip, wrist);

            // Update grab
            int curledCount = (indexCurled ? 1 : 0) + (middleCurled ? 1 : 0) +
                             (ringCurled ? 1 : 0) + (pinkyCurled ? 1 : 0);
            UpdateGrab(curledCount);

            // Update point
            bool indexExtended = !indexCurled;
            UpdatePoint(indexExtended && middleCurled && ringCurled && pinkyCurled);

            // Update open palm
            bool allExtended = !indexCurled && !middleCurled && !ringCurled && !pinkyCurled;
            UpdateOpenPalm(allExtended);

            // Update thumbs up
            UpdateThumbsUp(thumbExtended && curledCount >= _grabMinCurledFingers);

            // Fire hold events
            FireHoldEvents();
        }

        /// <summary>
        /// Simplified update using just pinch distance and grab strength.
        /// Use for providers that don't have full joint data.
        /// </summary>
        public void UpdateSimple(float pinchDistance, float grabStrength)
        {
            UpdatePinch(pinchDistance);

            // Convert grab strength (0-1) to curled finger count
            int curledCount = Mathf.RoundToInt(grabStrength * 4f);
            UpdateGrab(curledCount);

            FireHoldEvents();
        }

        #endregion

        #region Private Methods

        private void UpdatePinch(float distance)
        {
            // Convert distance to strength (1 = fully pinched, 0 = not pinched)
            _currentPinchStrength = 1f - Mathf.InverseLerp(_pinchStartThreshold, _pinchEndThreshold * 2f, distance);

            bool wasPinching = _isPinching;

            // Hysteresis: different thresholds for start vs end
            if (!_isPinching && distance <= _pinchStartThreshold)
            {
                _isPinching = true;
                _pinchStartTime = Time.time;
            }
            else if (_isPinching && distance >= _pinchEndThreshold)
            {
                _isPinching = false;
            }

            // Fire events
            if (_isPinching && !wasPinching)
                OnGestureStart?.Invoke(_hand, GestureType.Pinch);
            else if (!_isPinching && wasPinching)
                OnGestureEnd?.Invoke(_hand, GestureType.Pinch);
        }

        private void UpdateGrab(int curledFingerCount)
        {
            _currentGrabStrength = curledFingerCount / 4f;

            bool wasGrabbing = _isGrabbing;
            bool shouldGrab = curledFingerCount >= _grabMinCurledFingers;

            if (shouldGrab && !_isGrabbing)
            {
                _isGrabbing = true;
                _grabStartTime = Time.time;
            }
            else if (!shouldGrab && _isGrabbing)
            {
                _isGrabbing = false;
            }

            if (_isGrabbing && !wasGrabbing)
                OnGestureStart?.Invoke(_hand, GestureType.Grab);
            else if (!_isGrabbing && wasGrabbing)
                OnGestureEnd?.Invoke(_hand, GestureType.Grab);
        }

        private void UpdatePoint(bool isPointing)
        {
            bool wasPointing = _isPointing;

            if (isPointing && !_isPointing)
            {
                _isPointing = true;
                _pointStartTime = Time.time;
            }
            else if (!isPointing && _isPointing)
            {
                _isPointing = false;
            }

            if (_isPointing && !wasPointing)
                OnGestureStart?.Invoke(_hand, GestureType.Point);
            else if (!_isPointing && wasPointing)
                OnGestureEnd?.Invoke(_hand, GestureType.Point);
        }

        private void UpdateOpenPalm(bool isOpen)
        {
            bool wasOpen = _isOpenPalm;

            if (isOpen && !_isOpenPalm)
            {
                _isOpenPalm = true;
                _openPalmStartTime = Time.time;
            }
            else if (!isOpen && _isOpenPalm)
            {
                _isOpenPalm = false;
            }

            if (_isOpenPalm && !wasOpen)
                OnGestureStart?.Invoke(_hand, GestureType.OpenPalm);
            else if (!_isOpenPalm && wasOpen)
                OnGestureEnd?.Invoke(_hand, GestureType.OpenPalm);
        }

        private void UpdateThumbsUp(bool isThumbsUp)
        {
            bool wasThumbsUp = _isThumbsUp;

            if (isThumbsUp && !_isThumbsUp)
            {
                _isThumbsUp = true;
                _thumbsUpStartTime = Time.time;
            }
            else if (!isThumbsUp && _isThumbsUp)
            {
                _isThumbsUp = false;
            }

            if (_isThumbsUp && !wasThumbsUp)
                OnGestureStart?.Invoke(_hand, GestureType.ThumbsUp);
            else if (!_isThumbsUp && wasThumbsUp)
                OnGestureEnd?.Invoke(_hand, GestureType.ThumbsUp);
        }

        private bool IsFingerCurled(Vector3 baseJoint, Vector3 tipJoint, Vector3 wrist)
        {
            // A finger is curled if the tip is closer to the wrist than the base
            // or if the angle between base→tip and base→wrist is large
            Vector3 toTip = (tipJoint - baseJoint).normalized;
            Vector3 toWrist = (wrist - baseJoint).normalized;

            float angle = Vector3.Angle(toTip, -toWrist); // Angle from "straight" position
            return angle < (180f - _fingerCurlAngle); // Curled if not extended straight out
        }

        private void FireHoldEvents()
        {
            float time = Time.time;
            if (_isPinching && (time - _pinchStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.Pinch, time - _pinchStartTime);
            if (_isGrabbing && (time - _grabStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.Grab, time - _grabStartTime);
            if (_isPointing && (time - _pointStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.Point, time - _pointStartTime);
            if (_isOpenPalm && (time - _openPalmStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.OpenPalm, time - _openPalmStartTime);
            if (_isThumbsUp && (time - _thumbsUpStartTime) >= _holdDuration)
                OnGestureHold?.Invoke(_hand, GestureType.ThumbsUp, time - _thumbsUpStartTime);
        }

        private void ResetAllGestures()
        {
            if (_isPinching) OnGestureEnd?.Invoke(_hand, GestureType.Pinch);
            if (_isGrabbing) OnGestureEnd?.Invoke(_hand, GestureType.Grab);
            if (_isPointing) OnGestureEnd?.Invoke(_hand, GestureType.Point);
            if (_isOpenPalm) OnGestureEnd?.Invoke(_hand, GestureType.OpenPalm);
            if (_isThumbsUp) OnGestureEnd?.Invoke(_hand, GestureType.ThumbsUp);

            _isPinching = false;
            _isGrabbing = false;
            _isPointing = false;
            _isOpenPalm = false;
            _isThumbsUp = false;
            _currentPinchStrength = 0f;
            _currentGrabStrength = 0f;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Check if a specific gesture is currently active.
        /// </summary>
        public bool IsGestureActive(GestureType gesture)
        {
            return gesture switch
            {
                GestureType.Pinch => _isPinching,
                GestureType.Grab => _isGrabbing,
                GestureType.Point => _isPointing,
                GestureType.OpenPalm => _isOpenPalm,
                GestureType.ThumbsUp => _isThumbsUp,
                _ => false
            };
        }

        /// <summary>
        /// Get how long a gesture has been held (0 if not active).
        /// </summary>
        public float GetGestureHoldDuration(GestureType gesture)
        {
            if (!IsGestureActive(gesture)) return 0f;

            float startTime = gesture switch
            {
                GestureType.Pinch => _pinchStartTime,
                GestureType.Grab => _grabStartTime,
                GestureType.Point => _pointStartTime,
                GestureType.OpenPalm => _openPalmStartTime,
                GestureType.ThumbsUp => _thumbsUpStartTime,
                _ => Time.time
            };

            return Time.time - startTime;
        }

        #endregion
    }
}
