using UnityEngine;

public class Snail2D : MonsterBase, IToyCarAttractable
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float fallSpeed = 6f;

    [Header("Visual")]
    public Transform bodyVisual;
    public float visualNormalOffset = 0.1f;

    [Header("Animation")]
    public SnailAni snailAni;

    [Header("Areas (世界坐标 Center/Size)")]
    [Tooltip("平时随机游走范围，应小于识别区")]
    public Bounds idleArea;
    [Tooltip("检测 PickableObject 的范围，应包住 Idle 区")]
    public Bounds itemDetectArea;
    [Tooltip("出生点 / 吃完道具后回家的锚点，建议在 Idle 区内")]
    public Vector2 spawnPoint;

    [Header("Idle Wander")]
    public bool idleClockwise = true;
    [Tooltip("Idle 区域判定容差，避免贴边/吸附后仍被判在区外")]
    public float idleAreaTolerance = 0.2f;

    [Header("Eat Item")]
    [Tooltip("蜗牛会被吸引并吃掉的道具（默认牛奶）")]
    public ItemDataSO attractedItemData;
    public float eatWaitDuration = 5f;
    public float arriveThreshold = 0.08f;

    [Header("Attraction")]
    public float detectRadius = 8f;

    [Header("Debug")]
    public bool drawAreaGizmos = true;

    public SnailBehavior CurrentBehavior { get; set; } = SnailBehavior.IdleWander;

    private Vector3 baseVisualScale = Vector3.one;
    private float visualScaleSignX = -1f;

    /// <summary>
    /// 由 Motor 写入，避免用瞬时速度导致 scale.x 来回翻转。
    /// </summary>
    public int TravelSignAlongEdge { get; set; }

    public bool IsDownwardMovementPaused { get; private set; }

    private SnailRidePlatform ridePlatform;
    private SnailUtilityAI snailAI;

    public void SetDownwardMovementPaused(bool paused)
    {
        IsDownwardMovementPaused = paused;
    }

    public Vector2 PredictPositionAfterStep(IIntent intent)
    {
        if (motor is SnailMotor snailMotor)
        {
            return snailMotor.PredictPositionAfterStep(intent);
        }

        return Position;
    }

    private void Awake()
    {
        ridePlatform = GetComponentInChildren<SnailRidePlatform>(true);
    }

    private void OnEnable()
    {
        SnailRegistry.Register(this);
    }

    private void OnDisable()
    {
        SnailRegistry.Unregister(this);
    }

    protected override void FixedUpdate()
    {
        if (ai == null || motor == null)
        {
            return;
        }

        IIntent intent = ai.Evaluate(this);
        ridePlatform?.PrepareBeforeMotor(intent);
        motor.Execute(this, intent);
        ridePlatform?.SyncAfterMotor();
    }

    protected override void Init()
    {
        snailAI = new SnailUtilityAI(this);
        ai = snailAI;
        motor = new SnailMotor(this);

        if (snailAni == null)
        {
            snailAni = GetComponent<SnailAni>();
        }

        if (snailAni == null)
        {
            snailAni = GetComponentInChildren<SnailAni>(true);
        }

        ridePlatform = GetComponentInChildren<SnailRidePlatform>(true);

        EnsureDefaultAreas();

        SurfaceCrawlerVisual.CacheBaseScale(bodyVisual, ref baseVisualScale);
        transform.rotation = Quaternion.identity;

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            HasEdge = false;
            return;
        }

        Vector2 startPos = GetSpawnPosition();

        int edgeIndex = SurfaceEdgePath.FindClosestEdgeIndexInBounds(mgr, startPos, idleArea, idleAreaTolerance);
        Edge edge = mgr.GetEdge(edgeIndex);
        Vector2 snapped = SurfaceEdgePath.ClosestPointOnSegmentInsideBounds(
            edge.a,
            edge.b,
            idleArea,
            startPos,
            idleAreaTolerance
        );

        EdgeIndex = edgeIndex;
        CurrentEdge = edge;
        transform.position = snapped;
        HasEdge = true;
        Arrived = true;

        SurfaceEdgePath.SyncEdgeStateFromPosition(this);
        UpdateVisualOffset();
        snailAni?.RefreshMoveBaseScale();
    }

    private void OnValidate()
    {
        EnsureDefaultAreas();
    }

    public void EnsureDefaultAreas()
    {
        Vector3 center = transform.position;

        if (idleArea.size.sqrMagnitude < 0.01f)
        {
            idleArea = new Bounds(center, new Vector3(5f, 4f, 0.1f));
        }

        if (itemDetectArea.size.sqrMagnitude < 0.01f)
        {
            itemDetectArea = new Bounds(center, new Vector3(10f, 8f, 0.1f));
        }

        if (spawnPoint.sqrMagnitude < 0.0001f)
        {
            spawnPoint = center;
        }
    }

    public Vector2 GetSpawnPosition()
    {
        if (spawnPoint.sqrMagnitude > 0.0001f)
        {
            return spawnPoint;
        }

        return SurfaceEdgePath.HasArea(idleArea) ? (Vector2)idleArea.center : Position;
    }

    /// <summary>
    /// Idle 区内在 loop 上的锚点（spawn 投影到 idle 范围内最近边）。
    /// </summary>
    public Vector2 GetIdleAnchorOnEdge()
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return GetSpawnPosition();
        }

        Vector2 home = GetSpawnPosition();
        int edgeIndex = SurfaceEdgePath.FindClosestEdgeIndexInBounds(mgr, home, idleArea, idleAreaTolerance);
        Edge edge = mgr.GetEdge(edgeIndex);
        return SurfaceEdgePath.ClosestPointOnSegmentInsideBounds(
            edge.a,
            edge.b,
            idleArea,
            home,
            idleAreaTolerance
        );
    }

    public void SnapToIdleAnchor()
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return;
        }

        Vector2 anchor = GetIdleAnchorOnEdge();
        int edgeIndex = SurfaceEdgePath.FindClosestEdgeIndexInBounds(mgr, anchor, idleArea, idleAreaTolerance);
        Edge edge = mgr.GetEdge(edgeIndex);
        Vector2 snapped = SurfaceEdgePath.ClosestPointOnSegmentInsideBounds(
            edge.a,
            edge.b,
            idleArea,
            anchor,
            idleAreaTolerance
        );

        EdgeIndex = edgeIndex;
        CurrentEdge = edge;
        transform.position = snapped;
        HasEdge = true;
        Arrived = true;
        SurfaceEdgePath.SyncEdgeStateFromPosition(this);
        UpdateVisualOffset();
    }

    public bool ShouldBeInIdleArea()
    {
        return SurfaceEdgePath.HasArea(idleArea);
    }

    public bool NeedsReturnToIdle()
    {
        return ShouldBeInIdleArea() && !IsInsideIdleArea(Position);
    }

    public void UpdateVisualOffset()
    {
        if (!HasEdge)
        {
            return;
        }

        SurfaceCrawlerVisual.Apply(
            SurfaceCrawlerVisualStyle.Snail,
            transform,
            bodyVisual,
            CurrentEdge,
            baseVisualScale,
            visualNormalOffset,
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

        if (HasEdge && bodyVisual != null)
        {
            SurfaceCrawlerVisual.Apply(
                SurfaceCrawlerVisualStyle.Snail,
                transform,
                bodyVisual,
                CurrentEdge,
                baseVisualScale,
                visualNormalOffset,
                0,
                ref visualScaleSignX
            );
        }
    }

    public bool IsInsideIdleArea(Vector2 point)
    {
        return SurfaceEdgePath.IsInsideArea(idleArea, point, idleAreaTolerance);
    }

    public bool IsInsideDetectArea(Vector2 point)
    {
        return SurfaceEdgePath.IsInsideArea(itemDetectArea, point);
    }

    public bool IsAttractedPickable(PickableObject pickable)
    {
        if (pickable == null || pickable.ItemData == null || attractedItemData == null)
        {
            return false;
        }

        if (!pickable.IsSettledOnGround)
        {
            return false;
        }

        return pickable.ItemData == attractedItemData;
    }

    public virtual void OnBehaviorInterrupted()
    {
    }

    private void OnDrawGizmosSelected()
    {
        DrawAreaGizmos(1f);
    }

    private void OnDrawGizmos()
    {
        if (!drawAreaGizmos)
        {
            return;
        }

        DrawAreaGizmos(Application.isPlaying ? 0.85f : 0.55f);
    }

    private void DrawAreaGizmos(float alpha)
    {
        EnsureDefaultAreas();

        Gizmos.color = new Color(0.2f, 1f, 0.35f, alpha);
        Gizmos.DrawWireCube(idleArea.center, idleArea.size);

        Gizmos.color = new Color(0.2f, 0.75f, 1f, alpha * 0.9f);
        Gizmos.DrawWireCube(itemDetectArea.center, itemDetectArea.size);

        Gizmos.color = new Color(1f, 0.85f, 0.2f, alpha);
        Gizmos.DrawSphere(GetSpawnPosition(), 0.15f);
    }

    public void AttractToToyCar(Vector2 myToyCarPosition)
    {
        snailAI?.ForceAttractionRefresh();
    }
}
