using System;
using UnityEngine;

/// <summary>
/// 蚊香 + 火把的统一避让查询，供蝙蝠、狼蛛、鼹鼠等怕火/怕烟敌人使用。
/// </summary>
public static class RepellentAvoidance
{
    public static bool HasActiveZones()
    {
        return MosquitoCoilAvoidance.HasActiveCoils() || TorchAvoidance.HasActiveTorches();
    }

    public static bool IsInsideAnyZone(Vector2 point)
    {
        return MosquitoCoilAvoidance.IsInsideAnyActiveCoil(point)
            || TorchAvoidance.IsInsideAnyActiveTorch(point);
    }

    public static bool ShouldInterruptMovement(Vector2 point)
    {
        return IsInsideAnyZone(point);
    }

    public static bool TryPickValidPoint(Func<Vector2> generateCandidate, out Vector2 validPoint, int maxAttempts = 30)
    {
        validPoint = default;

        if (generateCandidate == null)
        {
            return false;
        }

        int attempts = Mathf.Max(1, maxAttempts);
        for (int i = 0; i < attempts; i++)
        {
            Vector2 candidate = generateCandidate();
            if (!IsInsideAnyZone(candidate))
            {
                validPoint = candidate;
                return true;
            }
        }

        return false;
    }

    public static Vector2 GetFleePointAwayFromAll(Vector2 from, float margin = 0.75f)
    {
        Vector2 result = MosquitoCoilAvoidance.GetFleePointAwayFromAllCoils(from, margin);
        return TorchAvoidance.GetFleePointAwayFromAllTorches(result, margin);
    }
}
