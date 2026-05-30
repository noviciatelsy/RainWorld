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

    private const int TrajectorySampleCount = 8;
    private const int LineOfSightStepSize = 4;

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

        SurfaceSnapResult rayResult = SnapByTileRaycast(worldHint, mgr, surfaceOffset, preferNear);

        if (rayResult.success && rayResult.normal.y > 0.2f)
        {
            return rayResult;
        }

        if (mgr.TryGetFloorTop(worldHint, out Vector2 floorPoint, surfaceOffset))
        {
            return new SurfaceSnapResult
            {
                success = true,
                point = floorPoint,
                normal = Vector2.up
            };
        }

        if (rayResult.success)
        {
            return rayResult;
        }

        int edgeIndex = FindBestEdgeIndex(mgr, worldHint, maxDistance);
        Edge edge = mgr.GetEdge(edgeIndex);
        Vector2 pointOnEdge = ClosestPointOnSegment(worldHint, edge.a, edge.b);
        float edgeDistanceSqr = (worldHint - pointOnEdge).sqrMagnitude;
        float maxDistanceSqr = maxDistance * maxDistance;

        if (edgeDistanceSqr > maxDistanceSqr)
        {
            return Fail();
        }

        if (mgr.TryGetStandPointOnEdge(edge, worldHint, surfaceOffset, out Vector2 standPoint, out Vector2 normal))
        {
            return new SurfaceSnapResult
            {
                success = true,
                point = standPoint,
                normal = normal
            };
        }

        return Fail();
    }

    private static int FindBestEdgeIndex(TileMapGuideManager mgr, Vector2 worldHint, float maxDistance)
    {
        float maxDistanceSqr = maxDistance * maxDistance;
        int bestIndex = mgr.FindClosestEdgeIndex(worldHint);
        float bestScore = float.MaxValue;

        int edgeCount = mgr.GetEdgeCount();

        for (int i = 0; i < edgeCount; i++)
        {
            Edge edge = mgr.GetEdge(i);
            Vector2 pointOnEdge = ClosestPointOnSegment(worldHint, edge.a, edge.b);
            float edgeDistanceSqr = (worldHint - pointOnEdge).sqrMagnitude;

            if (edgeDistanceSqr > maxDistanceSqr)
            {
                continue;
            }

            Vector2 normal = mgr.GetEdgeAirNormal(edge);
            float score = edgeDistanceSqr - normal.y * 2f;

            if (normal.y < 0f)
            {
                score += 100f;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public static SurfaceSnapResult SnapToFloorSurface(
        Vector2 worldHint,
        float maxDistance = 0.85f,
        float surfaceOffset = 0.08f)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return Fail();
        }

        if (mgr.TryGetFloorTop(worldHint, out Vector2 standPoint, surfaceOffset))
        {
            return new SurfaceSnapResult
            {
                success = true,
                point = standPoint,
                normal = Vector2.up
            };
        }

        return Fail();
    }

    public static bool IsJumpTrajectoryClear(
        Vector2 from,
        Vector2 to,
        float arcHeight,
        Vector2 arcNormal,
        int samples = TrajectorySampleCount)
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

        Vector2 outward = arcNormal.sqrMagnitude > 0.0001f ? arcNormal.normalized : Vector2.up;
        int safeSamples = Mathf.Max(4, samples);

        for (int i = 0; i <= safeSamples; i++)
        {
            float t = i / (float)safeSamples;
            Vector2 flatPosition = Vector2.Lerp(from, to, t);
            float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            Vector2 sample = flatPosition + outward * heightOffset;

            if (mgr.IsSolid(mgr.WorldToCell(sample)))
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
        float arcHeight,
        Vector2 arcNormal)
    {
        float distanceSqr = (to - from).sqrMagnitude;
        float minSqr = minDistance * minDistance;
        float maxSqr = maxDistance * maxDistance;

        if (distanceSqr < minSqr || distanceSqr > maxSqr)
        {
            return false;
        }

        return IsJumpTrajectoryClear(from, to, arcHeight, arcNormal);
    }

    public static SurfaceSnapResult SampleSurfaceInRing(
        Vector2 origin,
        float minDistance,
        float maxDistance,
        Vector2 biasDirection,
        int sampleCount = 12,
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
        float minSqr = minDistance * minDistance;
        float maxSqr = maxDistance * maxDistance;

        for (int i = 0; i < sampleCount; i++)
        {
            float angleOffset;
            float distance;

            if (deterministic)
            {
                float t = sampleCount <= 1 ? 0f : i / (float)(sampleCount - 1);
                angleOffset = Mathf.Lerp(-90f, 90f, t);
                distance = Mathf.Lerp(minDistance, maxDistance, t);
            }
            else
            {
                angleOffset = Random.Range(-90f, 90f);
                distance = Random.Range(minDistance, maxDistance);
            }

            Vector2 dir = Rotate(normalizedBias, angleOffset).normalized;
            Vector2 hint = origin + dir * distance;

            SurfaceSnapResult snap = SnapToSurface(hint, maxSnapDistance, surfaceOffset, origin);

            if (!snap.success)
            {
                continue;
            }

            float jumpDistanceSqr = (origin - snap.point).sqrMagnitude;

            if (jumpDistanceSqr < minSqr || jumpDistanceSqr > maxSqr)
            {
                continue;
            }

            float score = Vector2.Dot((snap.point - origin).normalized, normalizedBias);

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

        Vector2Int fromCell = mgr.WorldToCell(from);
        Vector2Int toCell = mgr.WorldToCell(to);

        int dx = Mathf.Abs(toCell.x - fromCell.x);
        int dy = Mathf.Abs(toCell.y - fromCell.y);
        int steps = Mathf.Max(dx, dy, 1);

        for (int i = 0; i <= steps; i += LineOfSightStepSize)
        {
            float t = i / (float)steps;
            Vector2 sample = Vector2.Lerp(from, to, t);

            if (mgr.IsSolid(mgr.WorldToCell(sample)))
            {
                return false;
            }
        }

        if (steps % LineOfSightStepSize != 0 && mgr.IsSolid(toCell))
        {
            return false;
        }

        return true;
    }

    public static void FillArcSamples(
        Vector2 from,
        Vector2 to,
        float arcHeight,
        Vector2 arcNormal,
        System.Collections.Generic.List<Vector2> output,
        int samples = 12)
    {
        if (output == null)
        {
            return;
        }

        Vector2 outward = arcNormal.sqrMagnitude > 0.0001f ? arcNormal.normalized : Vector2.up;
        output.Clear();
        int safeSamples = Mathf.Max(4, samples);

        for (int i = 0; i <= safeSamples; i++)
        {
            float t = i / (float)safeSamples;
            Vector2 flatPosition = Vector2.Lerp(from, to, t);
            float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            output.Add(flatPosition + outward * heightOffset);
        }
    }

    private static SurfaceSnapResult SnapByTileRaycast(
        Vector2 worldHint,
        TileMapGuideManager mgr,
        float surfaceOffset,
        Vector2? preferNear)
    {
        SurfaceSnapResult best = Fail();
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < ProbeDirections.Length; i++)
        {
            Vector2 dir = ProbeDirections[i];

            for (float step = 0.15f; step <= 0.75f; step += 0.15f)
            {
                Vector2 sample = worldHint + dir * step;
                Vector2Int cell = mgr.WorldToCell(sample);
                Vector2Int adjacentSolid = mgr.WorldToCell(sample - dir * 0.15f);

                if (!mgr.IsSolid(cell) && mgr.IsSolid(adjacentSolid))
                {
                    Vector2 normal = -dir.normalized;

                    if (normal.y < 0.2f)
                    {
                        continue;
                    }

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

                    float distSqr = (preferNear.Value - point).sqrMagnitude;

                    if (distSqr < bestDistanceSqr)
                    {
                        bestDistanceSqr = distSqr;
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
