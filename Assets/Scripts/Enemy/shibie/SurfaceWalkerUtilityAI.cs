using System.Collections.Generic;
using UnityEngine;

public struct SurfaceMoveIntent : IIntent
{
    public List<Vector2> pathVertices;
    public bool clockwise;
}

public class SurfaceWalkerUtilityAI : IMonsterAI
{
    private readonly SurfaceWalker2D walker;
    private List<Vector2> currentPath;

    public SurfaceWalkerUtilityAI(SurfaceWalker2D walker)
    {
        this.walker = walker;
    }

    public IIntent Evaluate(MonsterBase owner)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return new SurfaceMoveIntent();
        }

        if (currentPath == null || currentPath.Count == 0 || owner.Arrived)
        {
            currentPath = SurfaceEdgePath.BuildWanderPath(
                mgr,
                walker.GetOnEdgeWorldPosition(),
                owner.EdgeIndex,
                walker.travelClockwise,
                6
            );
            owner.Arrived = false;
        }

        return new SurfaceMoveIntent
        {
            pathVertices = currentPath,
            clockwise = walker.travelClockwise
        };
    }
}
