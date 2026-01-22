using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace XRRAI.Hologram
{
    /// <summary>
    /// Handles hologram placement and scaling via touch gestures.
    /// - Single tap/drag: Move hologram to AR plane hit position
    /// - Two-finger pinch: Scale hologram
    /// - Two-finger rotate: Rotate hologram (optional)
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    public class HologramAnchor : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] Transform _hologramRoot;

        [Header("Placement")]
        [Tooltip("Enable tap/drag to reposition hologram")]
        [SerializeField] bool _enablePlacement = true;
        [Tooltip("Only place on initial tap, not drag")]
        [SerializeField] bool _tapOnlyPlacement = false;

        [Header("Scaling")]
        [Tooltip("Enable two-finger pinch to scale")]
        [SerializeField] bool _scaleWithPinch = true;
        [SerializeField] float _minScale = 0.1f;
        [SerializeField] float _maxScale = 5f;
        [SerializeField] float _scaleSpeed = 1f;

        [Header("Rotation")]
        [Tooltip("Enable two-finger twist to rotate")]
        [SerializeField] bool _rotateWithTwist = true;
        [SerializeField] float _rotationSpeed = 1f;

        [Header("Debug")]
        [SerializeField] bool _debugLog = false;

        ARRaycastManager _raycaster;
        List<ARRaycastHit> _hits = new List<ARRaycastHit>();

        // Pinch/rotate state
        float _previousPinchDistance;
        float _previousAngle;
        bool _isScaling;
        Vector3 _initialScale;

        void Awake()
        {
            _raycaster = GetComponent<ARRaycastManager>();
        }

        void Start()
        {
            if (_hologramRoot != null)
            {
                _initialScale = _hologramRoot.localScale;
            }
        }

        void Update()
        {
            // Two-finger gestures (pinch/rotate)
            if (Input.touchCount == 2)
            {
                HandleTwoFingerGestures();
                return;
            }

            // Reset scaling state
            if (_isScaling && Input.touchCount < 2)
            {
                _isScaling = false;
            }

            // Single finger placement
            if (Input.touchCount == 1 && _enablePlacement)
            {
                HandleSingleTouchPlacement();
            }
        }

        void HandleSingleTouchPlacement()
        {
            var touch = Input.GetTouch(0);

            // Only process began phase for tap-only mode
            if (_tapOnlyPlacement && touch.phase != TouchPhase.Began)
                return;

            // Process began and moved phases for drag mode
            if (touch.phase != TouchPhase.Began && touch.phase != TouchPhase.Moved)
                return;

            if (_raycaster.Raycast(touch.position, _hits, TrackableType.PlaneWithinPolygon))
            {
                var hitPose = _hits[0].pose;
                if (_hologramRoot != null)
                {
                    _hologramRoot.position = hitPose.position;

                    if (_debugLog)
                        Debug.Log($"[HologramAnchor] Placed at {hitPose.position}");
                }
            }
        }

        void HandleTwoFingerGestures()
        {
            var touch0 = Input.GetTouch(0);
            var touch1 = Input.GetTouch(1);

            // Calculate pinch distance
            float currentDistance = Vector2.Distance(touch0.position, touch1.position);

            // Calculate rotation angle
            Vector2 delta = touch1.position - touch0.position;
            float currentAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            // Initialize on gesture start
            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                _previousPinchDistance = currentDistance;
                _previousAngle = currentAngle;
                _isScaling = true;
                return;
            }

            // Process scaling
            if (_scaleWithPinch && _hologramRoot != null)
            {
                float deltaDistance = currentDistance - _previousPinchDistance;
                float scaleFactor = 1f + (deltaDistance * _scaleSpeed * 0.001f);

                Vector3 newScale = _hologramRoot.localScale * scaleFactor;
                float clampedScale = Mathf.Clamp(newScale.x, _minScale, _maxScale);
                _hologramRoot.localScale = Vector3.one * clampedScale;

                if (_debugLog && Mathf.Abs(deltaDistance) > 1f)
                    Debug.Log($"[HologramAnchor] Scale: {clampedScale:F2}");
            }

            // Process rotation
            if (_rotateWithTwist && _hologramRoot != null)
            {
                float deltaAngle = Mathf.DeltaAngle(_previousAngle, currentAngle);
                _hologramRoot.Rotate(Vector3.up, deltaAngle * _rotationSpeed, Space.World);

                if (_debugLog && Mathf.Abs(deltaAngle) > 1f)
                    Debug.Log($"[HologramAnchor] Rotate: {deltaAngle:F1}°");
            }

            _previousPinchDistance = currentDistance;
            _previousAngle = currentAngle;
        }

        /// <summary>
        /// Reset hologram to initial scale.
        /// </summary>
        public void ResetScale()
        {
            if (_hologramRoot != null)
            {
                _hologramRoot.localScale = _initialScale;
            }
        }

        /// <summary>
        /// Set hologram scale directly.
        /// </summary>
        public void SetScale(float scale)
        {
            if (_hologramRoot != null)
            {
                float clampedScale = Mathf.Clamp(scale, _minScale, _maxScale);
                _hologramRoot.localScale = Vector3.one * clampedScale;
            }
        }
    }
}
