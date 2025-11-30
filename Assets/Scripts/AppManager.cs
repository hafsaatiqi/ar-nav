using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;        // add this


public class AppManager : MonoBehaviour {
    public QRScanner qrScanner;
    public Pathfinding pathfinder;
    public MapManager mapManager;
    public ARNavigator arNavigator;
    // using TMPro;
    public TMP_Text statusText;
    public TMP_Dropdown destDropdown;
    public float proximityThreshold = 1.0f;

    private string currentNode;
    private List<string> currentPath;
    private int pathIndex = 0;
    private bool navigating = false;

    void Start() {
        // load graph into pathfinder
        if (mapManager.runtimeGraph != null) pathfinder.LoadGraph(mapManager.runtimeGraph);
        // subscribe to QR events
        if (qrScanner != null) qrScanner.OnQRScanned += OnQRScanned;
        PopulateDestinations();
    }

    void OnDestroy() {
        if (qrScanner != null) qrScanner.OnQRScanned -= OnQRScanned;
    }

    void OnQRScanned(string payload) {
        Debug.Log("AppManager got QR payload: " + payload);
        // assume payload is node id
        currentNode = payload.Trim();
        statusText.text = "Current: " + currentNode;
    }

    public void OnDestinationSelected() {
    string destNode = destDropdown.options[destDropdown.value].text;
    Debug.Log("[AppManager] OnDestinationSelected called. currentNode=" + currentNode + " destNode=" + destNode);

    if (string.IsNullOrEmpty(currentNode)) {
        statusText.text = "Scan a QR to set your current location first.";
        Debug.Log("[AppManager] No current node set (scan QR first).");
        return;
    }

    currentPath = pathfinder.FindPath(currentNode, destNode);
    if (currentPath == null) {
        statusText.text = "No path found.";
        Debug.Log("[AppManager] Pathfinder returned null for " + currentNode + " -> " + destNode);
        return;
    }

    Debug.Log("[AppManager] Path found: " + string.Join(" -> ", currentPath));
    pathIndex = 0;
    navigating = true;
    ShowCurrentWaypoint();
}


    void ShowCurrentWaypoint() {
        Debug.Log("[AppManager] ShowCurrentWaypoint() index=" + pathIndex);
        if (currentPath == null || pathIndex >= currentPath.Count) {
            statusText.text = "Navigation finished.";
            navigating = false;
            arNavigator.ClearArrow();
            return;
        }
        string nodeId = currentPath[pathIndex];
        var pixel = mapManager.NodePixel(nodeId);
        arNavigator.ShowWaypoint(pixel);
        statusText.text = $"Going to {nodeId} ({pathIndex+1}/{currentPath.Count})";
    }

    void Update() {
        if (!navigating) return;
        if (Camera.main == null) return;

        Vector3 camPos = Camera.main.transform.position;
        Vector3 waypointPos = arNavigator.CurrentWaypointPosition();
        if (waypointPos == Vector3.zero) return;

        float dist = Vector3.Distance(new Vector3(camPos.x,0,camPos.z), new Vector3(waypointPos.x,0,waypointPos.z));
        if (dist < proximityThreshold) {
            // advance
            pathIndex++;
            if (pathIndex < currentPath.Count) {
                // update current node for TTS etc
                currentNode = currentPath[pathIndex-1];
            }
            ShowCurrentWaypoint();
        }
    }

    void PopulateDestinations() {
    if (destDropdown == null || mapManager == null || mapManager.graph == null) return;
    destDropdown.ClearOptions();
    var labels = new List<string>();
    foreach(var p in mapManager.graph.pois) {
        // show "label (nodeId)" in the dropdown but keep nodeId text simple
        labels.Add(p.node); // we keep it node id for now
    }
    destDropdown.AddOptions(labels);
    }
}
