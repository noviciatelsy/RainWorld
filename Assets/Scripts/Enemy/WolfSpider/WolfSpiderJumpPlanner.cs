using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 换点流程：方向射线 → tile 表面解析 → 距离/遮挡过滤 → 弧线验证 → 从前 3 随机落点。
/// </summary>
public static class WolfSpiderJumpPlanner
{
    private const float MinJumpSlack = 0.02f;
    private const float RejectedTargetEpsilonSqr = 0.1f * 0.1f;
    private const float RecentVisitExcludeSqr = 0.35f * 0.35f;
    private const float CandidateDedupeDistanceSqr = 0.15f * 0.15f;
    private const float DirectionCorrectDotThreshold = 0.12f;
    private const float GoalProgressEpsilon = 0.08f;
    private const float SolidSegmentStep = 0.14f;

    private const int HuntRandomRayCount = 10;
    private const int IdleRandomRayCount = 12;
    private const int TopCandidatePoolSize = 3;

    private static readonly float[] GuaranteedRayAngles =
    {
        0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f
    };

    private static readonly List<ReachableCandidate> sCandidates = new List<ReachableCandidate>(32);
    private static readonly List<ReachableCandidate> sTopValidCandidates = new List<ReachableCandidate>(3);
    private static readonly List<Vector2> sDedupePoints = new List<Vector2>(32);

    private static float sMarchStep = 0.12f;
    private static float sCellWidth = 0.5f;

    private struct ReachableCandidate
    {
        public Vector2 Point;
        public float JumpDist;
        public bool CrossLoop;
        public bool DirectionCorrect;
        public bool CloserToGoal;
        public float GoalDistSqr;

        public bool HasHuntProgress => DirectionCorrect || CloserToGoal;
    }

    public static bool TryPickHuntJumpTarget(
        Vector2 from,
        Vector2 goal,
        Vector2 arcNormal,
        int fromEdgeIndex,
        float minJumpDist,
        float maxJumpDist,
        float arcHeight,
        float surfaceSnapMaxDistance,
        float surfaceOffset,
        float bodyRadius,
        IReadOnlyList<Vector2> excludeTargets,
        bool hasRecentVisit,
        Vector2 recentVisitPoint,
        int raySeed,
        out Vector2 jumpTarget,
        out string pickReason,
        List<Vector2> debugCandidates = null,
        List<Vector2> debugRoute = null)
    {
        return TryPickJumpTarget(
            from, goal, arcNormal, fromEdgeIndex,
            minJumpDist, maxJumpDist, arcHeight, surfaceSnapMaxDistance, surfaceOffset, bodyRadius,
            default, restrictToActivityBounds: false,
            excludeTargets, hasRecentVisit, recentVisitPoint,
            huntMode: true, relaxFilters: false, raySeed,
            out jumpTarget, out pickReason, debugCandidates, debugRoute);
    }

    public static bool TryPickIdleJumpTarget(
        Vector2 from,
        Vector2 goalHint,
        Vector2 arcNormal,
        int fromEdgeIndex,
        float minJumpDist,
        float maxJumpDist,
        float arcHeight,
        float surfaceSnapMaxDistance,
        float surfaceOffset,
        float bodyRadius,
        Bounds activityBounds,
        IReadOnlyList<Vector2> excludeTargets,
        bool hasRecentVisit,
        Vector2 recentVisitPoint,
        int raySeed,
        out Vector2 jumpTarget,
        out string pickReason,
        List<Vector2> debugCandidates = null)
    {
        return TryPickJumpTarget(
            from, goalHint, arcNormal, fromEdgeIndex,
            minJumpDist, maxJumpDist, arcHeight, surfaceSnapMaxDistance, surfaceOffset, bodyRadius,
            activityBounds, restrictToActivityBounds: true,
            excludeTargets, hasRecentVisit, recentVisitPoint,
            huntMode: false, relaxFilters: false, raySeed,
            out jumpTarget, out pickReason, debugCandidates, null);
    }

