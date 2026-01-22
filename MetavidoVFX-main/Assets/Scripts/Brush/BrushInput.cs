// BrushInput.cs - AR touch input for brush drawing
// Part of Spec 011: OpenBrush Integration
//
// Converts AR touch/hand input to brush position and trigger state.
// Supports multiple input modes: touch raycast, hand tracking, controller.

using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Input handler for brush painting.
    /// Converts various input sources to brush position/rotation/pressure.
    /// </summary>
    public class BrushInput : MonoBehaviour
    {
        public enum InputMode
        {
            Touch,          // AR touch raycast to planes
            HandTracking,   // Hand tracking position
            Controller,     // XR controller
            Mouse           // Editor mouse input
        }

        [Header("Configuration")]
        [SerializeField] InputMode _inputMode = InputMode.Touch;
        [SerializeField] Camera _arCamera;
        [SerializeField] float _drawDistance = 0.5f;
        [SerializeField] LayerMask _drawableLayers = -1;

        [Header("Touch Settings")]
        [SerializeField] ARRaycastManager _raycastManager;
        [SerializeField] bool _requirePlaneHit = true;

        [Header("Hand Tracking (Optional)")]
        [SerializeField] Transform _handTransform;
        [SerializeField] float _pinchThreshold = 0.5f;

        [Header("Debug")]
        [SerializeField] bool _showDebugGizmos;
        [SerializeField] GameObject _brushTipVisualizer;

        // State
        Vector3 _brushPosition;
        Quaternion _brushRotation = Quaternion.identity;
        float _pressure = 1f;
        bool _isDrawing;
        bool _wasDrawing;

        // Touch state
        static readonly System.Collections.Generic.List<ARRaycastHit> _raycastHits = new();

        // Events
        public event System.Action OnDrawStart;
        public event System.Action OnDrawEnd;

        // Properties
        public Vector3 BrushPosition => _brushPosition;
        public Quaternion BrushRotation => _brushRotation;
        public float Pressure => _pressure;
        public bool IsDrawing => _isDrawing;
        public InputMode CurrentMode => _inputMode;

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

            switch (_inputMode)
            {
                case InputMode.Touch:
                    UpdateTouchInput();
                    break;
                case InputMode.HandTracking:
                    UpdateHandTrackingInput();
                    break;
                case InputMode.Controller:
                    UpdateControllerInput();
                    break;
                case InputMode.Mouse:
                    UpdateMouseInput();
                    break;
            }

            // Handle draw state transitions
            if (_isDrawing && !_wasDrawing)
            {
                OnDrawStart?.Invoke();
                BrushManager.Instance?.BeginStroke(_brushPosition, _brushRotation, _pressure);
            }
            else if (_isDrawing && _wasDrawing)
            {
                BrushManager.Instance?.UpdateStroke(_brushPosition, _brushRotation, _pressure);
            }
            else if (!_isDrawing && _wasDrawing)
            {
                OnDrawEnd?.Invoke();
                BrushManager.Instance?.EndStroke();
            }

            // Update visualizer
            if (_brushTipVisualizer != null)
            {
                _brushTipVisualizer.transform.position = _brushPosition;
                _brushTipVisualizer.transform.rotation = _brushRotation;
                _brushTipVisualizer.SetActive(_isDrawing);
            }
        }

        #region Touch Input

        void UpdateTouchInput()
        {
            if (Input.touchCount == 0)
            {
                _isDrawing = false;
                return;
            }

            Touch touch = Input.GetTouch(0);

            // Ignore if touch is on UI
            if (UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject(touch.fingerId) == true)
            {
                _isDrawing = false;
                return;
            }

            _isDrawing = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;

            if (!_isDrawing) return;

            // Try AR raycast first
            if (_raycastManager != null && _raycastManager.Raycast(touch.position, _raycastHits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                var hit = _raycastHits[0];
                _brushPosition = hit.pose.position;
                _brushRotation = hit.pose.rotation;
                _pressure = 1f;
                return;
            }

            // Fallback: raycast against world or fixed distance
            if (!_requirePlaneHit && _arCamera != null)
            {
                Ray ray = _arCamera.ScreenPointToRay(touch.position);

                if (Physics.Raycast(ray, out RaycastHit worldHit, 10f, _drawableLayers))
                {
                    _brushPosition = worldHit.point;
                    _brushRotation = Quaternion.LookRotation(worldHit.normal);
                }
                else
                {
                    // Draw at fixed distance from camera
                    _brushPosition = ray.GetPoint(_drawDistance);
                    _brushRotation = Quaternion.LookRotation(-ray.direction);
                }
                _pressure = 1f;
            }
            else
            {
                _isDrawing = false;
            }
        }

        #endregion

        #region Hand Tracking Input

        void UpdateHandTrackingInput()
        {
            if (_handTransform == null)
            {
                _isDrawing = false;
                return;
            }

            // Get pinch state from hand tracking system
            float pinchAmount = GetPinchAmount();
            _isDrawing = pinchAmount > _pinchThreshold;

            if (_isDrawing)
            {
                _brushPosition = _handTransform.position;
                _brushRotation = _handTransform.rotation;
                _pressure = Mathf.InverseLerp(_pinchThreshold, 1f, pinchAmount);
            }
        }

        float GetPinchAmount()
        {
            // This would integrate with your hand tracking provider
            // For now, return 0 - implement based on your hand tracking system
            // e.g., XRRAI.HandTracking.IHandTrackingProvider
            return 0f;
        }

        #endregion

        #region Controller Input

        void UpdateControllerInput()
        {
            // XR controller input - implement based on XR Input System
            // This is a placeholder for VR controller support
            _isDrawing = false;
        }

        #endregion

        #region Mouse Input (Editor)

        void UpdateMouseInput()
        {
#if UNITY_EDITOR
            _isDrawing = Input.GetMouseButton(0);

            if (!_isDrawing) return;

            if (_arCamera == null) return;

            Ray ray = _arCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 10f, _drawableLayers))
            {
                _brushPosition = hit.point;
                _brushRotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                _brushPosition = ray.GetPoint(_drawDistance);
                _brushRotation = Quaternion.LookRotation(-ray.direction);
            }

            // Simulate pressure with scroll wheel or shift key
            _pressure = Input.GetKey(KeyCode.LeftShift) ? 0.5f : 1f;
#else
            _isDrawing = false;
#endif
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set input mode at runtime
        /// </summary>
        public void SetInputMode(InputMode mode)
        {
            _inputMode = mode;
        }

        /// <summary>
        /// Set hand transform for hand tracking mode
        /// </summary>
        public void SetHandTransform(Transform handTransform)
        {
            _handTransform = handTransform;
        }

        /// <summary>
        /// Set draw distance for non-plane drawing
        /// </summary>
        public void SetDrawDistance(float distance)
        {
            _drawDistance = Mathf.Max(0.1f, distance);
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
            }
        }

        #endregion

        void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;

            Gizmos.color = _isDrawing ? Color.green : Color.red;
            Gizmos.DrawWireSphere(_brushPosition, 0.02f);

            if (_isDrawing)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(_brushPosition, _brushRotation * Vector3.forward * 0.1f);
            }
        }
    }
}
