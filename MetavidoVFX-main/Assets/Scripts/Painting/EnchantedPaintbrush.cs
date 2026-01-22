using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace XRRAI.BrushPainting
{
    public class EnchantedPaintbrush : MonoBehaviour
    {
        [Header("Dependencies")]
        public H3MParticleBrushManager brushManager;
        public Camera mainCamera;
        public ARRaycastManager raycastManager;

        [Header("Settings")]
        public float defaultDistance = 0.5f; // Distance to paint if no surface found
        public LayerMask paintingLayerMask = ~0; // Everything

        private bool isPainting = false;

        void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (raycastManager == null) raycastManager = FindObjectOfType<ARRaycastManager>();
            if (brushManager == null) brushManager = GetComponent<H3MParticleBrushManager>();
        }

        void Update()
        {
            HandleInput();
        }

        void HandleInput()
        {
            // 1. Determine Input State (Touch or Mouse)
            bool inputActive = false;
            Vector2 inputPosition = Vector2.zero;
            float pressure = 1.0f;

#if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                inputActive = true;
                inputPosition = Input.mousePosition;
            }
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    inputActive = true;
                    inputPosition = touch.position;
                    pressure = touch.pressure;
                }
            }
#endif

            // 2. Process Painting
            if (inputActive)
            {
                UpdateBrushPosition(inputPosition);
                brushManager.UpdatePressure(pressure);

                if (!isPainting)
                {
                    brushManager.StartPainting();
                    isPainting = true;
                }
            }
            else
            {
                if (isPainting)
                {
                    brushManager.StopPainting();
                    isPainting = false;
                }
            }
        }

        void UpdateBrushPosition(Vector2 screenPosition)
        {
            Vector3 targetPosition;

            // Try AR Raycast first (for planes/feature points)
            System.Collections.Generic.List<ARRaycastHit> hits = new System.Collections.Generic.List<ARRaycastHit>();
            if (raycastManager != null && raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                targetPosition = hits[0].pose.position;
            }
            // Fallback to Physics Raycast (for mesh colliders)
            else if (Physics.Raycast(mainCamera.ScreenPointToRay(screenPosition), out RaycastHit hit, 20f, paintingLayerMask))
            {
                targetPosition = hit.point;
            }
            // Fallback to fixed distance
            else
            {
                targetPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, defaultDistance));
            }

            // Smoothly move the brush cursor
            brushManager.spawnTransform.position = Vector3.Lerp(brushManager.spawnTransform.position, targetPosition, Time.deltaTime * 20f);

            // Orient brush to face camera
            brushManager.spawnTransform.LookAt(mainCamera.transform);
        }
    }
}
