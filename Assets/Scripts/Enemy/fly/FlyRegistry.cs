using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时 Fly 注册表，避免 WolfSpider 等每帧 FindObjectsOfType。
/// </summary>
public static class FlyRegistry
{
    private static readonly List<Fly2D> activeFlies = new List<Fly2D>();

    public static void Register(Fly2D fly)
    {
        if (fly == null || activeFlies.Contains(fly))
        {
            return;
        }

        activeFlies.Add(fly);
    }

    public static void Unregister(Fly2D fly)
    {
        if (fly == null)
        {
            return;
        }

        activeFlies.Remove(fly);
    }

    public static int ActiveCount => activeFlies.Count;

    public static Fly2D FindClosest(Vector2 origin, float maxRadius, out float closestDistSqr)
    {
        closestDistSqr = float.MaxValue;
        Fly2D closest = null;
        float maxRadiusSqr = maxRadius * maxRadius;

        for (int i = activeFlies.Count - 1; i >= 0; i--)
        {
            Fly2D fly = activeFlies[i];

            if (fly == null)
            {
                activeFlies.RemoveAt(i);
                continue;
            }

            float distSqr = (fly.Position - origin).sqrMagnitude;

            if (distSqr > maxRadiusSqr || distSqr >= closestDistSqr)
            {
                continue;
            }

            closestDistSqr = distSqr;
            closest = fly;
        }

        return closest;
    }
}
