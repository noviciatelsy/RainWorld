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
        int backtrackSteps = 0;
        const int maxBacktrackSteps = 256;

        while (back != startEdge && backtrackSteps < maxBacktrackSteps)
        {
            backtrackSteps++;
            edgeChain.Add(back);

            if (!cameFrom.TryGetValue(back, out int previous))
            {
                break;
            }

            back = previous;
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

    /// <summary>
    /// 从 from 到 goal 沿全图边链的有序折线（首点为 from 在起点边上的投影）。
    /// </summary>
    public static List<Vector2> BuildRoutePolylineAllLoops(Vector2 fromWorld, Vector2 toWorld)
    {
        List<Vector2> route = new List<Vector2>();
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || mgr.GetEdgeCount() == 0)
        {
            return route;
        }

        int startEdge = FindClosestEdgeIndex(mgr, fromWorld);
        Edge startEdgeData = mgr.GetEdge(startEdge);
        Vector2 startOnEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(
            fromWorld,
            startEdgeData.a,
            startEdgeData.b);
        route.Add(startOnEdge);

        List<Vector2> corners = FindVertexPathAllLoops(fromWorld, toWorld);
        Vector2 last = startOnEdge;

        for (int i = 0; i < corners.Count; i++)
        {
            if (!SameVertex(corners[i], last))
            {
                route.Add(corners[i]);
                last = corners[i];
            }
        }

        return route;
    }

    /// <summary>
    /// 将追击目标投影到轮廓上（沿 Dijkstra 选定的 goal 边），避免空中坐标导致无法折线逼近。
    /// </summary>
    public static Vector2 ProjectApproachGoalOnContour(Vector2 fromWorld, Vector2 toWorld)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || mgr.GetEdgeCount() == 0)
        {
            return toWorld;
        }

        int goalEdge = FindBestGoalEdgeForPath(fromWorld, toWorld);
        Edge edge = mgr.GetEdge(goalEdge);
        return SurfaceEdgeTraversal.ClosestPointOnSegment(toWorld, edge.a, edge.b);
    }

    public enum EdgeOrientationKind
    {
        Horizontal,
        Vertical
    }

    public static EdgeOrientationKind GetEdgeOrientation(Edge edge)
    {
        Vector2 delta = edge.b - edge.a;
        return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? EdgeOrientationKind.Horizontal
            : EdgeOrientationKind.Vertical;
    }

    public static bool IsDifferentOrientation(Edge fromEdge, Edge toEdge)
    {
        return GetEdgeOrientation(fromEdge) != GetEdgeOrientation(toEdge);
    }

    private const float SameLoopPathPenalty = 2.25f;

    /// <summary>
    /// 同 loop 上 CW/CCW 直接相邻边（沿轮廓爬行，非马步）。
    /// </summary>
    public static bool IsDirectNeighborInSameLoop(TileMapGuideManager mgr, int fromEdgeIndex, int toEdgeIndex)
    {
        if (mgr == null || fromEdgeIndex < 0 || toEdgeIndex < 0 || fromEdgeIndex == toEdgeIndex)
        {
            return false;
        }

        Edge fromEdge = mgr.GetEdge(fromEdgeIndex);
        Edge toEdge = mgr.GetEdge(toEdgeIndex);

        if (fromEdge.loopId != toEdge.loopId)
        {
            return false;
        }

        return toEdgeIndex == mgr.GetNextIndex(fromEdgeIndex, true)
            || toEdgeIndex == mgr.GetNextIndex(fromEdgeIndex, false);
    }

    /// <summary>
    /// 马步候选边：跨 loop 共享顶点边 + 同 loop 上 depth-2 的 L 形折线边（排除同边与 depth-1 爬行）。
    /// </summary>
    public static void CollectKnightCandidateEdges(
        TileMapGuideManager mgr,
        int fromEdgeIndex,
        HashSet<int> output)
    {
        output.Clear();

        if (mgr == null || fromEdgeIndex < 0)
        {
            return;
        }

        Edge fromEdge = mgr.GetEdge(fromEdgeIndex);
        int fromLoop = fromEdge.loopId;
        List<List<int>> adjacency = GetEdgeAdjacencyAllLoops(mgr);
        List<int> neighbors = adjacency[fromEdgeIndex];

        for (int i = 0; i < neighbors.Count; i++)
        {
            int edgeIndex = neighbors[i];

            if (edgeIndex == fromEdgeIndex)
            {
                continue;
            }

            if (mgr.GetEdge(edgeIndex).loopId != fromLoop)
            {
                output.Add(edgeIndex);
            }
        }

        HashSet<int> depthOne = new HashSet<int>();
        CollectNeighborEdgeIndices(mgr, fromEdgeIndex, 1, depthOne);
        HashSet<int> depthTwo = new HashSet<int>();
        CollectNeighborEdgeIndices(mgr, fromEdgeIndex, 2, depthTwo);

        foreach (int edgeIndex in depthTwo)
        {
            if (edgeIndex == fromEdgeIndex || depthOne.Contains(edgeIndex))
            {
                continue;
            }

            if (mgr.GetEdge(edgeIndex).loopId == fromLoop)
            {
                output.Add(edgeIndex);
            }
        }
    }

    /// <summary>
    /// 沿 Dijkstra 马步折线路线，在跳跃距离环内选取最远可达路点（优先跨 loop 拐角）。
    /// </summary>
    public static bool TryPickRouteWaypointInJumpRange(
        Vector2 fromWorld,
        int fromEdgeIndex,
        Vector2 goalWorld,
        float minJumpDist,
        float maxJumpDist,
        out Vector2 waypoint,
        out int waypointEdgeIndex)
    {
        waypoint = fromWorld;
        waypointEdgeIndex = fromEdgeIndex;

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || mgr.GetEdgeCount() == 0)
        {
            return false;
        }

        if (fromEdgeIndex < 0)
        {
            fromEdgeIndex = FindClosestEdgeIndex(mgr, fromWorld);
        }

        List<Vector2> route = BuildRoutePolylineAllLoops(fromWorld, goalWorld);

        if (route.Count < 2)
        {
            return false;
        }

        float minJumpSqr = minJumpDist * minJumpDist;
        float maxJumpSqr = maxJumpDist * maxJumpDist;
        int bestRouteIndex = -1;
        float bestGoalDistSqr = float.MaxValue;

        for (int i = 1; i < route.Count; i++)
        {
            Vector2 candidate = route[i];
            float jumpSqr = (candidate - fromWorld).sqrMagnitude;

            if (jumpSqr < minJumpSqr)
            {
                continue;
            }

            if (jumpSqr > maxJumpSqr)
            {
                Vector2 prev = route[i - 1];
                Vector2 seg = candidate - prev;
                float segLen = seg.magnitude;

                if (segLen > 0.001f)
                {
                    Vector2 dir = seg / segLen;
                    float targetDist = Mathf.Clamp(maxJumpDist * 0.95f, minJumpDist, maxJumpDist);
                    Vector2 clamped = fromWorld + (candidate - fromWorld).normalized * targetDist;
                    jumpSqr = (clamped - fromWorld).sqrMagnitude;
                    candidate = clamped;
                }

                if (jumpSqr < minJumpSqr || jumpSqr > maxJumpSqr)
                {
                    continue;
                }
            }

            float goalDistSqr = (candidate - goalWorld).sqrMagnitude;

            if (bestRouteIndex < 0 || goalDistSqr < bestGoalDistSqr - 0.0001f)
            {
                bestRouteIndex = i;
                bestGoalDistSqr = goalDistSqr;
                waypoint = candidate;
            }
        }

        if (bestRouteIndex >= 0)
        {
            waypointEdgeIndex = FindEdgeIndexForStandPoint(mgr, waypoint);
            return true;
        }

        Vector2 firstCorner = route[1];
        float cornerDistSqr = (firstCorner - fromWorld).sqrMagnitude;

        if (cornerDistSqr > maxJumpSqr)
        {
            Vector2 dir = (firstCorner - fromWorld).normalized;
            float clampDist = Mathf.Clamp(maxJumpDist * 0.95f, minJumpDist, maxJumpDist);
            Vector2 clamped = fromWorld + dir * clampDist;

            if ((clamped - fromWorld).sqrMagnitude >= minJumpSqr)
            {
                waypoint = clamped;
                waypointEdgeIndex = FindEdgeIndexForStandPoint(mgr, clamped);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 从起点边沿邻接图扩展 maxDepth 跳，收集候选边（不含起点边）。
    /// </summary>
    public static void CollectNeighborEdgeIndices(
        TileMapGuideManager mgr,
        int fromEdgeIndex,
        int maxDepth,
        HashSet<int> output)
    {
        output.Clear();

        if (mgr == null || fromEdgeIndex < 0 || maxDepth <= 0)
        {
            return;
        }

        List<List<int>> adjacency = GetEdgeAdjacencyAllLoops(mgr);
        HashSet<int> visited = new HashSet<int> { fromEdgeIndex };
        List<int> frontier = new List<int> { fromEdgeIndex };

        for (int depth = 0; depth < maxDepth && frontier.Count > 0; depth++)
        {
            List<int> nextFrontier = new List<int>();

            for (int i = 0; i < frontier.Count; i++)
            {
                List<int> neighbors = adjacency[frontier[i]];

                for (int n = 0; n < neighbors.Count; n++)
                {
                    int edgeIndex = neighbors[n];

                    if (!visited.Add(edgeIndex))
                    {
                        continue;
                    }

                    output.Add(edgeIndex);
                    nextFrontier.Add(edgeIndex);
                }
            }

            frontier = nextFrontier;
        }
    }

    private static int cachedAdjacencyEdgeCount = -1;
    private static List<List<int>> cachedEdgeAdjacency;

    /// <summary>
    /// 全图边邻接：同 loop 的 CW/CCW + 共享顶点的跨 loop 边。
    /// </summary>
    public static List<List<int>> GetEdgeAdjacencyAllLoops(TileMapGuideManager mgr)
    {
        int edgeCount = mgr.GetEdgeCount();

        if (cachedEdgeAdjacency != null && cachedAdjacencyEdgeCount == edgeCount)
        {
            return cachedEdgeAdjacency;
        }

        cachedEdgeAdjacency = BuildEdgeAdjacencyAllLoops(mgr);
        cachedAdjacencyEdgeCount = edgeCount;
        return cachedEdgeAdjacency;
    }

    public static int FindEdgeIndexForStandPoint(TileMapGuideManager mgr, Vector2 standPoint)
    {
        if (mgr == null)
        {
            return -1;
        }

        int closest = FindClosestEdgeIndex(mgr, standPoint);
        List<List<int>> adjacency = GetEdgeAdjacencyAllLoops(mgr);
        float bestDist = SurfaceEdgeTraversal.DistanceToSegment(
            standPoint,
            mgr.GetEdge(closest).a,
            mgr.GetEdge(closest).b);

        List<int> neighbors = adjacency[closest];

        for (int i = 0; i < neighbors.Count; i++)
        {
            int edgeIndex = neighbors[i];
            Edge edge = mgr.GetEdge(edgeIndex);
            float dist = SurfaceEdgeTraversal.DistanceToSegment(standPoint, edge.a, edge.b);

            if (dist < bestDist)
            {
                bestDist = dist;
                closest = edgeIndex;
            }
        }

        return closest;
    }

    /// <summary>
    /// 在目标附近候选边中，选从起点沿轮廓 Dijkstra 代价最小的 goal 边（避免只认最近 loop）。
    /// </summary>
    public static int FindBestGoalEdgeForPath(Vector2 fromWorld, Vector2 toWorld, float goalEdgeRadius = 1.35f)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || mgr.GetEdgeCount() == 0)
        {
            return 0;
        }

        int startEdge = FindClosestEdgeIndex(mgr, fromWorld);
        int hintEdge = FindClosestEdgeIndex(mgr, toWorld);
        List<List<int>> adjacency = GetEdgeAdjacencyAllLoops(mgr);
        HashSet<int> candidates = new HashSet<int> { hintEdge };

        for (int i = 0; i < adjacency[hintEdge].Count; i++)
        {
            candidates.Add(adjacency[hintEdge][i]);
        }

        int edgeCount = mgr.GetEdgeCount();

        for (int i = 0; i < edgeCount; i++)
        {
            Edge edge = mgr.GetEdge(i);

            if (SurfaceEdgeTraversal.DistanceToSegment(toWorld, edge.a, edge.b) <= goalEdgeRadius)
            {
                candidates.Add(i);
            }
        }

        int bestEdge = hintEdge;
        float bestCost = float.MaxValue;

        foreach (int candidate in candidates)
        {
            if (!TryGetContourDistanceToGoalEdge(mgr, startEdge, fromWorld, candidate, toWorld, out float cost))
            {
                continue;
            }

            if (cost < bestCost)
            {
                bestCost = cost;
                bestEdge = candidate;
            }
        }

        return bestEdge;
    }

    private static bool TryGetContourDistanceToGoalEdge(
        TileMapGuideManager mgr,
        int startEdge,
        Vector2 fromWorld,
        int goalEdge,
        Vector2 toWorld,
        out float cost)
    {
        cost = float.MaxValue;

        Edge startEdgeData = mgr.GetEdge(startEdge);
        Vector2 startOnEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(
            fromWorld,
            startEdgeData.a,
            startEdgeData.b);

        if (startEdge == goalEdge)
        {
            Edge goalEdgeData = mgr.GetEdge(goalEdge);
            Vector2 goalPoint = SurfaceEdgeTraversal.ClosestPointOnSegment(
                toWorld,
                goalEdgeData.a,
                goalEdgeData.b);
            cost = Vector2.Distance(startOnEdge, goalPoint);
            return true;
        }

        List<List<int>> adjacency = GetEdgeAdjacencyAllLoops(mgr);
        Dictionary<int, float> dist = new Dictionary<int, float>();
        Dictionary<int, Vector2> anchorOnEdge = new Dictionary<int, Vector2>();
        List<int> open = new List<int>();

        dist[startEdge] = 0f;
        anchorOnEdge[startEdge] = startOnEdge;
        open.Add(startEdge);

        int dijkstraSteps = 0;
        const int maxDijkstraSteps = 600;

        while (open.Count > 0 && dijkstraSteps < maxDijkstraSteps)
        {
            dijkstraSteps++;
            int current = PopClosestEdge(open, dist);

            if (current == goalEdge)
            {
                Edge goalEdgeData = mgr.GetEdge(goalEdge);
                Vector2 goalPoint = SurfaceEdgeTraversal.ClosestPointOnSegment(
                    toWorld,
                    goalEdgeData.a,
                    goalEdgeData.b);
                cost = dist[current] + Vector2.Distance(anchorOnEdge[current], goalPoint);
                return true;
            }

            Edge currentEdge = mgr.GetEdge(current);
            Vector2 onCurrent = anchorOnEdge[current];
            List<int> neighbors = adjacency[current];

            for (int i = 0; i < neighbors.Count; i++)
            {
                int next = neighbors[i];
                Edge nextEdge = mgr.GetEdge(next);
                Vector2 shared = GetSharedVertex(currentEdge, nextEdge);
                Vector2 exitPoint = shared.sqrMagnitude > 0.0001f
                    ? shared
                    : SurfaceEdgeTraversal.ClosestPointOnSegment(onCurrent, nextEdge.a, nextEdge.b);
                Vector2 enterPoint = SurfaceEdgeTraversal.ClosestPointOnSegment(
                    onCurrent,
                    currentEdge.a,
                    currentEdge.b);
                float stepCost = ComputeContourStepCost(mgr, current, next, enterPoint, exitPoint);
                float newDist = dist[current] + stepCost;

                if (dist.TryGetValue(next, out float oldDist) && newDist >= oldDist - 0.0001f)
                {
                    continue;
                }

                dist[next] = newDist;
                anchorOnEdge[next] = exitPoint;

                if (!open.Contains(next))
                {
                    open.Add(next);
                }
            }
        }

        return false;
    }

    private static float ComputeContourStepCost(
        TileMapGuideManager mgr,
        int fromEdgeIndex,
        int toEdgeIndex,
        Vector2 enterPoint,
        Vector2 exitPoint)
    {
        float stepCost = Mathf.Max(0.001f, Vector2.Distance(enterPoint, exitPoint));
        Edge fromEdge = mgr.GetEdge(fromEdgeIndex);
        Edge toEdge = mgr.GetEdge(toEdgeIndex);

        if (fromEdge.loopId == toEdge.loopId)
        {
            stepCost *= SameLoopPathPenalty;
        }

        return stepCost;
    }

    private static List<List<int>> BuildEdgeAdjacencyAllLoops(TileMapGuideManager mgr)
    {
        int edgeCount = mgr.GetEdgeCount();
        List<List<int>> adjacency = new List<List<int>>(edgeCount);
        Dictionary<long, List<int>> vertexToEdges = new Dictionary<long, List<int>>();

        for (int i = 0; i < edgeCount; i++)
        {
            adjacency.Add(new List<int>(6));
            Edge edge = mgr.GetEdge(i);
            RegisterVertexEdge(vertexToEdges, edge.a, i);
            RegisterVertexEdge(vertexToEdges, edge.b, i);
        }

        for (int i = 0; i < edgeCount; i++)
        {
            AddAdjacencyEdge(adjacency, i, mgr.GetNextIndex(i, true));
            AddAdjacencyEdge(adjacency, i, mgr.GetNextIndex(i, false));

            Edge edge = mgr.GetEdge(i);
            LinkVertexAdjacency(adjacency, vertexToEdges, edge.a, i);
            LinkVertexAdjacency(adjacency, vertexToEdges, edge.b, i);
        }

        return adjacency;
    }

    private static void RegisterVertexEdge(Dictionary<long, List<int>> vertexToEdges, Vector2 vertex, int edgeIndex)
    {
        long key = QuantizeVertexKey(vertex);

        if (!vertexToEdges.TryGetValue(key, out List<int> edges))
        {
            edges = new List<int>(4);
            vertexToEdges[key] = edges;
        }

        edges.Add(edgeIndex);
    }

    private static void LinkVertexAdjacency(
        List<List<int>> adjacency,
        Dictionary<long, List<int>> vertexToEdges,
        Vector2 vertex,
        int edgeIndex)
    {
        if (!vertexToEdges.TryGetValue(QuantizeVertexKey(vertex), out List<int> edges))
        {
            return;
        }

        for (int i = 0; i < edges.Count; i++)
        {
            AddAdjacencyEdge(adjacency, edgeIndex, edges[i]);
        }
    }

    private static void AddAdjacencyEdge(List<List<int>> adjacency, int from, int to)
    {
        if (from == to)
        {
            return;
        }

        List<int> neighbors = adjacency[from];

        for (int i = 0; i < neighbors.Count; i++)
        {
            if (neighbors[i] == to)
            {
                return;
            }
        }

        neighbors.Add(to);
    }

    private static long QuantizeVertexKey(Vector2 vertex)
    {
        int x = Mathf.RoundToInt(vertex.x / VertexEpsilon);
        int y = Mathf.RoundToInt(vertex.y / VertexEpsilon);
        return ((long)x << 32) ^ (uint)y;
    }

    /// <summary>
    /// 跨 loop 的边 Dijkstra 折线路径（共享顶点连通，代价为沿边几何距离）。
    /// </summary>
    public static List<Vector2> FindVertexPathAllLoops(Vector2 fromWorld, Vector2 toWorld, int maxEdgeSteps = 500)
    {
        List<Vector2> result = new List<Vector2>();
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || mgr.GetEdgeCount() == 0)
        {
            return result;
        }

        int startEdge = FindClosestEdgeIndex(mgr, fromWorld);
        int goalEdge = FindBestGoalEdgeForPath(fromWorld, toWorld);

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

        List<List<int>> adjacency = GetEdgeAdjacencyAllLoops(mgr);
        Dictionary<int, float> dist = new Dictionary<int, float>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        Dictionary<int, Vector2> anchorOnEdge = new Dictionary<int, Vector2>();
        List<int> open = new List<int>();

        Edge startEdgeData = mgr.GetEdge(startEdge);
        Vector2 startOnEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(
            fromWorld,
            startEdgeData.a,
            startEdgeData.b);

        dist[startEdge] = 0f;
        anchorOnEdge[startEdge] = startOnEdge;
        cameFrom[startEdge] = startEdge;
        open.Add(startEdge);

        bool found = false;
        int steps = 0;

        while (open.Count > 0 && steps < maxEdgeSteps)
        {
            steps++;
            int current = PopClosestEdge(open, dist);

            if (current == goalEdge)
            {
                found = true;
                break;
            }

            Edge currentEdge = mgr.GetEdge(current);
            Vector2 onCurrent = anchorOnEdge[current];
            List<int> neighbors = adjacency[current];

            for (int i = 0; i < neighbors.Count; i++)
            {
                int next = neighbors[i];
                Edge nextEdge = mgr.GetEdge(next);
                Vector2 shared = GetSharedVertex(currentEdge, nextEdge);
                Vector2 exitPoint = shared.sqrMagnitude > 0.0001f
                    ? shared
                    : SurfaceEdgeTraversal.ClosestPointOnSegment(onCurrent, nextEdge.a, nextEdge.b);
                Vector2 enterPoint = SurfaceEdgeTraversal.ClosestPointOnSegment(
                    onCurrent,
                    currentEdge.a,
                    currentEdge.b);
                float stepCost = ComputeContourStepCost(mgr, current, next, enterPoint, exitPoint);
                float newDist = dist[current] + stepCost;

                if (dist.TryGetValue(next, out float oldDist) && newDist >= oldDist - 0.0001f)
                {
                    continue;
                }

                dist[next] = newDist;
                cameFrom[next] = current;
                anchorOnEdge[next] = exitPoint;

                if (!open.Contains(next))
                {
                    open.Add(next);
                }
            }
        }

        if (!found)
        {
            result.Add(goalPoint);
            return result;
        }

        List<int> edgeChain = new List<int>();
        int back = goalEdge;
        int backtrackSteps = 0;
        const int maxBacktrackSteps = 256;

        while (back != startEdge && backtrackSteps < maxBacktrackSteps)
        {
            backtrackSteps++;
            edgeChain.Add(back);

            if (!cameFrom.TryGetValue(back, out int previous))
            {
                break;
            }

            back = previous;
        }

        edgeChain.Reverse();

        Vector2 cursor = startOnEdge;
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

    private static int PopClosestEdge(List<int> open, Dictionary<int, float> dist)
    {
        int bestListIndex = 0;
        float bestDist = dist[open[0]];

        for (int i = 1; i < open.Count; i++)
        {
            float candidateDist = dist[open[i]];

            if (candidateDist < bestDist)
            {
                bestDist = candidateDist;
                bestListIndex = i;
            }
        }

        int edgeIndex = open[bestListIndex];
        open.RemoveAt(bestListIndex);
        return edgeIndex;
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
