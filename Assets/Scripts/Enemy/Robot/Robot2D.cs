using UnityEngine;

public class Robot2D : MonsterBase
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float chargeSpeed = 7f;
    public float arriveThreshold = 0.08f;

    [Header("Areas")]
    [Tooltip("Idle 游荡可移动范围（世界坐标 Center/Size，固定区域）")]
    public Bounds idleBounds;
    [Tooltip("以机器人为中心的感知/激活范围（仅 Size 有效，Center 运行时随机器人更新）")]
    public Bounds activeBounds;

    [Header("Perception")]
    public LayerMask playerLayer;

    [Header("Combat")]
    public float attackRange = 0.9f;
    public int attackDamage = 12;
    [Tooltip("冲刺结束后原地停止时间")]
    public float recoverDuration = 2f;
    [Tooltip("单次冲刺最长持续时间，防止一直追")]
    public float chargeMaxDuration = 3f;

    [Header("Visual")]
    public Transform bodyVisual;

    [Header("Debug")]
    public bool drawDebugGizmos = true;

    public RobotBehavior CurrentBehavior { get; set; } = RobotBehavior.Idle;

    public bool DebugHasPlayer { get; private set; }
    public Vector2 DebugPlayerPosition { get; private set; }

    private readonly Collider2D[] overlapBuffer = new Collider2D[16];
    private float baseVisualScaleX = 1f;

    protected override void Init()
    {
        ai = new RobotUtilityAI(this);
        motor = new RobotMotor(this);

        Arrived = true;

        Transform visual = bodyVisual != null ? bodyVisual : transform;
        baseVisualScaleX = Mathf.Abs(visual.localScale.x);

        if (baseVisualScaleX < 0.001f)
        {
            baseVisualScaleX = 1f;
        }

        EnsureDefaultAreas();
        ResolvePlayerLayerMask();

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr != null)
        {
            transform.position = RobotGroundPath.SnapToFlatGround(Position);
        }

        Arrived = true;
    }

    private void OnValidate()
    {
        EnsureDefaultAreas();
        ResolvePlayerLayerMask();
    }

    public void EnsureDefaultAreas()
    {
        Vector3 center = transform.position;

        if (idleBounds.size.sqrMagnitude < 0.01f)
        {
            idleBounds = new Bounds(center, new Vector3(12f, 4f, 0.1f));
        }

        if (activeBounds.size.sqrMagnitude < 0.01f)
        {
            activeBounds = new Bounds(center, new Vector3(8f, 4f, 0.1f));
        }
    }

    public void ResolvePlayerLayerMask()
    {
        if (playerLayer.value != 0)
        {
            return;
        }

        int playerLayerIndex = LayerMask.NameToLayer("Player");

        if (playerLayerIndex >= 0)
        {
            playerLayer = 1 << playerLayerIndex;
        }
    }

    /// <summary>
    /// OverlapBox 的 size 为全宽/全高（非 halfExtents）。
    /// </summary>
    public Vector2 GetActiveBoxSize()
    {
        Bounds active = GetActiveBoundsWorld();
        return new Vector2(
            Mathf.Max(active.size.x, 0.5f),
            Mathf.Max(active.size.y, 0.5f)
        );
    }

    public float GetActiveSenseRadius()
    {
        Vector2 size = GetActiveBoxSize();
        return Mathf.Max(size.x, size.y) * 0.5f;
    }

    /// <summary>
    /// 以当前机器人位置为中心的 active 区域（感知/冲刺触发）。
    /// </summary>
    public Bounds GetActiveBoundsWorld()
    {
        return new Bounds(transform.position, activeBounds.size);
    }

    public bool IsInsideIdleBounds(Vector2 point)
    {
        return RobotGroundPath.IsInsideBoundsXY(idleBounds, point);
    }

    public bool IsInsideActiveBounds(Vector2 point)
    {
        return RobotGroundPath.IsInsideBoundsXY(GetActiveBoundsWorld(), point);
    }

    public int OverlapPlayerNonAlloc(out Collider2D[] buffer)
    {
        buffer = overlapBuffer;
        Vector2 boxSize = GetActiveBoxSize();
        float radius = GetActiveSenseRadius();
        int count = 0;

        if (playerLayer.value != 0)
        {
            count = Physics2D.OverlapBoxNonAlloc(
                Position,
                boxSize,
                0f,
                overlapBuffer,
                playerLayer
            );
        }
        else
        {
            count = Physics2D.OverlapBoxNonAlloc(Position, boxSize, 0f, overlapBuffer);
        }

        if (count > 0)
        {
            return count;
        }

        if (playerLayer.value != 0)
        {
            return Physics2D.OverlapCircleNonAlloc(
                Position,
                radius,
                overlapBuffer,
                playerLayer
            );
        }

        return Physics2D.OverlapCircleNonAlloc(Position, radius, overlapBuffer);
    }

    public bool IsPlayerCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.GetComponentInParent<Player>() != null)
        {
            return true;
        }

        if (collider.CompareTag("Player"))
        {
            return true;
        }

        return playerLayer.value != 0
            && (playerLayer.value & (1 << collider.gameObject.layer)) != 0;
    }

    public Transform FindClosestPlayerTransform()
    {
        DebugHasPlayer = false;

        int hitCount = OverlapPlayerNonAlloc(out Collider2D[] hits);
        Transform closest = null;
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || !IsPlayerCollider(hit))
            {
                continue;
            }

            Player player = hit.GetComponentInParent<Player>();

            if (player == null)
            {
                continue;
            }

            Vector2 playerPos = player.transform.position;

            if (!IsInsideActiveBounds(playerPos))
            {
                continue;
            }

            float distSqr = (playerPos - Position).sqrMagnitude;

            if (distSqr >= bestDistSqr)
            {
                continue;
            }

            bestDistSqr = distSqr;
            closest = player.transform;
        }

        if (closest == null && PlayerManager.Instance != null)
        {
            Player scenePlayer = PlayerManager.Instance.TryGetCurrentPlayer();

            if (scenePlayer != null)
            {
                Vector2 playerPos = scenePlayer.transform.position;

                if (IsInsideActiveBounds(playerPos))
                {
                    closest = scenePlayer.transform;
                }
            }
        }

        if (closest != null)
        {
            DebugHasPlayer = true;
            DebugPlayerPosition = closest.position;
        }

        return closest;
    }

    public bool TryDamagePlayer(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return false;
        }

        if (((Vector2)playerTransform.position - Position).sqrMagnitude > attackRange * attackRange)
        {
            return false;
        }

        Player player = playerTransform.GetComponentInParent<Player>();

        if (player == null)
        {
            return false;
        }

        PlayerVitals vitals = player.GetComponent<PlayerVitals>();

        if (vitals == null || vitals.IsDead)
        {
            return false;
        }

        vitals.ReduceHealth(attackDamage);
        return true;
    }

    public void UpdateFacingToward(Vector2 worldPoint)
    {
        float deltaX = worldPoint.x - Position.x;

        if (Mathf.Abs(deltaX) < 0.01f)
        {
            return;
        }

        Transform visual = bodyVisual != null ? bodyVisual : transform;
        Vector3 scale = visual.localScale;
        scale.x = baseVisualScaleX * (deltaX >= 0f ? 1f : -1f);
        visual.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        DrawDebugGizmos(1f);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        DrawDebugGizmos(Application.isPlaying ? 0.85f : 0.55f);
    }

    private void DrawDebugGizmos(float alpha)
    {
        EnsureDefaultAreas();

        Gizmos.color = new Color(0.2f, 1f, 0.35f, alpha * 0.55f);
        Gizmos.DrawWireCube(idleBounds.center, idleBounds.size);

        Bounds activeWorld = Application.isPlaying
            ? GetActiveBoundsWorld()
            : new Bounds(transform.position, activeBounds.size);

        Gizmos.color = new Color(1f, 0.55f, 0.15f, alpha * 0.65f);
        Gizmos.DrawWireCube(activeWorld.center, activeWorld.size);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, alpha * 0.6f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (DebugHasPlayer)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, DebugPlayerPosition);
            Gizmos.DrawWireSphere(DebugPlayerPosition, 0.25f);
        }
    }
}
