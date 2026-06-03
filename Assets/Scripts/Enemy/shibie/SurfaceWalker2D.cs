using UnityEngine;

public class SurfaceWalker2D : MonsterBase
{
    public float moveSpeed = 3f;
    public float fallSpeed = 6f;

    public SurfaceWalkerLegSystem legSystem;

    /// <summary>
    /// 由 Motor 写入；LegSystem 据此翻转 body 整体 scale。
    /// </summary>
    public bool TravelClockwise { get; set; }

    protected override void Init()
    {
        ai = new SurfaceWalkerUtilityAI();
        motor = new SurfaceWalkerMotor();

        if (legSystem == null)
        {
            legSystem = GetComponent<SurfaceWalkerLegSystem>();
        }

        if (legSystem != null)
        {
            legSystem.sw = this;
        }

        transform.rotation = Quaternion.identity;

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            HasEdge = false;
            return;
        }

        Vector2 snapFrom = legSystem != null && legSystem.body != null
            ? (Vector2)legSystem.body.position
            : Position;

        SurfaceEdgePath.TrySnapToNearestEdge(
            mgr,
            snapFrom,
            out int edgeIndex,
            out Edge edge,
            out Vector2 snapped
        );

        EdgeIndex = edgeIndex;
        CurrentEdge = edge;
        transform.position = snapped;
        HasEdge = true;
        Arrived = true;

        SurfaceEdgePath.SyncEdgeStateFromPosition(this);
    }

    private void LateUpdate()
    {
        if (transform.rotation != Quaternion.identity)
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
