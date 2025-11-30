using UnityEngine;

[ExecuteAlways]
public class NodeVisualizer : MonoBehaviour {
    public MapManager mapManager;
    public GameObject markerPrefab; // optional: small sphere prefab, else draw gizmos

    void OnDrawGizmos() {
        if (mapManager == null || mapManager.graph == null || mapManager.floorTexture == null) return;
        Gizmos.color = Color.green;
        foreach(var n in mapManager.graph.nodes) {
            Vector2 pix = new Vector2(n.x, n.y);
            Vector3 world = mapManager.MapPixelToWorld(pix);
            Gizmos.DrawSphere(world, 0.05f);
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(world + Vector3.up*0.1f, n.label + " (" + n.id + ")");
            #endif
        }
    }
}
