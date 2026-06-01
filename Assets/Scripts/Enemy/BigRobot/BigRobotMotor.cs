using UnityEngine;

/// <summary>
/// 大机器人不移动，仅在 Attack 意图下执行一次攻击判定。
/// </summary>
public class BigRobotMotor : IMonsterMotor
{
    private readonly BigRobot2D bigRobot;
    private readonly BigRobotUtilityAI bigRobotAI;

    public BigRobotMotor(BigRobot2D bigRobot, BigRobotUtilityAI bigRobotAI)
    {
        this.bigRobot = bigRobot;
        this.bigRobotAI = bigRobotAI;
    }

    public void Execute(MonsterBase owner, IIntent intent)
    {
        if (intent is not BigRobotIntent move || owner is not BigRobot2D br)
        {
            return;
        }

        br.CurrentBehavior = move.behavior;
        br.Arrived = true;

        if (move.behavior != BigRobotBehavior.Attack)
        {
            return;
        }

        if (move.attackTarget == null)
        {
            return;
        }

        if (!br.IsInsideActiveBounds(move.attackTarget.position))
        {
            return;
        }

        bool damageDealt = br.TryDamagePlayer(move.attackTarget);
        br.OnAttackPerformed(move.attackTarget, damageDealt);
        bigRobotAI?.NotifyAttackFinished();
    }
}
