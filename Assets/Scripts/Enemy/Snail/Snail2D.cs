using UnityEngine;

public class Snail2D : MonsterBase
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float fallSpeed = 6f;
    public Transform bodyVisual;

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
    public float eatWaitDuration = 5f;
    public float arriveThreshold = 0.08f;

    [Header("Debug")]
    public bool drawAreaGizmos = true;

    public SnailBehavior CurrentBehavior { get; set; } = SnailBehavior.IdleWander;

    protected override void Init()
    {
        ai = new SnailUtilityAI(this);
        motor = new SnailMotor(this);

        EnsureDefaultAreas();

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
        if (bodyVisual == null)
        {
            return;
        }

        Vector2 dir = (CurrentEdge.b - CurrentEdge.a).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);
        bodyVisual.localPosition = normal * 0.1f;
    }

    public bool IsInsideIdleArea(Vector2 point)
    {
        return SurfaceEdgePath.IsInsideArea(idleArea, point, idleAreaTolerance);
    }

    public bool IsInsideDetectArea(Vector2 point)
    {
        return SurfaceEdgePath.IsInsideArea(itemDetectArea, point);
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
}
