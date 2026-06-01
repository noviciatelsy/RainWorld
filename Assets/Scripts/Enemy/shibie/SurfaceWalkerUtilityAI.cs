using System.Collections.Generic;
using UnityEngine;

public struct SurfaceMoveIntent : IIntent
{
    public List<Vector2> pathVertices;
    public bool clockwise;
}

public class SurfaceWalkerUtilityAI : IMonsterAI
{
    private List<Vector2> currentPath;
    private bool clockwise = true;

    public IIntent Evaluate(MonsterBase owner)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return new SurfaceMoveIntent();
        }

        if (currentPath == null || currentPath.Count == 0 || owner.Arrived)
        {
            clockwise = ScoreDirection(owner, mgr);
            currentPath = SurfaceEdgePath.BuildWanderPath(
                mgr,
                owner.Position,
                owner.EdgeIndex,
                clockwise,
                6
            );
            owner.Arrived = false;
        }

        return new SurfaceMoveIntent
        {
            pathVertices = currentPath,
            clockwise = clockwise
        };
    }

    private bool ScoreDirection(MonsterBase owner, TileMapGuideManager mgr)
    {
        float cw = Score(owner, mgr, true);
        float ccw = Score(owner, mgr, false);
        return cw >= ccw;
    }

    private float Score(MonsterBase owner, TileMapGuideManager mgr, bool cw)
    {
        int next = mgr.GetNextIndex(owner.EdgeIndex, cw);
        Edge e = mgr.GetEdge(next);
        Vector2 mid = (e.a + e.b) * 0.5f;
        return 1f / (1f + Vector2.Distance(owner.Position, mid));
    }
}
