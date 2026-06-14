using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI 提供 loop 拐角路点；沿当前边爬行，到顶点显式切边，避免拐角 Sync 切错边。
/// </summary>
public class SurfaceWalkerMotor : IMonsterMotor
{
    private const float ArriveThreshold = 0.08f;

    private List<Vector2> activePath;
    private int pathIndex;

    public void Execute(MonsterBase owner, IIntent intent)
    {
        if (intent is not SurfaceMoveIntent move)
        {
            return;
        }

        SurfaceWalker2D sw = owner as SurfaceWalker2D;

        if (sw == null)
        {
            return;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return;
        }

        if (move.pathVertices == null || move.pathVertices.Count == 0)
        {
            if (!sw.HasEdge)
            {
                Fall(sw, mgr);
            }

            sw.Arrived = true;
            return;
        }

        DrivePath(sw, mgr, move);
    }

    private void DrivePath(SurfaceWalker2D sw, TileMapGuideManager mgr, SurfaceMoveIntent move)
    {
        List<Vector2> path = move.pathVertices;

        if (activePath != path)
        {
            activePath = path;
            pathIndex = 0;
            sw.Arrived = false;
        }

        if (pathIndex >= path.Count)
        {
            activePath = null;
            pathIndex = 0;
            sw.Arrived = true;
            sw.HasEdge = true;
            SurfaceEdgePath.SyncEdgeStateFromPosition(sw);
            sw.ApplyRootNormalOffset();
            return;
        }

        Vector2 nodeTarget = path[pathIndex];
        sw.CurrentTarget = nodeTarget;

        bool clockwise = sw.travelClockwise;
        Vector2 onEdge = sw.GetOnEdgeWorldPosition();
        Edge edge = sw.CurrentEdge;
        Vector2 forwardCorner = SurfaceEdgePath.GetForwardCorner(mgr, sw.EdgeIndex, onEdge, clockwise);
        Vector2 stepTarget = nodeTarget;

        Vector2 nodeOnCurrentEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(
            nodeTarget,
            edge.a,
            edge.b
        );

        if (!SurfaceEdgePath.SameVertex(nodeOnCurrentEdge, nodeTarget)
            && Vector2.Distance(onEdge, forwardCorner) > ArriveThreshold)
        {
            stepTarget = forwardCorner;
        }

        float step = sw.moveSpeed * Time.fixedDeltaTime;
        Vector2 newOnEdge = Vector2.MoveTowards(onEdge, stepTarget, step);
        newOnEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(newOnEdge, edge.a, edge.b);

        sw.transform.position = newOnEdge;

        if (Vector2.Distance(newOnEdge, forwardCorner) <= ArriveThreshold)
        {
            newOnEdge = forwardCorner;
            sw.transform.position = newOnEdge;

            Vector2 nextTarget = forwardCorner;
            SurfaceEdgeTraversal.AdvanceToNextEdge(
                mgr,
                ref sw.EdgeIndex,
                ref sw.CurrentEdge,
                ref nextTarget,
                forwardCorner,
                clockwise
            );
        }

        sw.ApplyRootNormalOffset();
        sw.ApplyVisual();

        if (Vector2.Distance(newOnEdge, nodeTarget) > ArriveThreshold)
        {
            return;
        }

        pathIndex++;
    }

    private void Fall(SurfaceWalker2D sw, TileMapGuideManager mgr)
    {
        sw.Transform.position += Vector3.down * sw.fallSpeed * Time.fixedDeltaTime;

        if (SurfaceEdgePath.TrySnapToNearestEdge(mgr, sw.GetOnEdgeWorldPosition(), out int edgeIndex, out Edge edge, out Vector2 snapped))
        {
            sw.EdgeIndex = edgeIndex;
            sw.CurrentEdge = edge;
            sw.transform.position = snapped;
            sw.HasEdge = true;
            SurfaceEdgePath.SyncEdgeStateFromPosition(sw, snapPositionToEdge: false);
            sw.ApplyRootNormalOffset();
            sw.ApplyVisual();
        }
    }
}
