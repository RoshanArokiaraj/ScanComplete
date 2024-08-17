using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ARDataVisualizer : MonoBehaviour
{
    // URL of the Python backend
    private string serverUrl = "http://localhost:5000/process_scan";

    // Prefab to represent a single voxel in the AR scene
    public GameObject voxelPrefab;

    // Parent object to hold the generated voxels
    public Transform visualizationParent;

    // Size of each voxel in the AR scene
    private float voxelSize = 0.05f;

    // List to store instantiated voxel GameObjects
    private List<GameObject> instantiatedVoxels = new List<GameObject>();

    // Dictionary to map semantic labels to colors
    private Dictionary<int, Color> semanticColors;

    void Start()
    {
        // Define colors for each semantic label (example: label 0 is red, label 1 is green, etc.)
        semanticColors = new Dictionary<int, Color>
        {
            { 0, Color.red },
            { 1, Color.green },
            { 2, Color.blue },
            { 3, Color.yellow },
            // Add more labels as needed
        };

        // Start the coroutine to get scan data from the server and visualize it
        StartCoroutine(GetScanDataFromServer());
    }

    IEnumerator GetScanDataFromServer()
    {
        // Send a request to the Python backend
        UnityWebRequest www = UnityWebRequest.Post(serverUrl, "");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            // Parse JSON response
            string jsonResponse = www.downloadHandler.text;
            ScanDataResponse scanData = JsonConvert.DeserializeObject<ScanDataResponse>(jsonResponse);

            // Process and visualize the received scan data
            VisualizeScanData(scanData);
        }
    }

    private void VisualizeScanData(ScanDataResponse scanData)
    {
        // Clear any previously instantiated voxels
        foreach (GameObject voxel in instantiatedVoxels)
        {
            Destroy(voxel);
        }
        instantiatedVoxels.Clear();

        // Get the size of the completed scan
        int depth = scanData.completed_scan.Count;
        int height = scanData.completed_scan[0].Count;
        int width = scanData.completed_scan[0][0].Count;

        // Iterate over the voxel grid and instantiate voxels based on the data
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = scanData.completed_scan[z][y][x];
                    int semanticLabel = scanData.semantic_labels[z][y];

                    // If the voxel is filled (value > 0), create a visual representation
                    if (value > 0)
                    {
                        Vector3 position = new Vector3(x, y, z) * voxelSize;
                        GameObject voxel = Instantiate(voxelPrefab, position, Quaternion.identity, visualizationParent);

                        // Apply color based on the semantic label
                        if (semanticColors.ContainsKey(semanticLabel))
                        {
                            voxel.GetComponent<Renderer>().material.color = semanticColors[semanticLabel];
                        }
                        else
                        {
                            voxel.GetComponent<Renderer>().material.color = Color.white;  // Default color
                        }

                        instantiatedVoxels.Add(voxel);
                    }
                }
            }
        }
    }
}

// Class to represent the structure of the response from the Python backend
public class ScanDataResponse
{
    public List<List<List<float>>> completed_scan;
    public List<List<int>> semantic_labels;
}
