// ARBrushInput.cs - AR Foundation touch input for brush drawing
// Part of Spec 010: Normcore AR Multiuser + Spec 011: OpenBrush Integration
//
// Specialized AR input handler that raycasts to AR planes for drawing.
// Designed for iOS AR Foundation with ARKit backend.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// AR-specific brush input using AR Foundation raycasting.
    /// Provides touch-to-world-position conversion with AR plane detection.
    /// </summary>
    public class ARBrushInput : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] ARRaycastManager _raycastManager;
        [SerializeField] Camera _arCamera;

        [Header("Drawing Settings")]
        [Tooltip("Types of AR trackables to hit")]
        [SerializeField] TrackableType _trackableTypes = TrackableType.PlaneWithinPolygon;

        [Tooltip("Fallback distance when no plane is hit")]
        [SerializeField] float _fallbackDistance = 0.5f;

        [Tooltip("Allow drawing when no plane is detected")]
        [SerializeField] bool _allowFallbackDrawing = true;

        [Tooltip("Smooth position over N frames")]
        [SerializeField, Range(1, 10)] int _positionSmoothFrames = 3;

        [Header("Touch Filtering")]
        [Tooltip("Ignore touches on UI elements")]
        [SerializeField] bool _ignoreUITouches = true;

        [Tooltip("Minimum touch movement to register as drawing")]
        [SerializeField] float _minMoveDistance = 0.001f;

        [Header("Pressure Simulation")]
        [Tooltip("Use touch radius for pressure (if available)")]
        [SerializeField] bool _useTouchRadius;

        [Tooltip("Touch radius range for pressure mapping")]
        [SerializeField] Vector2 _touchRadiusRange = new Vector2(5f, 30f);

        [Header("Debug")]
        [SerializeField] bool _showDebugInfo;
        [SerializeField] LineRenderer _debugRay;

        // State
        bool _isDrawing;
        bool _wasDrawing;
        Vector3 _currentPosition;
        Quaternion _currentRotation;
        float _currentPressure = 1f;
        Vector3 _lastTouchWorldPos;

        // Position smoothing
        Queue<Vector3> _positionHistory = new();

        // Raycast results
        static readonly List<ARRaycastHit> _hits = new();

        // Events
        public event System.Action OnDrawStart;
        public event System.Action OnDrawEnd;
        public event System.Action<Vector3, Quaternion, float> OnPositionUpdate;

        // Properties
        public bool IsDrawing => _isDrawing;
        public Vector3 Position => _currentPosition;
        public Quaternion Rotation => _currentRotation;
        public float Pressure => _currentPressure;
        public bool HasPlaneHit { get; private set; }

        void Awake()
        {
            if (_arCamera == null)
                _arCamera = Camera.main;

            if (_raycastManager == null)
                _raycastManager = FindFirstObjectByType<ARRaycastManager>();
        }

        void Update()
        {
            _wasDrawing = _isDrawing;

            ProcessTouchInput();

            // Handle state transitions
            if (_isDrawing && !_wasDrawing)
            {
                OnDrawStart?.Invoke();
                BrushManager.Instance?.BeginStroke(_currentPosition, _currentRotation, _currentPressure);
            }
            else if (_isDrawing && _wasDrawing)
            {
                // Only update if moved enough
                if (Vector3.Distance(_currentPosition, _lastTouchWorldPos) >= _minMoveDistance)
                {
                    OnPositionUpdate?.Invoke(_currentPosition, _currentRotation, _currentPressure);
                    BrushManager.Instance?.UpdateStroke(_currentPosition, _currentRotation, _currentPressure);
                    _lastTouchWorldPos = _currentPosition;
                }
            }
            else if (!_isDrawing && _wasDrawing)
            {
                OnDrawEnd?.Invoke();
                BrushManager.Instance?.EndStroke();
                _positionHistory.Clear();
            }

            UpdateDebugVisualization();
        }

        void ProcessTouchInput()
        {
            if (Input.touchCount == 0)
            {
                _isDrawing = false;
                return;
            }

            Touch touch = Input.GetTouch(0);

            // Skip if touching UI
            if (_ignoreUITouches && IsTouchOverUI(touch))
            {
                _isDrawing = false;
                return;
            }

            // Check touch phase
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _isDrawing = false;
                return;
            }

            // Try AR raycast
            if (_raycastManager != null && _raycastManager.Raycast(touch.position, _hits, _trackableTypes))
            {
                HasPlaneHit = true;
                var hit = _hits[0];

                Vector3 hitPos = hit.pose.position;
                Quaternion hitRot = hit.pose.rotation;

                // Smooth position
                AddToPositionHistory(hitPos);
                _currentPosition = GetSmoothedPosition();
                _currentRotation = hitRot;
                _currentPressure = GetTouchPressure(touch);
                _isDrawing = true;
            }
            else if (_allowFallbackDrawing && _arCamera != null)
            {
                // Fallback: draw at fixed distance from camera
                HasPlaneHit = false;
                Ray ray = _arCamera.ScreenPointToRay(touch.position);

                Vector3 fallbackPos = ray.GetPoint(_fallbackDistance);

                AddToPositionHistory(fallbackPos);
                _currentPosition = GetSmoothedPosition();
                _currentRotation = Quaternion.LookRotation(-ray.direction);
                _currentPressure = GetTouchPressure(touch);
                _isDrawing = true;
            }
            else
            {
                _isDrawing = false;
            }
        }

        bool IsTouchOverUI(Touch touch)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
                return false;

            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        }

        float GetTouchPressure(Touch touch)
        {
            if (!_useTouchRadius)
                return 1f;

            // Use touch radius as pressure proxy (larger radius = more pressure)
            float radius = touch.radius > 0 ? touch.radius : _touchRadiusRange.x;
            return Mathf.InverseLerp(_touchRadiusRange.x, _touchRadiusRange.y, radius);
        }

        void AddToPositionHistory(Vector3 pos)
        {
            _positionHistory.Enqueue(pos);
            while (_positionHistory.Count > _positionSmoothFrames)
                _positionHistory.Dequeue();
        }

        Vector3 GetSmoothedPosition()
        {
            if (_positionHistory.Count == 0)
                return _currentPosition;

            Vector3 sum = Vector3.zero;
            foreach (var pos in _positionHistory)
                sum += pos;

            return sum / _positionHistory.Count;
        }

        void UpdateDebugVisualization()
        {
            if (_debugRay == null) return;

            _debugRay.enabled = _isDrawing && _showDebugInfo;

            if (_debugRay.enabled && _arCamera != null)
            {
                _debugRay.SetPosition(0, _arCamera.transform.position);
                _debugRay.SetPosition(1, _currentPosition);
            }
        }

        #region Public API

        /// <summary>
        /// Set which AR trackable types to hit
        /// </summary>
        public void SetTrackableTypes(TrackableType types)
        {
            _trackableTypes = types;
        }

        /// <summary>
        /// Set fallback drawing distance
        /// </summary>
        public void SetFallbackDistance(float distance)
        {
            _fallbackDistance = Mathf.Max(0.1f, distance);
        }

        /// <summary>
        /// Enable/disable fallback drawing when no plane is hit
        /// </summary>
        public void SetAllowFallback(bool allow)
        {
            _allowFallbackDrawing = allow;
        }

        /// <summary>
        /// Cancel current drawing
        /// </summary>
        public void CancelDrawing()
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                BrushManager.Instance?.CancelStroke();
                OnDrawEnd?.Invoke();
                _positionHistory.Clear();
            }
        }

        /// <summary>
        /// Get the AR raycast manager (for external configuration)
        /// </summary>
        public ARRaycastManager GetRaycastManager() => _raycastManager;

        #endregion

        void OnDrawGizmos()
        {
            if (!_showDebugInfo) return;

            if (_isDrawing)
            {
                Gizmos.color = HasPlaneHit ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(_currentPosition, 0.02f);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(_currentPosition, _currentRotation * Vector3.forward * 0.1f);
            }
        }
    }
}
