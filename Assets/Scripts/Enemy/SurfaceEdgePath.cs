using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 沿 TileMapGuideManager 已构建的闭合 loop 外轮廓，生成顶点路点（与 MoleMotor 逐点移动一致）。
/// </summary>
public static class SurfaceEdgePath
{
    public const float VertexEpsilon = 0.02f;

    public static bool SameVertex(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude <= VertexEpsilon * VertexEpsilon;
    }

    /// <summary>
    /// 世界坐标下最近的边（任意 loop），用于落体后贴边。
    /// </summary>
    public static int FindClosestEdgeIndex(TileMapGuideManager mgr, Vector2 worldPos)
    {
        return mgr.FindClosestEdgeIndex(worldPos);
    }

    public static int GetLoopIdOfClosestEdge(TileMapGuideManager mgr, Vector2 worldPos)
    {
        return mgr.GetEdge(FindClosestEdgeIndex(mgr, worldPos)).loopId;
    }

    public static int FindClosestEdgeIndexInLoop(TileMapGuideManager mgr, Vector2 worldPos, int loopId)
    {
        float minDist = float.MaxValue;
        int bestIndex = 0;
        int edgeCount = mgr.GetEdgeCount();

        for (int i = 0; i < edgeCount; i++)
        {
            Edge edge = mgr.GetEdge(i);

            if (edge.loopId != loopId)
            {
                continue;
            }

            float dist = SurfaceEdgeTraversal.DistanceToSegment(worldPos, edge.a, edge.b);

            if (dist < minDist)
            {
                minDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// 吸附到距离 worldPos 最近的边（该边所在 loop），位置投影到边线段上，不跳到远处 loop。
    /// </summary>
    public static bool TrySnapToNearestEdge(
        TileMapGuideManager mgr,
        Vector2 worldPos,
        out int edgeIndex,
        out Edge edge,
        out Vector2 snappedOnEdge)
    {
        edgeIndex = FindClosestEdgeIndex(mgr, worldPos);
        edge = mgr.GetEdge(edgeIndex);
        snappedOnEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(worldPos, edge.a, edge.b);
        return true;
    }

    public static void SyncEdgeStateFromPosition(MonsterBase owner, bool snapPositionToEdge = false)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            owner.HasEdge = false;
            return;
        }

        int loopId = owner.HasEdge
            ? owner.CurrentEdge.loopId
            : GetLoopIdOfClosestEdge(mgr, owner.Position);

        owner.EdgeIndex = FindClosestEdgeIndexInLoop(mgr, owner.Position, loopId);
        owner.CurrentEdge = mgr.GetEdge(owner.EdgeIndex);
        owner.HasEdge = true;

        Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(
            owner.Position,
            owner.CurrentEdge.a,
            owner.CurrentEdge.b
        );

        if (snapPositionToEdge)
        {
            owner.Transform.position = onEdge;
        }

        owner.Target = GetForwardCorner(mgr, owner.EdgeIndex, onEdge, true);
        owner.CurrentTarget = owner.Target;
    }

    /// <summary>
    /// 沿同一 loop 顺/逆时针收集接下来若干拐角顶点（路点列表）。
    /// </summary>
    public static List<Vector2> BuildWanderPath(
        TileMapGuideManager mgr,
        Vector2 fromWorld,
        int startEdgeIndex,
        bool clockwise,
        int cornerCount = 5)
    {
        List<Vector2> path = new List<Vector2>();

        if (mgr == null || cornerCount <= 0)
        {
            return path;
        }

        Edge startEdge = mgr.GetEdge(startEdgeIndex);
        Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(fromWorld, startEdge.a, startEdge.b);
        Vector2 firstCorner = GetForwardCorner(mgr, startEdgeIndex, onEdge, clockwise);

        path.Add(firstCorner);

        int edgeIndex = mgr.GetNextIndex(startEdgeIndex, clockwise);
        Vector2 reachedVertex = firstCorner;

        for (int i = 1; i < cornerCount; i++)
        {
            Edge edge = mgr.GetEdge(edgeIndex);
            Vector2 corner = PickOtherEndpoint(edge, reachedVertex);
            path.Add(corner);
            reachedVertex = corner;
            edgeIndex = mgr.GetNextIndex(edgeIndex, clockwise);
        }

        return path;
    }

    /// <summary>
    /// 同一 loop 上 BFS，输出拐角路点（用于 Snail 去吃道具/回家）。
    /// </summary>
    public static List<Vector2> FindVertexPath(Vector2 fromWorld, Vector2 toWorld, int maxEdgeSteps = 500)
    {
        List<Vector2> result = new List<Vector2>();
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || mgr.GetEdgeCount() == 0)
        {
            return result;
        }

        int startEdge = FindClosestEdgeIndex(mgr, fromWorld);
        int loopId = mgr.GetEdge(startEdge).loopId;
        int goalEdge = FindClosestEdgeIndexInLoop(mgr, toWorld, loopId);

        Edge goalEdgeData = mgr.GetEdge(goalEdge);
        Vector2 goalPoint = SurfaceEdgeTraversal.ClosestPointOnSegment(
            toWorld,
            goalEdgeData.a,
            goalEdgeData.b
        );

        if (startEdge == goalEdge)
        {
            result.Add(goalPoint);
            return result;
        }

        Queue<int> queue = new Queue<int>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        HashSet<int> visited = new HashSet<int>();

        queue.Enqueue(startEdge);
        visited.Add(startEdge);
        cameFrom[startEdge] = startEdge;

        bool found = false;
        int steps = 0;

        while (queue.Count > 0 && steps < maxEdgeSteps)
        {
            steps++;
            int current = queue.Dequeue();

            if (current == goalEdge)
            {
                found = true;
                break;
            }

            TryEnqueue(mgr, current, true, loopId, queue, visited, cameFrom);
            TryEnqueue(mgr, current, false, loopId, queue, visited, cameFrom);
        }

        if (!found)
        {
            result.Add(goalPoint);
            return result;
        }

        List<int> edgeChain = new List<int>();
        int back = goalEdge;

        while (back != startEdge)
        {
            edgeChain.Add(back);
            back = cameFrom[back];
        }

        edgeChain.Reverse();

        Edge startEdgeData = mgr.GetEdge(startEdge);
        Vector2 cursor = SurfaceEdgeTraversal.ClosestPointOnSegment(
            fromWorld,
            startEdgeData.a,
            startEdgeData.b
        );

        int prevEdgeIndex = startEdge;

        for (int i = 0; i < edgeChain.Count; i++)
        {
            int nextEdgeIndex = edgeChain[i];
            Edge prevEdge = mgr.GetEdge(prevEdgeIndex);
            Edge nextEdge = mgr.GetEdge(nextEdgeIndex);
            Vector2 corner = GetSharedVertex(prevEdge, nextEdge);

            if (corner.sqrMagnitude > 0.0001f && !SameVertex(corner, cursor))
            {
                result.Add(corner);
                cursor = corner;
            }

            prevEdgeIndex = nextEdgeIndex;
        }

        if (!SameVertex(cursor, goalPoint))
        {
            result.Add(goalPoint);
        }

        return result;
    }

