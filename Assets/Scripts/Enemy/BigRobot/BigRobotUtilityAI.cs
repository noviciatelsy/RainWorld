using UnityEngine;

public class BigRobotUtilityAI : IMonsterAI
{
    private readonly BigRobot2D bigRobot;

    private float cooldownTimer;

    public BigRobotUtilityAI(BigRobot2D bigRobot)
    {
        this.bigRobot = bigRobot;
    }

    public IIntent Evaluate(MonsterBase owner)
    {
        if (owner is not BigRobot2D br)
        {
            return IdleIntent();
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.fixedDeltaTime;
            br.CurrentBehavior = BigRobotBehavior.Cooldown;
            return CooldownIntent();
        }

        Transform player = br.FindClosestPlayerInActiveBounds();

        if (player != null)
        {
            br.CurrentBehavior = BigRobotBehavior.Attack;
            return AttackIntent(player);
        }

        br.CurrentBehavior = BigRobotBehavior.Idle;
        return IdleIntent();
    }

    public void NotifyAttackFinished()
    {
        cooldownTimer = bigRobot.attackCooldown;
    }

    private static BigRobotIntent IdleIntent()
    {
        return new BigRobotIntent
        {
            behavior = BigRobotBehavior.Idle,
            attackTarget = null
        };
    }

    private static BigRobotIntent CooldownIntent()
    {
        return new BigRobotIntent
        {
            behavior = BigRobotBehavior.Cooldown,
            attackTarget = null
        };
    }

    private static BigRobotIntent AttackIntent(Transform target)
    {
        return new BigRobotIntent
        {
            behavior = BigRobotBehavior.Attack,
            attackTarget = target
        };
    }
}
