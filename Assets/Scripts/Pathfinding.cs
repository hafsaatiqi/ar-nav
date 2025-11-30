using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Node {
    public string id;
    public Vector2 pos;  // map pixels
}

[Serializable]
public class Edge {
    public string from, to;
    public float cost;
}

[Serializable]
public class Graph {
    public List<Node> nodes;
    public List<Edge> edges;
}

public class Pathfinding : MonoBehaviour {
    private Dictionary<string, Node> nodes = new Dictionary<string, Node>();
    private Dictionary<string, List<(string neighbor, float cost)>> adj = new Dictionary<string, List<(string,float)>>();

    public void LoadGraph(Graph graph) {
        nodes.Clear(); adj.Clear();
        foreach(var n in graph.nodes) {
            nodes[n.id] = n;
            adj[n.id] = new List<(string,float)>();
        }
        foreach(var e in graph.edges) {
            if (!adj.ContainsKey(e.from)) adj[e.from] = new List<(string,float)>();
            if (!adj.ContainsKey(e.to)) adj[e.to] = new List<(string,float)>();
            adj[e.from].Add((e.to,e.cost));
            adj[e.to].Add((e.from,e.cost)); // undirected
        }
    }

    public List<string> FindPath(string startId, string goalId) {
        var cameFrom = new Dictionary<string,string>();
        var gScore = new Dictionary<string,float>();
        var fScore = new Dictionary<string,float>();
        var open = new SimplePriorityQueue<string>();

        foreach(var k in nodes.Keys) {
            gScore[k] = float.PositiveInfinity;
            fScore[k] = float.PositiveInfinity;
        }
        gScore[startId] = 0;
        fScore[startId] = Heuristic(startId, goalId);
        open.Enqueue(startId, fScore[startId]);

        while(open.Count > 0) {
            var current = open.Dequeue();
            if(current == goalId) return ReconstructPath(cameFrom, current);

            if(!adj.ContainsKey(current)) continue;
            foreach(var (neighbor,cost) in adj[current]) {
                float tentative = gScore[current] + cost;
                if(!gScore.ContainsKey(neighbor) || tentative < gScore[neighbor]) {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative;
                    fScore[neighbor] = tentative + Heuristic(neighbor, goalId);
                    if(!open.Contains(neighbor)) open.Enqueue(neighbor, fScore[neighbor]);
                    else open.UpdatePriority(neighbor, fScore[neighbor]);
                }
            }
        }
        return null; // no path
    }

    private float Heuristic(string a, string b) {
        var pa = nodes[a].pos; var pb = nodes[b].pos;
        return Vector2.Distance(pa,pb);
    }

    private List<string> ReconstructPath(Dictionary<string,string> cameFrom, string current) {
        var total = new List<string>{current};
        while(cameFrom.ContainsKey(current)) {
            current = cameFrom[current];
            total.Insert(0,current);
        }
        return total;
    }
}
