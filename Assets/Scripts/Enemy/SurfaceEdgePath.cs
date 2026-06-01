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

    public static bool HasArea(Bounds area)
    {
        return area.size.sqrMagnitude > 0.01f;
    }

    public static bool IsInsideArea(Bounds area, Vector2 point)
    {
        return IsInsideArea(area, point, 0f);
    }

    /// <summary>
    /// 2D 区域判定（忽略 Z），margin 向外扩展判定范围，避免贴边/浮点误差卡在区外。
    /// </summary>
    public static bool IsInsideArea(Bounds area, Vector2 point, float margin)
    {
        if (!HasArea(area))
        {
            return true;
        }

        float minX = area.min.x - margin;
        float maxX = area.max.x + margin;
        float minY = area.min.y - margin;
        float maxY = area.max.y + margin;

        return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
    }

    public static bool EdgeTouchesArea(Edge edge, Bounds area)
    {
        return EdgeTouchesArea(edge, area, 0f);
    }

    public static bool EdgeTouchesArea(Edge edge, Bounds area, float margin)
    {
        if (!HasArea(area))
        {
            return true;
        }

        return ClipSegmentToBoundsXY(edge.a, edge.b, area, margin, out _, out _);
    }

    /// <summary>
    /// 线段与 AABB 相交部分上，离 reference 最近的点（用于 Idle 锚点/出生吸附）。
    /// </summary>
    public static Vector2 ClosestPointOnSegmentInsideBounds(
        Vector2 segmentA,
        Vector2 segmentB,
        Bounds bounds,
        Vector2 reference,
        float margin = 0f)
    {
        if (!HasArea(bounds))
        {
            return SurfaceEdgeTraversal.ClosestPointOnSegment(reference, segmentA, segmentB);
        }

        if (!ClipSegmentToBoundsXY(segmentA, segmentB, bounds, margin, out Vector2 clipA, out Vector2 clipB))
        {
            return IsInsideArea(bounds, reference, margin) ? reference : (Vector2)bounds.center;
        }

        return SurfaceEdgeTraversal.ClosestPointOnSegment(reference, clipA, clipB);
    }

    private static bool ClipSegmentToBoundsXY(
        Vector2 p0,
        Vector2 p1,
        Bounds bounds,
        float margin,
        out Vector2 clipA,
        out Vector2 clipB)
    {
        float minX = bounds.min.x - margin;
        float maxX = bounds.max.x + margin;
        float minY = bounds.min.y - margin;
        float maxY = bounds.max.y + margin;

        float t0 = 0f;
        float t1 = 1f;
        Vector2 d = p1 - p0;

        if (!ClipAxis(p0.x, d.x, minX, maxX, ref t0, ref t1))
        {
            clipA = clipB = p0;
            return false;
        }

        if (!ClipAxis(p0.y, d.y, minY, maxY, ref t0, ref t1))
        {
            clipA = clipB = p0;
            return false;
        }

        clipA = p0 + d * t0;
        clipB = p0 + d * t1;
        return t1 >= t0;
    }

    private static bool ClipAxis(float p, float dp, float min, float max, ref float t0, ref float t1)
    {
        if (Mathf.Abs(dp) < 1e-8f)
        {
            return p >= min && p <= max;
        }

        float tEnter = (min - p) / dp;
        float tExit = (max - p) / dp;

        if (tEnter > tExit)
        {
            float tmp = tEnter;
            tEnter = tExit;
            tExit = tmp;
        }

        t0 = Mathf.Max(t0, tEnter);
        t1 = Mathf.Min(t1, tExit);
        return t0 <= t1;
    }

    /// <summary>
    /// 在 bounds 内、距离 worldPos 最近的边（用于 Idle 区出生/贴边）。
    /// </summary>
    public static int FindClosestEdgeIndexInBounds(TileMapGuideManager mgr, Vector2 worldPos, Bounds bounds)
    {
        return FindClosestEdgeIndexInBounds(mgr, worldPos, bounds, 0f);
    }

    public static int FindClosestEdgeIndexInBounds(
        TileMapGuideManager mgr,
        Vector2 worldPos,
        Bounds bounds,
        float margin)
    {
        if (!HasArea(bounds))
        {
            return FindClosestEdgeIndex(mgr, worldPos);
        }

        float minDist = float.MaxValue;
        int bestIndex = -1;
        int edgeCount = mgr.GetEdgeCount();

        for (int i = 0; i < edgeCount; i++)
        {
            Edge edge = mgr.GetEdge(i);

            if (!EdgeTouchesArea(edge, bounds, margin))
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

        if (bestIndex < 0)
        {
            return FindClosestEdgeIndex(mgr, worldPos);
        }

        return bestIndex;
    }

    /// <summary>
    /// 在区域内沿 loop 收集路点；走出区域则停止，用于 Idle 来回游走。
    /// </summary>
    public static List<Vector2> BuildWanderPathInArea(
        TileMapGuideManager mgr,
        Vector2 fromWorld,
        int startEdgeIndex,
        bool clockwise,
        Bounds area,
        int cornerCount = 6)
    {
        List<Vector2> path = new List<Vector2>();

        if (mgr == null || cornerCount <= 0)
        {
            return path;
        }

        Edge startEdge = mgr.GetEdge(startEdgeIndex);
        Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(fromWorld, startEdge.a, startEdge.b);
        Vector2 firstCorner = GetForwardCorner(mgr, startEdgeIndex, onEdge, clockwise);

        if (HasArea(area) && !IsInsideArea(area, firstCorner))
        {
            return path;
        }

        path.Add(firstCorner);

        int edgeIndex = mgr.GetNextIndex(startEdgeIndex, clockwise);
        Vector2 reachedVertex = firstCorner;

        for (int i = 1; i < cornerCount; i++)
        {
            Edge edge = mgr.GetEdge(edgeIndex);
            Vector2 corner = PickOtherEndpoint(edge, reachedVertex);

            if (HasArea(area) && !IsInsideArea(area, corner))
            {
                break;
            }

            path.Add(corner);
            reachedVertex = corner;
            edgeIndex = mgr.GetNextIndex(edgeIndex, clockwise);
        }

        return path;
    }

    /// <summary>
    /// 优先 preferredClockwise，若无路点则尝试反方向。
    /// </summary>
    public static List<Vector2> BuildIdlePingPongPath(
        TileMapGuideManager mgr,
        Vector2 fromWorld,
        int startEdgeIndex,
        bool preferredClockwise,
        Bounds idleArea,
        int cornerCount,
        out bool usedClockwise)
    {
        usedClockwise = preferredClockwise;

        List<Vector2> path = BuildWanderPathInArea(
            mgr,
            fromWorld,
            startEdgeIndex,
            preferredClockwise,
            idleArea,
            cornerCount
        );

        if (path.Count > 0)
        {
            return path;
        }

        usedClockwise = !preferredClockwise;

        return BuildWanderPathInArea(
            mgr,
            fromWorld,
            startEdgeIndex,
            usedClockwise,
            idleArea,
            cornerCount
        );
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
