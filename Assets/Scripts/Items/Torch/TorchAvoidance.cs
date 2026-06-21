using System;
using UnityEngine;

/// <summary>
/// 火把范围避让：选点与移动目标不得落在活跃火把半径内。
/// </summary>
public static class TorchAvoidance
{
    private const int DefaultPickAttempts = 30;
    private const float DefaultFleeMargin = 0.75f;

    public static bool HasActiveTorches()
    {
        return TorchRegistry.HasActiveTorches();
    }

    public static bool IsInsideAnyActiveTorch(Vector2 point)
    {
        return TorchRegistry.IsInsideAnyActiveTorch(point);
    }

    public static bool ShouldInterruptMovement(Vector2 point)
    {
        return IsInsideAnyActiveTorch(point);
    }

    public static bool TryPickValidPoint(Func<Vector2> generateCandidate, out Vector2 validPoint, int maxAttempts = DefaultPickAttempts)
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
            if (!IsInsideAnyActiveTorch(candidate))
            {
                validPoint = candidate;
                return true;
            }
        }

        return false;
    }

    public static Vector2 GetFleePointAwayFromAllTorches(Vector2 from, float margin = DefaultFleeMargin)
    {
        if (!HasActiveTorches())
        {
            return from;
        }

        Vector2 result = from;

        for (int iteration = 0; iteration < 8; iteration++)
        {
            if (!TorchRegistry.TryGetStrongestRepelAt(result, out Vector2 torchCenter, out float radius))
            {
                break;
            }

            Vector2 offset = result - torchCenter;
            float dist = offset.magnitude;
            Vector2 direction = dist > 0.001f ? offset / dist : UnityEngine.Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.up;
            }

            result = torchCenter + direction * (radius + margin);
        }

        return result;
    }

    public static Vector2 GetFleePointFromTorch(Vector2 from, Vector2 torchCenter, float torchRadius, float margin = DefaultFleeMargin)
    {
        Vector2 offset = from - torchCenter;
        float dist = offset.magnitude;
        Vector2 direction = dist > 0.001f ? offset / dist : Vector2.up;
        return torchCenter + direction * (torchRadius + margin);
    }
}
