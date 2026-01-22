// GestureInterpreter - High-level gesture interpretation for brush painting (spec-012 T5.2)
// Interprets raw gestures into drawing actions

using System;
using UnityEngine;

namespace MetavidoVFX.HandTracking.Gestures
{
    /// <summary>
    /// Interprets hand gestures into high-level actions for brush painting.
    /// Handles: draw start/stop, palette open, brush switch, stroke manipulation.
    /// </summary>
    public class GestureInterpreter : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Configuration")]
        [SerializeField] private GestureConfig _config;

        [Header("Hand Assignment")]
        [SerializeField] private Hand _dominantHand = Hand.Right;

        [Header("Runtime Status (Read-Only)")]
        [SerializeField, Tooltip("Current drawing state")]
        private DrawingState _currentState = DrawingState.Idle;

        [SerializeField, Tooltip("Is palette visible")]
        private bool _isPaletteOpen = false;

        [SerializeField, Tooltip("Dominant hand tracked")]
        private bool _dominantTracked = false;

        [SerializeField, Tooltip("Non-dominant hand tracked")]
        private bool _nonDominantTracked = false;

        [SerializeField, Tooltip("Current draw position")]
        private Vector3 _drawPosition;

        [SerializeField, Tooltip("Current brush width (pinch-controlled)")]
        private float _brushWidth = 0.01f;

        #endregion

        #region State

        private IHandTrackingProvider _provider;
        private GestureDetector _dominantDetector;
        private GestureDetector _nonDominantDetector;

        private float _twoHandHoldStart;
        private bool _twoHandPinchPending;

        private Vector3 _lastDrawPosition;
        private float _drawSpeed;

        #endregion

        #region Events

        /// <summary>Fired when drawing starts (dominant hand pinch).</summary>
        public event Action<Vector3> OnDrawStart;

        /// <summary>Fired each frame while drawing (with position and width).</summary>
        public event Action<Vector3, float, float> OnDrawUpdate; // position, width, speed

        /// <summary>Fired when drawing ends.</summary>
        public event Action OnDrawEnd;

        /// <summary>Fired when palette opens (two-hand pinch).</summary>
        public event Action<Vector3> OnPaletteOpen; // position (between hands)

        /// <summary>Fired when palette closes.</summary>
        public event Action OnPaletteClose;

        /// <summary>Fired when pointing at palette (selection).</summary>
        public event Action<Vector3, Vector3> OnPalettePoint; // point position, direction

        /// <summary>Fired when stroke grab starts (fist gesture).</summary>
        public event Action<Vector3> OnStrokeGrabStart;

        /// <summary>Fired when stroke grab ends.</summary>
        public event Action OnStrokeGrabEnd;

        /// <summary>Fired when erase gesture detected (open palm).</summary>
        public event Action<Vector3> OnEraseGesture;

        #endregion

        #region Public Properties

        public DrawingState CurrentState => _currentState;
        public bool IsPaletteOpen => _isPaletteOpen;
        public Vector3 DrawPosition => _drawPosition;
        public float BrushWidth => _brushWidth;
        public float DrawSpeed => _drawSpeed;
        public Hand DominantHand => _dominantHand;

        public GestureConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                ApplyConfig();
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Try to find provider
            _provider = HandTrackingProviderManager.Instance?.ActiveProvider;

            // Create detectors
            _dominantDetector = _config != null
                ? _config.CreateDetector(_dominantHand)
                : new GestureDetector(_dominantHand);

            Hand nonDominant = _dominantHand == Hand.Right ? Hand.Left : Hand.Right;
            _nonDominantDetector = _config != null
                ? _config.CreateDetector(nonDominant)
                : new GestureDetector(nonDominant);

