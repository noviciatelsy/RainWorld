using System.Collections.Generic;
using UnityEngine;

public class RobotUtilityAI : IMonsterAI
{
    private enum RobotMode
    {
        Idle,
        Charge,
        Recover
    }

    private readonly Robot2D robot;

    private RobotMode mode = RobotMode.Idle;
    private List<Vector2> activePath;
    private Transform chargeTarget;
    private float recoverTimer;
    private float chargeTimer;

    public RobotUtilityAI(Robot2D robot)
    {
        this.robot = robot;
    }

    public IIntent Evaluate(MonsterBase owner)
    {
        if (owner is not Robot2D rb)
        {
            return RecoverIntent();
        }

        TickMode(rb);

        switch (mode)
        {
            case RobotMode.Recover:
                return RecoverIntent();

            case RobotMode.Charge:
                if (!rb.Arrived)
                {
                    return ChargeIntent(activePath, chargeTarget);
                }

                BeginRecover(rb);
                return RecoverIntent();

            default:
                if (TryBeginCharge(rb))
                {
                    return ChargeIntent(activePath, chargeTarget);
                }

                if (activePath != null && activePath.Count > 0 && !rb.Arrived)
                {
                    return IdleIntent(activePath);
                }

                if (rb.Arrived || activePath == null || activePath.Count == 0)
                {
                    activePath = RobotGroundPath.FindRandomIdlePath(rb.Position, robot.idleBounds);
                    rb.Arrived = activePath == null || activePath.Count == 0;
                    rb.DebugPath = activePath;
                }

                return IdleIntent(activePath);
        }
    }

    private void TickMode(Robot2D rb)
    {
        if (mode == RobotMode.Recover)
        {
            recoverTimer -= Time.fixedDeltaTime;

            if (recoverTimer <= 0f)
            {
                mode = RobotMode.Idle;
                activePath = null;
                rb.Arrived = true;
            }

            return;
        }

        if (mode == RobotMode.Charge)
        {
            chargeTimer -= Time.fixedDeltaTime;

            if (chargeTimer <= 0f)
            {
                rb.Arrived = true;
            }

            if (rb.Arrived)
            {
                BeginRecover(rb);
            }
        }
    }

    private bool TryBeginCharge(Robot2D rb)
    {
        Transform player = rb.FindClosestPlayerTransform();

        if (player == null)
        {
            return false;
        }

        chargeTarget = player;
        activePath = RobotGroundPath.BuildChargePath(rb.Position, player.position);
        mode = RobotMode.Charge;
        chargeTimer = robot.chargeMaxDuration;
        rb.Arrived = false;
        rb.DebugPath = activePath;
        rb.CurrentBehavior = RobotBehavior.Charge;
        return true;
    }

    private void BeginRecover(Robot2D rb)
    {
        mode = RobotMode.Recover;
        recoverTimer = robot.recoverDuration;
        chargeTarget = null;
        activePath = null;
        rb.Arrived = true;
    }

    private RobotMoveIntent IdleIntent(List<Vector2> path)
    {
        return new RobotMoveIntent
        {
            behavior = RobotBehavior.Idle,
            pathVertices = path,
            moveSpeed = robot.moveSpeed,
            chargeTarget = null
        };
    }

    private RobotMoveIntent ChargeIntent(List<Vector2> path, Transform target)
    {
        return new RobotMoveIntent
        {
            behavior = RobotBehavior.Charge,
            pathVertices = path,
            moveSpeed = robot.chargeSpeed,
            chargeTarget = target
        };
    }

    private RobotMoveIntent RecoverIntent()
    {
        return new RobotMoveIntent
        {
            behavior = RobotBehavior.Recover,
            pathVertices = null,
            moveSpeed = 0f,
            chargeTarget = null
        };
    }
}
