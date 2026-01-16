using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace H3M.Core
{
    [RequireComponent(typeof(ARRaycastManager))]
    public class HologramAnchor : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] Transform _hologramRoot;

        [Header("Settings")]
        [SerializeField] bool _scaleWithPinch = true;

        ARRaycastManager _raycaster;
        List<ARRaycastHit> _hits = new List<ARRaycastHit>();

        void Awake()
        {
            _raycaster = GetComponent<ARRaycastManager>();
        }

        void Update()
        {
            if (Input.touchCount == 0) return;

            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
            {
                if (_raycaster.Raycast(touch.position, _hits, TrackableType.PlaneWithinPolygon))
                {
                    var hitPose = _hits[0].pose;
                    if (_hologramRoot != null)
                    {
                        _hologramRoot.position = hitPose.position;
                        // Keep rotation flat? or look at camera?
                        // For now just position.
                    }
                }
            }
        }
    }
}
