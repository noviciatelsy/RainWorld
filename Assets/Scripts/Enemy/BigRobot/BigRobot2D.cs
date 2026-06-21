using System.Collections;
using UnityEngine;

public class BigRobot2D : MonsterBase, IContactWithLiquid, IAttractedByMilk
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
    [Tooltip("进入识别区域后攻击多少次后自动关机（与踩电池/泼水效果相同）")]
    [Min(1)]
    public int attacksBeforeShutdown = 3;

    [Tooltip("攻击动画保持时长（秒），需覆盖攻击 clip 长度")]
    public float attackSequenceDuration = 1.5f;
    [Tooltip("攻击动画开始后，延迟多久再判定伤害")]
    public float attackDelay = 1f;
    [Tooltip("攻击命中时对玩家的击退冲量（x 为远离机器人的水平冲量，y 为向上冲量）")]
    public Vector2 attackKnockbackForce = new Vector2(8f, 3f);

    [Header("Visual")]
    [Tooltip("做 scale 挤压的视觉根（通常为 Textures）")]
    public Transform bodyVisual;

    [Header("Battery")]
    [SerializeField] private BigRobotBattery battery;

    [Header("Liquid")]
    [Tooltip("液体泼洒检测盒 Size（Center 为相对机器人 pivot 的 offset）")]
    [SerializeField] private Vector2 liquidHitboxSize = new Vector2(5f, 6f);
    [SerializeField] private Vector2 liquidHitboxOffset = Vector2.zero;

    [Header("Debug")]
    public bool drawDebugGizmos = true;
    public bool enableDebugLog = true;

    public BigRobotBehavior CurrentBehavior { get; set; } = BigRobotBehavior.Idle;

    public bool IsInAttackSequence { get; private set; }
    public int AttackAnimVersion { get; private set; }
    public bool IsCoolingDown => CurrentBehavior == BigRobotBehavior.Cooldown;

    public bool IsBatteryBroken => battery != null && battery.IsBroken;
    public bool IsLiquidDisabled { get; private set; }
    public bool IsAttackLimitShutdown { get; private set; }
    public bool IsShutdown => IsBatteryBroken || IsLiquidDisabled || IsAttackLimitShutdown;
    public int CompletedAttackCount { get; private set; }

    public bool DebugHasPlayer { get; private set; }
    public Vector2 DebugPlayerPosition { get; private set; }

    private readonly Collider2D[] overlapBuffer = new Collider2D[16];
    private float attackSequenceTimer;
    private Coroutine attackDamageCoroutine;

    protected override void Init()
    {
        BigRobotUtilityAI utilityAI = new BigRobotUtilityAI(this);
        ai = utilityAI;
        motor = new BigRobotMotor(this, utilityAI);

        Arrived = true;
        EnsureDefaultAreas();
        ResolvePlayerLayerMask();
        ResolveBatteryReference();
        EnsureEnemyLayer();
        EnsureLiquidHitbox();
    }

    private void ResolveBatteryReference()
    {
        if (battery == null)
        {
            battery = GetComponentInChildren<BigRobotBattery>(true);
        }
    }

    public void NotifyBatteryBroken()
    {
        ApplyShutdownState();
    }

    public void ContactWithLiquid()
    {
        if (IsShutdown)
        {
            return;
        }

        IsLiquidDisabled = true;
        ApplyShutdownState();
    }

    public void AttractedByMilk(Vector2 milkPosition)
    {
        if (IsShutdown)
        {
            return;
        }

        if (!IsInsideActiveBounds(milkPosition))
        {
            return;
        }

        IsLiquidDisabled = true;
        ApplyShutdownState();
    }

    private void ApplyShutdownState()
    {
        IsInAttackSequence = false;
        CurrentBehavior = BigRobotBehavior.Idle;
        CancelPendingAttackDamage();

        BigRobotAni ani = GetComponent<BigRobotAni>();

        if (ani != null)
        {
            ani.ApplyShutdownVisual();
        }

        EnemyBigRobotAudioEmitter audioEmitter = GetComponent<EnemyBigRobotAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.PlayShutdown();
        }
    }

    private void EnsureEnemyLayer()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (enemyLayer < 0 || gameObject.layer == enemyLayer)
        {
            return;
        }

        gameObject.layer = enemyLayer;
    }

    private void EnsureLiquidHitbox()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        BoxCollider2D hitbox = gameObject.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = liquidHitboxSize;
        hitbox.offset = liquidHitboxOffset;
    }

    protected override void FixedUpdate()
    {
        if (IsShutdown)
        {
            CurrentBehavior = BigRobotBehavior.Idle;
            return;
        }

        base.FixedUpdate();
    }

    private void OnValidate()
    {
        EnsureDefaultAreas();
        ResolvePlayerLayerMask();
    }

    private void Update()
    {
        if (!IsInAttackSequence)
        {
            return;
        }

        attackSequenceTimer -= Time.deltaTime;
        if (attackSequenceTimer <= 0f)
        {
            IsInAttackSequence = false;
            NotifyAttackCycleFinished();
        }
    }

    private void NotifyAttackCycleFinished()
    {
        if (IsShutdown)
        {
            return;
        }

        CompletedAttackCount++;

        if (CompletedAttackCount < attacksBeforeShutdown)
        {
            return;
        }

        IsAttackLimitShutdown = true;
        ApplyShutdownState();
    }

    public void BeginAttackSequence()
    {
        IsInAttackSequence = true;
        AttackAnimVersion++;
        attackSequenceTimer = Mathf.Max(0.05f, attackSequenceDuration);

        EnemyBigRobotAudioEmitter audioEmitter = GetComponent<EnemyBigRobotAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.PlaySlash();
        }
    }

    public void ScheduleAttackDamage(Transform target)
    {
        CancelPendingAttackDamage();
        attackDamageCoroutine = StartCoroutine(DealAttackDamageAfterDelay(target));
    }

    private void CancelPendingAttackDamage()
    {
        if (attackDamageCoroutine == null)
        {
            return;
        }

        StopCoroutine(attackDamageCoroutine);
        attackDamageCoroutine = null;
    }

    private IEnumerator DealAttackDamageAfterDelay(Transform target)
    {
        float delay = Mathf.Max(0f, attackDelay);

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        attackDamageCoroutine = null;

        if (target == null)
        {
            yield break;
        }

        TryKnockbackPlayer(target);

        if (IsShutdown)
        {
            yield break;
        }

        bool damageDealt = TryDamagePlayer(target);
        OnAttackPerformed(target, damageDealt);
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

            if (player == null || !PlayerInvisibilityPerception.IsPlayerDetectable(player))
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

            if (scenePlayer != null
                && PlayerInvisibilityPerception.IsPlayerDetectable(scenePlayer)
                && IsInsideActiveBounds(scenePlayer.transform.position))
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

        return MonsterPlayerDamage.TryDealDamage(vitals, attackDamage);
    }

    public bool TryKnockbackPlayer(Transform playerTransform)
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

        return PlayerKnockbackUtility.TryApplyKnockbackFromSource(
            player,
            attackKnockbackForce,
            transform.position);
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
