using System.Collections.Generic;
using UnityEngine;

public class Bat2D : MonsterBase
{
    [Header("Flight")]
    public float moveSpeed = 4f;
    public float arriveThreshold = 0.05f;

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

    [Header("Debug")]
    public bool enableDebugLog = false;
    public bool drawDebugGizmos = true;

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
        RebuildPerceptionMask();
        OnBatInitialized();
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

        Collider2D[] hits = Physics2D.OverlapCircleAll(Position, attackRange);

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

        if ((preyTransform.position - (Vector3)Position).sqrMagnitude > attackRange * attackRange)
        {
            return false;
        }

        Fly2D fly = preyTransform.GetComponentInParent<Fly2D>();

        if (fly != null)
        {
            Destroy(fly.gameObject);
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

    protected virtual bool ApplyPlayerAttack(Player player)
    {
        if (player == null)
        {
            return false;
        }

        PlayerVitals vitals = player.GetComponent<PlayerVitals>();

        if (vitals != null && !vitals.IsDead)
        {
            vitals.ReduceHealth(attackDamage);
        }

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

        if (DebugHasPrey)
        {
            Gizmos.color = DebugPreyIsFly ? Color.cyan : Color.magenta;
            Gizmos.DrawLine(transform.position, DebugPreyPosition);
            Gizmos.DrawWireSphere(DebugPreyPosition, 0.25f);
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

        for (int i = 0; i < DebugPath.Count - 1; i++)
        {
            Gizmos.DrawLine(DebugPath[i], DebugPath[i + 1]);
        }

        Gizmos.color = Color.yellow;

        foreach (Vector2 pathPoint in DebugPath)
        {
            Gizmos.DrawSphere(pathPoint, 0.06f);
        }

        if (selectedOnlyExtra)
        {
            Gizmos.color = new Color(0.6f, 0.3f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, idleWanderRadiusMin);
            Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, idleWanderRadiusMax);
        }
    }
}
