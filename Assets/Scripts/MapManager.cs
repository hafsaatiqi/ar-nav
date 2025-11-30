using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

[Serializable]
public class GraphJson {
    public string floor;
    public string map_image;
    public float scale_m_per_pixel;
    public NodeJson[] nodes;
    public EdgeJson[] edges;
    public PoisJson[] pois;
}
[Serializable]
public class NodeJson { public string id; public float x; public float y; public string label; }
[Serializable]
public class EdgeJson { public string from; public string to; public float cost; }
[Serializable]
public class PoisJson { public string id; public string node; public string label; }

public class MapManager : MonoBehaviour {
    public GraphJson graph;
    public Texture2D floorTexture;
    public Graph runtimeGraph; // used by Pathfinding

    void Awake() {
        TextAsset ta = Resources.Load<TextAsset>("graph_floor1");
        if (ta == null) {
            Debug.LogError("graph_floor1.json not found in Resources!");
            return;
        }
        graph = JsonUtility.FromJson<GraphJson>(ta.text);
        floorTexture = Resources.Load<Texture2D>(graph.map_image.Replace(".png",""));
        BuildRuntimeGraph();
    }

    void BuildRuntimeGraph() {
        runtimeGraph = new Graph();
        runtimeGraph.nodes = graph.nodes.Select(n => new Node { id = n.id, pos = new Vector2(n.x, n.y) }).ToList();
        runtimeGraph.edges = graph.edges.Select(e => new Edge { from = e.from, to = e.to, cost = e.cost }).ToList();
    }

    public Vector3 MapPixelToWorld(Vector2 pixel) {
        // map pix center -> meters on X,Z plane
        float metersX = (pixel.x - floorTexture.width/2f) * graph.scale_m_per_pixel;
        float metersZ = (pixel.y - floorTexture.height/2f) * graph.scale_m_per_pixel;
        return new Vector3(metersX, 0, metersZ);
    }

    // helper: get node pixel
    public Vector2 NodePixel(string nodeId) {
        var nj = graph.nodes.FirstOrDefault(x => x.id == nodeId);
        if (nj == null) return Vector2.zero;
        return new Vector2(nj.x, nj.y);
    }
}
