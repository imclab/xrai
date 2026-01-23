// ARPlaneDrawing.cs - AR plane detection and drawing surface management
// Part of Spec 010: Normcore AR Multiuser + Spec 011: OpenBrush Integration
//
// Manages AR planes as drawing surfaces, providing visual feedback and
// anchoring for brush strokes. Works with AR Foundation plane detection.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Manages AR planes as drawing surfaces.
    /// Provides plane visualization, selection, and stroke anchoring.
    /// </summary>
    public class ARPlaneDrawing : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] ARPlaneManager _planeManager;

        [Header("Drawing Surface Settings")]
        [Tooltip("Only allow drawing on planes of this classification")]
        [SerializeField] UnityEngine.XR.ARSubsystems.PlaneClassification _allowedClassifications =
            (UnityEngine.XR.ARSubsystems.PlaneClassification)(-1); // All

        [Tooltip("Minimum plane area to consider for drawing (m²)")]
        [SerializeField] float _minPlaneArea = 0.1f;

        [Header("Plane Visualization")]
        [Tooltip("Material for drawing-enabled planes")]
        [SerializeField] Material _drawablePlaneMaterial;

        [Tooltip("Material for inactive planes")]
        [SerializeField] Material _inactivePlaneMaterial;

        [Tooltip("Show plane boundaries when drawing")]
        [SerializeField] bool _showPlaneBoundaries = true;

        [Header("Stroke Anchoring")]
        [Tooltip("Anchor strokes to AR planes")]
        [SerializeField] bool _anchorStrokes = true;

        [Tooltip("Parent for anchored strokes")]
        [SerializeField] Transform _anchoredStrokesParent;

        [Header("Debug")]
        [SerializeField] bool _logPlaneEvents;

        // State
        ARPlane _currentDrawingPlane;
        Dictionary<ARPlane, Material> _originalMaterials = new();
        List<ARAnchor> _strokeAnchors = new();

        // Events
        public event System.Action<ARPlane> OnPlaneSelected;
        public event System.Action<ARPlane> OnPlaneDeselected;
        public event System.Action<int> OnPlaneCountChanged;

        // Properties
        public ARPlane CurrentPlane => _currentDrawingPlane;
        public int PlaneCount => _planeManager != null ? _planeManager.trackables.count : 0;
        public bool HasDrawablePlane => _currentDrawingPlane != null;

        void Awake()
        {
            if (_planeManager == null)
                _planeManager = FindFirstObjectByType<ARPlaneManager>();

            if (_anchoredStrokesParent == null)
            {
                var go = new GameObject("AnchoredStrokes");
                go.transform.SetParent(transform);
                _anchoredStrokesParent = go.transform;
            }
        }

        void OnEnable()
        {
            if (_planeManager != null)
            {
                _planeManager.planesChanged += OnPlanesChanged;
            }
        }

        void OnDisable()
        {
            if (_planeManager != null)
            {
                _planeManager.planesChanged -= OnPlanesChanged;
            }
        }

        void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            // Handle added planes
            foreach (var plane in args.added)
            {
                if (IsPlaneDrawable(plane))
                {
                    SetupDrawablePlane(plane);

                    if (_logPlaneEvents)
                        Debug.Log($"[ARPlaneDrawing] Plane added: {plane.trackableId}, class: {plane.classification}");
                }
            }

            // Handle updated planes
            foreach (var plane in args.updated)
            {
                UpdatePlaneVisualization(plane);
            }

            // Handle removed planes
            foreach (var plane in args.removed)
            {
                if (_currentDrawingPlane == plane)
                {
                    OnPlaneDeselected?.Invoke(plane);
                    _currentDrawingPlane = null;
                }

                _originalMaterials.Remove(plane);

                if (_logPlaneEvents)
                    Debug.Log($"[ARPlaneDrawing] Plane removed: {plane.trackableId}");
            }

            OnPlaneCountChanged?.Invoke(PlaneCount);
        }

        bool IsPlaneDrawable(ARPlane plane)
        {
            // Check classification filter
            if (_allowedClassifications != (UnityEngine.XR.ARSubsystems.PlaneClassification)(-1))
            {
                if (!_allowedClassifications.HasFlag(plane.classification))
                    return false;
            }

            // Check minimum area
            if (CalculatePlaneArea(plane) < _minPlaneArea)
                return false;

            return true;
        }

        float CalculatePlaneArea(ARPlane plane)
        {
            return plane.size.x * plane.size.y;
        }

        void SetupDrawablePlane(ARPlane plane)
        {
            if (_drawablePlaneMaterial == null) return;

            var renderer = plane.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                _originalMaterials[plane] = renderer.material;
                renderer.material = _drawablePlaneMaterial;
            }
        }

        void UpdatePlaneVisualization(ARPlane plane)
        {
            if (!_showPlaneBoundaries) return;

            // Update visualization based on whether it's the active drawing plane
            var renderer = plane.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            if (plane == _currentDrawingPlane && _drawablePlaneMaterial != null)
            {
                renderer.material = _drawablePlaneMaterial;
            }
            else if (_inactivePlaneMaterial != null)
            {
                renderer.material = _inactivePlaneMaterial;
            }
        }

        /// <summary>
        /// Select a plane for drawing based on world position
        /// </summary>
        public ARPlane SelectPlaneAtPosition(Vector3 worldPosition)
        {
            if (_planeManager == null) return null;

            ARPlane closestPlane = null;
            float closestDistance = float.MaxValue;

            foreach (var plane in _planeManager.trackables)
            {
                if (!IsPlaneDrawable(plane)) continue;

                // Project position onto plane
                var planeNormal = plane.transform.up;
                var planePoint = plane.transform.position;
                float distance = Mathf.Abs(Vector3.Dot(worldPosition - planePoint, planeNormal));

                // Check if within plane bounds
                Vector3 localPos = plane.transform.InverseTransformPoint(worldPosition);
                if (Mathf.Abs(localPos.x) <= plane.size.x * 0.5f &&
                    Mathf.Abs(localPos.z) <= plane.size.y * 0.5f)
                {
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlane = plane;
                    }
                }
            }

            if (closestPlane != _currentDrawingPlane)
            {
                if (_currentDrawingPlane != null)
                    OnPlaneDeselected?.Invoke(_currentDrawingPlane);

                _currentDrawingPlane = closestPlane;

                if (_currentDrawingPlane != null)
                    OnPlaneSelected?.Invoke(_currentDrawingPlane);
            }

            return closestPlane;
        }

        /// <summary>
        /// Create an anchor at position for stroke persistence
        /// </summary>
        public ARAnchor CreateStrokeAnchor(Vector3 position, Quaternion rotation)
        {
            if (!_anchorStrokes) return null;

            var go = new GameObject("StrokeAnchor");
            go.transform.SetParent(_anchoredStrokesParent);
            go.transform.SetPositionAndRotation(position, rotation);

            var anchor = go.AddComponent<ARAnchor>();
            _strokeAnchors.Add(anchor);

            return anchor;
        }

        /// <summary>
        /// Attach a stroke to an AR anchor
        /// </summary>
        public void AttachStrokeToAnchor(BrushStroke stroke, ARAnchor anchor)
        {
            if (stroke == null || anchor == null) return;

            stroke.transform.SetParent(anchor.transform);
        }

        /// <summary>
        /// Get all drawable planes
        /// </summary>
        public List<ARPlane> GetDrawablePlanes()
        {
            var drawablePlanes = new List<ARPlane>();

            if (_planeManager == null) return drawablePlanes;

            foreach (var plane in _planeManager.trackables)
            {
                if (IsPlaneDrawable(plane))
                    drawablePlanes.Add(plane);
            }

            return drawablePlanes;
        }

        /// <summary>
        /// Set allowed plane classifications
        /// </summary>
        public void SetAllowedClassifications(UnityEngine.XR.ARSubsystems.PlaneClassification classifications)
        {
            _allowedClassifications = classifications;
        }

        /// <summary>
        /// Enable/disable stroke anchoring
        /// </summary>
        public void SetStrokeAnchoring(bool enabled)
        {
            _anchorStrokes = enabled;
        }

        /// <summary>
        /// Clear all stroke anchors
        /// </summary>
        public void ClearStrokeAnchors()
        {
            foreach (var anchor in _strokeAnchors)
            {
                if (anchor != null)
                    Destroy(anchor.gameObject);
            }
            _strokeAnchors.Clear();
        }

        /// <summary>
        /// Show/hide plane visualization
        /// </summary>
        public void SetPlaneVisualization(bool visible)
        {
            if (_planeManager != null)
            {
                foreach (var plane in _planeManager.trackables)
                {
                    var renderer = plane.GetComponent<MeshRenderer>();
                    if (renderer != null)
                        renderer.enabled = visible;
                }
            }
        }

        void OnDestroy()
        {
            ClearStrokeAnchors();
        }
    }
}
