// BrushController.cs - Manages active brush state and routes hand data to VFX (spec-012)
// Controls brush selection, parameter mapping, and VFX binding

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using XRRAI.HandTracking;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Central controller for brush painting system.
    /// Manages brush state, parameter mapping, and VFX binding.
    /// </summary>
    public class BrushController : MonoBehaviour
    {
        #region Brush Types

        public enum BrushType
        {
            ParticleTrail,
            Ribbon,
            Spray,
            Glow,
            Fire,
            Sparkle,
            Tube,
            Smoke
        }

        #endregion

        #region Serialized Fields

        [Header("Hand Tracking")]
        [SerializeField] private Hand _drawingHand = Hand.Right;
        [SerializeField] private bool _useSmoothing = true;
        [SerializeField] private float _positionSmoothTime = 0.05f;
        [SerializeField] private float _velocitySmoothTime = 0.1f;

        [Header("Brush Settings")]
        [SerializeField] private BrushType _currentBrush = BrushType.ParticleTrail;
        [SerializeField] private Color _brushColor = Color.cyan;
        [SerializeField] private float _baseBrushWidth = 0.02f;
        [SerializeField] private float _minBrushWidth = 0.005f;
        [SerializeField] private float _maxBrushWidth = 0.1f;

        [Header("Parameter Mapping")]
        [SerializeField] private AnimationCurve _speedToRateCurve = AnimationCurve.Linear(0, 0.1f, 5, 1f);
        [SerializeField] private AnimationCurve _pinchToWidthCurve = AnimationCurve.Linear(0, 1f, 1, 0.2f);
        [SerializeField] private float _speedMultiplier = 1f;

        [Header("VFX References")]
        [SerializeField] private VisualEffect[] _brushVFX = new VisualEffect[8];
        [SerializeField] private VisualEffect _activeVFX;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Private Members

        private IHandTrackingProvider _handProvider;
        private GestureInterpreter _gestureInterpreter;

        private Vector3 _smoothedPosition;
        private Vector3 _smoothedVelocity;
        private Vector3 _positionVelocity;
        private Vector3 _velocitySmoothing;

        private bool _isDrawing;
        private float _currentWidth;
        private float _currentRate;

        private readonly List<StrokePoint> _currentStroke = new List<StrokePoint>();

        // VFX property IDs
        private static readonly int _positionId = Shader.PropertyToID("BrushPosition");
        private static readonly int _velocityId = Shader.PropertyToID("BrushVelocity");
        private static readonly int _widthId = Shader.PropertyToID("BrushWidth");
        private static readonly int _colorId = Shader.PropertyToID("BrushColor");
        private static readonly int _rateId = Shader.PropertyToID("SpawnRate");
        private static readonly int _isDrawingId = Shader.PropertyToID("IsDrawing");

        #endregion

        #region Events

        public event Action<BrushType> OnBrushChanged;
        public event Action OnStrokeStart;
        public event Action<List<StrokePoint>> OnStrokeEnd;

        #endregion

        #region Properties

        public BrushType CurrentBrush => _currentBrush;
        public Color BrushColor => _brushColor;
        public float BrushWidth => _currentWidth;
        public bool IsDrawing => _isDrawing;
        public Vector3 BrushPosition => _smoothedPosition;
        public Vector3 BrushVelocity => _smoothedVelocity;

        #endregion

        #region MonoBehaviour

        private void Start()
        {
            _handProvider = HandTrackingProviderManager.Instance.ActiveProvider;
            _gestureInterpreter = GetComponent<GestureInterpreter>();

            if (_gestureInterpreter == null)
            {
                _gestureInterpreter = gameObject.AddComponent<GestureInterpreter>();
            }

            // Subscribe to gesture events
            _gestureInterpreter.OnPinchStart += HandlePinchStart;
            _gestureInterpreter.OnPinchEnd += HandlePinchEnd;
            _gestureInterpreter.OnPinchUpdate += HandlePinchUpdate;

            // Initialize active VFX
            SetBrush(_currentBrush);
        }

        private void OnDestroy()
        {
            if (_gestureInterpreter != null)
            {
                _gestureInterpreter.OnPinchStart -= HandlePinchStart;
                _gestureInterpreter.OnPinchEnd -= HandlePinchEnd;
                _gestureInterpreter.OnPinchUpdate -= HandlePinchUpdate;
            }
        }

        private void Update()
        {
            if (_handProvider == null)
            {
                _handProvider = HandTrackingProviderManager.Instance?.ActiveProvider;
                if (_handProvider == null) return;
            }

            UpdateHandTracking();
            UpdateVFXBindings();
        }

        #endregion

        #region Hand Tracking

        private void UpdateHandTracking()
        {
            if (!_handProvider.IsHandTracked(_drawingHand)) return;

            // Get raw position from index tip (drawing point)
            Vector3 rawPosition = _handProvider.GetJointPosition(_drawingHand, HandJointID.IndexTip);

            // Smooth position
            if (_useSmoothing)
            {
                _smoothedPosition = Vector3.SmoothDamp(
                    _smoothedPosition, rawPosition, ref _positionVelocity, _positionSmoothTime);

                // Calculate velocity from smoothed position
                Vector3 rawVelocity = _positionVelocity;
                _smoothedVelocity = Vector3.SmoothDamp(
                    _smoothedVelocity, rawVelocity, ref _velocitySmoothing, _velocitySmoothTime);
            }
            else
            {
                Vector3 previousPosition = _smoothedPosition;
                _smoothedPosition = rawPosition;
                _smoothedVelocity = (rawPosition - previousPosition) / Time.deltaTime;
            }

            // Calculate brush parameters from hand data
            float speed = _smoothedVelocity.magnitude;
            _currentRate = _speedToRateCurve.Evaluate(speed) * _speedMultiplier;

            // Width from pinch distance (if available)
            float pinchStrength = _handProvider.GetPinchStrength(_drawingHand);
            _currentWidth = Mathf.Lerp(_maxBrushWidth, _minBrushWidth, pinchStrength) * _baseBrushWidth;
            _currentWidth = Mathf.Clamp(_currentWidth, _minBrushWidth, _maxBrushWidth);

            // Record stroke point if drawing
            if (_isDrawing)
            {
                RecordStrokePoint();
            }
        }

        private void RecordStrokePoint()
        {
            var point = new StrokePoint
            {
                Position = _smoothedPosition,
                Direction = _smoothedVelocity.normalized,
                Width = _currentWidth,
                Color = _brushColor,
                Timestamp = Time.time
            };
            _currentStroke.Add(point);
        }

        #endregion

        #region VFX Binding

        private void UpdateVFXBindings()
        {
            if (_activeVFX == null) return;

            // Bind brush parameters to VFX
            if (_activeVFX.HasVector3(_positionId))
                _activeVFX.SetVector3(_positionId, _smoothedPosition);

            if (_activeVFX.HasVector3(_velocityId))
                _activeVFX.SetVector3(_velocityId, _smoothedVelocity);

            if (_activeVFX.HasFloat(_widthId))
                _activeVFX.SetFloat(_widthId, _currentWidth);

            if (_activeVFX.HasVector4(_colorId))
                _activeVFX.SetVector4(_colorId, _brushColor);

            if (_activeVFX.HasFloat(_rateId))
                _activeVFX.SetFloat(_rateId, _isDrawing ? _currentRate : 0f);

            if (_activeVFX.HasBool(_isDrawingId))
                _activeVFX.SetBool(_isDrawingId, _isDrawing);
        }

        #endregion

        #region Gesture Handlers

        private void HandlePinchStart(Hand hand)
        {
            if (hand != _drawingHand) return;

            _isDrawing = true;
            _currentStroke.Clear();
            OnStrokeStart?.Invoke();

            if (_debugMode)
                Debug.Log($"[BrushController] Stroke started with {_currentBrush}");
        }

        private void HandlePinchEnd(Hand hand)
        {
            if (hand != _drawingHand) return;

            _isDrawing = false;

            // Notify listeners with completed stroke
            if (_currentStroke.Count > 0)
            {
                OnStrokeEnd?.Invoke(new List<StrokePoint>(_currentStroke));
            }

            if (_debugMode)
                Debug.Log($"[BrushController] Stroke ended with {_currentStroke.Count} points");
        }

        private void HandlePinchUpdate(Hand hand, float strength)
        {
            if (hand != _drawingHand) return;

            // Modulate width based on pinch strength
            _currentWidth = Mathf.Lerp(_baseBrushWidth, _minBrushWidth,
                _pinchToWidthCurve.Evaluate(strength));
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the active brush type.
        /// </summary>
        public void SetBrush(BrushType brush)
        {
            _currentBrush = brush;

            // Disable all brush VFX
            for (int i = 0; i < _brushVFX.Length; i++)
            {
                if (_brushVFX[i] != null)
                    _brushVFX[i].gameObject.SetActive(false);
            }

            // Enable selected brush VFX
            int brushIndex = (int)brush;
            if (brushIndex < _brushVFX.Length && _brushVFX[brushIndex] != null)
            {
                _activeVFX = _brushVFX[brushIndex];
                _activeVFX.gameObject.SetActive(true);
            }

            OnBrushChanged?.Invoke(brush);

            if (_debugMode)
                Debug.Log($"[BrushController] Brush changed to {brush}");
        }

        /// <summary>
        /// Set the brush color.
        /// </summary>
        public void SetColor(Color color)
        {
            _brushColor = color;
        }

        /// <summary>
        /// Set the base brush width.
        /// </summary>
        public void SetWidth(float width)
        {
            _baseBrushWidth = Mathf.Clamp(width, _minBrushWidth, _maxBrushWidth);
        }

        /// <summary>
        /// Set which hand is used for drawing.
        /// </summary>
        public void SetDrawingHand(Hand hand)
        {
            _drawingHand = hand;
        }

        /// <summary>
        /// Cycle to next brush type.
        /// </summary>
        public void NextBrush()
        {
            int next = ((int)_currentBrush + 1) % 8;
            SetBrush((BrushType)next);
        }

        /// <summary>
        /// Cycle to previous brush type.
        /// </summary>
        public void PreviousBrush()
        {
            int prev = ((int)_currentBrush - 1 + 8) % 8;
            SetBrush((BrushType)prev);
        }

        #endregion
    }
}
