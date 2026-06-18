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
    private int patrolDir;

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
                    return ChargeIntent(chargeTarget);
                }

                BeginRecover(rb);
                return RecoverIntent();

            default:
                if (TryBeginCharge(rb))
                {
                    return ChargeIntent(chargeTarget);
                }

                if (activePath != null && activePath.Count > 0 && !rb.Arrived)
                {
                    return IdleIntent(activePath);
                }

                if (rb.Arrived || activePath == null || activePath.Count == 0)
                {
                    activePath = BuildNextPatrolPath(rb);

                    if (activePath == null || activePath.Count == 0)
                    {
                        patrolDir *= -1;
                        activePath = BuildNextPatrolPath(rb);
                    }

                    rb.Arrived = activePath == null || activePath.Count == 0;
                    rb.DebugPath = activePath;
                    rb.CurrentBehavior = RobotBehavior.Idle;
                }

                return IdleIntent(activePath);
        }
    }

    private List<Vector2> BuildNextPatrolPath(Robot2D rb)
    {
        if (!RobotGroundPath.IsInsideBoundsXY(robot.idleBounds, rb.Position))
        {
            List<Vector2> returnPath = RobotGroundPath.BuildReturnToIdleBoundsPath(
                rb.Position,
                robot.idleBounds,
                robot.feetYOffset);

            if (returnPath.Count > 0)
            {
                return returnPath;
            }
        }

        if (patrolDir == 0)
        {
            patrolDir = RobotGroundPath.PickPatrolDirection(
                rb.Position,
                robot.idleBounds,
                robot.feetYOffset);
        }

        return RobotGroundPath.BuildPatrolPath(
            rb.Position,
            patrolDir,
            robot.idleBounds,
            robot.feetYOffset
        );
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
            if (chargeTarget != null
                && (!PlayerInvisibilityPerception.IsPlayerDetectable(chargeTarget)
                    || !robot.IsInsideActiveBounds(chargeTarget.position)))
            {
                BeginRecover(rb);
                return;
            }

            chargeTimer -= Time.fixedDeltaTime;

            if (chargeTimer <= 0f)
            {
                rb.Arrived = true;
            }

            if (rb.Arrived)
            {
                BeginRecover(rb);
            }

            return;
        }

        if (mode == RobotMode.Idle && rb.Arrived && activePath != null && activePath.Count > 0)
        {
            if (RobotGroundPath.IsInsideBoundsXY(robot.idleBounds, rb.Position))
            {
                patrolDir *= -1;
            }

            activePath = null;
        }
    }

    private bool TryBeginCharge(Robot2D rb)
    {
        Transform player = rb.FindClosestPlayerTransform();

        if (player == null)
        {
            return false;
        }

        if (rb.IsOnPlatformSurface() && !IsPlayerAhead(rb, player.position))
        {
            return false;
        }

        chargeTarget = player;
        activePath = null;
        mode = RobotMode.Charge;
        chargeTimer = robot.chargeMaxDuration;
        rb.Arrived = false;
        rb.DebugPath = null;
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

    private RobotMoveIntent ChargeIntent(Transform target)
    {
        return new RobotMoveIntent
        {
            behavior = RobotBehavior.Charge,
            pathVertices = null,
            moveSpeed = robot.chargeSpeed,
            chargeTarget = target
        };
    }

    private bool IsPlayerAhead(Robot2D rb, Vector2 playerPos)
    {
        float deltaX = playerPos.x - rb.Position.x;

        if (Mathf.Abs(deltaX) <= robot.arriveThreshold)
        {
            return true;
        }

        int moveDir = patrolDir;

        if (moveDir == 0)
        {
            Transform visual = robot.bodyVisual != null ? robot.bodyVisual : rb.transform;
            moveDir = visual.localScale.x >= 0f ? 1 : -1;
        }

        return Mathf.Sign(deltaX) == moveDir;
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
