using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;

[System.Serializable]
public class LocationData
{
    public int meeting_key;
    public int session_key;
    public int driver_number;
    public string date;
    public float x;
    public float y;
    public float z;
}

public class CarMove : MonoBehaviour
{
    public GameObject F1CarObject;
    public GameObject objectToPlace;
    public GameObject placementIndicator;
    public string jsonFileName = "location";
    public float updateInterval = 0.05f; // Interval in seconds
    public float smoothTime = 0.1F;

    private List<LocationData> locationDataList;
    private int currentIndex = 0;
    private Vector3 velocity = Vector3.zero;
    private ARSession arSession;
    private ARRaycastManager arRaycastManager;
    private Pose placementPose;
    private bool placementPoseIsValid = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        LoadLocationData();
        arSession = FindObjectOfType<ARSession>();
        arRaycastManager = FindObjectOfType<ARRaycastManager>();
        StartCoroutine(UpdateCarPositionRoutine());
    }

    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();

        if (placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            PlaceObject();
        }
    }

    void LoadLocationData()
    {
        TextAsset jsonData = Resources.Load<TextAsset>(jsonFileName);
        if (jsonData != null)
        {
            locationDataList = JsonUtility.FromJson<LocationDataList>(WrapToJsonArray(jsonData.text)).Items;
        }
        else
        {
            Debug.LogError("Cannot find file: " + jsonFileName);
        }
    }

    IEnumerator UpdateCarPositionRoutine()
    {
        while (currentIndex < locationDataList.Count)
        {
            UpdateCarPosition();
            currentIndex++;
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void UpdateCarPosition()
    {
        if (currentIndex >= locationDataList.Count) return;

        LocationData currentData = locationDataList[currentIndex];
        
        // Original position before rotation
        Vector3 originalPosition = new Vector3(currentData.x / 1500.0f, currentData.y / 1500.0f, currentData.z / 1500.0f);

        // Rotate position by 90 degrees around the x-axis
        Vector3 rotatedPosition = RotatePointAroundOrigin(originalPosition, new Vector3(90, 0, 0));

        // Calculate direction
        Vector3 direction = rotatedPosition - F1CarObject.transform.position;
        if (direction != Vector3.zero)
        {
            // Calculate rotation towards the direction while maintaining the 90-degree x-axis rotation
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion xRotation = Quaternion.Euler(0, 180, 0);
            F1CarObject.transform.rotation = toRotation * xRotation;
        }

        // Smoothly move the car object to the new position
        F1CarObject.transform.position = Vector3.SmoothDamp(F1CarObject.transform.position, rotatedPosition, ref velocity, smoothTime);
        // F1CarObject.transform.position = rotatedPosition;
    }

    Vector3 RotatePointAroundOrigin(Vector3 point, Vector3 angles)
    {
        Quaternion rotation = Quaternion.Euler(angles);
        return rotation * point;
    }

    string WrapToJsonArray(string json)
    {
        return "{\"Items\":" + json + "}";
    }

    private void PlaceObject()
    {
        Instantiate(objectToPlace, placementPose.position, placementPose.rotation);
    }

    private void UpdatePlacementIndicator()
    {
        if (placementPoseIsValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }

    private void UpdatePlacementPose()
    {
        var screenCenter = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        arRaycastManager.Raycast(screenCenter, hits, TrackableType.Planes);

        placementPoseIsValid = hits.Count > 0;
        if (placementPoseIsValid)
        {
            placementPose = hits[0].pose;

            var cameraForward = Camera.current.transform.forward;
            var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            placementPose.rotation = Quaternion.LookRotation(cameraBearing);
        }
    }
}

[System.Serializable]
public class LocationDataList
{
    public List<LocationData> Items;
}