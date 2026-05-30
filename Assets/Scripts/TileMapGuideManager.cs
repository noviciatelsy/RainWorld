using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;



public class TileMapGuideManager : MonoBehaviour
{
    public static TileMapGuideManager Instance;

    public Tilemap tilemap;

    private HashSet<Vector2Int> air = new();     // 可飞行空间
    private HashSet<Vector2Int> ground = new();  // 可站立表面
    private HashSet<Vector2Int> solid = new();   // 你已有（墙）
    private Vector2 cellSize;

    // =========================
    // 核心结构
    // =========================
    private BoundsInt bounds;

    private List<List<Edge>> edgeLoops = new();
    private List<Edge> flatEdges = new();
    // 每条 edge 属于哪个 loop
    private Dictionary<int, int> edgeToLoop = new();
    // 每个 loop 内的 next
    private Dictionary<int, int> nextCW = new();
    private Dictionary<int, int> nextCCW = new();

    //点
    // 替换原来的 DIRS
    private static readonly Vector2Int[] DIRS = new Vector2Int[]
    {
    new Vector2Int(1,0),
    new Vector2Int(-1,0),
    new Vector2Int(0,1),
    new Vector2Int(0,-1),

    new Vector2Int(1,1),
    new Vector2Int(1,-1),
    new Vector2Int(-1,1),
    new Vector2Int(-1,-1)
    };

    void Awake()
    {
        Instance = this;
        Rebuild();
    }

    public void Rebuild()
    {
        Build();
    }

    // =================================================
    // BUILD
    // =================================================
    void Build()
    {
        solid.Clear();
        air.Clear();
        ground.Clear();
        edgeLoops.Clear();
        flatEdges.Clear();
        edgeToLoop.Clear();
        nextCW.Clear();
        nextCCW.Clear();

        bounds = new BoundsInt(tilemap.origin, tilemap.size);
        cellSize = tilemap.cellSize;
        // =========================
        // 1. 收集 tile
        // =========================
        foreach (var p in bounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(p)) air.Add(new Vector2Int(p.x, p.y)); 
            else solid.Add(new Vector2Int(p.x, p.y));
        }

        foreach (Vector2Int cell in air)
        {
            Vector2Int below = cell + Vector2Int.down;

            if (IsSolid(below))
            {
                ground.Add(cell);
            }
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
        // 3. split loops
        // =========================
        edgeLoops = BuildAllLoops(rawEdges);

        // =========================
        // 4. build per-loop structure（关键修复）
        // =========================
        int globalIndex = 0;

        for (int loopId = 0; loopId < edgeLoops.Count; loopId++)
        {
            var loop = edgeLoops[loopId];

            int count = loop.Count;

            for (int i = 0; i < count; i++)
            {
                Edge e = loop[i];
                e.loopId = loopId;

                flatEdges.Add(e);

                edgeToLoop[globalIndex] = loopId;

                globalIndex++;
            }
        }

        // =========================
        // 5. per-loop next（关键修复）
        // =========================
        int index = 0;

        for (int loopId = 0; loopId < edgeLoops.Count; loopId++)
        {
            var loop = edgeLoops[loopId];

            int start = index;

            for (int i = 0; i < loop.Count; i++)
            {
                int current = start + i;
                int next = start + (i + 1) % loop.Count;
                int prev = start + (i - 1 + loop.Count) % loop.Count;

                nextCW[current] = next;
                nextCCW[current] = prev;
            }

            index += loop.Count;
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

        Vector2 right = new Vector2(cellSize.x, 0);
        Vector2 up = new Vector2(0, cellSize.y);

        switch (type)
        {
            case 0:
                e.a = c;
                e.b = c + right;
                break;

            case 1:
                e.a = c + right;
                e.b = c + right + up;
                break;

            case 2:
                e.a = c + right + up;
                e.b = c + up;
                break;

            case 3:
                e.a = c + up;
                e.b = c;
                break;
        }

        list.Add(e);
    }

    // =================================================
    // LOOP 构建（修复稳定版）
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

                    var c = input[j];

                    if (SamePoint(c.a, pivot))
                    {
                        current = c;
                    }
                    else if (SamePoint(c.b, pivot))
                    {
                        current = new Edge { a = c.b, b = c.a };
                    }
                    else continue;

                    loop.Add(current);
                    used.Add(j);

                    pivot = current.b;

                    if (SamePoint(pivot, start))
                        goto END;

                    found = true;
                    break;
                }

