using UnityEngine;

public class SurfaceWalkerMotor : IMonsterMotor
{
    public void Execute(MonsterBase owner, MoveIntent intent)
    {
        var sw = owner as SurfaceWalker2D;

        if (sw.HasEdge)
            Move(sw, intent.clockwise);
        else
            Fall(sw);
    }

    void Move(SurfaceWalker2D sw, bool clockwise)
    {
        sw.Transform.position = Vector2.MoveTowards(
            sw.Transform.position,
            sw.Target,
            sw.moveSpeed * Time.fixedDeltaTime
        );

        if (Vector2.Distance(sw.Transform.position, sw.Target) < 0.01f)
        {
            Advance(sw, clockwise);
        }
    }

    void Advance(SurfaceWalker2D sw, bool clockwise)
    {
        var mgr = TileMapGuideManager.Instance;

        sw.EdgeIndex = mgr.GetNextIndex(sw.EdgeIndex, clockwise);
        Edge e = mgr.GetEdge(sw.EdgeIndex);

        sw.CurrentEdge = e;

        sw.Target =
            Vector2.Distance(sw.Transform.position, e.a) <
            Vector2.Distance(sw.Transform.position, e.b)
            ? e.b : e.a;
    }

    void Fall(SurfaceWalker2D sw)
    {
        sw.Transform.position += Vector3.down * sw.fallSpeed * Time.fixedDeltaTime;

        var mgr = TileMapGuideManager.Instance;

        int nearest = mgr.FindClosestEdgeIndex(sw.Transform.position);
        Edge e = mgr.GetEdge(nearest);

        float dist = Vector2.Distance(sw.Transform.position, (e.a + e.b) * 0.5f);

        if (dist < 0.1f)
        {
            sw.EdgeIndex = nearest;
            sw.CurrentEdge = e;
            sw.HasEdge = true;
        }
    }
}