using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 与 MoleMotor 相同：Idle 用路点列表；Charge 沿平地朝玩家 X 冲刺（路点可选）。
/// </summary>
public class RobotMotor : IMonsterMotor
{
    private const float MinChargeTravelBeforeHit = 0.25f;

    private readonly Robot2D robot;

    private List<Vector2> activePath;
    private int pathIndex;
    private bool chargeDamageDealt;
    private Vector2 chargeStartPosition;

    public RobotMotor(Robot2D robot)
    {
        this.robot = robot;
    }

    public void Execute(MonsterBase owner, IIntent intent)
    {
        if (intent is not RobotMoveIntent move || owner is not Robot2D rb)
        {
            return;
        }

        rb.CurrentBehavior = move.behavior;

        if (move.behavior == RobotBehavior.Recover)
        {
            activePath = null;
            pathIndex = 0;
            rb.Arrived = true;
            return;
        }

        if (move.behavior == RobotBehavior.Charge)
        {
            if (activePath != move.pathVertices)
            {
                chargeDamageDealt = false;
                chargeStartPosition = rb.Position;
            }

            DriveCharge(rb, move.pathVertices, move.moveSpeed, move.chargeTarget);
            return;
        }

        if (move.pathVertices == null || move.pathVertices.Count == 0)
        {
            activePath = null;
            pathIndex = 0;
            rb.Arrived = true;
            return;
        }

        DriveMovement(rb, move.pathVertices, move.moveSpeed, null, false);
    }

    /// <summary>
    /// 冲刺：保持当前 Y，朝玩家 X（或路点）快速移动。
    /// </summary>
    private void DriveCharge(
        Robot2D rb,
        List<Vector2> path,
        float speed,
        Transform chargeTarget)
    {
        if (activePath != path)
        {
            activePath = path;
            pathIndex = 0;
        }

        rb.Arrived = false;

        Vector2 moveTarget = GetChargeMoveTarget(rb, path, chargeTarget);

        if ((moveTarget - rb.Position).sqrMagnitude < 0.0001f)
        {
            TryFinishCharge(rb, chargeTarget);
            return;
        }

        rb.CurrentTarget = moveTarget;
        rb.DebugTarget = moveTarget;

        rb.Transform.position = Vector2.MoveTowards(
            rb.Position,
            moveTarget,
            speed * Time.fixedDeltaTime
        );

        rb.UpdateFacingToward(moveTarget);

        AdvanceChargePathIndex(rb, path, moveTarget);

        if (Vector2.Distance(rb.Position, moveTarget) <= rb.arriveThreshold)
        {
            AdvanceChargePathIndex(rb, path, moveTarget);
        }

        TryFinishCharge(rb, chargeTarget);
    }

    private Vector2 GetChargeMoveTarget(Robot2D rb, List<Vector2> path, Transform chargeTarget)
    {
        if (chargeTarget != null)
        {
            return new Vector2(chargeTarget.position.x, rb.Position.y);
        }

        if (path != null && pathIndex < path.Count)
        {
            Vector2 node = path[pathIndex];
            return new Vector2(node.x, rb.Position.y);
        }

        return rb.Position;
    }

    private void AdvanceChargePathIndex(Robot2D rb, List<Vector2> path, Vector2 moveTarget)
    {
        if (path == null || pathIndex >= path.Count)
        {
            return;
        }

        if (Vector2.Distance(rb.Position, moveTarget) <= rb.arriveThreshold)
        {
            pathIndex++;
        }
    }

    private void TryFinishCharge(Robot2D rb, Transform chargeTarget)
    {
        if (chargeTarget == null)
        {
            return;
        }

        float travelSqr = (rb.Position - chargeStartPosition).sqrMagnitude;
        float attackRangeSqr = rb.attackRange * rb.attackRange;
        float distToPlayerSqr = ((Vector2)chargeTarget.position - rb.Position).sqrMagnitude;

        if (travelSqr < MinChargeTravelBeforeHit * MinChargeTravelBeforeHit
            && distToPlayerSqr > attackRangeSqr)
        {
            return;
        }

        if (distToPlayerSqr <= attackRangeSqr)
        {
            TryChargeHit(rb, chargeTarget);
            rb.Arrived = true;
            activePath = null;
            pathIndex = 0;
            return;
        }

        if (pathIndex >= (activePath?.Count ?? 0)
            && Mathf.Abs(chargeTarget.position.x - rb.Position.x) <= rb.arriveThreshold)
        {
            rb.Arrived = true;
            activePath = null;
            pathIndex = 0;
        }
    }

    private void DriveMovement(
        Robot2D rb,
        List<Vector2> path,
        float speed,
        Transform chargeTarget,
        bool isCharging)
    {
        if (activePath != path)
        {
            activePath = path;
            pathIndex = 0;
            rb.Arrived = false;
        }

        if (pathIndex >= path.Count)
        {
            activePath = null;
            pathIndex = 0;
            rb.Arrived = true;
            return;
        }

        Vector2 nodeTarget = path[pathIndex];
        rb.CurrentTarget = nodeTarget;
        rb.DebugTarget = nodeTarget;

        rb.Transform.position = Vector2.MoveTowards(
            rb.Position,
            nodeTarget,
            speed * Time.fixedDeltaTime
        );

        rb.UpdateFacingToward(nodeTarget);

        if (Vector2.Distance(rb.Position, nodeTarget) > rb.arriveThreshold)
        {
            return;
        }

        pathIndex++;

        if (pathIndex >= path.Count)
        {
            activePath = null;
            pathIndex = 0;
            rb.Arrived = true;
        }
    }

    private void TryChargeHit(Robot2D rb, Transform chargeTarget)
    {
        if (chargeDamageDealt || chargeTarget == null)
        {
            return;
        }

        if ((rb.Position - chargeStartPosition).sqrMagnitude
            < MinChargeTravelBeforeHit * MinChargeTravelBeforeHit)
        {
            return;
        }

        if (rb.TryDamagePlayer(chargeTarget))
        {
            chargeDamageDealt = true;
        }
    }
}
