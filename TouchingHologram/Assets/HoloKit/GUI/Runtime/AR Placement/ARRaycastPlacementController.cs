using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using HoloInteractive.XR.HoloKit;
using HoloInteractive.Library.Math;

public class ARRaycastPlacementController : MonoBehaviour
{
    public Transform HitPoint;
    public GameObject Prefab;
    private GameObject prefabInstance;
    [HideInInspector] public bool isHit = false;
    private Transform centerEye;
    private ARRaycastManager arRaycastManager;

    [Header("Editor Mode")]
    public bool HitDebugger = false;

    [Header("Events")]
    public UnityEvent HitEvent;

    void Start()
    {
        centerEye = FindObjectOfType<HoloKitCameraManager>().CenterEyePose;
        arRaycastManager = FindObjectOfType<ARRaycastManager>();
    }

    void Update()
    {
        Vector3 horizontalForward = new Vector3(centerEye.forward.x, 0, centerEye.forward.z);

#if UNITY_IOS && !UNITY_EDITOR
            Vector3 gazeOrientation = centerEye.forward;
            var dot = Vector3.Dot(gazeOrientation, Vector3.down);
            var tilt = dot / 1;
            var aimPointDistance = 0f;
            if (tilt > 0)
            {
                aimPointDistance = MathHelpers.Remap(tilt, 1, 0, .05f, 3, true);
            }
            else
            {
                aimPointDistance = MathHelpers.Remap(tilt, 0, -1, 3, .05f, true);
            }

            Vector3 rayOrigin = centerEye.position + horizontalForward.normalized * aimPointDistance;
            Ray ray = new(rayOrigin, Vector3.down);
            List<ARRaycastHit> hitResults = new();

            if (arRaycastManager.Raycast(ray, hitResults, TrackableType.Planes))
            {
                foreach (var hitResult in hitResults)
                {
                    var arPlane = hitResult.trackable.GetComponent<ARPlane>();

                    if (arPlane.alignment == PlaneAlignment.HorizontalUp && arPlane.classification == PlaneClassification.Floor)
                    {
                        HitPoint.position = hitResult.pose.position;
                        isHit = true;
                        HitEvent?.Invoke();
                        return;
                    }
                }
                isHit = false;
                HitPoint.position = centerEye.position + horizontalForward.normalized * 1.5f + (transform.up * -1f);
            }
            else
            {
                isHit = false;
                HitPoint.position = centerEye.position + horizontalForward.normalized * 1.5f + (transform.up * -1f);
            }
#else

        if (HitDebugger)
        {
            HitPoint.position = centerEye.position + horizontalForward.normalized * 1.5f + (transform.up * -1f);
            HitEvent?.Invoke();
            HitDebugger = false;
        }
        else
        {
            HitPoint.position = centerEye.position + horizontalForward.normalized * 1.5f + (transform.up * -1f);
        }
#endif
    }

    public void InitPrefab()
    {
        prefabInstance = GameObject.Instantiate(Prefab);
        prefabInstance.transform.position = HitPoint.position;
    }
}