    private static void TryEnqueue(
        TileMapGuideManager mgr,
        int edgeIndex,
        bool clockwise,
        int loopId,
        Queue<int> queue,
        HashSet<int> visited,
        Dictionary<int, int> cameFrom)
    {
        int next = mgr.GetNextIndex(edgeIndex, clockwise);
        Edge nextEdge = mgr.GetEdge(next);

        if (nextEdge.loopId != loopId || visited.Contains(next))
        {
            return;
        }

        visited.Add(next);
        cameFrom[next] = edgeIndex;
        queue.Enqueue(next);
    }

    public static Vector2 GetForwardCorner(
        TileMapGuideManager mgr,
        int edgeIndex,
        Vector2 onEdge,
        bool clockwise)
    {
        Edge edge = mgr.GetEdge(edgeIndex);
        int nextIndex = mgr.GetNextIndex(edgeIndex, clockwise);
        Edge nextEdge = mgr.GetEdge(nextIndex);

        if (SameVertex(edge.b, nextEdge.a) || SameVertex(edge.b, nextEdge.b))
        {
            return edge.b;
        }

        if (SameVertex(edge.a, nextEdge.a) || SameVertex(edge.a, nextEdge.b))
        {
            return edge.a;
        }

        return Vector2.Distance(onEdge, edge.a) < Vector2.Distance(onEdge, edge.b)
            ? edge.b
            : edge.a;
    }

    private static Vector2 PickOtherEndpoint(Edge edge, Vector2 knownVertex)
    {
        if (SameVertex(knownVertex, edge.a))
        {
            return edge.b;
        }

        return edge.a;
    }

    private static Vector2 GetSharedVertex(Edge a, Edge b)
    {
        if (SameVertex(a.a, b.a) || SameVertex(a.a, b.b))
        {
            return a.a;
        }

        if (SameVertex(a.b, b.a) || SameVertex(a.b, b.b))
        {
            return a.b;
        }

        return Vector2.zero;
    }
}
