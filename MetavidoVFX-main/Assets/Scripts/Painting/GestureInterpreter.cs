// GestureInterpreter.cs - Interprets hand gestures for brush painting (spec-012)
// Detects pinch, grab, palette activation, and swipe gestures

using System;
using UnityEngine;
using MetavidoVFX.HandTracking;

namespace MetavidoVFX.Painting
{
    /// <summary>
    /// Interprets hand tracking data into painting gestures.
    /// Provides pinch detection with hysteresis, palette activation, and swipe detection.
    /// </summary>
    public class GestureInterpreter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Pinch Detection")]
        [SerializeField] private float _pinchStartThreshold = 0.7f;
        [SerializeField] private float _pinchEndThreshold = 0.3f;
        [SerializeField] private float _pinchHoldTime = 0.1f;

        [Header("Grab Detection")]
        [SerializeField] private float _grabStartThreshold = 0.8f;
        [SerializeField] private float _grabEndThreshold = 0.4f;

        [Header("Palette Gesture")]
        [SerializeField] private float _paletteActivationDistance = 0.15f;
        [SerializeField] private float _paletteHoldTime = 0.5f;

        [Header("Swipe Detection")]
        [SerializeField] private float _swipeMinDistance = 0.1f;
        [SerializeField] private float _swipeMaxTime = 0.5f;
        [SerializeField] private float _swipeMinSpeed = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Events

        // Pinch events (drawing)
        public event Action<Hand> OnPinchStart;
        public event Action<Hand> OnPinchEnd;
        public event Action<Hand, float> OnPinchUpdate;
        public event Action<Hand, float> OnPinchHold;

        // Grab events (manipulation)
        public event Action<Hand> OnGrabStart;
        public event Action<Hand> OnGrabEnd;

        // Palette events
        public event Action OnPaletteActivate;
        public event Action OnPaletteDeactivate;

        // Swipe events (brush switching)
        public event Action<Hand, SwipeDirection> OnSwipe;

        #endregion

        #region Enums

        public enum SwipeDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        #endregion

        #region Private State

        private IHandTrackingProvider _provider;

        // Per-hand gesture state
        private readonly GestureState[] _handStates = new GestureState[2];

        // Palette state
        private bool _paletteActive;
        private float _paletteHoldTimer;

        // Swipe tracking
        private readonly SwipeTracker[] _swipeTrackers = new SwipeTracker[2];

        #endregion

        #region State Classes

        private class GestureState
        {
            public bool IsPinching;
            public bool IsGrabbing;
            public float PinchStartTime;
            public float PinchStrength;
            public float GrabStrength;
            public Vector3 LastWristPosition;
        }

        private class SwipeTracker
        {
            public bool IsTracking;
            public Vector3 StartPosition;
            public float StartTime;
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            // Initialize state arrays
            for (int i = 0; i < 2; i++)
            {
                _handStates[i] = new GestureState();
                _swipeTrackers[i] = new SwipeTracker();
            }
        }

        private void Start()
        {
            _provider = HandTrackingProviderManager.Instance?.ActiveProvider;
        }

        private void Update()
        {
            if (_provider == null)
            {
                _provider = HandTrackingProviderManager.Instance?.ActiveProvider;
                if (_provider == null) return;
            }

            UpdatePinchDetection(Hand.Left);
            UpdatePinchDetection(Hand.Right);

            UpdateGrabDetection(Hand.Left);
            UpdateGrabDetection(Hand.Right);

            UpdatePaletteDetection();

            UpdateSwipeDetection(Hand.Left);
            UpdateSwipeDetection(Hand.Right);
        }

        #endregion

        #region Pinch Detection

        private void UpdatePinchDetection(Hand hand)
        {
            int index = (int)hand;
            var state = _handStates[index];

            if (!_provider.IsHandTracked(hand))
            {
                if (state.IsPinching)
                {
                    state.IsPinching = false;
                    OnPinchEnd?.Invoke(hand);
                }
                return;
            }

            float pinchStrength = _provider.GetPinchStrength(hand);
            state.PinchStrength = pinchStrength;

            // Hysteresis: different thresholds for start vs end
            bool shouldPinch = state.IsPinching
                ? pinchStrength > _pinchEndThreshold
                : pinchStrength > _pinchStartThreshold;

            if (shouldPinch && !state.IsPinching)
            {
                // Pinch started
                state.IsPinching = true;
                state.PinchStartTime = Time.time;
                OnPinchStart?.Invoke(hand);

                if (_debugMode)
                    Debug.Log($"[GestureInterpreter] Pinch started: {hand}");
            }
            else if (!shouldPinch && state.IsPinching)
            {
                // Pinch ended
                state.IsPinching = false;
                OnPinchEnd?.Invoke(hand);

                if (_debugMode)
                    Debug.Log($"[GestureInterpreter] Pinch ended: {hand}");
            }

            // Update events
            if (state.IsPinching)
            {
                OnPinchUpdate?.Invoke(hand, pinchStrength);

                // Check for hold
                float holdDuration = Time.time - state.PinchStartTime;
                if (holdDuration > _pinchHoldTime)
                {
                    OnPinchHold?.Invoke(hand, holdDuration);
                }
            }
        }

        #endregion

        #region Grab Detection

