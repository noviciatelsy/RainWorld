using UnityEngine;

public class Snail2D : MonsterBase
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float fallSpeed = 6f;
    public Transform bodyVisual;

    [Header("Areas")]
    public Bounds idleArea;
    public Bounds itemDetectArea;
    public Vector2 spawnPoint;

    [Header("Eat Item")]
    public float eatWaitDuration = 5f;
    public float arriveThreshold = 0.08f;

    public SnailBehavior CurrentBehavior { get; set; } = SnailBehavior.IdleWander;

    protected override void Init()
    {
        ai = new SnailUtilityAI(this);
        motor = new SnailMotor(this);

        if (spawnPoint.sqrMagnitude < 0.0001f)
        {
            spawnPoint = Position;
        }

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            HasEdge = false;
            return;
        }

        SurfaceEdgePath.TrySnapToNearestEdge(
            mgr,
            spawnPoint,
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
        return idleArea.size.sqrMagnitude < 0.01f || idleArea.Contains(point);
    }

    public bool IsInsideDetectArea(Vector2 point)
    {
        return itemDetectArea.size.sqrMagnitude < 0.01f || itemDetectArea.Contains(point);
    }

    /// <summary>被打断时调用（玩家攻击等），当前仅道具消失由 AI 处理。</summary>
    public virtual void OnBehaviorInterrupted()
    {
    }
}
