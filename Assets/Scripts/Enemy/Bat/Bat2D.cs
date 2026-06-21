using System.Collections.Generic;
using UnityEngine;

public class Bat2D : MonsterBase, IMosquitoCoilRepellable, ITorchRepellable, IMeatBaitAttractable, IToyCarAttractable
{
    [Header("Flight")]
    public float moveSpeed = 4f;
    public float arriveThreshold = 0.05f;

    [Header("Hunt Sector")]
    [Tooltip("追踪玩家/飞虫时，在猎物上方该半径的扇形内选点")]
    public float huntFanRadius = 2f;
    [Tooltip("扇形总角度（以正上方为中心，默认 120°）")]
    [Range(30f, 180f)]
    public float huntFanAngle = 120f;

    [Header("Perception")]
    public float detectRadius = 10f;
    public float perceptionInterval = 0.2f;
    public float pathPickInterval = 0.35f;
    public LayerMask playerLayer;
    public LayerMask flyLayer;
    [Tooltip("可选：第三优先级猎物 Layer")]
    public LayerMask otherPreyLayer;
    public string flyTag = "Fly";

    [Header("Combat")]
    public float attackRange = 1.4f;
    public float attackStiffDuration = 1f;
    public int attackDamage = 8;
    [Tooltip("每次命中玩家时尝试掉落的道具件数")]
    [Min(0)]
    public int knockItemCount = 1;
    [Tooltip("每件道具的掉落概率 0~1")]
    [Range(0f, 1f)]
    public float knockItemChance = 1f;

    [Header("Attack Motion (纯位移，无动画)")]
    [Tooltip("前探/退回相对锚点的小位移（世界单位）")]
    public float attackLungeDistance = 0.35f;
    public float attackLungeSpeed = 10f;
    public float attackRetreatSpeed = 10f;
    [Tooltip("前探/退回到位判定距离")]
    public float attackPhaseArriveThreshold = 0.03f;
    [Tooltip("打击判定停留（秒），无动画时保持很短即可")]
    public float attackStrikeHoldDuration = 0.05f;

    [Header("Visual")]
    [Tooltip("贴图子物体；仅左右翻转 scale.x，永不旋转")]
    public Transform bodyVisual;

    [Header("Aggro")]
    public float aggroMemoryDuration = 3f;

    [Header("Idle")]
    public Bounds activityBounds;
    public float idleMoveInterval = 2f;
    public float idleWanderRadiusMin = 2f;
    public float idleWanderRadiusMax = 8f;

    [Header("Path")]
    public float maxStepAlongPath = 4f;
    [Tooltip("超过该距离（世界单位）不尝试 A*")]
    public float maxPathFindDistance = 14f;
    [Tooltip("与 TileMapGuideManager A* 搜索半径一致（格子）")]
    public int maxPathSearchCells = 50;
    [Tooltip("单次 A* 最大迭代次数（隔墙失败时尽快放弃）")]
    public int maxPathFindIterations = 400;

    [Header("Debug")]
    public bool enableDebugLog = false;
    public bool drawDebugGizmos = false;

    public int PerceptionMask { get; private set; }
    public BatBehavior CurrentBehavior { get; set; } = BatBehavior.Idle;

    public bool IsAttacking { get; set; }
    public bool IsInAttackSequence { get; set; }
    public bool IsCoolingDown { get; set; }
    public Vector2 LastMoveDirection { get; private set; } = Vector2.right;

    private float baseVisualScaleX = 1f;

    public bool DebugHasPrey { get; set; }
    public bool DebugPreyIsFly { get; set; }
    public string DebugPreyName { get; set; } = "None";
    public Vector2 DebugPreyPosition { get; set; }
    public float DebugAggroTimer { get; set; }
    public string DebugPickReason { get; set; } = string.Empty;

    /// <summary>当前追击目标经 A* 判定不可达；AI/Motor 进入停留，interval 后再试。</summary>
    public bool HuntPathUnreachable { get; set; }

    private readonly Collider2D[] overlapBuffer = new Collider2D[16];
    protected BatUtilityAI batAI;

