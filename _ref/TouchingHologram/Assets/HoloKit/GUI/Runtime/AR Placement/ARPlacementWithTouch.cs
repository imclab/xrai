using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events;
using HoloInteractive.XR.HoloKit;

public class ARPlacementWithTouch : MonoBehaviour
{
    public GameObject SpawnedPrefab;
    public GameObject indicatorPrefab;

    private GameObject spawnedInstance;
    private GameObject indicatorInstance;
    private Pose PlacementPose;
    private ARRaycastManager aRRaycastManager;
    private bool placementPoseIsValid = false;
    [SerializeField]
    UnityEvent placementEvent;
    Vector3 InitPosition =new Vector3(0,-1,1);

    void Start()
    {
        aRRaycastManager = FindObjectOfType<ARRaycastManager>();
    }

    // need to update placement indicator, placement pose and spawn 
    void Update()
    {
        if (Application.isEditor)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                PlacementPose.position = Vector3.zero;
                PlacementPose.rotation = Quaternion.Euler(0, 180, 0);
                ARPlaceObject(InitPosition);
            }
        }
        else
        {
            UpdatePlacementPose();
            if (indicatorPrefab) UpdatePlacementIndicator();

            if (placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                ARPlaceObject();
            }
        }
    }

    void UpdatePlacementIndicator()
    {
        if (spawnedInstance == null && placementPoseIsValid)
        {
            if (indicatorInstance == null)
            {
                indicatorInstance = Instantiate(indicatorPrefab);
                indicatorInstance.transform.position = new Vector3(0, -1, 1);
            }
            indicatorInstance.SetActive(true);
            indicatorInstance.transform.SetPositionAndRotation(PlacementPose.position, PlacementPose.rotation);
        }
        else
        {
            indicatorInstance.SetActive(false);
        }
    }

    void UpdatePlacementPose()
    {
        var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();
        aRRaycastManager.Raycast(screenCenter, hits, TrackableType.Planes);

        placementPoseIsValid = hits.Count > 0;
        if (placementPoseIsValid)
        {
            PlacementPose = hits[0].pose;
        }
    }

    void ARPlaceObject()
    {
        var direction = Vector3.ProjectOnPlane(PlacementPose.position - FindObjectOfType<HoloKitCameraManager>().CenterEyePose.position, Vector3.up);
        if (spawnedInstance)
        {
            spawnedInstance.transform.position = PlacementPose.position;
            spawnedInstance.transform.rotation = PlacementPose.rotation;
        }
        else
        {
            spawnedInstance = Instantiate(SpawnedPrefab, PlacementPose.position, PlacementPose.rotation);
            placementEvent?.Invoke();
        }
        spawnedInstance.transform.LookAt(spawnedInstance.transform.position - (direction.normalized * 0.1f));
    }

    void ARPlaceObject(Vector3 arPos)
    {
        placementEvent?.Invoke();
        spawnedInstance = Instantiate(SpawnedPrefab, arPos, PlacementPose.rotation);
    }

}