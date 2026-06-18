using UnityEngine;

public class Ghost : MonsterBase
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    [Tooltip("绕行强度：0=直线，1≈绕玩家一圈，越大弯越紧")]
    public float spiralCurvature = 0.8f;
    [Tooltip("每隔多久重新规划一段螺旋路径")]
    public float pathPlanInterval = 2f;

    [Header("Combat")]
    public float attackRange = 0.9f;
    public int attackDamage = 30;
    [Tooltip("攻击后原地等待时间")]
    public float waitDuration = 1f;

    [Header("Debug")]
    public bool drawDebugGizmos = true;

    protected override void Init()
    {
        ai = new ghostAI();
        motor = new ghostMotor();
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
        if (playerTransform == null || !IsPlayerInAttackRange(playerTransform))
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

        if (GameStateManager.Instance != null
            && GameStateManager.Instance.currentGameState != GameState.Game)
        {
            return false;
        }

        vitals.ReduceHealth(attackDamage);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (DebugPath != null && DebugPath.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < DebugPath.Count - 1; i++)
            {
                Gizmos.DrawLine(DebugPath[i], DebugPath[i + 1]);
            }
        }
    }
}
