using System.Collections.Generic;
using UnityEngine;

public static class MeatBaitRegistry
{
    private static readonly List<MeatBaitProjectile> activeBaits = new List<MeatBaitProjectile>();

    public static int ActiveCount => activeBaits.Count;

    public static void Register(MeatBaitProjectile bait)
    {
        if (bait == null || activeBaits.Contains(bait))
        {
            return;
        }

        activeBaits.Add(bait);
    }

    public static void Unregister(MeatBaitProjectile bait)
    {
        if (bait == null)
        {
            return;
        }

        activeBaits.Remove(bait);
    }

    public static bool TryFindClosest(
        Vector2 from,
        float queryRadius,
        out MeatBaitProjectile closest,
        out float closestDistSqr)
    {
        PruneInvalidBaits();

        closest = null;
        closestDistSqr = float.MaxValue;
        float queryRadiusSqr = queryRadius * queryRadius;

        for (int i = 0; i < activeBaits.Count; i++)
        {
            MeatBaitProjectile bait = activeBaits[i];
            if (bait == null || !bait.IsAttracting)
            {
                continue;
            }

            Vector2 baitPosition = bait.AttractionCenter;
            float distSqr = ((Vector2)baitPosition - from).sqrMagnitude;

            if (distSqr > queryRadiusSqr)
            {
                continue;
            }

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = bait;
            }
        }

        return closest != null;
    }

    private static void PruneInvalidBaits()
    {
        for (int i = activeBaits.Count - 1; i >= 0; i--)
        {
            MeatBaitProjectile bait = activeBaits[i];
            if (bait == null || !bait.IsAttracting)
            {
                activeBaits.RemoveAt(i);
            }
        }
    }
}
