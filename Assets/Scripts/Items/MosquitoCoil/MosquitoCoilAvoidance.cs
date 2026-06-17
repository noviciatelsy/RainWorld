using System;
using UnityEngine;

/// <summary>
/// 蚊香范围避让：选点与移动目标不得落在活跃蚊香半径内。
/// </summary>
public static class MosquitoCoilAvoidance
{
    private const int DefaultPickAttempts = 30;
    private const float DefaultFleeMargin = 0.75f;

    public static bool HasActiveCoils()
    {
        return MosquitoCoilRegistry.HasActiveCoils();
    }

    public static bool IsInsideAnyActiveCoil(Vector2 point)
    {
        return MosquitoCoilRegistry.IsInsideAnyActiveCoil(point);
    }

    public static bool ShouldInterruptMovement(Vector2 point)
    {
        return IsInsideAnyActiveCoil(point);
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
            if (!IsInsideAnyActiveCoil(candidate))
            {
                validPoint = candidate;
                return true;
            }
        }

        return false;
    }

    public static Vector2 GetFleePointAwayFromAllCoils(Vector2 from, float margin = DefaultFleeMargin)
    {
        if (!HasActiveCoils())
        {
            return from;
        }

        Vector2 result = from;

        for (int iteration = 0; iteration < 8; iteration++)
        {
            if (!MosquitoCoilRegistry.TryGetStrongestRepelAt(result, out Vector2 coilCenter, out float radius))
            {
                break;
            }

            Vector2 offset = result - coilCenter;
            float dist = offset.magnitude;
            Vector2 direction = dist > 0.001f ? offset / dist : UnityEngine.Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.up;
            }

            result = coilCenter + direction * (radius + margin);
        }

        return result;
    }

    public static Vector2 GetFleePointFromCoil(Vector2 from, Vector2 coilCenter, float coilRadius, float margin = DefaultFleeMargin)
    {
        Vector2 offset = from - coilCenter;
        float dist = offset.magnitude;
        Vector2 direction = dist > 0.001f ? offset / dist : Vector2.up;
        return coilCenter + direction * (coilRadius + margin);
    }
}
