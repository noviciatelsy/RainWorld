using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时 Snail 注册表，供蝙蝠等敌人按距离索敌。
/// </summary>
public static class SnailRegistry
{
    private static readonly List<Snail2D> activeSnails = new List<Snail2D>();

    public static void Register(Snail2D snail)
    {
        if (snail == null || activeSnails.Contains(snail))
        {
            return;
        }

        activeSnails.Add(snail);
    }

    public static void Unregister(Snail2D snail)
    {
        if (snail == null)
        {
            return;
        }

        activeSnails.Remove(snail);
    }

    public static Snail2D FindClosest(Vector2 origin, float maxRadius, out float closestDistSqr)
    {
        closestDistSqr = float.MaxValue;
        Snail2D closest = null;
        float maxRadiusSqr = maxRadius * maxRadius;

        for (int i = activeSnails.Count - 1; i >= 0; i--)
        {
            Snail2D snail = activeSnails[i];

            if (snail == null)
            {
                activeSnails.RemoveAt(i);
                continue;
            }

            float distSqr = (snail.Position - origin).sqrMagnitude;

            if (distSqr > maxRadiusSqr || distSqr >= closestDistSqr)
            {
                continue;
            }

            closestDistSqr = distSqr;
            closest = snail;
        }

        return closest;
    }
}
