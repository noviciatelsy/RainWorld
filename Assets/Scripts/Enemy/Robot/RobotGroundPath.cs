using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 机器人在同一高度（同一 cell 行）上的地面寻路，仅左右移动，不跨高度。
/// </summary>
public static class RobotGroundPath
{
    private static readonly Vector2Int[] HorizontalDirs =
    {
        Vector2Int.left,
        Vector2Int.right
    };

    private const float FeetYOffset = -0.45f;
    private const float BoundsMargin = 0.15f;

    public static bool IsFlatWalkable(TileMapGuideManager mgr, Vector2Int cell)
    {
        if (mgr == null)
        {
            return false;
        }

        return !mgr.IsSolid(cell) && mgr.IsSolid(cell + Vector2Int.down);
    }

    public static Vector2 CellToFeetWorld(TileMapGuideManager mgr, Vector2Int cell)
    {
        return mgr.CellToWorld(cell) + new Vector2(0f, FeetYOffset);
    }

    public static bool IsInsideBoundsXY(Bounds bounds, Vector2 point, float margin = BoundsMargin)
    {
        if (bounds.size.sqrMagnitude < 0.01f)
        {
            return true;
        }

        float minX = bounds.min.x - margin;
        float maxX = bounds.max.x + margin;
        float minY = bounds.min.y - margin;
        float maxY = bounds.max.y + margin;

        return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
    }

