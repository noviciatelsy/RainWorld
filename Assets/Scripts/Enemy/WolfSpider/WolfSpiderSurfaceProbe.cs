using UnityEngine;

public struct SurfaceSnapResult
{
    public bool success;
    public Vector2 point;
    public Vector2 normal;
}

public static class WolfSpiderSurfaceProbe
{
    private static readonly Vector2[] ProbeDirections =
    {
        Vector2.down,
        Vector2.up,
        Vector2.left,
        Vector2.right
    };

    public static SurfaceSnapResult SnapToSurface(
        Vector2 worldHint,
        float maxDistance = 0.85f,
        float surfaceOffset = 0.08f,
        Vector2? preferNear = null)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return Fail();
        }

        int edgeIndex = mgr.FindClosestEdgeIndex(worldHint);
        Edge edge = mgr.GetEdge(edgeIndex);

        Vector2 pointOnEdge = ClosestPointOnSegment(worldHint, edge.a, edge.b);
        float edgeDistance = Vector2.Distance(worldHint, pointOnEdge);

        if (edgeDistance > maxDistance)
        {
            SurfaceSnapResult rayResult = SnapByTileRaycast(worldHint, mgr, surfaceOffset, preferNear);

            if (rayResult.success)
            {
                return rayResult;
            }

            return Fail();
        }

        Vector2 tangent = (edge.b - edge.a).normalized;

        if (tangent.sqrMagnitude < 0.0001f)
        {
            return Fail();
        }

        Vector2 normalA = new Vector2(-tangent.y, tangent.x);
        Vector2 normalB = -normalA;
        Vector2 normal = PickAirNormal(mgr, pointOnEdge, normalA, normalB, preferNear);

        return new SurfaceSnapResult
        {
            success = true,
            point = pointOnEdge + normal * surfaceOffset,
            normal = normal
        };
    }

    public static bool IsOnValidSurface(Vector2 point, float maxDistance = 0.85f)
    {
        return SnapToSurface(point, maxDistance).success;
    }

    public static bool IsJumpTrajectoryClear(Vector2 from, Vector2 to, float arcHeight, int samples = 20)
    {
        if (!HasLineOfSight(from, to))
        {
            return false;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return true;
        }

        int safeSamples = Mathf.Max(4, samples);

        for (int i = 0; i <= safeSamples; i++)
        {
            float t = i / (float)safeSamples;
            Vector2 flatPosition = Vector2.Lerp(from, to, t);
            float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            Vector2 sample = flatPosition + Vector2.up * heightOffset;
            Vector2Int cell = mgr.WorldToCell(sample);

            if (mgr.IsSolid(cell))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsValidJumpTarget(
        Vector2 from,
        Vector2 to,
        float minDistance,
        float maxDistance,
        float arcHeight)
    {
        float distance = Vector2.Distance(from, to);

        if (distance < minDistance || distance > maxDistance)
        {
            return false;
        }

        return IsJumpTrajectoryClear(from, to, arcHeight);
    }

    public static SurfaceSnapResult SampleSurfaceInRing(
        Vector2 origin,
        float minDistance,
        float maxDistance,
        Vector2 biasDirection,
        int sampleCount = 24,
        float maxSnapDistance = 0.85f,
        float surfaceOffset = 0.08f,
        bool deterministic = false)
    {
        if (sampleCount < 1)
        {
            return Fail();
        }

        Vector2 normalizedBias = biasDirection.sqrMagnitude > 0.0001f
            ? biasDirection.normalized
            : Vector2.right;

        SurfaceSnapResult best = Fail();
        float bestScore = float.MinValue;

        for (int i = 0; i < sampleCount; i++)
        {
            float angleOffset;
            float distance;

            if (deterministic)
            {
                float t = sampleCount <= 1 ? 0f : i / (float)(sampleCount - 1);
                angleOffset = Mathf.Lerp(-120f, 120f, t);
                distance = Mathf.Lerp(minDistance, maxDistance, t);
            }
            else
            {
                angleOffset = Random.Range(-120f, 120f);
                distance = Random.Range(minDistance, maxDistance);
            }

            Vector2 dir = Rotate(normalizedBias, angleOffset).normalized;
            Vector2 hint = origin + dir * distance;

            SurfaceSnapResult snap = SnapToSurface(hint, maxSnapDistance, surfaceOffset, origin);

            if (!snap.success)
            {
                continue;
            }

            float jumpDistance = Vector2.Distance(origin, snap.point);

            if (jumpDistance < minDistance || jumpDistance > maxDistance)
            {
                continue;
            }

            float score = Vector2.Dot((snap.point - origin).normalized, normalizedBias) - jumpDistance * 0.02f;

            if (score > bestScore)
            {
                bestScore = score;
                best = snap;
            }
        }

        return best;
    }

    public static bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return true;
        }

        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / 0.15f));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 sample = Vector2.Lerp(from, to, t);
            Vector2Int cell = mgr.WorldToCell(sample);

            if (mgr.IsSolid(cell))
            {
                return false;
            }
        }

        return true;
    }

    public static void FillArcSamples(Vector2 from, Vector2 to, float arcHeight, System.Collections.Generic.List<Vector2> output, int samples = 24)
    {
        if (output == null)
        {
            return;
        }

        output.Clear();
        int safeSamples = Mathf.Max(4, samples);

        for (int i = 0; i <= safeSamples; i++)
        {
            float t = i / (float)safeSamples;
            Vector2 flatPosition = Vector2.Lerp(from, to, t);
            float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            output.Add(flatPosition + Vector2.up * heightOffset);
        }
    }

    private static SurfaceSnapResult SnapByTileRaycast(
        Vector2 worldHint,
        TileMapGuideManager mgr,
        float surfaceOffset,
        Vector2? preferNear)
    {
        SurfaceSnapResult best = Fail();
        float bestDistance = float.MaxValue;

        for (int i = 0; i < ProbeDirections.Length; i++)
        {
            Vector2 dir = ProbeDirections[i];

            for (float step = 0.1f; step <= 1.2f; step += 0.1f)
            {
                Vector2 sample = worldHint + dir * step;
                Vector2Int cell = mgr.WorldToCell(sample);
                Vector2Int adjacentSolid = mgr.WorldToCell(sample - dir * 0.15f);

                if (!mgr.IsSolid(cell) && mgr.IsSolid(adjacentSolid))
                {
                    Vector2 normal = -dir.normalized;
                    Vector2 point = sample + normal * surfaceOffset;

                    if (!preferNear.HasValue)
                    {
                        return new SurfaceSnapResult
                        {
                            success = true,
                            point = point,
                            normal = normal
                        };
                    }

                    float dist = Vector2.Distance(preferNear.Value, point);

                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        best = new SurfaceSnapResult
                        {
                            success = true,
                            point = point,
                            normal = normal
                        };
                    }
                }
            }
        }

        return best;
    }

    private static Vector2 PickAirNormal(
        TileMapGuideManager mgr,
        Vector2 pointOnEdge,
        Vector2 normalA,
        Vector2 normalB,
        Vector2? preferNear)
    {
        Vector2Int cellA = mgr.WorldToCell(pointOnEdge + normalA * 0.08f);
        Vector2Int cellB = mgr.WorldToCell(pointOnEdge + normalB * 0.08f);
        bool airA = !mgr.IsSolid(cellA);
        bool airB = !mgr.IsSolid(cellB);

        if (airA && !airB)
        {
            return normalA;
        }

        if (airB && !airA)
        {
            return normalB;
        }

        if (preferNear.HasValue)
        {
            float distA = Vector2.Distance(preferNear.Value, pointOnEdge + normalA * 0.08f);
            float distB = Vector2.Distance(preferNear.Value, pointOnEdge + normalB * 0.08f);
            return distA <= distB ? normalA : normalB;
        }

        return normalA;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;

        if (ab.sqrMagnitude < 0.0001f)
        {
            return a;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
        return a + ab * t;
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private static SurfaceSnapResult Fail()
    {
        return new SurfaceSnapResult
        {
            success = false,
            point = default,
            normal = Vector2.up
        };
    }
}
