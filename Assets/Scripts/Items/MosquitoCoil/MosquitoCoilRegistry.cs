using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时活跃蚊香注册表，供 AI 选点避让查询。
/// </summary>
public static class MosquitoCoilRegistry
{
    private static readonly List<MosquitoCoil> activeCoils = new List<MosquitoCoil>();

    public static int ActiveCount => activeCoils.Count;

    public static void Register(MosquitoCoil coil)
    {
        if (coil == null || activeCoils.Contains(coil))
        {
            return;
        }

        activeCoils.Add(coil);
    }

    public static void Unregister(MosquitoCoil coil)
    {
        if (coil == null)
        {
            return;
        }

        activeCoils.Remove(coil);
    }

    public static bool HasActiveCoils()
    {
        PruneInvalidCoils();
        return activeCoils.Count > 0;
    }

    public static bool IsInsideAnyActiveCoil(Vector2 point)
    {
        PruneInvalidCoils();

        for (int i = 0; i < activeCoils.Count; i++)
        {
            MosquitoCoil coil = activeCoils[i];
            if (coil != null && coil.IsPointInsideRadius(point))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetStrongestRepelAt(Vector2 point, out Vector2 coilCenter, out float radius)
    {
        PruneInvalidCoils();

        coilCenter = default;
        radius = 0f;

        float strongestPush = 0f;
        bool found = false;

        for (int i = 0; i < activeCoils.Count; i++)
        {
            MosquitoCoil coil = activeCoils[i];
            if (coil == null)
            {
                continue;
            }

            Vector2 center = coil.CenterPosition;
            float coilRadius = coil.Radius;
            float dist = Vector2.Distance(point, center);

            if (dist >= coilRadius)
            {
                continue;
            }

            float push = coilRadius - dist;
            if (!found || push > strongestPush)
            {
                found = true;
                strongestPush = push;
                coilCenter = center;
                radius = coilRadius;
            }
        }

        return found;
    }

    public static float GetRadiusAtCenter(Vector2 center, float epsilon = 0.15f)
    {
        PruneInvalidCoils();

        float epsilonSqr = epsilon * epsilon;
        for (int i = 0; i < activeCoils.Count; i++)
        {
            MosquitoCoil coil = activeCoils[i];
            if (coil == null)
            {
                continue;
            }

            if ((coil.CenterPosition - center).sqrMagnitude <= epsilonSqr)
            {
                return coil.Radius;
            }
        }

        return 3f;
    }

    private static void PruneInvalidCoils()
    {
        for (int i = activeCoils.Count - 1; i >= 0; i--)
        {
            MosquitoCoil coil = activeCoils[i];
            if (coil == null || !coil.IsActive)
            {
                activeCoils.RemoveAt(i);
            }
        }
    }
}