            // Subscribe to gesture events
            _dominantDetector.OnGestureStart += OnDominantGestureStart;
            _dominantDetector.OnGestureEnd += OnDominantGestureEnd;
            _nonDominantDetector.OnGestureStart += OnNonDominantGestureStart;
            _nonDominantDetector.OnGestureEnd += OnNonDominantGestureEnd;
        }

        private void OnEnable()
        {
            // Subscribe to provider changes
            if (HandTrackingProviderManager.Instance != null)
            {
                HandTrackingProviderManager.Instance.OnProviderChanged += OnProviderChanged;
            }
        }

        private void OnDisable()
        {
            if (HandTrackingProviderManager.Instance != null)
            {
                HandTrackingProviderManager.Instance.OnProviderChanged -= OnProviderChanged;
            }
        }

        private void Update()
        {
            if (_provider == null)
            {
                _provider = HandTrackingProviderManager.Instance?.ActiveProvider;
                if (_provider == null) return;
            }

            // Update tracking status
            _dominantTracked = _provider.IsHandTracked(_dominantHand);
            Hand nonDominant = _dominantHand == Hand.Right ? Hand.Left : Hand.Right;
            _nonDominantTracked = _provider.IsHandTracked(nonDominant);

            // Update detectors
            _dominantDetector.UpdateFromProvider(_provider);
            _nonDominantDetector.UpdateFromProvider(_provider);

            // Check two-hand gestures
            CheckTwoHandGestures();

            // Update drawing state
            UpdateDrawingState();

            // Update runtime status
            UpdateRuntimeStatus();
        }

        private void OnDestroy()
        {
            _dominantDetector.OnGestureStart -= OnDominantGestureStart;
            _dominantDetector.OnGestureEnd -= OnDominantGestureEnd;
            _nonDominantDetector.OnGestureStart -= OnNonDominantGestureStart;
            _nonDominantDetector.OnGestureEnd -= OnNonDominantGestureEnd;
        }

        #endregion

        #region Private Methods

        private void ApplyConfig()
        {
            if (_config == null) return;
            _config.ApplyTo(_dominantDetector);
            _config.ApplyTo(_nonDominantDetector);
        }

        private void OnProviderChanged(IHandTrackingProvider newProvider)
        {
            _provider = newProvider;
        }

        private void CheckTwoHandGestures()
        {
            if (!_dominantTracked || !_nonDominantTracked) return;

            bool dominantPinch = _dominantDetector.IsPinching;
            bool nonDominantPinch = _nonDominantDetector.IsPinching;

            // Both hands pinching
            if (dominantPinch && nonDominantPinch)
            {
                // Check distance between hands
                Vector3 domPos = GetHandPosition(_dominantHand);
                Hand nonDominant = _dominantHand == Hand.Right ? Hand.Left : Hand.Right;
                Vector3 nonDomPos = GetHandPosition(nonDominant);

                float distance = Vector3.Distance(domPos, nonDomPos);
                float maxDist = _config != null ? _config.TwoHandMaxDistance : 0.3f;
                float holdTime = _config != null ? _config.TwoHandHoldTime : 0.3f;

                if (distance < maxDist)
                {
                    if (!_twoHandPinchPending)
                    {
                        _twoHandPinchPending = true;
                        _twoHandHoldStart = Time.time;
                    }
                    else if (Time.time - _twoHandHoldStart >= holdTime && !_isPaletteOpen)
                    {
                        // Open palette
                        _isPaletteOpen = true;
                        Vector3 midpoint = (domPos + nonDomPos) * 0.5f;
                        OnPaletteOpen?.Invoke(midpoint);
                        _currentState = DrawingState.Palette;
                    }
                }
                else
                {
                    _twoHandPinchPending = false;
                }
            }
            else
            {
                _twoHandPinchPending = false;

                // Close palette when both pinches released
                if (_isPaletteOpen && !dominantPinch && !nonDominantPinch)
                {
                    _isPaletteOpen = false;
                    OnPaletteClose?.Invoke();
                    _currentState = DrawingState.Idle;
                }
            }

            // Point gesture while palette open
            if (_isPaletteOpen && _dominantDetector.IsPointing)
            {
                Vector3 indexTip = _provider.GetJointPosition(_dominantHand, HandJointID.IndexTip);
                Vector3 indexBase = _provider.GetJointPosition(_dominantHand, HandJointID.IndexProximal);
                Vector3 direction = (indexTip - indexBase).normalized;
                OnPalettePoint?.Invoke(indexTip, direction);
            }
        }

        private void UpdateDrawingState()
        {
            if (!_dominantTracked) return;

            // Calculate draw position (index fingertip)
            _drawPosition = _provider.GetJointPosition(_dominantHand, HandJointID.IndexTip);

            // Calculate speed
            _drawSpeed = Vector3.Distance(_drawPosition, _lastDrawPosition) / Time.deltaTime;
            _lastDrawPosition = _drawPosition;

            // Calculate brush width from pinch strength
            float pinchStrength = _dominantDetector.CurrentPinchStrength;
            _brushWidth = Mathf.Lerp(0.001f, 0.05f, pinchStrength);

            // Update based on state
            if (_currentState == DrawingState.Drawing)
            {
                OnDrawUpdate?.Invoke(_drawPosition, _brushWidth, _drawSpeed);
            }
        }

        private void UpdateRuntimeStatus()
        {
            // Already updated in other methods
        }

        private Vector3 GetHandPosition(Hand hand)
        {
            if (_provider == null) return Vector3.zero;
            return _provider.GetJointPosition(hand, HandJointID.Wrist);
        }

        private void OnDominantGestureStart(Hand hand, GestureType gesture)
        {
            if (_isPaletteOpen) return; // Don't draw while palette open

            switch (gesture)
            {
                case GestureType.Pinch:
                    if (_currentState == DrawingState.Idle)
                    {
                        _currentState = DrawingState.Drawing;
                        OnDrawStart?.Invoke(_drawPosition);
                    }
                    break;

                case GestureType.Grab:
                    _currentState = DrawingState.Grabbing;
                    OnStrokeGrabStart?.Invoke(GetHandPosition(hand));
                    break;

                case GestureType.OpenPalm:
                    OnEraseGesture?.Invoke(GetHandPosition(hand));
                    break;
            }
        }

        private void OnDominantGestureEnd(Hand hand, GestureType gesture)
        {
            switch (gesture)
            {
                case GestureType.Pinch:
                    if (_currentState == DrawingState.Drawing)
                    {
                        _currentState = DrawingState.Idle;
                        OnDrawEnd?.Invoke();
                    }
                    break;

                case GestureType.Grab:
                    if (_currentState == DrawingState.Grabbing)
                    {
                        _currentState = DrawingState.Idle;
                        OnStrokeGrabEnd?.Invoke();
                    }
                    break;
            }
        }

        private void OnNonDominantGestureStart(Hand hand, GestureType gesture)
        {
            // Non-dominant gestures handled in CheckTwoHandGestures
        }

        private void OnNonDominantGestureEnd(Hand hand, GestureType gesture)
        {
            // Non-dominant gestures handled in CheckTwoHandGestures
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Swap dominant hand.
        /// </summary>
        public void SetDominantHand(Hand hand)
        {
            if (_dominantHand == hand) return;

            _dominantHand = hand;

            // Recreate detectors
            if (_dominantDetector != null)
            {
                _dominantDetector.OnGestureStart -= OnDominantGestureStart;
                _dominantDetector.OnGestureEnd -= OnDominantGestureEnd;
            }
            if (_nonDominantDetector != null)
            {
                _nonDominantDetector.OnGestureStart -= OnNonDominantGestureStart;
                _nonDominantDetector.OnGestureEnd -= OnNonDominantGestureEnd;
            }

            _dominantDetector = _config != null
                ? _config.CreateDetector(_dominantHand)
                : new GestureDetector(_dominantHand);

            Hand nonDominant = _dominantHand == Hand.Right ? Hand.Left : Hand.Right;
            _nonDominantDetector = _config != null
                ? _config.CreateDetector(nonDominant)
                : new GestureDetector(nonDominant);

            _dominantDetector.OnGestureStart += OnDominantGestureStart;
            _dominantDetector.OnGestureEnd += OnDominantGestureEnd;
            _nonDominantDetector.OnGestureStart += OnNonDominantGestureStart;
            _nonDominantDetector.OnGestureEnd += OnNonDominantGestureEnd;
        }

        /// <summary>
        /// Force cancel any active drawing.
        /// </summary>
        public void CancelDrawing()
        {
            if (_currentState == DrawingState.Drawing)
            {
                _currentState = DrawingState.Idle;
                OnDrawEnd?.Invoke();
            }
        }

        #endregion
    }

    /// <summary>
    /// Drawing state machine states.
    /// </summary>
    public enum DrawingState
    {
        Idle,       // Not drawing
        Drawing,    // Actively drawing
        Grabbing,   // Grabbing existing stroke
        Palette     // Palette open
    }
}
