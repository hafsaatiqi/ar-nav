using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

[System.Serializable]
public class SerializableVector3 {
    public float x, y, z;
    public SerializableVector3() {}
    public SerializableVector3(Vector3 v){ x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x,y,z);
}

[System.Serializable]
public class NodeEntry {
    public string id;
    public SerializableVector3 pos;
    public string label; // human label/description
}

[System.Serializable]
public class AnchorEntry {
    public string id;
    public SerializableVector3 pos;
    public string node; // mapped node ID
}

[System.Serializable]
public class MapExport {
    public List<NodeEntry> nodes = new List<NodeEntry>();
    public List<string[]> edges = new List<string[]>(); // optional: keep empty
    public List<AnchorEntry> anchors = new List<AnchorEntry>();
}

public class MapCreator : MonoBehaviour
{
    // --- Inspector fields ---
    [Header("AR / XR origin (drop your XR Origin / AR Session Origin here)")]
    public Transform arOrigin;                // drag XR Origin or AR Session Origin transform here
    [Header("Optional: fallback camera if origin doesn't contain one")]
    public Camera arCamera;                   // fallback: assign AR Camera if needed

    [Header("Prefabs & UI")]
    public GameObject nodePrefab;             // small sphere prefab (should contain a child TMP text for label)
    public GameObject qrPrefab;               // anchor prefab
    public TMP_Dropdown dropdown;             // node selector dropdown
    public TextMeshProUGUI statusText;        // optional status UI

    public TMP_InputField nodeLabelInput;     // input for human-readable label/description for node
    public TMP_InputField newNodeIdInput;     // input + button to add a new node ID on device

    [TextArea(3,10)]
    public string nodeIdCSV = "N0,N10,N14,N30,N33,N49,N52,N74,N84,N100,N108,N120,N130"; // fallback

    // flattening configuration
    public bool flattenY = true;
    public float floorY = 0f;

    // debug
    public bool debugLogCameraPosition = false;

    // --- internal storage ---
    private Dictionary<string, NodeEntry> nodeMap = new Dictionary<string, NodeEntry>();
    private Dictionary<string, AnchorEntry> anchorMap = new Dictionary<string, AnchorEntry>();
    private List<GameObject> visualNodes = new List<GameObject>();
    private List<GameObject> visualAnchors = new List<GameObject>();

    // file names
    private string jsonFileName = "map_floor1.json";
    private string nodesSavedFileName = "nodes_saved.txt";

    void Start()
    {
        // fallback camera assignment: try to find camera under arOrigin, else arCamera, else Camera.main
        if (arCamera == null && arOrigin != null)
        {
            Camera c = arOrigin.GetComponentInChildren<Camera>();
            if (c != null) arCamera = c;
        }
        if (arCamera == null) arCamera = Camera.main;

        PopulateDropdownFromCSV();
        UpdateStatus("Ready. Walk to a node and press Save Node.");
    }

    void Update()
    {
        if (debugLogCameraPosition && arCamera != null)
        {
            Vector3 p = arCamera.transform.position;
            Debug.Log($"[DEBUG] AR Camera world pos = {p.ToString("F4")}");
        }
    }

    // --- Dropdown population (reads StreamingAssets/nodes.txt or falls back to nodeIdCSV) ---
    void PopulateDropdownFromCSV()
    {
        StartCoroutine(PopulateDropdownCoroutine());
    }

    IEnumerator PopulateDropdownCoroutine()
    {
        dropdown.ClearOptions();

        string content = null;
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "nodes.txt");

        // Use UnityWebRequest because StreamingAssets path resolution differs by platform
        if (!string.IsNullOrEmpty(streamingPath))
        {
            using (UnityWebRequest www = UnityWebRequest.Get(streamingPath))
            {
                yield return www.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
#else
                if (www.isNetworkError || www.isHttpError)
#endif
                {
                    // ignore - fallback to CSV below
                    Debug.Log("nodes.txt not loaded from StreamingAssets, using inspector CSV fallback.");
                }
                else
                {
                    content = www.downloadHandler.text;
                }
            }
        }

        if (string.IsNullOrEmpty(content))
        {
            content = nodeIdCSV;
        }

