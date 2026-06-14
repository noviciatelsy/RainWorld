using UnityEngine;

/// <summary>
/// 贴边爬行共用：沿边线段移动、过顶点切边。不修改 TileMapGuideManager 的边构建。
/// </summary>
public static class SurfaceEdgeTraversal
{
    public const float ArriveEpsilon = 0.05f;
    public const float OnEdgeMaxDistance = 0.4f;

    public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;

        if (ab.sqrMagnitude < 0.0001f)
        {
            return a;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
        return a + ab * t;
    }

    public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        return Vector2.Distance(point, ClosestPointOnSegment(point, a, b));
    }

    /// <summary>
    /// 到达当前边的端点 vertex 后，沿 loop 切到下一条边，并指向新边的另一端。
    /// </summary>
    public static void AdvanceToNextEdge(
        TileMapGuideManager mgr,
        ref int edgeIndex,
        ref Edge currentEdge,
        ref Vector2 target,
        Vector2 reachedVertex,
        bool clockwise)
    {
        edgeIndex = mgr.GetNextIndex(edgeIndex, clockwise);
        Edge nextEdge = mgr.GetEdge(edgeIndex);
        currentEdge = nextEdge;
        target = PickForwardEndpoint(nextEdge, reachedVertex);
    }

    public static Vector2 PickForwardEndpoint(Edge edge, Vector2 fromVertex)
    {
        if (Vector2.Distance(fromVertex, edge.a) <= ArriveEpsilon)
        {
            return edge.b;
        }

        if (Vector2.Distance(fromVertex, edge.b) <= ArriveEpsilon)
        {
            return edge.a;
        }

        return Vector2.Distance(fromVertex, edge.a) < Vector2.Distance(fromVertex, edge.b)
            ? edge.b
            : edge.a;
    }

    public static Vector2 PickInitialTarget(Vector2 position, Edge edge)
    {
        return Vector2.Distance(position, edge.a) < Vector2.Distance(position, edge.b)
            ? edge.b
            : edge.a;
    }

    public static bool TrySnapToClosestEdge(
        TileMapGuideManager mgr,
        Vector2 worldPos,
        out int edgeIndex,
        out Edge edge,
        out Vector2 snappedOnEdge,
        out Vector2 target)
    {
        edgeIndex = mgr.FindClosestEdgeIndex(worldPos);
        edge = mgr.GetEdge(edgeIndex);
        snappedOnEdge = ClosestPointOnSegment(worldPos, edge.a, edge.b);
        target = PickInitialTarget(snappedOnEdge, edge);
        return true;
    }

    public static bool IsNearEdge(Vector2 position, Edge edge, float maxDistance = OnEdgeMaxDistance)
    {
        return DistanceToSegment(position, edge.a, edge.b) <= maxDistance;
    }

    public struct LoopFootState
    {
        public int edgeIndex;
        public Vector2 point;
    }

    public static LoopFootState ResolveFootOnLoop(
        TileMapGuideManager mgr,
        int loopId,
        int previousEdgeIndex,
        Vector2 worldPoint,
        float stickThreshold = 0.35f)
    {
        Edge previous = mgr.GetEdge(previousEdgeIndex);

        if (previous.loopId == loopId)
        {
            float distPrev = DistanceToSegment(worldPoint, previous.a, previous.b);

            if (distPrev <= stickThreshold)
            {
                return new LoopFootState
                {
                    edgeIndex = previousEdgeIndex,
                    point = ClosestPointOnSegment(worldPoint, previous.a, previous.b)
                };
            }
        }

        int edgeIndex = SurfaceEdgePath.FindClosestEdgeIndexInLoop(mgr, worldPoint, loopId);
        Edge edge = mgr.GetEdge(edgeIndex);

        return new LoopFootState
        {
            edgeIndex = edgeIndex,
            point = ClosestPointOnSegment(worldPoint, edge.a, edge.b)
        };
    }

    public static Vector2 ProjectOntoEdge(TileMapGuideManager mgr, int edgeIndex, Vector2 worldPoint)
    {
        Edge edge = mgr.GetEdge(edgeIndex);
        return ClosestPointOnSegment(worldPoint, edge.a, edge.b);
    }

    public static LoopFootState WalkAlongLoop(
        TileMapGuideManager mgr,
        int edgeIndex,
        Vector2 fromPoint,
        bool clockwise,
        float distance,
        int loopId)
    {
        float remaining = Mathf.Max(0f, distance);
        int currentEdgeIndex = edgeIndex;
        Vector2 currentPoint = fromPoint;
        int safety = 0;

        while (remaining > 0.0001f && safety++ < 128)
        {
            Edge edge = mgr.GetEdge(currentEdgeIndex);

            if (edge.loopId != loopId)
            {
                break;
            }

            currentPoint = ClosestPointOnSegment(currentPoint, edge.a, edge.b);
            Vector2 corner = SurfaceEdgePath.GetForwardCorner(mgr, currentEdgeIndex, currentPoint, clockwise);
            float distToCorner = Vector2.Distance(currentPoint, corner);

            if (remaining <= distToCorner + ArriveEpsilon)
            {
                Vector2 dir = corner - currentPoint;

                if (dir.sqrMagnitude < 0.0001f)
                {
                    dir = PickForwardEndpoint(edge, corner) - corner;
                }

                if (dir.sqrMagnitude < 0.0001f)
                {
                    return new LoopFootState { edgeIndex = currentEdgeIndex, point = corner };
                }

                currentPoint += dir.normalized * remaining;
                currentPoint = ClosestPointOnSegment(currentPoint, edge.a, edge.b);

                return new LoopFootState { edgeIndex = currentEdgeIndex, point = currentPoint };
            }

            remaining -= distToCorner;
            currentPoint = corner;
            currentEdgeIndex = mgr.GetNextIndex(currentEdgeIndex, clockwise);
        }

        Edge finalEdge = mgr.GetEdge(currentEdgeIndex);
        currentPoint = ClosestPointOnSegment(currentPoint, finalEdge.a, finalEdge.b);

        return new LoopFootState { edgeIndex = currentEdgeIndex, point = currentPoint };
    }

    public static float DistanceAlongLoopForward(
        TileMapGuideManager mgr,
        int fromEdgeIndex,
        Vector2 fromPoint,
        int toEdgeIndex,
        Vector2 toPoint,
        bool clockwise,
        int loopId)
    {
        float total = 0f;
        int currentEdgeIndex = fromEdgeIndex;
        Vector2 currentPoint = fromPoint;
        int safety = 0;

        while (safety++ < 256)
        {
            Edge edge = mgr.GetEdge(currentEdgeIndex);

            if (edge.loopId != loopId)
            {
                break;
            }

            currentPoint = ClosestPointOnSegment(currentPoint, edge.a, edge.b);

            if (currentEdgeIndex == toEdgeIndex)
            {
                Vector2 target = ClosestPointOnSegment(toPoint, edge.a, edge.b);
                total += Vector2.Distance(currentPoint, target);
                return total;
            }

            Vector2 corner = SurfaceEdgePath.GetForwardCorner(mgr, currentEdgeIndex, currentPoint, clockwise);
            total += Vector2.Distance(currentPoint, corner);
            currentPoint = corner;
            currentEdgeIndex = mgr.GetNextIndex(currentEdgeIndex, clockwise);
        }

        return total;
    }

    public static LoopFootState MoveOnLoopTowards(
        TileMapGuideManager mgr,
        int edgeIndex,
        Vector2 fromPoint,
        int targetEdgeIndex,
        Vector2 targetPoint,
        bool clockwise,
        float maxStep,
        int loopId)
    {
        float remaining = DistanceAlongLoopForward(
            mgr,
            edgeIndex,
            fromPoint,
            targetEdgeIndex,
            targetPoint,
            clockwise,
            loopId
        );

        if (remaining <= ArriveEpsilon)
        {
            return new LoopFootState
            {
                edgeIndex = targetEdgeIndex,
                point = ProjectOntoEdge(mgr, targetEdgeIndex, targetPoint)
            };
        }

        float step = Mathf.Min(maxStep, remaining);
        return WalkAlongLoop(mgr, edgeIndex, fromPoint, clockwise, step, loopId);
    }

    public static bool SameLoopFoot(LoopFootState a, LoopFootState b)
    {
        return a.edgeIndex == b.edgeIndex && Vector2.Distance(a.point, b.point) <= ArriveEpsilon;
    }
}