        private void UpdateGrabDetection(Hand hand)
        {
            int index = (int)hand;
            var state = _handStates[index];

            if (!_provider.IsHandTracked(hand))
            {
                if (state.IsGrabbing)
                {
                    state.IsGrabbing = false;
                    OnGrabEnd?.Invoke(hand);
                }
                return;
            }

            float grabStrength = _provider.GetGrabStrength(hand);
            state.GrabStrength = grabStrength;

            // Hysteresis for grab
            bool shouldGrab = state.IsGrabbing
                ? grabStrength > _grabEndThreshold
                : grabStrength > _grabStartThreshold;

            if (shouldGrab && !state.IsGrabbing)
            {
                state.IsGrabbing = true;
                OnGrabStart?.Invoke(hand);

                if (_debugMode)
                    Debug.Log($"[GestureInterpreter] Grab started: {hand}");
            }
            else if (!shouldGrab && state.IsGrabbing)
            {
                state.IsGrabbing = false;
                OnGrabEnd?.Invoke(hand);

                if (_debugMode)
                    Debug.Log($"[GestureInterpreter] Grab ended: {hand}");
            }
        }

        #endregion

        #region Palette Detection

        private void UpdatePaletteDetection()
        {
            // Two-hand gesture: both palms facing each other
            if (!_provider.IsHandTracked(Hand.Left) || !_provider.IsHandTracked(Hand.Right))
            {
                ResetPalette();
                return;
            }

            // Check if both hands are pinching (palette activation gesture)
            float leftPinch = _provider.GetPinchStrength(Hand.Left);
            float rightPinch = _provider.GetPinchStrength(Hand.Right);

            if (leftPinch > _pinchStartThreshold && rightPinch > _pinchStartThreshold)
            {
                // Get palm positions
                Vector3 leftPalm = _provider.GetJointPosition(Hand.Left, HandJointID.Palm);
                Vector3 rightPalm = _provider.GetJointPosition(Hand.Right, HandJointID.Palm);

                float distance = Vector3.Distance(leftPalm, rightPalm);

                if (distance < _paletteActivationDistance)
                {
                    _paletteHoldTimer += Time.deltaTime;

                    if (_paletteHoldTimer >= _paletteHoldTime && !_paletteActive)
                    {
                        _paletteActive = true;
                        OnPaletteActivate?.Invoke();

                        if (_debugMode)
                            Debug.Log("[GestureInterpreter] Palette activated");
                    }
                }
                else
                {
                    ResetPalette();
                }
            }
            else if (_paletteActive)
            {
                // Deactivate when pinch released
                _paletteActive = false;
                _paletteHoldTimer = 0f;
                OnPaletteDeactivate?.Invoke();

                if (_debugMode)
                    Debug.Log("[GestureInterpreter] Palette deactivated");
            }
        }

        private void ResetPalette()
        {
            if (_paletteActive)
            {
                _paletteActive = false;
                OnPaletteDeactivate?.Invoke();
            }
            _paletteHoldTimer = 0f;
        }

        #endregion

        #region Swipe Detection

        private void UpdateSwipeDetection(Hand hand)
        {
            int index = (int)hand;
            var tracker = _swipeTrackers[index];

            if (!_provider.IsHandTracked(hand))
            {
                tracker.IsTracking = false;
                return;
            }

            Vector3 wristPos = _provider.GetJointPosition(hand, HandJointID.Wrist);

            if (!tracker.IsTracking)
            {
                // Start tracking
                tracker.IsTracking = true;
                tracker.StartPosition = wristPos;
                tracker.StartTime = Time.time;
                return;
            }

            float elapsed = Time.time - tracker.StartTime;

            // Check for swipe completion
            if (elapsed > _swipeMaxTime)
            {
                // Reset tracking
                tracker.StartPosition = wristPos;
                tracker.StartTime = Time.time;
                return;
            }

            Vector3 delta = wristPos - tracker.StartPosition;
            float distance = delta.magnitude;

            if (distance >= _swipeMinDistance)
            {
                float speed = distance / elapsed;

                if (speed >= _swipeMinSpeed)
                {
                    // Determine swipe direction
                    SwipeDirection direction = GetSwipeDirection(delta);
                    OnSwipe?.Invoke(hand, direction);

                    if (_debugMode)
                        Debug.Log($"[GestureInterpreter] Swipe: {hand} {direction}");

                    // Reset tracking
                    tracker.StartPosition = wristPos;
                    tracker.StartTime = Time.time;
                }
            }
        }

        private SwipeDirection GetSwipeDirection(Vector3 delta)
        {
            // Project to camera-relative horizontal/vertical
            Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            Vector3 camUp = Camera.main != null ? Camera.main.transform.up : Vector3.up;

            float horizontal = Vector3.Dot(delta, camRight);
            float vertical = Vector3.Dot(delta, camUp);

            if (Mathf.Abs(horizontal) > Mathf.Abs(vertical))
            {
                return horizontal > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else
            {
                return vertical > 0 ? SwipeDirection.Up : SwipeDirection.Down;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if hand is currently pinching.
        /// </summary>
        public bool IsPinching(Hand hand) => _handStates[(int)hand].IsPinching;

        /// <summary>
        /// Check if hand is currently grabbing.
        /// </summary>
        public bool IsGrabbing(Hand hand) => _handStates[(int)hand].IsGrabbing;

        /// <summary>
        /// Get current pinch strength for hand.
        /// </summary>
        public float GetPinchStrength(Hand hand) => _handStates[(int)hand].PinchStrength;

        /// <summary>
        /// Get current grab strength for hand.
        /// </summary>
        public float GetGrabStrength(Hand hand) => _handStates[(int)hand].GrabStrength;

        /// <summary>
        /// Check if palette is active.
        /// </summary>
        public bool IsPaletteActive => _paletteActive;

        #endregion
    }
}