    public static bool TryPickRelaxedFromCandidates(
        Vector2 from,
        float minJumpDist,
        float maxJumpDist,
        bool hasRecentVisit,
        Vector2 recentVisitPoint,
        int raySeed,
        Bounds activityBounds,
        bool restrictToActivityBounds,
        out Vector2 jumpTarget,
        out string pickReason)
    {
        jumpTarget = from;
        pickReason = "Stay";

        if (sCandidates.Count == 0)
        {
            return false;
        }

        float minJump = minJumpDist + MinJumpSlack;
        float minJumpSqr = minJump * minJump;
        float maxJumpSqr = maxJumpDist * maxJumpDist;
        int topCount = Mathf.Min(TopCandidatePoolSize, sCandidates.Count);
        sTopValidCandidates.Clear();

        for (int i = 0; i < topCount; i++)
        {
            ReachableCandidate candidate = sCandidates[i];
            float jumpSqr = candidate.JumpDist * candidate.JumpDist;

            if (jumpSqr < minJumpSqr || jumpSqr > maxJumpSqr)
            {
                continue;
            }

            if (IsRecentVisitPoint(candidate.Point, hasRecentVisit, recentVisitPoint))
            {
                continue;
            }

            if (!IsInsideBounds(activityBounds, candidate.Point, restrictToActivityBounds))
            {
                continue;
            }

            sTopValidCandidates.Add(candidate);
        }

        if (sTopValidCandidates.Count > 0)
        {
            Random.State previousRandom = Random.state;
            Random.InitState(unchecked(raySeed ^ (int)0x85EBCA6B));
            int pickIndex = Random.Range(0, sTopValidCandidates.Count);
            Random.state = previousRandom;

            jumpTarget = sTopValidCandidates[pickIndex].Point;
            pickReason = "RelaxedRndTop3+" + pickIndex;
            return true;
        }

        for (int i = topCount; i < sCandidates.Count; i++)
        {
            ReachableCandidate candidate = sCandidates[i];
            float jumpSqr = candidate.JumpDist * candidate.JumpDist;

            if (jumpSqr < minJumpSqr || jumpSqr > maxJumpSqr)
            {
                continue;
            }

            if (IsRecentVisitPoint(candidate.Point, hasRecentVisit, recentVisitPoint))
            {
                continue;
            }

            if (!IsInsideBounds(activityBounds, candidate.Point, restrictToActivityBounds))
            {
                continue;
            }

            jumpTarget = candidate.Point;
            pickReason = "RelaxedFallback+" + i;
            return true;
        }

        return false;
    }

    public static bool TryPickDesperateJump(
        Vector2 from,
        Vector2 goal,
        Vector2 arcNormal,
        int fromEdgeIndex,
        float minJumpDist,
        float maxJumpDist,
        float arcHeight,
        float surfaceOffset,
        float bodyRadius,
        bool huntMode,
        Bounds activityBounds,
        bool restrictToActivityBounds,
        out Vector2 jumpTarget,
        out string pickReason,
        List<Vector2> debugCandidates = null)
    {
        int raySeed = HashPickSeed(from, goal);

        return TryPickJumpTarget(
            from, goal, arcNormal, fromEdgeIndex,
            minJumpDist, maxJumpDist, arcHeight,
            surfaceSnapMaxDistance: 1.1f, surfaceOffset, bodyRadius,
            activityBounds, restrictToActivityBounds,
            excludeTargets: null, hasRecentVisit: false, recentVisitPoint: default,
            huntMode, relaxFilters: true, raySeed,
            out jumpTarget, out pickReason, debugCandidates, null);
    }

