using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

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
    public GameObject trackPrefab;
    public string jsonFileName = "location";
    private List<LocationData> locationDataList;
    private int currentIndex = 0;
    public float updateInterval = 0.05f; // Interval in seconds

    [SerializeField]
    ARRaycastManager m_RaycastManager;
    List<ARRaycastHit> m_Hit = new List<ARRaycastHit>();
    public Camera arCam;
    private GameObject spawnedCar;
    private GameObject spawnedTrack;

    // Start is called before the first frame update
    void Start()
    {
        LoadLocationData();
    }

    void Update()
    {
        if (Input.touchCount == 0)
            return;
        
        Ray ray = arCam.ScreenPointToRay(Input.GetTouch(0).position);

        if (m_RaycastManager.Raycast(Input.GetTouch(0).position, m_Hit))
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began && spawnedCar == null && spawnedTrack == null)
            {
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.gameObject.tag == "Spawnable")
                    {
                        spawnedCar = hit.collider.gameObject;
                    }
                    else
                    {
                        SpawnCarAndTrack(m_Hit[0].pose.position);
                    }
                }
            }
            else if (Input.GetTouch(0).phase == TouchPhase.Moved && spawnedCar != null && spawnedTrack != null)
            {
                Vector3 newPosition = m_Hit[0].pose.position;
                spawnedCar.transform.position = newPosition;
                spawnedTrack.transform.position = newPosition;
            }
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
        if (currentIndex >= locationDataList.Count || spawnedCar == null) return;

        LocationData currentData = locationDataList[currentIndex];
        
        // Original position before rotation
        Vector3 originalPosition = new Vector3(currentData.x / 1500.0f, currentData.y / 1500.0f, currentData.z / 1500.0f);

        // Rotate position by 90 degrees around the x-axis
        Vector3 rotatedPosition = RotatePointAroundOrigin(originalPosition, new Vector3(90, 0, 0));

        // Calculate direction
        Vector3 direction = rotatedPosition - spawnedCar.transform.position;
        if (direction != Vector3.zero)
        {
            // Calculate rotation towards the direction while maintaining the 180-degree y-axis rotation because the car is pointed in the opp direction in the model
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion yRotation = Quaternion.Euler(0, 180, 0);
            spawnedCar.transform.rotation = toRotation * yRotation;
        }

        spawnedCar.transform.position = rotatedPosition;
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

    private void SpawnCarAndTrack(Vector3 spawnPosition)
    {
        spawnedCar = Instantiate(F1CarObject, spawnPosition, Quaternion.identity);
        spawnedTrack = Instantiate(trackPrefab, spawnPosition, Quaternion.identity);
        StartCoroutine(UpdateCarPositionRoutine());
    }
}

[System.Serializable]
public class LocationDataList
{
    public List<LocationData> Items;
}