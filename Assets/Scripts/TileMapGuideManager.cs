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

    // =========================
    // 🔥 多边界结构
    // =========================
    private List<List<Edge>> edgeLoops = new();

    private List<int> nextCW = new();
    private List<int> nextCCW = new();

    private List<Edge> flatEdges = new(); // 用于AI查询

    void Awake()
    {
        Instance = this;
        Rebuild();
    }

    public void Rebuild()
    {
        Build();
    }

    void Build()
    {
        solid.Clear();
        edgeLoops.Clear();
        flatEdges.Clear();
        nextCW.Clear();
        nextCCW.Clear();

        BoundsInt bounds = new BoundsInt(tilemap.origin, tilemap.size);

        // =========================
        // 1. 收集 tile
        // =========================
        foreach (var p in bounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(p)) continue;
            solid.Add(new Vector2Int(p.x, p.y));
        }

        // =========================
        // 2. raw edges
        // =========================
        List<Edge> rawEdges = new();

        foreach (var cell in solid)
        {
            TryAddEdge(cell, Vector2Int.down, 0, rawEdges);
            TryAddEdge(cell, Vector2Int.right, 1, rawEdges);
            TryAddEdge(cell, Vector2Int.up, 2, rawEdges);
            TryAddEdge(cell, Vector2Int.left, 3, rawEdges);
        }

        // =========================
        // 3. 🔥 拆分多个连通 loop
        // =========================
        edgeLoops = BuildAllLoops(rawEdges);

        // =========================
        // 4. flatten + index
        // =========================
        int index = 0;

        foreach (var loop in edgeLoops)
        {
            foreach (var e in loop)
            {
                flatEdges.Add(e);

                nextCW.Add(index + 1);
                nextCCW.Add(index - 1);

                index++;
            }
        }

        // 修正闭环
        for (int i = 0; i < flatEdges.Count; i++)
        {
            nextCW[i] = (i + 1) % flatEdges.Count;
            nextCCW[i] = (i - 1 + flatEdges.Count) % flatEdges.Count;
        }
    }

    // =================================================
    // Edge 构建
    // =================================================
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

    // =================================================
    // 🔥 多 loop 构建核心
    // =================================================
    List<List<Edge>> BuildAllLoops(List<Edge> input)
    {
        List<List<Edge>> loops = new();
        HashSet<int> used = new();

        for (int i = 0; i < input.Count; i++)
        {
            if (used.Contains(i)) continue;

            List<Edge> loop = new();

            Edge current = input[i];
            loop.Add(current);
            used.Add(i);

            Vector2 start = current.a;
            Vector2 pivot = current.b;

            while (true)
            {
                bool found = false;

                for (int j = 0; j < input.Count; j++)
                {
                    if (used.Contains(j)) continue;

                    var candidate = input[j];

                    if (SamePoint(candidate.a, pivot))
                    {
                        current = candidate;
                    }
                    else if (SamePoint(candidate.b, pivot))
                    {
                        current = new Edge { a = candidate.b, b = candidate.a };
                    }
                    else continue;

                    loop.Add(current);
                    used.Add(j);

                    pivot = current.b;

                    if (SamePoint(pivot, start))
                        goto LOOP_END;

                    found = true;
                    break;
                }

                if (!found) break;
            }

        LOOP_END:
            loops.Add(loop);
        }

        return loops;
    }

    // =================================================
    // API
    // =================================================
    public Edge GetEdge(int index) => flatEdges[index];

    public int GetNextIndex(int index, bool clockwise)
        => clockwise ? nextCW[index] : nextCCW[index];

    public int FindClosestEdgeIndex(Vector2 pos)
    {
        float minDist = float.MaxValue;
        int best = 0;

        for (int i = 0; i < flatEdges.Count; i++)
        {
            float d = DistanceToSegment(pos, flatEdges[i].a, flatEdges[i].b);
            if (d < minDist)
            {
                minDist = d;
                best = i;
            }
        }

        return best;
    }

    public bool IsSolid(Vector2Int cell)
        => solid.Contains(cell);

    public Vector2 CellCorner(Vector2Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld((Vector3Int)cell);
        return center - new Vector3(0.5f, 0.5f);
    }

    bool SamePoint(Vector2 a, Vector2 b)
        => Vector2.Distance(a, b) < 0.01f;

    float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return Vector2.Distance(p, a + ab * t);
    }
}