using UnityEngine;

public class SurfaceWalker2D : MonsterBase
{
    public float moveSpeed = 3f;
    public float fallSpeed = 6f;

    [Header("Skeleton")]
    [Tooltip("与 SurfaceWalkerLegSystem.body 一致，贴图/骨骼根")]
    public Transform crawlBody;
    public SurfaceWalkerLegSystem legSystem;

    [Header("Visual")]
    [Tooltip("可选；未设置时使用 crawlBody")]
    public Transform bodyVisual;
    public float visualNormalOffset = 0.1f;

    private Vector3 baseVisualScale = Vector3.one;
    private float visualScaleSignX = -1f;

    public int TravelSignAlongEdge { get; set; }

    protected override void Init()
    {
        ai = new SurfaceWalkerUtilityAI();
        motor = new SurfaceWalkerMotor();

        ResolveCrawlBody();
        SurfaceCrawlerVisual.CacheBaseScale(GetVisualTransform(), ref baseVisualScale);
        transform.rotation = Quaternion.identity;

        if (legSystem != null)
        {
            legSystem.sw = this;
            if (legSystem.body == null)
            {
                legSystem.body = crawlBody;
            }
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            HasEdge = false;
            return;
        }

        Vector2 snapFrom = crawlBody != null ? (Vector2)crawlBody.position : Position;

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
        UpdateVisualOffset();
    }

    private void ResolveCrawlBody()
    {
        if (legSystem == null)
        {
            legSystem = GetComponent<SurfaceWalkerLegSystem>();
        }

        if (crawlBody == null && legSystem != null)
        {
            crawlBody = legSystem.body;
        }

        if (bodyVisual == null)
        {
            bodyVisual = crawlBody;
        }
    }

    public Transform GetVisualTransform()
    {
        ResolveCrawlBody();
        return crawlBody != null ? crawlBody : bodyVisual;
    }

    public void UpdateVisualOffset()
    {
        if (!HasEdge)
        {
            return;
        }

        Transform visual = GetVisualTransform();

        if (visual == null)
        {
            return;
        }

        SurfaceCrawlerVisual.ApplySurfaceWalker(
            transform,
            visual,
            CurrentEdge,
            baseVisualScale,
            TravelSignAlongEdge,
            ref visualScaleSignX
        );
    }

    private void LateUpdate()
    {
        if (transform.rotation != Quaternion.identity)
        {
            transform.rotation = Quaternion.identity;
        }

        if (!HasEdge)
        {
            return;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr != null)
        {
            SurfaceEdgePath.SyncEdgeStateFromPosition(this, snapPositionToEdge: false);
        }

        Transform visual = GetVisualTransform();

        if (visual != null)
        {
            SurfaceCrawlerVisual.ApplySurfaceWalker(
                transform,
                visual,
                CurrentEdge,
                baseVisualScale,
                0,
                ref visualScaleSignX
            );
        }
    }
}
