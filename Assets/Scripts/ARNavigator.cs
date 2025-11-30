using UnityEngine;

public class ARNavigator : MonoBehaviour {
    public GameObject arrowPrefab;
    public MapManager mapManager;
    private GameObject currentArrow;

    public void ShowWaypoint(Vector2 pixelPos) {
        Vector3 world = mapManager.MapPixelToWorld(pixelPos);
        if(currentArrow) Destroy(currentArrow);
        currentArrow = Instantiate(arrowPrefab, world + Vector3.up*0.2f, Quaternion.identity);
        // point arrow toward camera
        if (Camera.main != null) currentArrow.transform.LookAt(Camera.main.transform);
    }

    public void ClearArrow() {
        if(currentArrow) Destroy(currentArrow);
    }

    public Vector3 CurrentWaypointPosition() {
        return currentArrow ? currentArrow.transform.position : Vector3.zero;
    }
}
