using UnityEngine;

/// <summary>
/// 保证 Fly 生成在可飞行、可寻路的位置，避免卡墙后每帧重算路径。
/// </summary>
public static class FlySpawnUtility
{
    private const int RingCount = 6;
    private const int SamplesPerRing = 12;
    private const float MinProbeDistance = 1.2f;
    private const int MinReachableDirections = 2;

    public static Vector2 ResolveSpawnPosition(Vector2 desiredPosition, float searchRadius = 4f)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return desiredPosition;
        }

        if (CanFlyMoveFrom(mgr, desiredPosition))
        {
            return desiredPosition;
        }

        Vector2 best = desiredPosition;
        float bestScore = float.MinValue;

        for (int ring = 1; ring <= RingCount; ring++)
        {
            float radius = searchRadius * ring / RingCount;

            for (int i = 0; i < SamplesPerRing; i++)
            {
                float angle = (i / (float)SamplesPerRing) * Mathf.PI * 2f;
                Vector2 candidate = desiredPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                if (!CanFlyMoveFrom(mgr, candidate))
                {
                    continue;
                }

                float score = -((candidate - desiredPosition).sqrMagnitude);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (bestScore > float.MinValue)
            {
                return best;
            }
        }

        return best;
    }

    public static bool CanFlyMoveFrom(Vector2 position)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return true;
        }

        return CanFlyMoveFrom(mgr, position);
    }

    private static bool CanFlyMoveFrom(TileMapGuideManager mgr, Vector2 position)
    {
        Vector2Int cell = mgr.WorldToCell(position);

        if (mgr.IsSolid(cell))
        {
            return false;
        }

        int reachableDirections = 0;

        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            Vector2 probe = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * MinProbeDistance;
            var path = mgr.FindPath(position, probe);

            if (path != null && path.Count > 1)
            {
                reachableDirections++;
            }
        }

        return reachableDirections >= MinReachableDirections;
    }
}
