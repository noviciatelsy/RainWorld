using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 保证 Fly 生成在可飞行、可寻路的位置，并预校验首个漫游目标。
/// </summary>
public static class FlySpawnUtility
{
    private const int RingCount = 8;
    private const int SamplesPerRing = 16;
    private const float MinWanderDistance = 2f;
    private const float MaxWanderDistance = 8f;
    private const int MinReachableWanderTargets = 2;

    public static Vector2 ResolveSpawnPosition(Vector2 desiredPosition, float searchRadius = 6f)
    {
        if (TryResolveSpawn(desiredPosition, out Vector2 spawnPosition, out _, searchRadius))
        {
            return spawnPosition;
        }

        return desiredPosition;
    }

    public static bool TryResolveSpawn(
        Vector2 desiredPosition,
        out Vector2 spawnPosition,
        out Vector2 initialTarget,
        float searchRadius = 6f)
    {
        spawnPosition = desiredPosition;
        initialTarget = desiredPosition;

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return true;
        }

        List<Vector2> candidates = BuildCandidatePositions(mgr, desiredPosition, searchRadius);

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2 candidate = candidates[i];

            if (!IsValidFlySpawnCell(mgr, candidate))
            {
                continue;
            }

            if (!TryPickWanderTarget(mgr, candidate, out Vector2 wanderTarget))
            {
                continue;
            }

            if (CountReachableWanderTargets(mgr, candidate) < MinReachableWanderTargets)
            {
                continue;
            }

            spawnPosition = candidate;
            initialTarget = wanderTarget;
            return true;
        }

        return false;
    }

    public static bool CanFlyMoveFrom(Vector2 position)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return true;
        }

        return IsValidFlySpawnCell(mgr, position)
            && CountReachableWanderTargets(mgr, position) >= MinReachableWanderTargets;
    }

    private static List<Vector2> BuildCandidatePositions(
        TileMapGuideManager mgr,
        Vector2 desiredPosition,
        float searchRadius)
    {
        HashSet<Vector2Int> seenCells = new HashSet<Vector2Int>();
        List<Vector2> candidates = new List<Vector2>();

        void AddWorldPoint(Vector2 worldPoint)
        {
            Vector2Int cell = mgr.WorldToCell(worldPoint);

            if (!seenCells.Add(cell))
            {
                return;
            }

            candidates.Add(mgr.CellToWorld(cell));
        }

        AddWorldPoint(desiredPosition);

        for (int ring = 1; ring <= RingCount; ring++)
        {
            float radius = searchRadius * ring / RingCount;

            for (int i = 0; i < SamplesPerRing; i++)
            {
                float angle = (i / (float)SamplesPerRing) * Mathf.PI * 2f;
                AddWorldPoint(desiredPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        Vector2Int centerCell = mgr.WorldToCell(desiredPosition);
        float cellWidth = EstimateCellWidth(mgr, centerCell);
        int cellRadius = Mathf.Max(1, Mathf.CeilToInt(searchRadius / cellWidth));

        for (int y = -cellRadius; y <= cellRadius; y++)
        {
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                if (x * x + y * y > cellRadius * cellRadius)
                {
                    continue;
                }

                Vector2Int cell = centerCell + new Vector2Int(x, y);

                if (!seenCells.Add(cell))
                {
                    continue;
                }

                candidates.Add(mgr.CellToWorld(cell));
            }
        }

        candidates.Sort((a, b) =>
            (a - desiredPosition).sqrMagnitude.CompareTo((b - desiredPosition).sqrMagnitude));

        return candidates;
    }

    private static float EstimateCellWidth(TileMapGuideManager mgr, Vector2Int cell)
    {
        Vector2 center = mgr.CellToWorld(cell);
        Vector2 right = mgr.CellToWorld(cell + Vector2Int.right);
        return Mathf.Max(0.01f, Vector2.Distance(center, right));
    }

    private static bool IsValidFlySpawnCell(TileMapGuideManager mgr, Vector2 position)
    {
        Vector2Int cell = mgr.WorldToCell(position);

        if (mgr.IsSolid(cell))
        {
            return false;
        }

        return HasPathToProbe(mgr, position, position + Vector2.right * MinWanderDistance)
            || HasPathToProbe(mgr, position, position + Vector2.left * MinWanderDistance)
            || HasPathToProbe(mgr, position, position + Vector2.up * MinWanderDistance)
            || HasPathToProbe(mgr, position, position + Vector2.down * MinWanderDistance);
    }

    private static int CountReachableWanderTargets(TileMapGuideManager mgr, Vector2 position)
    {
        int count = 0;
        float[] distances = { MinWanderDistance, MinWanderDistance * 2f, MaxWanderDistance * 0.5f };

        for (int d = 0; d < distances.Length; d++)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector2 probe = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distances[d];

                if (HasPathToProbe(mgr, position, probe))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool TryPickWanderTarget(TileMapGuideManager mgr, Vector2 from, out Vector2 target)
    {
        target = from;
        float[] distances = { MinWanderDistance, MinWanderDistance * 2f, MaxWanderDistance * 0.5f, MaxWanderDistance };

        for (int d = 0; d < distances.Length; d++)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector2 candidate = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distances[d];

                if (HasPathToProbe(mgr, from, candidate))
                {
                    target = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasPathToProbe(TileMapGuideManager mgr, Vector2 from, Vector2 to)
    {
        Vector2Int fromCell = mgr.WorldToCell(from);
        Vector2Int toCell = mgr.WorldToCell(to);

        if (fromCell == toCell)
        {
            return false;
        }

        if (mgr.IsSolid(toCell))
        {
            return false;
        }

        List<Vector2> path = mgr.FindPath(from, to);

        return path != null && path.Count > 1;
    }
}
