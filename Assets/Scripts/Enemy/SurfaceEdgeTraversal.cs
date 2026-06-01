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
}
