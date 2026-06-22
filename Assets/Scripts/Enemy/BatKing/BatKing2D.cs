using UnityEngine;

/// <summary>
/// 蝙蝠王：与 Bat2D 共用 AI/Motor/意图，更高伤害与多件掉物；连续完成 N 次攻击后传送玩家。
/// </summary>
public class BatKing2D : Bat2D
{
    [Header("Bat King")]
    [Tooltip("连续完成多少次攻击序列后触发特殊功能")]
    [Min(1)]
    public int attacksForSpecial = 5;

    [Header("Player Teleport")]
    [Tooltip("连续攻击达到次数后，将玩家传送至此世界坐标")]
    [SerializeField] private Vector2 playerTeleportWorldPosition;

    public int ConsecutiveAttackCount { get; private set; }

    protected override void OnBatInitialized()
    {
        base.OnBatInitialized();

        if (attackDamage < 15)
        {
            attackDamage = 18;
        }

        if (knockItemCount < 2)
        {
            knockItemCount = 3;
        }
    }

    protected override void OnAttackSequenceFinished()
    {
        base.OnAttackSequenceFinished();

        ConsecutiveAttackCount++;

        if (ConsecutiveAttackCount < attacksForSpecial)
        {
            return;
        }

        ConsecutiveAttackCount = 0;
        OnSpecialAttackComboReady();
    }

    /// <summary>
    /// 连续攻击达到 attacksForSpecial 次时调用。
    /// </summary>
    protected virtual void OnSpecialAttackComboReady()
    {
        TryTeleportPlayerToTarget();

        if (enableDebugLog)
        {
            Debug.Log(
                $"[BatKing {name}] 连续攻击 {attacksForSpecial} 次，传送玩家至 {playerTeleportWorldPosition}。",
                this
            );
        }
    }

    public void ResetConsecutiveAttackCount()
    {
        ConsecutiveAttackCount = 0;
    }

    private void TryTeleportPlayerToTarget()
    {
        Player player = PlayerManager.Instance != null
            ? PlayerManager.Instance.TryGetCurrentPlayer()
            : null;

        if (player == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[BatKing {name}] 传送失败：未找到玩家。", this);
            }

            return;
        }

        Vector3 targetPosition = new Vector3(
            playerTeleportWorldPosition.x,
            playerTeleportWorldPosition.y,
            player.transform.position.z);
        player.transform.position = targetPosition;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(playerTeleportWorldPosition, 0.35f);
        Gizmos.DrawLine(transform.position, playerTeleportWorldPosition);
    }
#endif
}
