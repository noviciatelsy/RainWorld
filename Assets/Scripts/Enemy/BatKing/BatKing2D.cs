using UnityEngine;

/// <summary>
/// 蝙蝠王：与 Bat2D 共用 AI/Motor/意图，更高伤害与多件掉物；连续完成 N 次攻击后触发特殊回调。
/// </summary>
public class BatKing2D : Bat2D
{
    [Header("Bat King")]
    [Tooltip("连续完成多少次攻击序列后触发特殊功能")]
    [Min(1)]
    public int attacksForSpecial = 3;

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
        OnTripleAttackComboReady();
    }

    /// <summary>
    /// 连续攻击达到 attacksForSpecial 次时调用；后续可在此接剧情/召唤/强化等。
    /// </summary>
    protected virtual void OnTripleAttackComboReady()
    {
        Debug.Log($"[BatKing {name}] 连续攻击 {attacksForSpecial} 次，触发自定义功能。", this);
    }

    public void ResetConsecutiveAttackCount()
    {
        ConsecutiveAttackCount = 0;
    }
}
