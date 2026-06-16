using System.Collections.Generic;
using UnityEngine;

public class SnailMotor : IMonsterMotor
{
    private readonly Snail2D snail;

    private List<Vector2> activePath;
    private int pathIndex;

    public SnailMotor(Snail2D snail)
    {
        this.snail = snail;
    }

    public void Execute(MonsterBase owner, IIntent intent)
    {
        if (intent is not SnailMoveIntent move || owner is not Snail2D sw)
        {
            return;
        }

        if (sw.IsDownwardMovementPaused)
        {
            if (move.holdPosition)
            {
                sw.Arrived = true;
            }

            return;
        }

        sw.CurrentBehavior = move.behavior;
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return;
        }

        if (move.pathVertices != null && move.pathVertices.Count > 0)
        {
            DrivePath(sw, move.pathVertices);
            return;
        }

        if (move.holdPosition)
        {
            sw.Arrived = true;
            return;
        }

        if (move.behavior == SnailBehavior.IdleWander)
        {
            if (!sw.HasEdge)
            {
                Fall(sw, mgr);
                return;
            }

            DriveIdleOnEdge(sw, mgr);
            return;
        }

        if (!sw.HasEdge)
        {
            Fall(sw, mgr);
        }

        sw.Arrived = true;
    }

    public Vector2 PredictPositionAfterStep(IIntent intent)
    {
        if (intent is not SnailMoveIntent move || snail.IsDownwardMovementPaused)
        {
            return snail.Position;
        }

        if (move.holdPosition)
        {
            return snail.Position;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;
        if (mgr == null)
        {
            return snail.Position;
        }

        if (move.pathVertices != null && move.pathVertices.Count > 0)
        {
            return PredictPathPosition(snail, move.pathVertices);
        }

        if (move.behavior == SnailBehavior.IdleWander)
        {
            if (!snail.HasEdge)
            {
                return snail.Position + Vector2.down * snail.fallSpeed * Time.fixedDeltaTime;
            }

            return PredictIdleOnEdge(snail, mgr);
        }

        if (!snail.HasEdge)
        {
            return snail.Position + Vector2.down * snail.fallSpeed * Time.fixedDeltaTime;
        }

        return snail.Position;
    }

    private static Vector2 PredictIdleOnEdge(Snail2D sw, TileMapGuideManager mgr)
    {
        Edge edge = sw.CurrentEdge;
        Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(sw.Position, edge.a, edge.b);
        Vector2 forwardCorner = SurfaceEdgePath.GetForwardCorner(mgr, sw.EdgeIndex, onEdge, sw.idleClockwise);
        float step = sw.moveSpeed * Time.fixedDeltaTime;
        Vector2 newPos = Vector2.MoveTowards(onEdge, forwardCorner, step);
        newPos = SurfaceEdgeTraversal.ClosestPointOnSegment(newPos, edge.a, edge.b);

        if (SurfaceEdgePath.HasArea(sw.idleArea))
        {
            if (!sw.IsInsideIdleArea(onEdge))
            {
                return onEdge;
            }

            if (!sw.IsInsideIdleArea(newPos))
            {
                newPos = onEdge;
            }
        }

        if (Vector2.Distance(newPos, forwardCorner) <= sw.arriveThreshold)
        {
            return forwardCorner;
        }

        return newPos;
    }

    private Vector2 PredictPathPosition(Snail2D sw, List<Vector2> path)
    {
        int index = activePath == path ? pathIndex : 0;
        if (index >= path.Count)
        {
            return sw.Position;
        }

        Vector2 nodeTarget = path[index];
        return Vector2.MoveTowards(sw.Position, nodeTarget, sw.moveSpeed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Idle：沿当前边朝前顶点爬行，出 Idle 区域边界时反向。
    /// </summary>
    private void DriveIdleOnEdge(Snail2D sw, TileMapGuideManager mgr)
    {
        sw.Arrived = false;

        Edge edge = sw.CurrentEdge;
        Vector2 onEdge = SurfaceEdgeTraversal.ClosestPointOnSegment(sw.Position, edge.a, edge.b);
        sw.Transform.position = onEdge;

        bool clockwise = sw.idleClockwise;
        Vector2 forwardCorner = SurfaceEdgePath.GetForwardCorner(mgr, sw.EdgeIndex, onEdge, clockwise);
        sw.CurrentTarget = forwardCorner;
        sw.Target = forwardCorner;

        int fallbackSign = sw.TravelSignAlongEdge != 0
            ? sw.TravelSignAlongEdge
            : (clockwise ? 1 : -1);
        sw.TravelSignAlongEdge = SurfaceCrawlerVisual.ComputeTravelSignAlongEdge(
            edge,
            sw.Position,
            forwardCorner,
            fallbackSign
        );

        float step = sw.moveSpeed * Time.fixedDeltaTime;
        Vector2 newPos = Vector2.MoveTowards(onEdge, forwardCorner, step);
        newPos = SurfaceEdgeTraversal.ClosestPointOnSegment(newPos, edge.a, edge.b);

        if (SurfaceEdgePath.HasArea(snail.idleArea))
        {
            bool wasInside = snail.IsInsideIdleArea(onEdge);

            if (!wasInside)
            {
                sw.Arrived = true;
                return;
            }

            if (!snail.IsInsideIdleArea(newPos))
            {
                sw.idleClockwise = !sw.idleClockwise;
                newPos = onEdge;
            }
        }

        sw.Transform.position = newPos;
        SurfaceEdgePath.SyncEdgeStateFromPosition(sw, snapPositionToEdge: false);
        sw.UpdateVisualOffset();

        if (Vector2.Distance(newPos, forwardCorner) <= sw.arriveThreshold)
        {
            sw.Transform.position = forwardCorner;

            Vector2 nextTarget = forwardCorner;
            SurfaceEdgeTraversal.AdvanceToNextEdge(
                mgr,
                ref sw.EdgeIndex,
                ref sw.CurrentEdge,
                ref nextTarget,
                forwardCorner,
                sw.idleClockwise
            );

            sw.Target = nextTarget;
            sw.CurrentTarget = nextTarget;
            sw.UpdateVisualOffset();
        }
    }

    private void DrivePath(Snail2D sw, List<Vector2> path)
    {
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
            sw.UpdateVisualOffset();
            return;
        }

        Vector2 nodeTarget = path[pathIndex];
        sw.CurrentTarget = nodeTarget;

        int fallbackSign = sw.TravelSignAlongEdge;
        sw.TravelSignAlongEdge = SurfaceCrawlerVisual.ComputeTravelSignAlongEdge(
            sw.CurrentEdge,
            sw.Position,
            nodeTarget,
            fallbackSign
        );

        sw.Transform.position = Vector2.MoveTowards(
            sw.Position,
            nodeTarget,
            sw.moveSpeed * Time.fixedDeltaTime
        );

        SurfaceEdgePath.SyncEdgeStateFromPosition(sw, snapPositionToEdge: false);
        sw.UpdateVisualOffset();

        if (Vector2.Distance(sw.Position, nodeTarget) > sw.arriveThreshold)
        {
            return;
        }

        pathIndex++;
    }

    private void Fall(Snail2D sw, TileMapGuideManager mgr)
    {
        sw.Transform.position += Vector3.down * sw.fallSpeed * Time.fixedDeltaTime;

        if (SurfaceEdgePath.TrySnapToNearestEdge(mgr, sw.Position, out int edgeIndex, out Edge edge, out Vector2 snapped))
        {
            sw.EdgeIndex = edgeIndex;
            sw.CurrentEdge = edge;
            sw.Transform.position = snapped;
            sw.HasEdge = true;
            SurfaceEdgePath.SyncEdgeStateFromPosition(sw, snapPositionToEdge: false);
            sw.UpdateVisualOffset();
        }
    }
}