    protected override void Init()
    {
        CreateAIAndMotor();

        Arrived = true;
        IsAttacking = false;
        IsInAttackSequence = false;
        IsCoolingDown = false;

        Transform visual = bodyVisual != null ? bodyVisual : transform;
        baseVisualScaleX = Mathf.Abs(visual.localScale.x);

        if (baseVisualScaleX < 0.001f)
        {
            baseVisualScaleX = 1f;
        }

        transform.rotation = Quaternion.identity;

        LockVisualRotation();

        if (activityBounds.size.sqrMagnitude < 0.01f)
        {
            activityBounds = new Bounds(transform.position, new Vector3(14f, 10f, 1f));
        }

        ResolveFlyLayerMask();
        ResolvePlayerLayerMask();
        RebuildPerceptionMask();
        EnemyStompReceiver.Ensure(
            this,
            bodyVisual != null ? bodyVisual : transform,
            new Vector2(0.9f, 0.12f));
        ApplyFlightPerformanceDefaults();
        OnBatInitialized();
    }

    /// <summary>
    /// 运行时覆盖 Prefab 中可能遗留的高开销 Debug/寻路参数；BatKing2D 继承同样逻辑。
    /// </summary>
    protected virtual void ApplyFlightPerformanceDefaults()
    {
        if (!enableDebugLog)
        {
            drawDebugGizmos = false;
        }

        if (maxPathFindIterations <= 0)
        {
            maxPathFindIterations = 400;
        }

        if (maxPathFindDistance <= 0f)
        {
            maxPathFindDistance = 14f;
        }

        if (maxPathSearchCells <= 0)
        {
            maxPathSearchCells = 50;
        }
    }

    private void OnValidate()
    {
        ResolveFlyLayerMask();
        ResolvePlayerLayerMask();
        RebuildPerceptionMask();
    }

    protected virtual void CreateAIAndMotor()
    {
        batAI = new BatUtilityAI(this);
        ai = batAI;
        motor = new BatMotor(this);
    }

    protected virtual void OnBatInitialized()
    {
    }

    public void RebuildPerceptionMask()
    {
        PerceptionMask = playerLayer.value | flyLayer.value | otherPreyLayer.value;
    }

    public int OverlapPreyNonAlloc(out Collider2D[] buffer)
    {
        buffer = overlapBuffer;
        float radius = detectRadius;

        if (PerceptionMask != 0)
        {
            return Physics2D.OverlapCircleNonAlloc(Position, radius, overlapBuffer, PerceptionMask);
        }

        return Physics2D.OverlapCircleNonAlloc(Position, radius, overlapBuffer);
    }

