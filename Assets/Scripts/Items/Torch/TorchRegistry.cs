using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时活跃火把注册表，供 AI 选点避让查询。
/// </summary>
public static class TorchRegistry
{
    private static readonly List<TorchProjectile> activeTorches = new List<TorchProjectile>();

    public static int ActiveCount => activeTorches.Count;

    public static void Register(TorchProjectile torch)
    {
        if (torch == null || activeTorches.Contains(torch))
        {
            return;
        }

        activeTorches.Add(torch);
    }

    public static void Unregister(TorchProjectile torch)
    {
        if (torch == null)
        {
            return;
        }

        activeTorches.Remove(torch);
    }

    public static bool HasActiveTorches()
    {
        PruneInvalidTorches();
        return activeTorches.Count > 0;
    }

    public static bool IsInsideAnyActiveTorch(Vector2 point)
    {
        PruneInvalidTorches();

        for (int i = 0; i < activeTorches.Count; i++)
        {
            TorchProjectile torch = activeTorches[i];
            if (torch != null && torch.IsRepelActive && torch.IsPointInsideRepelRadius(point))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetStrongestRepelAt(Vector2 point, out Vector2 torchCenter, out float radius)
    {
        PruneInvalidTorches();

        torchCenter = default;
        radius = 0f;

        float strongestPush = 0f;
        bool found = false;

        for (int i = 0; i < activeTorches.Count; i++)
        {
            TorchProjectile torch = activeTorches[i];
            if (torch == null || !torch.IsRepelActive)
            {
                continue;
            }

            Vector2 center = torch.RepelCenterPosition;
            float torchRadius = torch.RepelRadius;
            float dist = Vector2.Distance(point, center);

            if (dist >= torchRadius)
            {
                continue;
            }

            float push = torchRadius - dist;
            if (!found || push > strongestPush)
            {
                found = true;
                strongestPush = push;
                torchCenter = center;
                radius = torchRadius;
            }
        }

        return found;
    }

    public static float GetRadiusAtCenter(Vector2 center, float epsilon = 0.15f)
    {
        PruneInvalidTorches();

        float epsilonSqr = epsilon * epsilon;
        for (int i = 0; i < activeTorches.Count; i++)
        {
            TorchProjectile torch = activeTorches[i];
            if (torch == null || !torch.IsRepelActive)
            {
                continue;
            }

            if ((torch.RepelCenterPosition - center).sqrMagnitude <= epsilonSqr)
            {
                return torch.RepelRadius;
            }
        }

        return 3f;
    }

    private static void PruneInvalidTorches()
    {
        for (int i = activeTorches.Count - 1; i >= 0; i--)
        {
            TorchProjectile torch = activeTorches[i];
            if (torch == null || !torch.IsRepelActive)
            {
                activeTorches.RemoveAt(i);
            }
        }
    }
}
