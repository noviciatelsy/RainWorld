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

        if (!sw.HasEdge)
        {
            Fall(sw, mgr);
        }

        sw.Arrived = true;
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

        sw.Transform.position = Vector2.MoveTowards(
            sw.Position,
            nodeTarget,
            sw.moveSpeed * Time.fixedDeltaTime
        );

        UpdateFacing(sw, nodeTarget);

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
            UpdateFacing(sw, sw.Target);
        }
    }

    private void UpdateFacing(Snail2D sw, Vector2 lookTarget)
    {
        Vector2 dir = lookTarget - sw.Position;

        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        dir.Normalize();

        float angle = Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
            ? (dir.x > 0f ? 0f : 180f)
            : (dir.y > 0f ? 90f : -90f);

        sw.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