        List<string> ids = new List<string>();
        if (content.Contains("\n") || content.Contains("\r"))
        {
            var lines = content.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var l in lines) ids.Add(l.Trim());
        }
        else // maybe comma separated
        {
            var parts = content.Split(',');
            foreach (var p in parts) ids.Add(p.Trim());
        }

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (var id in ids)
        {
            if (string.IsNullOrEmpty(id)) continue;
            options.Add(new TMP_Dropdown.OptionData(id));
            if (!nodeMap.ContainsKey(id))
                nodeMap[id] = new NodeEntry { id = id, pos = new SerializableVector3(Vector3.zero), label = id };
        }

        dropdown.AddOptions(options);
        UpdateStatus("Loaded " + options.Count + " node IDs.");
    }

    // helper to get the camera to read transform.position
    Camera GetARCamera()
    {
        if (arOrigin != null)
        {
            Camera c = arOrigin.GetComponentInChildren<Camera>();
            if (c != null) return c;
        }
        if (arCamera != null) return arCamera;
        return Camera.main;
    }

    // --- Save Node Position (with flattenY + optional label) ---
    public void SaveNodePosition()
    {
        Camera cam = GetARCamera();
        if (cam == null)
        {
            UpdateStatus("ERROR: AR Camera not found. Assign arOrigin or arCamera in inspector.");
            Debug.LogError("AR Camera not found. Assign arOrigin or arCamera in inspector.");
            return;
        }

        string id = dropdown.options[dropdown.value].text;
        Vector3 worldPos = cam.transform.position;

        if (flattenY)
        {
            worldPos.y = floorY;
        }

        string humanLabel = id;
        if (nodeLabelInput != null && !string.IsNullOrEmpty(nodeLabelInput.text))
            humanLabel = nodeLabelInput.text.Trim();

        NodeEntry e = new NodeEntry { id = id, pos = new SerializableVector3(worldPos), label = humanLabel };
        nodeMap[id] = e;

        // Clean previous visual if exists
        GameObject existing = visualNodes.Find(x => x.name == id);
        if (existing != null) { visualNodes.Remove(existing); Destroy(existing); }

        Transform parent = (arOrigin != null) ? arOrigin : this.transform;
        GameObject g = Instantiate(nodePrefab, worldPos, Quaternion.identity, parent);
        g.name = id;

        // set label text on prefab (try both 3D and UI TMP types)
        var tmp3 = g.GetComponentInChildren<TextMeshPro>();
        if (tmp3 != null) tmp3.text = humanLabel;
        else {
            var tmpUI = g.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpUI != null) tmpUI.text = humanLabel;
        }

        visualNodes.Add(g);

        UpdateStatus($"Saved Node {id} ({humanLabel}) @ {worldPos.ToString("F3")}");
        Debug.Log($"Node saved: {id} -> {worldPos} label:{humanLabel}");

        SaveNodeIdListToPersistent();
    }

    // --- Save QR Anchor (maps anchor.node to currently selected node id) ---
    public void SaveQRAnchorPosition()
    {
        Camera cam = GetARCamera();
        if (cam == null)
        {
            UpdateStatus("ERROR: AR Camera not found. Assign arOrigin or arCamera in inspector.");
            Debug.LogError("AR Camera not found. Assign arOrigin or arCamera in inspector.");
            return;
        }

        string nodeId = dropdown.options[dropdown.value].text;
        string anchorId = "QR_" + nodeId;

        Vector3 worldPos = cam.transform.position;
        if (flattenY) worldPos.y = floorY;

        AnchorEntry a = new AnchorEntry { id = anchorId, pos = new SerializableVector3(worldPos), node = nodeId };
        anchorMap[anchorId] = a;

        GameObject existing = visualAnchors.Find(x => x.name == anchorId);
        if (existing != null) { visualAnchors.Remove(existing); Destroy(existing); }

        Transform parent = (arOrigin != null) ? arOrigin : this.transform;
        GameObject g = Instantiate(qrPrefab, worldPos, Quaternion.identity, parent);
        g.name = anchorId;

        var tmp3 = g.GetComponentInChildren<TextMeshPro>();
        if (tmp3 != null) tmp3.text = anchorId;
        else {
            var tmpUI = g.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpUI != null) tmpUI.text = anchorId;
        }

        visualAnchors.Add(g);

        UpdateStatus($"Saved anchor {anchorId} (maps -> {nodeId}) at {worldPos.ToString("F3")}");
        Debug.Log($"Anchor saved: {anchorId} -> {worldPos} mapped to {nodeId}");
    }

    // --- Export JSON and also write nodes_saved.txt listing current node ids ---
    public void ExportJSON()
    {
        MapExport me = new MapExport();

        // EXPORT ALL nodes including placeholders (user requested placeholders remain)
        foreach (var kv in nodeMap)
        {
            me.nodes.Add(kv.Value);
        }

        foreach (var kv in anchorMap)
            me.anchors.Add(kv.Value);

        string json = JsonUtility.ToJson(me, true);
        string path = Path.Combine(Application.persistentDataPath, jsonFileName);
        File.WriteAllText(path, json);

        SaveNodeIdListToPersistent(); // also write nodes list

        UpdateStatus($"Exported JSON: {path}");
        Debug.Log("Saved map to: " + path);
    }

    // Write current node id list to persistentDataPath (so you can pull it back to PC)
    void SaveNodeIdListToPersistent()
    {
        string[] ids = new string[nodeMap.Count];
        nodeMap.Keys.CopyTo(ids, 0);
        string content = string.Join("\n", ids);
        string path = Path.Combine(Application.persistentDataPath, nodesSavedFileName);
        File.WriteAllText(path, content);
        Debug.Log("Saved node id list to: " + path);
    }

    // --- Add new node id at runtime (UI button hooks this) ---
    public void AddNewNodeIDFromInput()
    {
        if (newNodeIdInput == null) { UpdateStatus("No input field for new node id."); return; }
        string newId = newNodeIdInput.text.Trim();
        if (string.IsNullOrEmpty(newId)) { UpdateStatus("Please enter a new node ID."); return; }
        if (nodeMap.ContainsKey(newId)) { UpdateStatus("Node ID already exists: " + newId); return; }

        // add to dropdown
        var opts = dropdown.options;
        opts.Add(new TMP_Dropdown.OptionData(newId));
        dropdown.ClearOptions();
        dropdown.AddOptions(opts);

        nodeMap[newId] = new NodeEntry { id = newId, pos = new SerializableVector3(Vector3.zero), label = newId };
        UpdateStatus($"Added new node id: {newId}. Select it and press Save Node when at the point.");
        newNodeIdInput.text = "";

        SaveNodeIdListToPersistent();
    }

    void UpdateStatus(string s)
    {
        if (statusText != null) statusText.text = s;
        Debug.Log(s);
    }
}
