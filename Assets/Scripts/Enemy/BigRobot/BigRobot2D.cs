using UnityEngine;

public class BigRobot2D : MonsterBase
{
    [Header("Areas")]
    [Tooltip("以机器人为中心的感知/攻击触发范围（仅 Size 有效）")]
    public Bounds activeBounds;

    [Header("Perception")]
    public LayerMask playerLayer;

    [Header("Combat")]
    [Tooltip("参考 Gizmo 红圈；扣血判定以 activeBounds 为准（与攻击触发一致）")]
    public float attackRange = 2.5f;
    public int attackDamage = 20;
    [Tooltip("两次攻击之间的间隔")]
    public float attackCooldown = 1.5f;

    [Header("Debug")]
    public bool drawDebugGizmos = true;
    public bool enableDebugLog = true;

    public BigRobotBehavior CurrentBehavior { get; set; } = BigRobotBehavior.Idle;

    public bool DebugHasPlayer { get; private set; }
    public Vector2 DebugPlayerPosition { get; private set; }

    private readonly Collider2D[] overlapBuffer = new Collider2D[16];

    protected override void Init()
    {
        BigRobotUtilityAI utilityAI = new BigRobotUtilityAI(this);
        ai = utilityAI;
        motor = new BigRobotMotor(this, utilityAI);

        Arrived = true;
        EnsureDefaultAreas();
        ResolvePlayerLayerMask();
    }

    private void OnValidate()
    {
        EnsureDefaultAreas();
        ResolvePlayerLayerMask();
    }

    public void EnsureDefaultAreas()
    {
        if (activeBounds.size.sqrMagnitude < 0.01f)
        {
            activeBounds = new Bounds(transform.position, new Vector3(10f, 6f, 0.1f));
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

    public Bounds GetActiveBoundsWorld()
    {
        return new Bounds(transform.position, activeBounds.size);
    }

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

    public bool IsInsideActiveBounds(Vector2 point)
    {
        return RobotGroundPath.IsInsideBoundsXY(GetActiveBoundsWorld(), point);
    }

    public int OverlapPlayerNonAlloc(out Collider2D[] buffer)
    {
        buffer = overlapBuffer;
        Vector2 boxSize = GetActiveBoxSize();
        float radius = GetActiveSenseRadius();
        int count;

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

    public Transform FindClosestPlayerInActiveBounds()
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

            if (scenePlayer != null && IsInsideActiveBounds(scenePlayer.transform.position))
            {
                closest = scenePlayer.transform;
            }
        }

        if (closest != null)
        {
            DebugHasPlayer = true;
            DebugPlayerPosition = closest.position;
        }

        return closest;
    }

    public bool IsPlayerInAttackRange(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return false;
        }

        return ((Vector2)playerTransform.position - Position).sqrMagnitude <= attackRange * attackRange;
    }

    public bool TryDamagePlayer(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return false;
        }

        if (!IsInsideActiveBounds(playerTransform.position))
        {
            return false;
        }

        Player player = playerTransform.GetComponentInParent<Player>();

        if (player == null)
        {
            return false;
        }

        PlayerVitals vitals = player.GetComponentInChildren<PlayerVitals>();

        if (vitals == null || vitals.IsDead)
        {
            return false;
        }

        vitals.ReduceHealth(attackDamage);
        return true;
    }

    /// <summary>
    /// 攻击时调用，默认 Debug.Log；子类可重写接入其他接口。
    /// </summary>
    public virtual void OnAttackPerformed(Transform target, bool damageDealt)
    {
        if (!enableDebugLog)
        {
            return;
        }

        string targetName = target != null ? target.name : "null";

        if (!damageDealt && target != null)
        {
            string reason = GetDamageFailReason(target);
            Debug.Log(
                $"[BigRobot {name}] 攻击玩家 {targetName}，造成伤害=False（{reason}）",
                this
            );
            return;
        }

        Debug.Log($"[BigRobot {name}] 攻击玩家 {targetName}，造成伤害={damageDealt}", this);
    }

    private string GetDamageFailReason(Transform target)
    {
        if (!IsInsideActiveBounds(target.position))
        {
            return "玩家不在 activeBounds 内";
        }

        Player player = target.GetComponentInParent<Player>();

        if (player == null)
        {
            return "未找到 Player 组件";
        }

        PlayerVitals vitals = player.GetComponentInChildren<PlayerVitals>();

        if (vitals == null)
        {
            return "未找到 PlayerVitals";
        }

        if (vitals.IsDead)
        {
            return "玩家已死亡";
        }

        return "未知原因";
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
            Gizmos.DrawWireSphere(DebugPlayerPosition, 0.3f);
        }
    }
}