    public void ResolveFlyLayerMask()
    {
        int flyLayerIndex = LayerMask.NameToLayer("fly");

        if (flyLayerIndex >= 0)
        {
            flyLayer = 1 << flyLayerIndex;
        }

        RebuildPerceptionMask();
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
            RebuildPerceptionMask();
        }
    }

    public Vector2 PickRandomHuntSectorPoint(Vector2 preyWorldPos)
    {
        Vector2 fallback = preyWorldPos + Vector2.up * huntFanRadius;
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        for (int i = 0; i < 8; i++)
        {
            float halfFanRad = huntFanAngle * 0.5f * Mathf.Deg2Rad;
            const float upAngle = Mathf.PI * 0.5f;
            float angle = upAngle + Random.Range(-halfFanRad, halfFanRad);
            float radius = huntFanRadius * Random.Range(0.85f, 1f);
            Vector2 candidate = preyWorldPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            if (RepellentAvoidance.IsInsideAnyZone(candidate))
            {
                continue;
            }

            if (mgr == null || !RepellentAvoidance.IsInsideAnyZone(candidate))
            {
                return candidate;
            }
        }

        if (!RepellentAvoidance.IsInsideAnyZone(fallback))
        {
            return fallback;
        }

        return RepellentAvoidance.GetFleePointAwayFromAll(Position);
    }

    public bool IsWithinHuntSector(Vector2 preyWorldPos, Vector2 worldPoint)
    {
        Vector2 offset = worldPoint - preyWorldPos;

        if (offset.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        if (offset.y <= 0f)
        {
            return false;
        }

        if (offset.magnitude > huntFanRadius + 0.05f)
        {
            return false;
        }

        return Vector2.Angle(Vector2.up, offset) <= huntFanAngle * 0.5f + 0.01f;
    }

    public bool CanAttackPosition(Vector2 preyWorldPos)
    {
        if (IsWithinHuntSector(preyWorldPos, Position))
        {
            return true;
        }

        float attackRangeSqr = attackRange * attackRange;
        return (preyWorldPos - Position).sqrMagnitude <= attackRangeSqr;
    }

    public bool IsInsideActivityBounds(Vector2 point)
    {
        return activityBounds.size.sqrMagnitude < 0.01f || activityBounds.Contains(point);
    }

    public virtual void NotifyAttackPerformed()
    {
        batAI?.NotifyAttackPerformed();
        OnAttackSequenceFinished();
    }

    /// <summary>
    /// 一次完整攻击（前探-打击-退回）结束后的回调，子类可扩展连击等逻辑。
    /// </summary>
    protected virtual void OnAttackSequenceFinished()
    {
    }

    public bool IsFlyCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(flyTag) && collider.CompareTag(flyTag))
        {
            return true;
        }

        return flyLayer.value != 0
            && (flyLayer.value & (1 << collider.gameObject.layer)) != 0;
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

    public bool IsOtherPreyCollider(Collider2D collider)
    {
        if (collider == null || otherPreyLayer.value == 0)
        {
            return false;
        }

        if (IsFlyCollider(collider) || IsPlayerCollider(collider))
        {
            return false;
        }

        return (otherPreyLayer.value & (1 << collider.gameObject.layer)) != 0;
    }

    public void SetLastMoveDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.0001f)
        {
            LastMoveDirection = direction.normalized;
        }
    }

    /// <summary>
    /// 仅左右翻转贴图，不旋转 Transform。
    /// </summary>
    public void UpdateFacingToward(Vector2 worldPoint)
    {
        transform.rotation = Quaternion.identity;

        float deltaX = worldPoint.x - Position.x;

        if (Mathf.Abs(deltaX) < 0.01f)
        {
            return;
        }

        Transform visual = bodyVisual != null ? bodyVisual : transform;
        visual.localRotation = Quaternion.identity;

        Vector3 scale = visual.localScale;
        scale.x = baseVisualScaleX * (deltaX >= 0f ? 1f : -1f);
        visual.localScale = scale;
    }

    private void LateUpdate()
    {
        LockVisualRotation();
    }

    private void LockVisualRotation()
    {
        if (transform.rotation != Quaternion.identity)
        {
            transform.rotation = Quaternion.identity;
        }

        if (bodyVisual == null)
        {
            return;
        }

        if (bodyVisual.localRotation != Quaternion.identity)
        {
            bodyVisual.localRotation = Quaternion.identity;
        }
    }

    public void PerformAttack(Transform focusTarget = null)
    {
        if (focusTarget != null)
        {
            if (TryAttackPreyTransform(focusTarget))
            {
                return;
            }
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(Position, GetStrikeRange());

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null)
            {
                TryAttackPreyTransform(hits[i].transform);
            }
        }
    }

    protected virtual bool TryAttackPreyTransform(Transform preyTransform)
    {
        if (preyTransform == null)
        {
            return false;
        }

        if ((preyTransform.position - (Vector3)Position).sqrMagnitude > GetStrikeRangeSqr())
        {
            return false;
        }

        Fly2D fly = preyTransform.GetComponentInParent<Fly2D>();

        if (fly != null)
        {
            fly.TakeEnemyHit();
            return true;
        }

        Collider2D col = preyTransform.GetComponent<Collider2D>();

        if (col != null && !IsPlayerCollider(col))
        {
            if (IsOtherPreyCollider(col) || IsFlyCollider(col))
            {
                Destroy(preyTransform.root.gameObject);
                return true;
            }
        }

        Player player = preyTransform.GetComponentInParent<Player>();

        if (player == null)
        {
            return false;
        }

        return ApplyPlayerAttack(player);
    }

    private float GetStrikeRange()
    {
        return IsInAttackSequence ? attackRange * 1.2f : attackRange;
    }

    private float GetStrikeRangeSqr()
    {
        float range = GetStrikeRange();
        return range * range;
    }

    protected virtual bool ApplyPlayerAttack(Player player)
    {
        if (player == null)
        {
            return false;
        }

        PlayerVitals vitals = player.GetComponent<PlayerVitals>();
        MonsterPlayerDamage.TryDealDamage(vitals, attackDamage);

        InventoryPlayer inventory = player.GetComponent<InventoryPlayer>();

        if (inventory != null && knockItemCount > 0)
        {
            for (int i = 0; i < knockItemCount; i++)
            {
                if (Random.value > knockItemChance)
                {
                    continue;
                }

                TryKnockRandomItemFromInventory(inventory);
            }
        }

        return true;
    }

    public static bool TryKnockRandomItemFromInventory(InventoryPlayer inventory)
    {
        if (inventory == null || inventory.inventoryItems == null)
        {
            return false;
        }

        List<InventoryItem> items = inventory.inventoryItems;

        if (items.Count <= 0)
        {
            return false;
        }

        for (int attempt = 0; attempt < items.Count; attempt++)
        {
            int index = Random.Range(0, items.Count);
            InventoryItem item = items[index];

            if (item == null || item.ItemData == null)
            {
                continue;
            }

            ItemDataSO data = item.ItemData;

            if (inventory.holdingItem == item)
            {
                inventory.ClearHoldingItem();
            }

            inventory.ClearQuickItem(item);
            inventory.RemoveItem(item);
            inventory.ValidateQuickItems(null);
            inventory.ValidateHoldingItem(null);
            inventory.DropItem(data);
            return true;
        }

        return false;
    }

    public void LogDebug(string message)
    {
        if (!enableDebugLog)
        {
            return;
        }

        Debug.Log($"[Bat {name}] {message}", this);
    }

    public void RepelByMosquitoCoil(Vector2 coilPosition)
    {
        batAI?.NotifyRepelledByMosquitoCoil(coilPosition);
    }

    public void FleeFromTorch(Vector2 torchPosition)
    {
        batAI?.NotifyRepelledByTorch(torchPosition);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || !Application.isPlaying)
        {
            return;
        }

        DrawDebugGizmosInternal(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos || !Application.isPlaying)
        {
            return;
        }

        DrawDebugGizmosInternal(true);
    }

    private void DrawDebugGizmosInternal(bool selectedOnlyExtra)
    {
        Gizmos.color = new Color(0.4f, 0.2f, 0.9f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = new Color(1f, 0f, 0.5f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (activityBounds.size.sqrMagnitude > 0.01f)
        {
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(activityBounds.center, activityBounds.size);
        }

        if (DebugHasPrey && drawDebugGizmos)
        {
            Gizmos.color = DebugPreyIsFly ? Color.cyan : Color.magenta;
            Gizmos.DrawLine(transform.position, DebugPreyPosition);
            Gizmos.DrawWireSphere(DebugPreyPosition, 0.25f);
            DrawHuntSectorGizmo(DebugPreyPosition);
        }

        Gizmos.color = CurrentBehavior switch
        {
            BatBehavior.Hunt => Color.yellow,
            BatBehavior.Attack => Color.red,
            _ => Color.white
        };
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(DebugTarget, 0.15f);

        if (DebugPath == null || DebugPath.Count < 2)
        {
            return;
        }

        Gizmos.color = Color.green;

        int maxSegments = Mathf.Min(DebugPath.Count - 1, 64);

        for (int i = 0; i < maxSegments; i++)
        {
            Gizmos.DrawLine(DebugPath[i], DebugPath[i + 1]);
        }

        if (selectedOnlyExtra && DebugPath.Count >= 2)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(DebugPath[DebugPath.Count - 1], 0.06f);
        }

        if (selectedOnlyExtra)
        {
            Gizmos.color = new Color(0.6f, 0.3f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, idleWanderRadiusMin);
            Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, idleWanderRadiusMax);
        }
    }

    private void DrawHuntSectorGizmo(Vector2 preyPos)
    {
        float halfFanRad = huntFanAngle * 0.5f * Mathf.Deg2Rad;
        const float upAngle = Mathf.PI * 0.5f;
        const int segments = 16;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.75f);
        Vector2 prevPoint = preyPos + new Vector2(Mathf.Cos(upAngle - halfFanRad), Mathf.Sin(upAngle - halfFanRad)) * huntFanRadius;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(upAngle - halfFanRad, upAngle + halfFanRad, t);
            Vector2 point = preyPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * huntFanRadius;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        Gizmos.DrawLine(preyPos, preyPos + new Vector2(Mathf.Cos(upAngle - halfFanRad), Mathf.Sin(upAngle - halfFanRad)) * huntFanRadius);
        Gizmos.DrawLine(preyPos, preyPos + new Vector2(Mathf.Cos(upAngle + halfFanRad), Mathf.Sin(upAngle + halfFanRad)) * huntFanRadius);
    }

    public void AttractToMeatBait(Vector2 myMeatBaitPosition)
    {
        batAI?.ForcePerceptionRefresh();
    }

    public void AttractToToyCar(Vector2 myToyCarPosition)
    {
        batAI?.ForcePerceptionRefresh();
    }
}