    /// <summary>
    /// 将世界坐标对齐到当前行上最近的可行走格子的脚底点。
    /// </summary>
    public static Vector2 SnapToFlatGround(Vector2 worldPos)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return worldPos;
        }

        Vector2Int startCell = mgr.WorldToCell(worldPos);
        Vector2Int walkable = ResolveWalkableCellOnRow(mgr, startCell);

        if (IsFlatWalkable(mgr, walkable))
        {
            return CellToFeetWorld(mgr, walkable);
        }

        return worldPos;
    }

    public static List<Vector2> FindFlatPath(Vector2 fromWorld, Vector2 toWorld, int maxSteps = 500)
    {
        List<Vector2> path = new List<Vector2>();
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return path;
        }

        Vector2Int startCell = ResolveWalkableCellOnRow(mgr, mgr.WorldToCell(fromWorld));
        Vector2Int endCell = ResolveTargetOnRow(mgr, startCell, toWorld);

        if (startCell == endCell)
        {
            return BuildDirectFlatTarget(fromWorld, toWorld);
        }

        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(startCell);
        parentMap.Add(startCell, startCell);

        bool found = false;
        int steps = 0;

        while (queue.Count > 0 && steps < maxSteps)
        {
            steps++;
            Vector2Int current = queue.Dequeue();

            if (current == endCell)
            {
                found = true;
                break;
            }

            EnqueueHorizontalNeighbors(mgr, startCell.y, current, parentMap, queue);
        }

        if (!found)
        {
            return path;
        }

        return ReconstructPath(mgr, parentMap, startCell, endCell);
    }

    /// <summary>
    /// 同层 BFS 失败时，沿当前行朝目标 X 方向冲刺（最多 12 格）。
    /// </summary>
    public static List<Vector2> FindFlatDashToward(Vector2 fromWorld, Vector2 toWorld)
    {
        List<Vector2> path = FindFlatPath(fromWorld, toWorld);

        if (path.Count > 0)
        {
            return path;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return path;
        }

        Vector2Int startCell = ResolveWalkableCellOnRow(mgr, mgr.WorldToCell(fromWorld));
        int dir = toWorld.x >= fromWorld.x ? 1 : -1;

        for (int step = 1; step <= 12; step++)
        {
            Vector2Int cell = new Vector2Int(startCell.x + dir * step, startCell.y);

            if (!IsFlatWalkable(mgr, cell))
            {
                break;
            }

            path.Add(CellToFeetWorld(mgr, cell));
        }

        return path;
    }

    /// <summary>
    /// 识别到玩家后用的冲刺路径：BFS → 同行冲刺 → 同行直线目标（保证至少有一个路点）。
    /// </summary>
    public static List<Vector2> BuildChargePath(Vector2 fromWorld, Vector2 toWorld)
    {
        List<Vector2> path = FindFlatPath(fromWorld, toWorld);

        if (path.Count > 0)
        {
            return path;
        }

        path = FindFlatDashToward(fromWorld, toWorld);

        if (path.Count > 0)
        {
            return path;
        }

        return BuildDirectFlatTarget(fromWorld, toWorld);
    }

    private static List<Vector2> BuildDirectFlatTarget(Vector2 fromWorld, Vector2 toWorld)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2 target = new Vector2(toWorld.x, fromWorld.y);

        if ((target - fromWorld).sqrMagnitude > 0.02f * 0.02f)
        {
            path.Add(target);
        }

        return path;
    }

    public static List<Vector2> FindRandomIdlePath(Vector2 fromWorld, Bounds idleBounds, int maxSteps = 500)
    {
        List<Vector2> path = new List<Vector2>();
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return BuildBackupPath(fromWorld, idleBounds);
        }

        Vector2Int startCell = ResolveWalkableCellOnRow(mgr, mgr.WorldToCell(fromWorld));
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        List<Vector2Int> reachableCells = new List<Vector2Int>();

        queue.Enqueue(startCell);
        parentMap.Add(startCell, startCell);

        int steps = 0;

        while (queue.Count > 0 && steps < maxSteps)
        {
            steps++;
            Vector2Int current = queue.Dequeue();
            Vector2 feetWorld = CellToFeetWorld(mgr, current);

            if (current != startCell
                && IsFlatWalkable(mgr, current)
                && IsInsideBoundsXY(idleBounds, feetWorld))
            {
                reachableCells.Add(current);
            }

            EnqueueHorizontalNeighbors(mgr, startCell.y, current, parentMap, queue);
        }

        if (reachableCells.Count > 0)
        {
            Vector2Int chosenEnd = reachableCells[Random.Range(0, reachableCells.Count)];
            return ReconstructPath(mgr, parentMap, startCell, chosenEnd);
        }

        return BuildBackupPath(fromWorld, idleBounds, mgr, startCell);
    }

    private static void EnqueueHorizontalNeighbors(
        TileMapGuideManager mgr,
        int rowY,
        Vector2Int current,
        Dictionary<Vector2Int, Vector2Int> parentMap,
        Queue<Vector2Int> queue)
    {
        foreach (Vector2Int dir in HorizontalDirs)
        {
            Vector2Int neighbor = current + dir;

            if (neighbor.y != rowY)
            {
                continue;
            }

            if (parentMap.ContainsKey(neighbor))
            {
                continue;
            }

            if (!IsFlatWalkable(mgr, neighbor))
            {
                continue;
            }

            parentMap.Add(neighbor, current);
            queue.Enqueue(neighbor);
        }
    }

    private static List<Vector2> ReconstructPath(
        TileMapGuideManager mgr,
        Dictionary<Vector2Int, Vector2Int> parentMap,
        Vector2Int startCell,
        Vector2Int endCell)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2Int backtrack = endCell;

        while (backtrack != startCell)
        {
            path.Insert(0, CellToFeetWorld(mgr, backtrack));
            backtrack = parentMap[backtrack];
        }

        return path;
    }

    private static List<Vector2> BuildBackupPath(Vector2 fromWorld, Bounds idleBounds)
    {
        return new List<Vector2>
        {
            new Vector2(
                fromWorld.x + Random.Range(-2f, 2f),
                fromWorld.y
            )
        };
    }

    private static List<Vector2> BuildBackupPath(
        Vector2 fromWorld,
        Bounds idleBounds,
        TileMapGuideManager mgr,
        Vector2Int startCell)
    {
        List<Vector2> path = new List<Vector2>();

        for (int attempt = 0; attempt < 8; attempt++)
        {
            int dir = Random.value > 0.5f ? 1 : -1;
            int distance = Random.Range(1, 4);

            for (int step = 1; step <= distance; step++)
            {
                Vector2Int cell = new Vector2Int(startCell.x + dir * step, startCell.y);

                if (!IsFlatWalkable(mgr, cell))
                {
                    break;
                }

                Vector2 feet = CellToFeetWorld(mgr, cell);

                if (!IsInsideBoundsXY(idleBounds, feet))
                {
                    break;
                }

                path.Add(feet);
            }

            if (path.Count > 0)
            {
                return path;
            }
        }

        return BuildBackupPath(fromWorld, idleBounds);
    }

    private static Vector2Int ResolveWalkableCellOnRow(TileMapGuideManager mgr, Vector2Int preferred)
    {
        if (IsFlatWalkable(mgr, preferred))
        {
            return preferred;
        }

        int row = preferred.y;

        for (int delta = 1; delta <= 8; delta++)
        {
            Vector2Int left = new Vector2Int(preferred.x - delta, row);

            if (IsFlatWalkable(mgr, left))
            {
                return left;
            }

            Vector2Int right = new Vector2Int(preferred.x + delta, row);

            if (IsFlatWalkable(mgr, right))
            {
                return right;
            }
        }

        return preferred;
    }

    private static Vector2Int ResolveTargetOnRow(
        TileMapGuideManager mgr,
        Vector2Int startCell,
        Vector2 goalWorld)
    {
        int row = startCell.y;
        Vector2Int preferred = new Vector2Int(mgr.WorldToCell(goalWorld).x, row);

        if (IsFlatWalkable(mgr, preferred))
        {
            return preferred;
        }

        for (int delta = 1; delta <= 24; delta++)
        {
            Vector2Int left = new Vector2Int(preferred.x - delta, row);

            if (IsFlatWalkable(mgr, left))
            {
                return left;
            }

            Vector2Int right = new Vector2Int(preferred.x + delta, row);

            if (IsFlatWalkable(mgr, right))
            {
                return right;
            }
        }

        return startCell;
    }
}
