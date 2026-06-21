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
    private EnemyAttractionSource activeSource = EnemyAttractionSource.None;
    private float perceptionTimer;
    private float pathRefreshTimer;
    private Vector2 committedChasePosition;
    private bool hasCommittedChasePosition;

    private const float PathRefreshInterval = 0.45f;

    private static readonly EnemyAttractionCapabilities AttractionCapabilities =
        EnemyAttractionCapabilities.MeatBait | EnemyAttractionCapabilities.ToyCar;

    public SurfaceWalkerUtilityAI(SurfaceWalker2D walker)
    {
        this.walker = walker;
        perceptionTimer = 0f;
    }

    public void ForcePerceptionRefresh()
    {
        perceptionTimer = 0f;
        pathRefreshTimer = 0f;
        currentPath = null;
        hasCommittedChasePosition = false;
    }

    public void NotifyRepelledByTorch(Vector2 torchPosition)
    {
        ForcePerceptionRefresh();
        activeSource = EnemyAttractionSource.None;
        walker.travelClockwise = !walker.travelClockwise;
        walker.Arrived = false;
    }

    public IIntent Evaluate(MonsterBase owner)
    {
        perceptionTimer -= Time.fixedDeltaTime;

        if (perceptionTimer <= 0f)
        {
            perceptionTimer = Mathf.Max(0.05f, walker.perceptionInterval);
            UpdateAttractionTarget(owner);
        }

        if (activeSource != EnemyAttractionSource.None)
        {
            pathRefreshTimer -= Time.fixedDeltaTime;

            if (owner.Arrived || pathRefreshTimer <= 0f || currentPath == null || currentPath.Count == 0)
            {
                pathRefreshTimer = PathRefreshInterval;
                RefreshChasePath(owner);
            }

            if (currentPath != null && currentPath.Count > 0)
            {
                return new SurfaceMoveIntent
                {
                    pathVertices = currentPath,
                    clockwise = walker.travelClockwise
                };
            }
        }

        return BuildWanderIntent(owner);
    }

    private void UpdateAttractionTarget(MonsterBase owner)
    {
        Vector2 from = walker.GetOnEdgeWorldPosition();
        float radius = walker.detectRadius;

        if (TryResolveWalkerTarget(from, radius, out EnemyAttractionTarget target))
        {
            committedChasePosition = target.Position;
            hasCommittedChasePosition = true;

            if (activeSource != target.Source)
            {
                activeSource = target.Source;
                owner.Arrived = false;
                currentPath = null;
            }

            return;
        }

        if (activeSource == EnemyAttractionSource.ToyCar
            && hasCommittedChasePosition
            && ToyCarRegistry.HasActiveCar()
            && !owner.Arrived)
        {
            return;
        }

        activeSource = EnemyAttractionSource.None;
        currentPath = null;
        hasCommittedChasePosition = false;
    }

    private void RefreshChasePath(MonsterBase owner)
    {
        Vector2 from = walker.GetOnEdgeWorldPosition();
        float radius = walker.detectRadius;

        if (TryResolveWalkerTarget(from, radius, out EnemyAttractionTarget target))
        {
            activeSource = target.Source;
            committedChasePosition = target.Position;
            hasCommittedChasePosition = true;
            currentPath = SurfaceEdgePath.FindVertexPath(from, target.Position);
            owner.Arrived = currentPath == null || currentPath.Count == 0;
            return;
        }

        if (activeSource == EnemyAttractionSource.ToyCar
            && hasCommittedChasePosition
            && ToyCarRegistry.HasActiveCar())
        {
            currentPath = SurfaceEdgePath.FindVertexPath(from, committedChasePosition);
            owner.Arrived = currentPath == null || currentPath.Count == 0;
            return;
        }

        activeSource = EnemyAttractionSource.None;
        currentPath = null;
        hasCommittedChasePosition = false;
    }

    private bool TryResolveWalkerTarget(Vector2 from, float radius, out EnemyAttractionTarget target)
    {
        return EnemyAttractionUtility.TryResolveTarget(
            from,
            radius,
            AttractionCapabilities,
            destination => SurfaceEdgePath.FindVertexPath(from, destination).Count > 0,
            out target);
    }

    private SurfaceMoveIntent BuildWanderIntent(MonsterBase owner)
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
