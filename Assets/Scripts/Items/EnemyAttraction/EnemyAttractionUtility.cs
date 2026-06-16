using System;
using UnityEngine;

public static class EnemyAttractionUtility
{
    public static bool TryResolveTarget(
        Vector2 from,
        float queryRadius,
        EnemyAttractionCapabilities capabilities,
        Func<Vector2, bool> reachabilityCheck,
        out EnemyAttractionTarget target)
    {
        target = default;

        if (queryRadius <= 0f)
        {
            return false;
        }

        if (HasCapability(capabilities, EnemyAttractionCapabilities.MeatBait)
            && MeatBaitRegistry.TryFindClosest(from, queryRadius, out MeatBaitProjectile meatBait, out _))
        {
            Vector2 baitPosition = meatBait.AttractionCenter;
            if (IsReachable(reachabilityCheck, baitPosition))
            {
                target = new EnemyAttractionTarget(
                    EnemyAttractionSource.MeatBait,
                    baitPosition,
                    meatBait.transform);
                return true;
            }
        }

        if (HasCapability(capabilities, EnemyAttractionCapabilities.ToyCar)
            && ToyCarRegistry.TryFindClosest(from, queryRadius, out ToyCarController toyCar, out _))
        {
            Vector2 carPosition = toyCar.AttractionCenter;
            if (IsReachable(reachabilityCheck, carPosition))
            {
                target = new EnemyAttractionTarget(
                    EnemyAttractionSource.ToyCar,
                    carPosition,
                    toyCar.transform);
                return true;
            }
        }

        if (HasCapability(capabilities, EnemyAttractionCapabilities.Fly))
        {
            Fly2D fly = FlyRegistry.FindClosest(from, queryRadius, out float flyDistSqr);
            if (fly != null)
            {
                target = new EnemyAttractionTarget(
                    EnemyAttractionSource.Fly,
                    fly.Position,
                    fly.transform);
                return true;
            }
        }

        if (HasCapability(capabilities, EnemyAttractionCapabilities.Player))
        {
            Player player = PlayerManager.Instance != null
                ? PlayerManager.Instance.TryGetCurrentPlayer()
                : null;

            if (player != null)
            {
                float distSqr = ((Vector2)player.transform.position - from).sqrMagnitude;
                if (distSqr <= queryRadius * queryRadius)
                {
                    target = new EnemyAttractionTarget(
                        EnemyAttractionSource.Player,
                        player.transform.position,
                        player.transform);
                    return true;
                }
            }
        }

        return false;
    }

    public static bool CanReachByPath(Vector2 from, Vector2 to)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;
        if (mgr == null)
        {
            return true;
        }

        var path = mgr.FindPath(from, to);
        return path != null && path.Count > 1;
    }

    private static bool HasCapability(EnemyAttractionCapabilities capabilities, EnemyAttractionCapabilities flag)
    {
        return (capabilities & flag) != 0;
    }

    private static bool IsReachable(Func<Vector2, bool> reachabilityCheck, Vector2 destination)
    {
        if (reachabilityCheck == null)
        {
            return true;
        }

        return reachabilityCheck(destination);
    }
}