                if (!found) break;
            }

        END:
            loops.Add(loop);
        }

        return loops;
    }

    // =================================================
    // API（关键修复点）
    // =================================================

    public Edge GetEdge(int index) => flatEdges[index];

    public int GetNextIndex(int index, bool clockwise)
    {
        return clockwise ? nextCW[index] : nextCCW[index];
    }

    public int FindClosestEdgeIndex(Vector2 pos)
    {
        float min = float.MaxValue;
        int best = 0;

        for (int i = 0; i < flatEdges.Count; i++)
        {
            float d = DistanceToSegment(pos, flatEdges[i].a, flatEdges[i].b);
            if (d < min)
            {
                min = d;
                best = i;
            }
        }

        return best;
    }

    public bool IsSolid(Vector2Int cell) => solid.Contains(cell);

    public bool IsGroundAirCell(Vector2Int cell) => ground.Contains(cell);

    /// <summary>
    /// 获取 solid 格子顶边世界坐标（底角 + 格子高度）。
    /// </summary>
    public Vector2 GetSolidCellTop(Vector2Int solidCell)
    {
        return CellCorner(solidCell) + new Vector2(0f, cellSize.y);
    }

    /// <summary>
    /// 从世界坐标查找可站立地面顶面（air 格 + 下方 solid）。
    /// 在同一列中选与 worldHint 高度最接近的地面，避免误吸到远处层。
    /// </summary>
    public bool TryGetFloorTop(Vector2 worldHint, out Vector2 standPoint, float surfaceOffset = 0.08f)
    {
        standPoint = default;
        Vector2Int baseCell = WorldToCell(worldHint);
        bool found = false;
        float bestHeightDelta = float.MaxValue;

        for (int dy = -6; dy <= 8; dy++)
        {
            Vector2Int airCell = baseCell + new Vector2Int(0, dy);
            Vector2Int solidBelow = airCell + Vector2Int.down;

            if (!InBounds(airCell) || !InBounds(solidBelow))
            {
                continue;
            }

            if (!IsSolid(airCell) && IsSolid(solidBelow))
            {
                float floorTopY = GetSolidCellTop(solidBelow).y;
                float heightDelta = Mathf.Abs(floorTopY - worldHint.y);

                if (heightDelta < bestHeightDelta)
                {
                    bestHeightDelta = heightDelta;
                    standPoint = new Vector2(worldHint.x, floorTopY + surfaceOffset);
                    found = true;
                }
            }
        }

        if (found)
        {
            return true;
        }

        if (InBounds(baseCell) && IsSolid(baseCell))
        {
            standPoint = new Vector2(
                worldHint.x,
                GetSolidCellTop(baseCell).y + surfaceOffset
            );
            return true;
        }

        return false;
    }

    /// <summary>
    /// 根据边两侧 solid/air 关系，计算角色站立点（避免底边法线朝下导致陷入地板）。
    /// </summary>
    public bool TryGetStandPointOnEdge(
        Edge edge,
        Vector2 worldHint,
        float surfaceOffset,
        out Vector2 standPoint,
        out Vector2 normal)
    {
        standPoint = default;
        normal = Vector2.up;

        Vector2 pointOnEdge = ClosestPointOnSegment(worldHint, edge.a, edge.b);
        Vector2 mid = (edge.a + edge.b) * 0.5f;
        const float probe = 0.08f;

        Vector2Int cellAbove = WorldToCell(mid + Vector2.up * probe);
        Vector2Int cellBelow = WorldToCell(mid + Vector2.down * probe);
        bool solidAbove = InBounds(cellAbove) && IsSolid(cellAbove);
        bool solidBelow = InBounds(cellBelow) && IsSolid(cellBelow);

        // 底边：solid 在边上侧、空气在下侧 → 应站在 solid 顶面，不能用朝下的空气法线偏移
        if (solidAbove && !solidBelow)
        {
            standPoint = new Vector2(
                worldHint.x,
                GetSolidCellTop(cellAbove).y + surfaceOffset
            );
            normal = Vector2.up;
            return true;
        }

        // 顶边：solid 在下、空气在上
        if (solidBelow && !solidAbove)
        {
            Vector2 airNormal = GetEdgeAirNormal(edge);

            if (airNormal.y < 0.2f)
            {
                airNormal = Vector2.up;
            }

            standPoint = pointOnEdge + airNormal * surfaceOffset;
            normal = airNormal;
            return true;
        }

        // 竖边 / 兜底：用空气法线，但禁止朝下
        Vector2 fallbackNormal = GetEdgeAirNormal(edge);

        if (fallbackNormal.y < 0.2f)
        {
            if (TryGetFloorTop(worldHint, out Vector2 floorPoint, surfaceOffset))
            {
                standPoint = floorPoint;
                normal = Vector2.up;
                return true;
            }

            fallbackNormal = Vector2.up;
        }

        standPoint = pointOnEdge + fallbackNormal * surfaceOffset;
        normal = fallbackNormal;
        return true;
    }

    public bool TryGetStandPointOnEdge(
        int edgeIndex,
        Vector2 worldHint,
        float surfaceOffset,
        out Vector2 standPoint,
        out Vector2 normal)
    {
        return TryGetStandPointOnEdge(flatEdges[edgeIndex], worldHint, surfaceOffset, out standPoint, out normal);
    }

    static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;

        if (ab.sqrMagnitude < 0.0001f)
        {
            return a;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
        return a + ab * t;
    }

    /// <summary>
    /// 返回边朝向空气一侧的单位法线。
    /// 注意：实心块「底边」(type0) 的法线向下，贴地移动请用 TryGetFloorTop / TryGetStandPointOnEdge。
    /// </summary>
    public Vector2 GetEdgeAirNormal(Edge edge)
    {
        Vector2 tangent = (edge.b - edge.a).normalized;

        if (tangent.sqrMagnitude < 0.0001f)
        {
            return Vector2.up;
        }

        Vector2 normalA = new Vector2(-tangent.y, tangent.x);
        Vector2 normalB = -normalA;
        Vector2 mid = (edge.a + edge.b) * 0.5f;
        const float probe = 0.06f;

        bool airA = !IsSolid(WorldToCell(mid + normalA * probe));
        bool airB = !IsSolid(WorldToCell(mid + normalB * probe));

        if (airA && !airB)
        {
            return normalA;
        }

        if (airB && !airA)
        {
            return normalB;
        }

        return normalA.y >= normalB.y ? normalA : normalB;
    }

    public Vector2 GetEdgeAirNormal(int edgeIndex)
    {
        return GetEdgeAirNormal(flatEdges[edgeIndex]);
    }

    public int GetEdgeCount() => flatEdges.Count;

    public Vector2 CellCorner(Vector2Int cell)
    {
        Vector3 c = tilemap.GetCellCenterWorld((Vector3Int)cell);
        return c - new Vector3(
            cellSize.x * 0.5f,
            cellSize.y * 0.5f
        );
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

    public List<Vector2> FindPath(Vector2 start, Vector2 end)
    {

        Vector2Int startCell = WorldToCell(start);
        Vector2Int endCell = WorldToCell(end);
        // 必加
        if (!InBounds(startCell) || !InBounds(endCell))
            return null;

        var open = new List<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        var gScore = new Dictionary<Vector2Int, float>();
        var fScore = new Dictionary<Vector2Int, float>();

        open.Add(startCell);
        gScore[startCell] = 0;
        fScore[startCell] = Heuristic(startCell, endCell);

        int maxIterations = 2000;
        int iter = 0;

        while (open.Count > 0)
        {
            iter++;
            if (iter > maxIterations)
            {
                Debug.LogWarning("findpathfailed");
                return null;
            }

            Vector2Int current = GetLowestF(open, fScore);

            if (current == endCell)
                return Reconstruct(cameFrom, current);

            open.Remove(current);

            int maxSearchDist = 50; // 可调（非常关键）
            foreach (var dir in DIRS)
            {
                Vector2Int neighbor = current + dir;

                if (!InBounds(neighbor)) continue;
                if (IsSolid(neighbor) ) continue;
                if (Mathf.Abs(neighbor.x - startCell.x) > maxSearchDist ||
                    Mathf.Abs(neighbor.y - startCell.y) > maxSearchDist)
                    continue;

                float cost = (dir.x != 0 && dir.y != 0) ? 1.4142f : 1f;
                float tentativeG = gScore[current] + cost;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, endCell);

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }

        return null;
    }

    public Vector2Int WorldToCell(Vector2 pos)
    {
        Vector3Int cell = tilemap.WorldToCell(pos);
        return new Vector2Int(cell.x, cell.y);
    }

    float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Vector2Int.Distance(a, b);
    }

    Vector2Int GetLowestF(List<Vector2Int> open, Dictionary<Vector2Int, float> fScore)
    {
        Vector2Int best = open[0];
        float bestScore = float.MaxValue;

        foreach (var n in open)
        {
            float score = fScore.ContainsKey(n) ? fScore[n] : float.MaxValue;
            if (score < bestScore)
            {
                bestScore = score;
                best = n;
            }
        }

        return best;
    }

    List<Vector2> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> cellPath = new();

        cellPath.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            cellPath.Add(current);
        }

        cellPath.Reverse();

        List<Vector2> result = new();

        Vector2 lastPoint = CellToWorld(cellPath[0]);
        result.Add(lastPoint);

        for (int i = 1; i < cellPath.Count; i++)
        {
            Vector2 world = CellToWorld(cellPath[i]);

            if (HasLineOfSight(lastPoint, world))
            {
                // 如果是最后一个点，必须加入
                if (i == cellPath.Count - 1)
                {
                    result.Add(world);
                }
                continue;
            }
            else
            {
                Vector2 prev = CellToWorld(cellPath[i - 1]);
                result.Add(prev);
                lastPoint = prev;
            }
        }

        return result;
    }

    bool HasLineOfSight(Vector2 a, Vector2 b)
    {
        float dist = Vector2.Distance(a, b);
        int steps = Mathf.CeilToInt(dist / 0.2f);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 p = Vector2.Lerp(a, b, t);

            Vector2Int cell = WorldToCell(p);

            if (!InBounds(cell)) return false;
            if (IsSolid(cell))
                return false;
        }

        return true;
    }

    public Vector2 CellToWorld(Vector2Int cell)
    {
        return tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
    }

    bool InBounds(Vector2Int cell)
    {
        return cell.x >= bounds.xMin &&
               cell.x < bounds.xMax &&
               cell.y >= bounds.yMin &&
               cell.y < bounds.yMax;
    }

    void OnDrawGizmos()
    {
        if (flatEdges == null) return;

        Gizmos.color = Color.green;

        foreach (var e in flatEdges)
        {
            Vector3 a = e.a;
            Vector3 b = e.b;

            Gizmos.DrawLine(a, b);

            // 起点
            Gizmos.DrawSphere(a, 0.03f);

            // 方向箭头
            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = (b - a).normalized;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(mid, mid + dir * 0.2f);

            Gizmos.color = Color.green;
        }
    }
}