    private static bool TryPickJumpTarget(
        Vector2 from,
        Vector2 goal,
        Vector2 arcNormal,
        int fromEdgeIndex,
        float minJumpDist,
        float maxJumpDist,
        float arcHeight,
        float surfaceSnapMaxDistance,
        float surfaceOffset,
        float bodyRadius,
        Bounds activityBounds,
        bool restrictToActivityBounds,
        IReadOnlyList<Vector2> excludeTargets,
        bool hasRecentVisit,
        Vector2 recentVisitPoint,
        bool huntMode,
        bool relaxFilters,
        int raySeed,
        out Vector2 jumpTarget,
        out string pickReason,
        List<Vector2> debugCandidates,
        List<Vector2> debugRoute)
    {
        jumpTarget = from;
        pickReason = "Stay";
        debugCandidates?.Clear();
        debugRoute?.Clear();

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || mgr.GetEdgeCount() == 0)
        {
            return false;
        }

        if (fromEdgeIndex < 0)
        {
            fromEdgeIndex = SurfaceEdgePath.FindClosestEdgeIndex(mgr, from);
        }

        if (huntMode && debugRoute != null)
        {
            List<Vector2> route = SurfaceEdgePath.BuildRoutePolylineAllLoops(from, goal);

            if (route.Count > 0)
            {
                debugRoute.AddRange(route);
            }
        }

        float minJump = minJumpDist + MinJumpSlack;
        float maxJump = maxJumpDist;
        float minJumpSqr = minJump * minJump;
        float maxJumpSqr = maxJump * maxJump;
        float fromGoalDistSqr = (goal - from).sqrMagnitude;
        Vector2 toGoalDir = fromGoalDistSqr > 0.0001f ? (goal - from).normalized : Vector2.right;
        int fromLoop = mgr.GetEdge(fromEdgeIndex).loopId;

        CacheTileMetrics(mgr);

        sCandidates.Clear();
        sDedupePoints.Clear();

        CollectRayCandidates(
            mgr, from, goal, fromLoop, toGoalDir, fromGoalDistSqr,
            huntMode, minJump, maxJump, minJumpSqr, maxJumpSqr, surfaceOffset,
            activityBounds, restrictToActivityBounds,
            excludeTargets, hasRecentVisit, recentVisitPoint,
            relaxFilters, raySeed, debugCandidates);

        if (sCandidates.Count == 0)
        {
            return false;
        }

        SortCandidatesByPriority(huntMode);

        if (TryPickFromTopCandidates(
                from, arcNormal, minJump, maxJump, arcHeight, surfaceSnapMaxDistance,
                huntMode, raySeed, hasRecentVisit, recentVisitPoint,
                out jumpTarget, out pickReason))
        {
            return true;
        }

