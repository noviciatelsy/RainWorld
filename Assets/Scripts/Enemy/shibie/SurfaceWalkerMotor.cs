using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 与 MoleMotor 相同：AI 提供路点列表，逐点 MoveTowards；路点来自同一 loop 外轮廓拐角。
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

        DrivePath(sw, move.pathVertices);
    }

    private void DrivePath(SurfaceWalker2D sw, List<Vector2> path)
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

        SurfaceEdgePath.SyncEdgeStateFromPosition(sw, snapPositionToEdge: false);
        sw.UpdateVisualOffset();

        if (Vector2.Distance(sw.Position, nodeTarget) > ArriveThreshold)
        {
            return;
        }

        pathIndex++;
    }

    private void Fall(SurfaceWalker2D sw, TileMapGuideManager mgr)
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
