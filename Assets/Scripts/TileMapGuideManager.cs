using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct Edge
{
    public Vector2 a;
    public Vector2 b;

    public Vector2 Dir => (b - a).normalized;
}

public class TileMapGuideManager : MonoBehaviour
{
    public static TileMapGuideManager Instance;

    public Tilemap tilemap;

    private HashSet<Vector2Int> solid = new();

    private List<Edge> edges = new();

    // 👉 O(1)：edge index 邻接
    private List<int> nextCW = new();
    private List<int> nextCCW = new();

    void Awake()
    {
        Instance = this;
        Build();
    }

    void Build()
    {
        solid.Clear();
        edges.Clear();
        nextCW.Clear();
        nextCCW.Clear();

        BoundsInt b = tilemap.cellBounds;

        foreach (var p in b.allPositionsWithin)
        {
            if (!tilemap.HasTile(p)) continue;
            solid.Add(new Vector2Int(p.x, p.y));
        }

        List<Edge> rawEdges = new();

        foreach (var cell in solid)
        {
            TryAddEdge(cell, Vector2Int.down, 0, rawEdges);
            TryAddEdge(cell, Vector2Int.right, 1, rawEdges);
            TryAddEdge(cell, Vector2Int.up, 2, rawEdges);
            TryAddEdge(cell, Vector2Int.left, 3, rawEdges);
        }

        edges = BuildOrderedLoop(rawEdges);

        // 👉 构建 O(1) 邻接
        int count = edges.Count;

        for (int i = 0; i < count; i++)
        {
            nextCW.Add((i + 1) % count);
            nextCCW.Add((i - 1 + count) % count);
        }
    }

    void TryAddEdge(Vector2Int cell, Vector2Int dir, int type, List<Edge> list)
    {
        if (IsSolid(cell + dir)) return;

        Vector2 c = CellCorner(cell);

        Edge e = new();

        switch (type)
        {
            case 0: e.a = c; e.b = c + Vector2.right; break;
            case 1: e.a = c + Vector2.right; e.b = c + Vector2.right + Vector2.up; break;
            case 2: e.a = c + Vector2.right + Vector2.up; e.b = c + Vector2.up; break;
            case 3: e.a = c + Vector2.up; e.b = c; break;
        }

        list.Add(e);
    }

    List<Edge> BuildOrderedLoop(List<Edge> input)
    {
        List<Edge> result = new();
        if (input.Count == 0) return result;

        HashSet<int> used = new();

        Edge current = input[0];
        result.Add(current);
        used.Add(0);

        while (true)
        {
            Vector2 pivot = current.b;

            bool found = false;

            for (int i = 0; i < input.Count; i++)
            {
                if (used.Contains(i)) continue;

                var candidate = input[i];

                if (SamePoint(candidate.a, pivot))
                {
                    current = candidate;
                }
                else if (SamePoint(candidate.b, pivot))
                {
                    current = new Edge { a = candidate.b, b = candidate.a };
                }
                else continue;

                result.Add(current);
                used.Add(i);
                found = true;
                break;
            }

            if (!found) break;
        }

        return result;
    }

    // =================================================
    // O(1) API
    // =================================================
    public Edge GetEdge(int index) => edges[index];

    public int GetNextIndex(int index, bool clockwise)
    {
        return clockwise ? nextCW[index] : nextCCW[index];
    }

    public int FindClosestEdgeIndex(Vector2 pos)
    {
        float minDist = float.MaxValue;
        int best = 0;

        for (int i = 0; i < edges.Count; i++)
        {
            float d = DistanceToSegment(pos, edges[i].a, edges[i].b);
            if (d < minDist)
            {
                minDist = d;
                best = i;
            }
        }

        return best;
    }

    public bool IsSolid(Vector2Int cell)
    {
        return solid.Contains(cell);
    }

    public Vector2 CellCorner(Vector2Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld((Vector3Int)cell);
        return center - new Vector3(0.5f, 0.5f);
    }

    bool SamePoint(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b) < 0.01f;
    }

    float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return Vector2.Distance(p, a + ab * t);
    }

    void OnDrawGizmos()
    {
        if (edges == null) return;

        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];

            Gizmos.color = Color.green;
            Gizmos.DrawLine(e.a, e.b);

            Gizmos.DrawSphere(e.a, 0.04f);

            Vector2 mid = (e.a + e.b) * 0.5f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(mid, mid + e.Dir * 0.3f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(mid, i.ToString());
#endif
        }
    }
}