        return TryPickRelaxedFromCandidates(
            from, minJumpDist, maxJumpDist, hasRecentVisit, recentVisitPoint, raySeed,
            activityBounds, restrictToActivityBounds,
            out jumpTarget, out pickReason);
    }

    private static void CacheTileMetrics(TileMapGuideManager mgr)
    {
        Vector2 cellDelta = mgr.CellToWorld(new Vector2Int(1, 0)) - mgr.CellToWorld(Vector2Int.zero);
        sCellWidth = Mathf.Max(0.05f, Mathf.Abs(cellDelta.x));
        float cellHeight = Mathf.Max(0.05f, Mathf.Abs(cellDelta.y));
        sMarchStep = Mathf.Max(0.1f, Mathf.Min(sCellWidth, cellHeight) * 0.25f);
    }

    private static void CollectRayCandidates(
        TileMapGuideManager mgr,
        Vector2 from,
        Vector2 goal,
        int fromLoop,
        Vector2 toGoalDir,
        float fromGoalDistSqr,
        bool huntMode,
        float minJump,
        float maxJump,
        float minJumpSqr,
        float maxJumpSqr,
        float surfaceOffset,
        Bounds activityBounds,
        bool restrictToActivityBounds,
        IReadOnlyList<Vector2> excludeTargets,
        bool hasRecentVisit,
        Vector2 recentVisitPoint,
        bool relaxFilters,
        int raySeed,
        List<Vector2> debugCandidates)
    {
        Random.State previousRandom = Random.state;
        Random.InitState(raySeed);

        int maxMarchSteps = Mathf.CeilToInt(maxJump / sMarchStep) + 1;
        int randomCount = huntMode ? HuntRandomRayCount : IdleRandomRayCount;
        float goalAngleDeg = Mathf.Atan2(toGoalDir.y, toGoalDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < GuaranteedRayAngles.Length; i++)
        {
            CastRayInDirection(
                mgr, from, goal, fromLoop, toGoalDir, fromGoalDistSqr,
                AngleToDirection(GuaranteedRayAngles[i]),
                minJump, maxJump, minJumpSqr, maxJumpSqr, maxMarchSteps, surfaceOffset,
                activityBounds, restrictToActivityBounds,
                excludeTargets, hasRecentVisit, recentVisitPoint, relaxFilters, debugCandidates);
        }

        for (int i = 0; i < randomCount; i++)
        {
            float angle = huntMode && i < Mathf.Max(1, randomCount * 3 / 4)
                ? goalAngleDeg + Random.Range(-55f, 55f)
                : Random.Range(0f, 360f);

            CastRayInDirection(
                mgr, from, goal, fromLoop, toGoalDir, fromGoalDistSqr,
                AngleToDirection(angle),
                minJump, maxJump, minJumpSqr, maxJumpSqr, maxMarchSteps, surfaceOffset,
                activityBounds, restrictToActivityBounds,
                excludeTargets, hasRecentVisit, recentVisitPoint, relaxFilters, debugCandidates);
        }

        Random.state = previousRandom;
    }

    private static void CastRayInDirection(
        TileMapGuideManager mgr,
        Vector2 from,
        Vector2 goal,
        int fromLoop,
        Vector2 toGoalDir,
        float fromGoalDistSqr,
        Vector2 dir,
        float minJump,
        float maxJump,
        float minJumpSqr,
        float maxJumpSqr,
        int maxMarchSteps,
        float surfaceOffset,
        Bounds activityBounds,
        bool restrictToActivityBounds,
        IReadOnlyList<Vector2> excludeTargets,
        bool hasRecentVisit,
        Vector2 recentVisitPoint,
        bool relaxFilters,
        List<Vector2> debugCandidates)
    {
        if (!TryCastTileSurfaceRay(
                mgr, from, dir, minJump, maxJump, maxMarchSteps, surfaceOffset,
                out Vector2 standPoint, out float hitDistance))
        {
            return;
        }

        int hitEdgeIndex = SurfaceEdgePath.FindClosestEdgeIndex(mgr, standPoint);

        TryAddStandCandidate(
            mgr, from, goal, fromLoop, toGoalDir, fromGoalDistSqr,
            standPoint, hitEdgeIndex, minJumpSqr, maxJumpSqr,
            activityBounds, restrictToActivityBounds,
            excludeTargets, hasRecentVisit, recentVisitPoint,
            relaxFilters, debugCandidates, hitDistance);
    }

    /// <summary>
    /// 沿射线步进，返回第一个落在 [minJump,maxJump] 内的可站立面（遇实心格终止，不穿透）。
    /// </summary>
    private static bool TryCastTileSurfaceRay(
        TileMapGuideManager mgr,
        Vector2 from,
        Vector2 dir,
        float minJump,
        float maxJump,
        int maxMarchSteps,
        float surfaceOffset,
        out Vector2 standPoint,
        out float hitDistance)
    {
        standPoint = from;
        hitDistance = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        dir = dir.normalized;

        for (int stepIndex = 1; stepIndex <= maxMarchSteps; stepIndex++)
        {
            float traveled = stepIndex * sMarchStep;

            if (traveled > maxJump + sMarchStep)
            {
                break;
            }

            Vector2 probe = from + dir * traveled;

            if (mgr.IsSolid(mgr.WorldToCell(probe)))
            {
                return false;
            }

            if (!TryResolveSurfaceAt(mgr, probe, dir, surfaceOffset, out Vector2 candidate, out _))
            {
                continue;
            }

            float dist = (candidate - from).magnitude;

            if (dist < minJump)
            {
                continue;
            }

            if (dist > maxJump)
            {
                return false;
            }

            standPoint = candidate;
            hitDistance = dist;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 按射线方向解析 tile 表面：上射天花板、下射地板、水平射墙面/同高地板。
    /// </summary>
    private static bool TryResolveSurfaceAt(
        TileMapGuideManager mgr,
        Vector2 probe,
        Vector2 incomingDir,
        float surfaceOffset,
        out Vector2 standPoint,
        out Vector2 normal)
    {
        standPoint = probe;
        normal = Vector2.up;

        Vector2Int airCell = mgr.WorldToCell(probe);

        if (mgr.IsSolid(airCell))
        {
            return false;
        }

        Vector2Int below = airCell + Vector2Int.down;
        Vector2Int above = airCell + Vector2Int.up;
        bool solidBelow = mgr.IsSolid(below);
        bool solidAbove = mgr.IsSolid(above);

        if (solidAbove && incomingDir.y > 0.2f)
        {
            standPoint = new Vector2(probe.x, mgr.CellCorner(above).y - surfaceOffset);
            normal = Vector2.down;
            return true;
        }

        if (solidBelow && incomingDir.y < -0.2f)
        {
            standPoint = new Vector2(probe.x, mgr.GetSolidCellTop(below).y + surfaceOffset);
            normal = Vector2.up;
            return true;
        }

        if (solidBelow && Mathf.Abs(incomingDir.y) <= 0.65f)
        {
            standPoint = new Vector2(probe.x, mgr.GetSolidCellTop(below).y + surfaceOffset);
            normal = Vector2.up;
            return true;
        }

        if (incomingDir.x > 0.3f && mgr.IsSolid(airCell + Vector2Int.right))
        {
            Vector2Int solidCell = airCell + Vector2Int.right;
            standPoint = new Vector2(mgr.CellCorner(solidCell).x - surfaceOffset, probe.y);
            normal = Vector2.left;
            return true;
        }

        if (incomingDir.x < -0.3f && mgr.IsSolid(airCell + Vector2Int.left))
        {
            Vector2Int solidCell = airCell + Vector2Int.left;
            standPoint = new Vector2(mgr.CellCorner(solidCell).x + sCellWidth + surfaceOffset, probe.y);
            normal = Vector2.right;
            return true;
        }

        return false;
    }

    private static bool SegmentCrossesSolidInterior(TileMapGuideManager mgr, Vector2 from, Vector2 to)
    {
        float dist = Vector2.Distance(from, to);

        if (dist < 0.05f)
        {
            return false;
        }

        int steps = Mathf.Max(2, Mathf.CeilToInt(dist / SolidSegmentStep));

        for (int i = 1; i < steps; i++)
        {
            float t = i / (float)steps;
            Vector2 sample = Vector2.Lerp(from, to, t);

            if (mgr.IsSolid(mgr.WorldToCell(sample)))
            {
                return true;
            }
        }

        return false;
    }

    private static void TryAddStandCandidate(
        TileMapGuideManager mgr,
        Vector2 from,
        Vector2 goal,
        int fromLoop,
        Vector2 toGoalDir,
        float fromGoalDistSqr,
        Vector2 standPoint,
        int edgeIndex,
        float minJumpSqr,
        float maxJumpSqr,
        Bounds activityBounds,
        bool restrictToActivityBounds,
        IReadOnlyList<Vector2> excludeTargets,
        bool hasRecentVisit,
        Vector2 recentVisitPoint,
        bool relaxFilters,
        List<Vector2> debugCandidates,
        float jumpDistance)
    {
        float jumpSqr = jumpDistance * jumpDistance;

        if (jumpSqr < minJumpSqr || jumpSqr > maxJumpSqr)
        {
            return;
        }

        if (IsNearExcludedPoint(standPoint, excludeTargets, RejectedTargetEpsilonSqr))
        {
            return;
        }

        if (IsRecentVisitPoint(standPoint, hasRecentVisit, recentVisitPoint))
        {
            return;
        }

        if (!IsInsideBounds(activityBounds, standPoint, restrictToActivityBounds))
        {
            return;
        }

        if (IsDuplicateCandidate(standPoint))
        {
            return;
        }

        if (SegmentCrossesSolidInterior(mgr, from, standPoint))
        {
            return;
        }

        if (!relaxFilters && !WolfSpiderSurfaceProbe.HasStandHeadroom(standPoint, ResolveStandNormal(mgr, edgeIndex)))
        {
            return;
        }

        debugCandidates?.Add(standPoint);

        Vector2 jumpDir = (standPoint - from).normalized;
        bool crossLoop = edgeIndex >= 0 && mgr.GetEdge(edgeIndex).loopId != fromLoop;
        bool directionCorrect = Vector2.Dot(jumpDir, toGoalDir) >= DirectionCorrectDotThreshold;
        float goalDistSqr = (standPoint - goal).sqrMagnitude;
        bool closerToGoal = goalDistSqr + GoalProgressEpsilon * GoalProgressEpsilon < fromGoalDistSqr;

        sDedupePoints.Add(standPoint);
        sCandidates.Add(new ReachableCandidate
        {
            Point = standPoint,
            JumpDist = jumpDistance,
            CrossLoop = crossLoop,
            DirectionCorrect = directionCorrect,
            CloserToGoal = closerToGoal,
            GoalDistSqr = goalDistSqr
        });
    }

    private static bool TryPickFromTopCandidates(
        Vector2 from,
        Vector2 arcNormal,
        float minJump,
        float maxJump,
        float arcHeight,
        float surfaceSnapMaxDistance,
        bool huntMode,
        int raySeed,
        bool hasRecentVisit,
        Vector2 recentVisitPoint,
        out Vector2 jumpTarget,
        out string pickReason)
    {
        jumpTarget = from;
        pickReason = "Stay";
        sTopValidCandidates.Clear();

        int topCount = Mathf.Min(TopCandidatePoolSize, sCandidates.Count);

        for (int i = 0; i < topCount; i++)
        {
            ReachableCandidate candidate = sCandidates[i];

            if (IsRecentVisitPoint(candidate.Point, hasRecentVisit, recentVisitPoint))
            {
                continue;
            }

            if (!ValidateTrajectory(from, candidate.Point, arcNormal, minJump, maxJump, arcHeight, surfaceSnapMaxDistance))
            {
                continue;
            }

            sTopValidCandidates.Add(candidate);
        }

        if (sTopValidCandidates.Count > 0)
        {
            Random.State previousRandom = Random.state;
            Random.InitState(unchecked(raySeed ^ (int)0x9E3779B9));
            int pickIndex = Random.Range(0, sTopValidCandidates.Count);
            Random.state = previousRandom;

            ReachableCandidate picked = sTopValidCandidates[pickIndex];
            jumpTarget = picked.Point;
            pickReason = DescribePickReason(picked, huntMode) + "+RndTop3";
            return true;
        }

        for (int i = topCount; i < sCandidates.Count; i++)
        {
            ReachableCandidate candidate = sCandidates[i];

            if (IsRecentVisitPoint(candidate.Point, hasRecentVisit, recentVisitPoint))
            {
                continue;
            }

            if (!ValidateTrajectory(from, candidate.Point, arcNormal, minJump, maxJump, arcHeight, surfaceSnapMaxDistance))
            {
                continue;
            }

            jumpTarget = candidate.Point;
            pickReason = DescribePickReason(candidate, huntMode);
            return true;
        }

        return false;
    }

    private static bool IsRecentVisitPoint(Vector2 point, bool hasRecentVisit, Vector2 recentVisitPoint)
    {
        return hasRecentVisit && (point - recentVisitPoint).sqrMagnitude <= RecentVisitExcludeSqr;
    }

    private static bool ValidateTrajectory(
        Vector2 from,
        Vector2 to,
        Vector2 arcNormal,
        float minJump,
        float maxJump,
        float arcHeight,
        float surfaceSnapMaxDistance)
    {
        Vector2 resolvedArcNormal = WolfSpiderSurfaceProbe.ResolveJumpArcNormal(
            from, to, arcNormal, surfaceSnapMaxDistance);

        return WolfSpiderSurfaceProbe.IsValidJumpTarget(
            from, to, minJump, maxJump, arcHeight, resolvedArcNormal);
    }

    private static void SortCandidatesByPriority(bool huntMode)
    {
        sCandidates.Sort((a, b) => CompareCandidates(a, b, huntMode));
    }

    private static int CompareCandidates(ReachableCandidate a, ReachableCandidate b, bool huntMode)
    {
        if (huntMode)
        {
            if (a.GoalDistSqr < b.GoalDistSqr - 0.0001f)
            {
                return -1;
            }

            if (b.GoalDistSqr < a.GoalDistSqr - 0.0001f)
            {
                return 1;
            }

            if (a.HasHuntProgress != b.HasHuntProgress)
            {
                return a.HasHuntProgress ? -1 : 1;
            }

            if (a.CrossLoop != b.CrossLoop)
            {
                return a.CrossLoop ? -1 : 1;
            }

            return a.JumpDist.CompareTo(b.JumpDist);
        }

        if (a.CrossLoop != b.CrossLoop)
        {
            return a.CrossLoop ? -1 : 1;
        }

        return b.JumpDist.CompareTo(a.JumpDist);
    }

    private static Vector2 ResolveStandNormal(TileMapGuideManager mgr, int edgeIndex)
    {
        if (edgeIndex < 0 || edgeIndex >= mgr.GetEdgeCount())
        {
            return Vector2.up;
        }

        Edge edge = mgr.GetEdge(edgeIndex);

        if (!mgr.TryGetStandPointOnEdge(edge, edge.a, 0f, out _, out Vector2 normal))
        {
            return Vector2.up;
        }

        return normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector2.up;
    }

    private static string DescribePickReason(ReachableCandidate candidate, bool huntMode)
    {
        if (huntMode)
        {
            if (candidate.CrossLoop && candidate.HasHuntProgress)
            {
                return "CrossLoop+Progress";
            }

            if (candidate.CrossLoop)
            {
                return "CrossLoop";
            }

            if (candidate.HasHuntProgress)
            {
                return candidate.CloserToGoal ? "CloserToGoal" : "DirectionOK";
            }

            return "HuntFallback";
        }

        return candidate.CrossLoop ? "IdleCrossLoop" : "IdleSameLoop";
    }

    private static int HashPickSeed(Vector2 from, Vector2 goal)
    {
        unchecked
        {
            int seed = 17;
            seed = seed * 31 + Mathf.RoundToInt(from.x * 100f);
            seed = seed * 31 + Mathf.RoundToInt(from.y * 100f);
            seed = seed * 31 + Mathf.RoundToInt(goal.x * 100f);
            seed = seed * 31 + Mathf.RoundToInt(goal.y * 100f);
            return seed;
        }
    }

    private static bool IsDuplicateCandidate(Vector2 point)
    {
        for (int i = 0; i < sDedupePoints.Count; i++)
        {
            if ((sDedupePoints[i] - point).sqrMagnitude <= CandidateDedupeDistanceSqr)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector2 AngleToDirection(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private static bool IsNearExcludedPoint(
        Vector2 candidate,
        IReadOnlyList<Vector2> excludedPoints,
        float radiusSqr)
    {
        if (excludedPoints == null || excludedPoints.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < excludedPoints.Count; i++)
        {
            if ((excludedPoints[i] - candidate).sqrMagnitude <= radiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideBounds(Bounds bounds, Vector2 point, bool restrict)
    {
        if (!restrict || bounds.size.sqrMagnitude < 0.01f)
        {
            return true;
        }

        return bounds.Contains(point);
    }
}
