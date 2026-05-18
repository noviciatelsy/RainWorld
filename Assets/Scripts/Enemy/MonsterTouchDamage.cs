using UnityEngine;

public class MonsterTouchDamage : MonoBehaviour
{
    [Header("伤害设置")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float cooldownTime = 2f;

    [Header("目标检测")]
    [SerializeField] private LayerMask playerLayer;

    private float cooldownTimer = 0f;

    private void Update()
    {
        // 冷却计时器倒计时
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    // 适用于 2D Trigger 触发器 (如果怪物的 Collider 勾选了 Is Trigger)
    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealDamage(other.gameObject);
    }

    // 适用于 2D 普通碰撞体 (如果怪物的 Collider 是实体碰撞)
    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDealDamage(collision.gameObject);
    }

    /// <summary>
    /// 核心伤害判定
    /// </summary>
    private void TryDealDamage(GameObject target)
    {
        // 1. 检查是否在冷却中
        if (cooldownTimer > 0f) return;

        // 2. 使用 LayerMask 检查碰到的物体是否是玩家
        // (1 << target.layer) 可以把物体的 layer 转换为对应的二进制位进行位与运算
        if (((1 << target.layer) & playerLayer) != 0)
        {
            // 3. 尝试获取玩家的生命组件
            PlayerVitals playerVitals = target.GetComponent<PlayerVitals>();

            if (playerVitals != null && !playerVitals.IsDead)
            {
                // 4. 实施伤害并重置冷却时间
                playerVitals.ReduceHealth(damageAmount);
                cooldownTimer = cooldownTime;

                Debug.Log($"【怪物伤害】{gameObject.name} 触碰了玩家，造成 {damageAmount} 点伤害！" +
                          $"下次伤害冷却：{cooldownTime}秒。");
            }
        }
    }
}