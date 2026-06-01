using UnityEngine;

public class SurfaceWalker2D : MonsterBase
{
    public float moveSpeed = 3f;
    public float fallSpeed = 6f;

    [Header("Visual")]
    public Transform bodyVisual;
    public float visualNormalOffset = 0.1f;

    private Vector3 baseVisualScale = Vector3.one;

    protected override void Init()
    {
        ai = new SurfaceWalkerUtilityAI();
        motor = new SurfaceWalkerMotor();

        SurfaceCrawlerVisual.CacheBaseScale(bodyVisual, ref baseVisualScale);
        transform.rotation = Quaternion.identity;

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            HasEdge = false;
            return;
        }

        SurfaceEdgePath.TrySnapToNearestEdge(
            mgr,
            Position,
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
        UpdateVisualOffset();
    }

    public void UpdateVisualOffset()
    {
        if (!HasEdge)
        {
            return;
        }

        SurfaceCrawlerVisual.Apply(
            transform,
            bodyVisual,
            CurrentEdge,
            baseVisualScale,
            visualNormalOffset
        );
    }

    private void LateUpdate()
    {
        if (bodyVisual != null && bodyVisual.localRotation != Quaternion.identity)
        {
            bodyVisual.localRotation = Quaternion.identity;
        }

        if (transform.rotation != Quaternion.identity)
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
