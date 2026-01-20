using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Simple AR object placement and manipulation.
/// - Tap to place/reposition on AR plane (always enabled)
/// - One finger drag: translate X/Z
/// - Two finger drag: translate Y
/// - Pinch: scale
/// - Two finger twist: rotate Y-axis
///
/// Minimal and clean - inspired by Paint-AR patterns.
/// </summary>
public class HologramPlacer : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Object to place and manipulate")]
    [SerializeField] Transform _target;

    [Header("AR")]
    [SerializeField] ARRaycastManager _raycastManager;
    [SerializeField] ARPlaneManager _planeManager;

    [Header("Placement")]
    [Tooltip("Show reticle before placement")]
    [SerializeField] bool _showReticle = true;
    [SerializeField] GameObject _reticlePrefab;

    [Tooltip("Optional: Auto-place at this transform on start (skips tap-to-place)")]
    [SerializeField] Transform _initialPlacementTarget;

    [Header("Constraints")]
    [SerializeField] float _minScale = 0.05f;
    [SerializeField] float _maxScale = 2f;
    [SerializeField] float _dragSpeed = 0.002f;
    [SerializeField] float _heightSpeed = 0.003f;
    [SerializeField] float _scaleSpeed = 0.01f;
    [SerializeField] float _rotateSpeed = 0.5f;

    // State
    bool _isPlaced;
    GameObject _reticle;
    List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    // Touch tracking
    float _lastPinchDistance;
    float _lastTwistAngle;
    Vector2 _lastTouchCenter;
    int _lastTouchCount;

    // Public API
    public bool IsPlaced => _isPlaced;
    public Transform Target => _target;

    void Awake()
    {
        // Auto-find components
        if (_raycastManager == null)
            _raycastManager = FindAnyObjectByType<ARRaycastManager>();
        if (_planeManager == null)
            _planeManager = FindAnyObjectByType<ARPlaneManager>();

        // Create reticle
        if (_showReticle)
            CreateReticle();

        // Hide target until placed
        if (_target != null)
            _target.gameObject.SetActive(false);
    }

    void Start()
    {
        // Auto-place at initial target if specified
        if (_initialPlacementTarget != null)
        {
            StartCoroutine(WaitAndAutoPlace());
        }
    }

    IEnumerator WaitAndAutoPlace()
    {
        // Wait 2 frames to ensure other scripts have initialized
        yield return null;
        yield return null;

        // Check if ARDepthSource exists and wait briefly for it to be ready
        var depthSource = ARDepthSource.Instance;
        if (depthSource != null)
        {
            // Wait up to 1 second for AR data
            float timeout = 1f;
            float elapsed = 0f;

            while (elapsed < timeout && !depthSource.IsReady)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (depthSource.IsReady)
            {
                Debug.Log($"[HologramPlacer] ARDepthSource ready after {elapsed:F2}s");
            }
            else
            {
                Debug.Log($"[HologramPlacer] ARDepthSource not ready yet (waiting for AR Foundation Remote?)");
            }
        }

        // Place at target position
        PlaceAt(_initialPlacementTarget.position);
        Debug.Log($"[HologramPlacer] Auto-placed at {_initialPlacementTarget.name} ({_initialPlacementTarget.position})");
    }

    void Update()
    {
        // Always check for tap placement (repositioning)
        CheckForPlacement();

        // Update reticle when not placed
        if (!_isPlaced)
        {
            UpdateReticle();
        }
        else
        {
            HandleManipulation();
        }
    }

    #region Reticle

    void CreateReticle()
    {
        if (_reticlePrefab != null)
        {
            _reticle = Instantiate(_reticlePrefab);
        }
        else
        {
            // Create minimal procedural reticle
            _reticle = new GameObject("Reticle");

            // Ring
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(_reticle.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(0.15f, 0.002f, 0.15f);
            Destroy(ring.GetComponent<Collider>());

            // Inner ring (slightly smaller, different color)
            var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inner.transform.SetParent(_reticle.transform);
            inner.transform.localPosition = Vector3.zero;
            inner.transform.localScale = new Vector3(0.12f, 0.003f, 0.12f);
            Destroy(inner.GetComponent<Collider>());

            // Center dot
            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.transform.SetParent(_reticle.transform);
            dot.transform.localPosition = Vector3.zero;
            dot.transform.localScale = Vector3.one * 0.02f;
            Destroy(dot.GetComponent<Collider>());

            // Material - simple unlit white
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(1f, 1f, 1f, 0.8f);
            ring.GetComponent<Renderer>().material = mat;
            dot.GetComponent<Renderer>().material = mat;

            var matInner = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            matInner.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            inner.GetComponent<Renderer>().material = matInner;
        }

        _reticle.SetActive(false);
    }

    void UpdateReticle()
    {
        if (_reticle == null) return;

        // Raycast from screen center
        var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (_raycastManager.Raycast(screenCenter, _hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
        {
            var pose = _hits[0].pose;
            _reticle.transform.position = pose.position;
            _reticle.transform.rotation = pose.rotation;
            _reticle.SetActive(true);
        }
        else
        {
            _reticle.SetActive(false);
        }
    }

    #endregion

    #region Placement

    void CheckForPlacement()
    {
        if (Input.touchCount != 1) return;

        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        // Raycast from touch position
        if (_raycastManager.Raycast(touch.position, _hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
        {
            PlaceAt(_hits[0].pose.position);
        }
    }

    public void PlaceAt(Vector3 position)
    {
        if (_target == null) return;

        _target.position = position;
        _target.gameObject.SetActive(true);
        _isPlaced = true;

        // Hide reticle and planes
        if (_reticle != null)
            _reticle.SetActive(false);

        // Optionally hide planes after placement
        if (_planeManager != null)
        {
            foreach (var plane in _planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
            if (_planeManager.planePrefab != null)
                _planeManager.planePrefab.SetActive(false);
        }

        Debug.Log($"[HologramPlacer] Placed at {position}");
    }

    public void Reset()
    {
        _isPlaced = false;
        if (_target != null)
            _target.gameObject.SetActive(false);

        // Show planes again
        if (_planeManager != null)
        {
            foreach (var plane in _planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }
    }

    #endregion

    #region Manipulation

    void HandleManipulation()
    {
        if (_target == null) return;

        int touchCount = Input.touchCount;

        if (touchCount == 1)
        {
            // One finger: drag X/Z
            HandleDrag();
        }
        else if (touchCount == 2)
        {
            // Two fingers: height + scale
            HandleTwoFingerGesture();
        }

        _lastTouchCount = touchCount;
    }

    void HandleDrag()
    {
        var touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Moved)
        {
            // Convert screen delta to world XZ movement
            var delta = touch.deltaPosition * _dragSpeed;

            // Get camera forward/right projected onto XZ plane
            var cam = Camera.main;
            var forward = cam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            var right = cam.transform.right;
            right.y = 0;
            right.Normalize();

            // Apply movement
            _target.position += right * delta.x + forward * delta.y;
        }
    }

    void HandleTwoFingerGesture()
    {
        var touch0 = Input.GetTouch(0);
        var touch1 = Input.GetTouch(1);

        var center = (touch0.position + touch1.position) / 2f;
        var distance = Vector2.Distance(touch0.position, touch1.position);
        var delta = touch1.position - touch0.position;
        var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        if (_lastTouchCount != 2)
        {
            // Just started two-finger gesture
            _lastPinchDistance = distance;
            _lastTouchCenter = center;
            _lastTwistAngle = angle;
            return;
        }

        // Height: two-finger vertical drag
        float centerDeltaY = center.y - _lastTouchCenter.y;
        _target.position += Vector3.up * centerDeltaY * _heightSpeed;

        // Scale: pinch
        float pinchDelta = distance - _lastPinchDistance;
        float newScale = _target.localScale.x + pinchDelta * _scaleSpeed;
        newScale = Mathf.Clamp(newScale, _minScale, _maxScale);
        _target.localScale = Vector3.one * newScale;

        // Rotation: twist
        float angleDelta = Mathf.DeltaAngle(_lastTwistAngle, angle);
        _target.Rotate(Vector3.up, -angleDelta * _rotateSpeed, Space.World);

        _lastPinchDistance = distance;
        _lastTouchCenter = center;
        _lastTwistAngle = angle;
    }

    #endregion

    #region Debug GUI

    void OnGUI()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        GUILayout.BeginArea(new Rect(10, Screen.height - 120, 200, 110));
        GUILayout.BeginVertical("box");

        GUILayout.Label($"Placed: {_isPlaced}");
        if (_target != null)
        {
            GUILayout.Label($"Scale: {_target.localScale.x:F2}");
            GUILayout.Label($"Height: {_target.position.y:F2}m");
            GUILayout.Label($"Rotation: {_target.eulerAngles.y:F0}°");
        }

        if (_isPlaced && GUILayout.Button("Reset"))
            Reset();

        GUILayout.EndVertical();
        GUILayout.EndArea();
        #endif
    }

    #endregion
}
