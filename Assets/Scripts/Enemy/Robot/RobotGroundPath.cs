using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    private const float BoundsMargin = 0.15f;

    /// <summary>鼹鼠等沿边单位脚底偏移（格子中心向下）。</summary>
    public const float DefaultFeetYOffset = -0.45f;

    private const float PlatformStandTolerance = 0.32f;
    private const float SurfaceStepHeightFactor = 1.15f;

    private static Tilemap cachedPlatformTilemap;

    public struct RobotSurfaceSupport
    {
        public bool IsValid;
        public bool IsPlatform;
        public float FeetY;
        public int RowY;
        public Vector3Int PlatformCell;
    }

    public static bool TryResolveSurfaceSupport(
        Vector2 worldPos,
        float feetYOffset,
        out RobotSurfaceSupport support)
    {
        support = default;

        bool hasGround = TryGetFlatRowCell(worldPos, feetYOffset, out int groundRowY, out _);
        float groundFeetY = hasGround ? GetRowFeetY(groundRowY, feetYOffset) : float.PositiveInfinity;
        float groundDelta = hasGround ? Mathf.Abs(worldPos.y - groundFeetY) : float.PositiveInfinity;

        bool hasPlatform = TryGetPlatformSupport(worldPos, feetYOffset, out Vector3Int platformCell, out float platformFeetY);
        float platformDelta = hasPlatform ? Mathf.Abs(worldPos.y - platformFeetY) : float.PositiveInfinity;

        if (!hasGround && !hasPlatform)
        {
            return false;
        }

        bool usePlatform = hasPlatform
            && (!hasGround || platformDelta + 0.01f < groundDelta);

        if (usePlatform)
        {
            TileMapGuideManager mgr = TileMapGuideManager.Instance;
            support = new RobotSurfaceSupport
            {
                IsValid = true,
                IsPlatform = true,
                FeetY = platformFeetY,
                RowY = mgr != null
                    ? mgr.WorldToCell(new Vector2(worldPos.x, platformFeetY)).y
                    : platformCell.y,
                PlatformCell = platformCell
            };
            return true;
        }

        support = new RobotSurfaceSupport
        {
            IsValid = true,
            IsPlatform = false,
            FeetY = groundFeetY,
            RowY = groundRowY
        };
        return true;
    }

    public static bool TryQueryStandPoint(
        Vector2 worldPos,
        float feetYOffset,
        float referenceFeetY,
        out RobotSurfaceSupport support)
    {
        support = default;

        if (!TryResolveSurfaceSupport(worldPos, feetYOffset, out support))
        {
            return false;
        }

        float maxStep = GetSurfaceStepHeight();
        return Mathf.Abs(support.FeetY - referenceFeetY) <= maxStep + 0.001f;
    }

    public static float ProbeSurfaceDistance(
        Vector2 fromWorld,
        RobotSurfaceSupport startSupport,
        int dirSign,
        float maxDistance,
        float feetYOffset)
    {
        if (dirSign == 0 || maxDistance <= 0f || !startSupport.IsValid)
        {
            return 0f;
        }

        float step = ResolveProbeStep();
        float lastValid = 0f;
        float currentFeetY = startSupport.FeetY;

        for (float distance = step; distance <= maxDistance + step * 0.5f; distance += step)
        {
            float testX = fromWorld.x + dirSign * distance;
            Vector2 testPos = new Vector2(testX, currentFeetY);

            if (!TryQueryStandPoint(testPos, feetYOffset, currentFeetY, out RobotSurfaceSupport stand))
            {
                break;
            }

            currentFeetY = stand.FeetY;
            lastValid = Mathf.Min(distance, maxDistance);
        }

        return lastValid;
    }

    public static bool TryGetPlatformSupport(
        Vector2 worldPos,
        float feetYOffset,
        out Vector3Int platformCell,
        out float feetY)
    {
        platformCell = default;
        feetY = worldPos.y;

        Tilemap platformTilemap = GetPlatformTilemap();

        if (platformTilemap == null)
        {
            return false;
        }

        Vector3Int baseCell = platformTilemap.WorldToCell(worldPos);
        bool found = false;
        float bestDelta = float.MaxValue;
        Vector3Int bestCell = default;
        float bestFeetY = worldPos.y;

        for (int dy = -4; dy <= 3; dy++)
        {
            Vector3Int cell = baseCell + new Vector3Int(0, dy, 0);

            if (!platformTilemap.HasTile(cell))
            {
                continue;
            }

            float candidateFeetY = PlatformCellToFeetWorld(platformTilemap, cell, feetYOffset).y;
            float delta = Mathf.Abs(worldPos.y - candidateFeetY);

            if (delta >= bestDelta)
            {
                continue;
            }

            bestDelta = delta;
            bestCell = cell;
            bestFeetY = candidateFeetY;
            found = true;
        }

        if (!found || bestDelta > PlatformStandTolerance)
        {
            return false;
        }

        platformCell = bestCell;
        feetY = bestFeetY;
        return true;
    }

    public static Vector2 PlatformCellToFeetWorld(Tilemap platformTilemap, Vector3Int cell, float feetYOffset)
    {
        Vector3Int standCell = cell + Vector3Int.up;
        Vector3 standCenter = platformTilemap.GetCellCenterWorld(standCell);
        return new Vector2(standCenter.x, standCenter.y + feetYOffset);
    }

    public static Tilemap GetPlatformTilemap()
    {
        if (cachedPlatformTilemap != null)
        {
            return cachedPlatformTilemap;
        }

        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];

            if (tilemap != null && tilemap.gameObject.name == "Tilemap_Platform")
            {
                cachedPlatformTilemap = tilemap;
                return cachedPlatformTilemap;
            }
        }

        int platformLayer = LayerMask.NameToLayer("Platform");

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];

            if (tilemap != null && platformLayer >= 0 && tilemap.gameObject.layer == platformLayer)
            {
                cachedPlatformTilemap = tilemap;
                return cachedPlatformTilemap;
            }
        }

        return null;
    }

    public static float GetSurfaceStepHeight()
    {
        Tilemap platformTilemap = GetPlatformTilemap();

        if (platformTilemap != null)
        {
            return platformTilemap.cellSize.y * SurfaceStepHeightFactor;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr != null)
        {
            return mgr.CellToWorld(Vector2Int.one).y - mgr.CellToWorld(Vector2Int.zero).y;
        }

        return 0.575f;
    }

    // 兼容旧接口
    public static bool TryResolveGroundSupport(
        Vector2 worldPos,
        float feetYOffset,
        LayerMask _,
        out int rowY,
        out float feetY,
        out bool onPlatform)
    {
        onPlatform = false;
        rowY = 0;
        feetY = worldPos.y;

        if (!TryResolveSurfaceSupport(worldPos, feetYOffset, out RobotSurfaceSupport support))
        {
            return false;
        }

        rowY = support.RowY;
        feetY = support.FeetY;
        onPlatform = support.IsPlatform;
        return true;
    }

    public static bool CanStandAtX(
        float worldX,
        int rowY,
        float feetY,
        bool onPlatform,
        float feetYOffset,
        LayerMask _)
    {
        return TryQueryStandPoint(
            new Vector2(worldX, feetY),
            feetYOffset,
            feetY,
            out RobotSurfaceSupport _);
    }

    public static float ProbeSupportDistance(
        Vector2 fromWorld,
        int rowY,
        float feetY,
        bool onPlatform,
        int dirSign,
        float maxDistance,
        float feetYOffset,
        LayerMask _)
    {
        RobotSurfaceSupport support = new RobotSurfaceSupport
        {
            IsValid = true,
            IsPlatform = onPlatform,
            FeetY = feetY,
            RowY = rowY
        };

        return ProbeSurfaceDistance(fromWorld, support, dirSign, maxDistance, feetYOffset);
    }

    private static float ResolveProbeStep()
    {
        Tilemap platformTilemap = GetPlatformTilemap();

        if (platformTilemap != null)
        {
            return Mathf.Clamp(platformTilemap.cellSize.x * 0.5f, 0.2f, 1f);
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return 0.25f;
        }

        return Mathf.Clamp(
            mgr.CellToWorld(Vector2Int.one).x - mgr.CellToWorld(Vector2Int.zero).x,
            0.2f,
            1f) * 0.5f;
    }

    public static bool IsFlatWalkable(TileMapGuideManager mgr, Vector2Int cell)
    {
        if (mgr == null)
        {
            return false;
        }

        return !mgr.IsSolid(cell) && mgr.IsSolid(cell + Vector2Int.down);
    }

    public static Vector2 CellToFeetWorld(
        TileMapGuideManager mgr,
        Vector2Int cell,
        float feetYOffset = DefaultFeetYOffset)
    {
        return mgr.CellToWorld(cell) + new Vector2(0f, feetYOffset);
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

    public static bool IsInsideBoundsX(Bounds bounds, Vector2 point, float margin = BoundsMargin)
    {
        if (bounds.size.sqrMagnitude < 0.01f)
        {
            return true;
        }

        float minX = bounds.min.x - margin;
        float maxX = bounds.max.x + margin;
        return point.x >= minX && point.x <= maxX;
    }

    /// <summary>
    /// 解析当前位置所在的可行走平地格，并锁定行 Y（cell.y）。
    /// </summary>
    public static bool TryGetFlatRowCell(
        Vector2 worldPos,
        float feetYOffset,
        out int rowY,
        out Vector2Int cell)
    {
        rowY = 0;
        cell = default;

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return false;
        }

        cell = ResolveWalkableCell(mgr, mgr.WorldToCell(worldPos));

        if (!IsFlatWalkable(mgr, cell))
        {
            return false;
        }

        rowY = cell.y;
        return true;
    }

    public static float GetRowFeetY(int rowY, float feetYOffset = DefaultFeetYOffset)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return 0f;
        }

        return CellToFeetWorld(mgr, new Vector2Int(0, rowY), feetYOffset).y;
    }

    /// <summary>
    /// 在已锁定的行上，判断 worldX 处是否仍可站立（不接受跨行/高度差）。
    /// </summary>
    public static bool CanStandOnRowAtX(int rowY, float worldX, float feetYOffset = DefaultFeetYOffset)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return false;
        }

        float feetY = GetRowFeetY(rowY, feetYOffset);
        int cellX = mgr.WorldToCell(new Vector2(worldX, feetY)).x;
        Vector2Int cell = new Vector2Int(cellX, rowY);

        return IsFlatWalkable(mgr, cell);
    }

    /// <summary>
    /// 沿锁定行逐格探测可行走距离，遇平台边缘或竖直墙面即停止。
    /// </summary>
    public static float ProbeFlatDistance(
        Vector2 fromWorld,
        int rowY,
        int dirSign,
        float maxDistance,
        float feetYOffset = DefaultFeetYOffset)
    {
        if (dirSign == 0 || maxDistance <= 0f)
        {
            return 0f;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || !TryGetFlatRowCell(fromWorld, feetYOffset, out _, out Vector2Int startCell))
        {
            return 0f;
        }

        float lastValid = 0f;

        for (int step = 1; step <= 64; step++)
        {
            Vector2Int cell = new Vector2Int(startCell.x + dirSign * step, rowY);

            if (!IsFlatWalkable(mgr, cell))
            {
                break;
            }

            Vector2 feet = CellToFeetWorld(mgr, cell, feetYOffset);
            float dist = Mathf.Abs(feet.x - fromWorld.x);

            if (dist > maxDistance)
            {
                return maxDistance;
            }

            lastValid = dist;
        }

        return lastValid;
    }

    public static int PickPatrolDirection(
        Vector2 fromWorld,
        Bounds idleBounds,
        float feetYOffset = DefaultFeetYOffset,
        LayerMask _ = default)
    {
        if (!TryResolveSurfaceSupport(fromWorld, feetYOffset, out RobotSurfaceSupport support))
        {
            return Random.value > 0.5f ? 1 : -1;
        }

        float leftDist = ProbeSurfaceDistance(fromWorld, support, -1, 24f, feetYOffset);
        float rightDist = ProbeSurfaceDistance(fromWorld, support, 1, 24f, feetYOffset);

        if (Mathf.Approximately(leftDist, rightDist))
        {
            return Random.value > 0.5f ? 1 : -1;
        }

        return rightDist > leftDist ? 1 : -1;
    }

    /// <summary>
    /// 沿锁定行朝 dirSign 构建巡逻目标：单个路点，位于平台边缘/墙面/Idle 边界前最后一格。
    /// </summary>
    public static List<Vector2> BuildPatrolPath(
        Vector2 fromWorld,
        int dirSign,
        Bounds idleBounds,
        float feetYOffset = DefaultFeetYOffset,
        int maxCells = 48,
        LayerMask platformLayer = default)
    {
        List<Vector2> path = BuildPatrolPathInternal(
            fromWorld,
            dirSign,
            idleBounds,
            feetYOffset,
            maxCells,
            clipToBounds: true,
            platformLayer);

        if (path.Count > 0)
        {
            return path;
        }

        return BuildPatrolPathInternal(
            fromWorld,
            dirSign,
            idleBounds,
            feetYOffset,
            maxCells,
            clipToBounds: false,
            platformLayer);
    }

    /// <summary>
    /// 当前位置在 idleBounds 外时，沿同行平地回到 bounds 内最近可站立点。
    /// </summary>
    public static List<Vector2> BuildReturnToIdleBoundsPath(
        Vector2 fromWorld,
        Bounds idleBounds,
        float feetYOffset = DefaultFeetYOffset,
        LayerMask platformLayer = default)
    {
        List<Vector2> path = new List<Vector2>();

        if (IsInsideBoundsXY(idleBounds, fromWorld))
        {
            return path;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null
            || !TryResolveSurfaceSupport(fromWorld, feetYOffset, out RobotSurfaceSupport startSupport))
        {
            return path;
        }

        float feetY = startSupport.FeetY;
        int rowY = startSupport.RowY;

        int minCellX = mgr.WorldToCell(new Vector2(idleBounds.min.x, feetY)).x;
        int maxCellX = mgr.WorldToCell(new Vector2(idleBounds.max.x, feetY)).x;

        if (minCellX > maxCellX)
        {
            (minCellX, maxCellX) = (maxCellX, minCellX);
        }

        Vector2 bestFeet = fromWorld;
        float bestDistSqr = float.MaxValue;
        bool found = false;

        for (int cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            Vector2Int cell = new Vector2Int(cellX, rowY);
            float standX = CellToFeetWorld(mgr, cell, feetYOffset).x;

            if (!TryQueryStandPoint(new Vector2(standX, feetY), feetYOffset, feetY, out RobotSurfaceSupport stand))
            {
                continue;
            }

            Vector2 feet = new Vector2(standX, stand.FeetY);

            if (!IsInsideBoundsXY(idleBounds, feet))
            {
                continue;
            }

            float distSqr = (feet - fromWorld).sqrMagnitude;

            if (distSqr >= bestDistSqr)
            {
                continue;
            }

            bestDistSqr = distSqr;
            bestFeet = feet;
            found = true;
        }

        if (!found || bestDistSqr <= 0.02f * 0.02f)
        {
            return path;
        }

        path.Add(bestFeet);
        return path;
    }

    private static List<Vector2> BuildPatrolPathInternal(
        Vector2 fromWorld,
        int dirSign,
        Bounds idleBounds,
        float feetYOffset,
        int maxCells,
        bool clipToBounds,
        LayerMask _)
    {
        List<Vector2> path = new List<Vector2>();

        if (dirSign == 0 || !TryResolveSurfaceSupport(fromWorld, feetYOffset, out RobotSurfaceSupport startSupport))
        {
            return path;
        }

        float step = ResolveProbeStep();
        float maxDistance = step * 2f * maxCells;
        Vector2 bestPoint = fromWorld;
        bool found = false;
        float currentFeetY = startSupport.FeetY;

        for (float distance = step; distance <= maxDistance + step * 0.5f; distance += step)
        {
            float testX = fromWorld.x + dirSign * distance;
            Vector2 testPos = new Vector2(testX, currentFeetY);

            if (!TryQueryStandPoint(testPos, feetYOffset, currentFeetY, out RobotSurfaceSupport stand))
            {
                break;
            }

            Vector2 candidate = new Vector2(testX, stand.FeetY);

            if (clipToBounds && !IsInsideBoundsXY(idleBounds, candidate))
            {
                break;
            }

            currentFeetY = stand.FeetY;
            bestPoint = candidate;
            found = true;
        }

        if (!found || (bestPoint - fromWorld).sqrMagnitude <= step * step * 0.25f)
        {
            return path;
        }

        path.Add(bestPoint);
        return path;
    }

    public static Vector2 GetFeetOnRowAtDistance(
        Vector2 fromWorld,
        int rowY,
        int dirSign,
        float distance,
        float feetYOffset = DefaultFeetYOffset)
    {
        float feetY = GetRowFeetY(rowY, feetYOffset);
        return new Vector2(fromWorld.x + dirSign * distance, feetY);
    }

    /// <summary>
    /// 仅对齐 Y 到锁定行脚底高度，保留 X（避免每帧把 X 吸回格心导致无法移动）。
    /// </summary>
    public static Vector2 SnapToFlatGroundOnRow(
        Vector2 worldPos,
        int rowY,
        float feetYOffset = DefaultFeetYOffset)
    {
        return new Vector2(worldPos.x, GetRowFeetY(rowY, feetYOffset));
    }

    /// <summary>
    /// 将世界坐标对齐到当前行上最近的可行走格子的脚底点。
    /// </summary>
    public static Vector2 SnapToFlatGround(
        Vector2 worldPos,
        float feetYOffset = DefaultFeetYOffset,
        LayerMask _ = default)
    {
        if (TryResolveSurfaceSupport(worldPos, feetYOffset, out RobotSurfaceSupport support))
        {
            return new Vector2(worldPos.x, support.FeetY);
        }

        return worldPos;
    }

    public static List<Vector2> FindFlatPath(
        Vector2 fromWorld,
        Vector2 toWorld,
        int maxSteps = 500,
        float feetYOffset = DefaultFeetYOffset)
    {
        List<Vector2> path = new List<Vector2>();
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return path;
        }

        Vector2Int startCell = ResolveWalkableCell(mgr, mgr.WorldToCell(fromWorld));
        Vector2Int endCell = ResolveTargetOnRow(mgr, startCell, toWorld);

        if (startCell == endCell)
        {
            return BuildDirectFlatTarget(fromWorld, toWorld, feetYOffset);
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

        return ReconstructPath(mgr, parentMap, startCell, endCell, feetYOffset);
    }

    /// <summary>
    /// 同层 BFS 失败时，沿当前行朝目标 X 方向冲刺（最多 12 格）。
    /// </summary>
    public static List<Vector2> FindFlatDashToward(
        Vector2 fromWorld,
        Vector2 toWorld,
        float feetYOffset = DefaultFeetYOffset)
    {
        List<Vector2> path = FindFlatPath(fromWorld, toWorld, feetYOffset: feetYOffset);

        if (path.Count > 0)
        {
            return path;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return path;
        }

        Vector2Int startCell = ResolveWalkableCell(mgr, mgr.WorldToCell(fromWorld));
        int dir = toWorld.x >= fromWorld.x ? 1 : -1;

        for (int step = 1; step <= 12; step++)
        {
            Vector2Int cell = new Vector2Int(startCell.x + dir * step, startCell.y);

            if (!IsFlatWalkable(mgr, cell))
            {
                break;
            }

            path.Add(CellToFeetWorld(mgr, cell, feetYOffset));
        }

        return path;
    }

    /// <summary>
    /// 识别到玩家后用的冲刺路径：BFS → 同行冲刺 → 同行直线目标（保证至少有一个路点）。
    /// </summary>
    public static List<Vector2> BuildChargePath(
        Vector2 fromWorld,
        Vector2 toWorld,
        float feetYOffset = DefaultFeetYOffset)
    {
        List<Vector2> path = FindFlatPath(fromWorld, toWorld, feetYOffset: feetYOffset);

        if (path.Count > 0)
        {
            return path;
        }

        path = FindFlatDashToward(fromWorld, toWorld, feetYOffset);

        if (path.Count > 0)
        {
            return path;
        }

        return BuildDirectFlatTarget(fromWorld, toWorld, feetYOffset);
    }

    private static List<Vector2> BuildDirectFlatTarget(
        Vector2 fromWorld,
        Vector2 toWorld,
        float feetYOffset = DefaultFeetYOffset)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2 target = SnapToFlatGround(new Vector2(toWorld.x, fromWorld.y), feetYOffset);

        if ((target - fromWorld).sqrMagnitude > 0.02f * 0.02f)
        {
            path.Add(target);
        }

        return path;
    }

    public static List<Vector2> FindRandomIdlePath(
        Vector2 fromWorld,
        Bounds idleBounds,
        int maxSteps = 500,
        float feetYOffset = DefaultFeetYOffset)
    {
        List<Vector2> path = new List<Vector2>();
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return BuildBackupPath(fromWorld, idleBounds, feetYOffset);
        }

        Vector2Int startCell = ResolveWalkableCell(mgr, mgr.WorldToCell(fromWorld));
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
            Vector2 feetWorld = CellToFeetWorld(mgr, current, feetYOffset);

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
            return ReconstructPath(mgr, parentMap, startCell, chosenEnd, feetYOffset);
        }

        return BuildBackupPath(fromWorld, idleBounds, mgr, startCell, feetYOffset);
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
        Vector2Int endCell,
        float feetYOffset = DefaultFeetYOffset)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2Int backtrack = endCell;

        while (backtrack != startCell)
        {
            path.Insert(0, CellToFeetWorld(mgr, backtrack, feetYOffset));
            backtrack = parentMap[backtrack];
        }

        return path;
    }

    private static List<Vector2> BuildBackupPath(
        Vector2 fromWorld,
        Bounds idleBounds,
        float feetYOffset = DefaultFeetYOffset)
    {
        float groundY = SnapToFlatGround(fromWorld, feetYOffset).y;

        return new List<Vector2>
        {
            new Vector2(
                fromWorld.x + Random.Range(-2f, 2f),
                groundY
            )
        };
    }

    private static List<Vector2> BuildBackupPath(
        Vector2 fromWorld,
        Bounds idleBounds,
        TileMapGuideManager mgr,
        Vector2Int startCell,
        float feetYOffset = DefaultFeetYOffset)
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

                Vector2 feet = CellToFeetWorld(mgr, cell, feetYOffset);

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

        return BuildBackupPath(fromWorld, idleBounds, feetYOffset);
    }

    /// <summary>
    /// 解析可行走空气格：pivot 落在实心格或略偏低时先上下搜索，再同行左右搜索。
    /// </summary>
    private static Vector2Int ResolveWalkableCell(TileMapGuideManager mgr, Vector2Int preferred)
    {
        if (IsFlatWalkable(mgr, preferred))
        {
            return preferred;
        }

        for (int delta = 1; delta <= 8; delta++)
        {
            Vector2Int up = new Vector2Int(preferred.x, preferred.y + delta);

            if (IsFlatWalkable(mgr, up))
            {
                return up;
            }

            Vector2Int down = new Vector2Int(preferred.x, preferred.y - delta);

            if (IsFlatWalkable(mgr, down))
            {
                return down;
            }
        }

        return ResolveWalkableCellOnRow(mgr, preferred);
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
