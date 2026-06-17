using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Idle 沿平地来回巡逻；Charge 固定水平冲刺，遇边缘/墙面即停，撞可破坏墙时触发碎裂。
/// </summary>
public class RobotMotor : IMonsterMotor
{
    private const float MinChargeTravelBeforeHit = 0.25f;

    private readonly Robot2D robot;

    private List<Vector2> activePath;
    private int pathIndex;
    private bool chargeDamageDealt;
    private Vector2 chargeStartPosition;

    private float lockedFeetY;
    private int lockedRowY = int.MinValue;
    private bool chargeActive;
    private int chargeDir;
    private float chargeEndX;

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
            ResetMovementState(rb);
            rb.Arrived = true;
            return;
        }

        if (move.behavior == RobotBehavior.Charge)
        {
            if (!chargeActive)
            {
                BeginCharge(rb, move.chargeTarget);
            }

            DriveCharge(rb, move.moveSpeed, move.chargeTarget);
            return;
        }

        chargeActive = false;

        if (move.pathVertices == null || move.pathVertices.Count == 0)
        {
            ResetMovementState(rb);
            rb.Arrived = true;
            return;
        }

        DriveMovement(rb, move.pathVertices, move.moveSpeed);
    }

    private bool TryLockRow(Robot2D rb)
    {
        if (!RobotGroundPath.TryGetFlatRowCell(rb.Position, rb.feetYOffset, out lockedRowY, out _))
        {
            return false;
        }

        lockedFeetY = RobotGroundPath.GetRowFeetY(lockedRowY, rb.feetYOffset);
        return true;
    }

    private void BeginCharge(Robot2D rb, Transform chargeTarget)
    {
        chargeDamageDealt = false;
        chargeStartPosition = rb.Position;
        chargeActive = true;
        activePath = null;
        pathIndex = 0;

        if (chargeTarget == null || !TryLockRow(rb))
        {
            chargeActive = false;
            rb.Arrived = true;
            return;
        }

        AlignToLockedRow(rb);

        float dx = chargeTarget.position.x - rb.Position.x;
        chargeDir = Mathf.Abs(dx) < 0.01f
            ? RobotGroundPath.PickPatrolDirection(rb.Position, rb.idleBounds, rb.feetYOffset)
            : (dx >= 0f ? 1 : -1);

        float walkableDist = RobotGroundPath.ProbeFlatDistance(
            rb.Position,
            lockedRowY,
            chargeDir,
            rb.chargeDistance,
            rb.feetYOffset
        );

        float travelDist = Mathf.Min(rb.chargeDistance, walkableDist);
        chargeEndX = rb.Position.x + chargeDir * travelDist;

        rb.Arrived = false;
        rb.CurrentTarget = new Vector2(chargeEndX, lockedFeetY);
        rb.DebugTarget = rb.CurrentTarget;
    }

    private void DriveCharge(Robot2D rb, float speed, Transform chargeTarget)
    {
        if (!chargeActive)
        {
            rb.Arrived = true;
            return;
        }

        rb.Arrived = false;

        if (chargeTarget != null)
        {
            float travelSqr = (rb.Position - chargeStartPosition).sqrMagnitude;
            float attackRangeSqr = rb.attackRange * rb.attackRange;
            float distToPlayerSqr = ((Vector2)chargeTarget.position - rb.Position).sqrMagnitude;

            if (travelSqr >= MinChargeTravelBeforeHit * MinChargeTravelBeforeHit
                && distToPlayerSqr <= attackRangeSqr)
            {
                TryChargeHit(rb, chargeTarget);
                FinishCharge(rb);
                return;
            }
        }

        if (IsChargeComplete(rb))
        {
            FinishCharge(rb);
            return;
        }

        float step = speed * Time.fixedDeltaTime;
        float nextX = rb.Position.x + chargeDir * step;

        if (chargeDir > 0)
        {
            nextX = Mathf.Min(nextX, chargeEndX);
        }
        else
        {
            nextX = Mathf.Max(nextX, chargeEndX);
        }

        if (!RobotGroundPath.CanStandOnRowAtX(lockedRowY, nextX, rb.feetYOffset))
        {
            FinishCharge(rb);
            return;
        }

        rb.Transform.position = new Vector3(nextX, lockedFeetY, rb.Transform.position.z);
        rb.CurrentTarget = new Vector2(chargeEndX, lockedFeetY);
        rb.DebugTarget = rb.CurrentTarget;

        if (IsChargeComplete(rb))
        {
            FinishCharge(rb);
        }
    }

    private bool IsChargeComplete(Robot2D rb)
    {
        float traveledX = Mathf.Abs(rb.Position.x - chargeStartPosition.x);

        if (traveledX >= rb.chargeDistance - rb.arriveThreshold)
        {
            return true;
        }

        if (chargeDir > 0)
        {
            return rb.Position.x >= chargeEndX - rb.arriveThreshold;
        }

        return rb.Position.x <= chargeEndX + rb.arriveThreshold;
    }

    private void FinishCharge(Robot2D rb)
    {
        if (chargeActive && !chargeDamageDealt)
        {
            TryBreakDestructibleWallOnChargeImpact(rb);
        }

        rb.Arrived = true;
        chargeActive = false;
        activePath = null;
        pathIndex = 0;
    }

    private void DriveMovement(Robot2D rb, List<Vector2> path, float speed)
    {
        if (activePath != path)
        {
            activePath = path;
            pathIndex = 0;
            rb.Arrived = false;

            if (!TryLockRow(rb))
            {
                rb.Arrived = true;
                activePath = null;
                pathIndex = 0;
                return;
            }

            AlignToLockedRow(rb);
        }

        if (pathIndex >= path.Count)
        {
            ResetMovementState(rb);
            rb.Arrived = true;
            return;
        }

        float targetX = path[pathIndex].x;
        rb.CurrentTarget = new Vector2(targetX, lockedFeetY);
        rb.DebugTarget = rb.CurrentTarget;

        float step = speed * Time.fixedDeltaTime;
        float dir = Mathf.Sign(targetX - rb.Position.x);

        if (Mathf.Abs(targetX - rb.Position.x) <= rb.arriveThreshold)
        {
            pathIndex++;

            if (pathIndex >= path.Count)
            {
                ResetMovementState(rb);
                rb.Arrived = true;
            }

            return;
        }

        float nextX = rb.Position.x + dir * step;

        if (dir > 0f)
        {
            nextX = Mathf.Min(nextX, targetX);
        }
        else
        {
            nextX = Mathf.Max(nextX, targetX);
        }

        if (!RobotGroundPath.CanStandOnRowAtX(lockedRowY, nextX, rb.feetYOffset))
        {
            ResetMovementState(rb);
            rb.Arrived = true;
            return;
        }

        rb.Transform.position = new Vector3(nextX, lockedFeetY, rb.Transform.position.z);

        if (Mathf.Abs(targetX - rb.Position.x) <= rb.arriveThreshold)
        {
            pathIndex++;

            if (pathIndex >= path.Count)
            {
                ResetMovementState(rb);
                rb.Arrived = true;
            }
        }
    }

    private void AlignToLockedRow(Robot2D rb)
    {
        rb.Transform.position = new Vector3(rb.Position.x, lockedFeetY, rb.Transform.position.z);
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

    private void TryBreakDestructibleWallOnChargeImpact(Robot2D rb)
    {
        if (chargeDir == 0)
        {
            return;
        }

        if (Mathf.Abs(rb.Position.x - chargeStartPosition.x) < MinChargeTravelBeforeHit)
        {
            return;
        }

        IDestructibleWallNotify wall = FindDestructibleWallAhead(rb);
        if (wall != null)
        {
            wall.NotifyWallDestroy();
        }
    }

    private IDestructibleWallNotify FindDestructibleWallAhead(Robot2D rb)
    {
        float probeDistance = Mathf.Max(0.1f, rb.chargeDestructibleWallProbeDistance);
        float halfHeight = Mathf.Max(0.1f, rb.chargeDestructibleWallProbeHalfHeight);

        Vector2 feetPos = new Vector2(rb.Position.x, lockedFeetY);
        Vector2 center = feetPos + new Vector2(chargeDir * probeDistance * 0.5f, halfHeight * 0.5f);
        Vector2 size = new Vector2(probeDistance, halfHeight * 2f);

        IDestructibleWallNotify wall = FindWallNotifyFromOverlaps(
            Physics2D.OverlapBoxAll(center, size, 0f),
            rb.transform);

        if (wall != null)
        {
            return wall;
        }

        return FindWallNotifyAtBlockedFrontCell(rb);
    }

    private IDestructibleWallNotify FindWallNotifyAtBlockedFrontCell(Robot2D rb)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || chargeDir == 0)
        {
            return null;
        }

        float feetY = lockedFeetY;
        int cellX = mgr.WorldToCell(new Vector2(rb.Position.x, feetY)).x;
        Vector2Int frontCell = new Vector2Int(cellX + chargeDir, lockedRowY);

        if (RobotGroundPath.IsFlatWalkable(mgr, frontCell))
        {
            return null;
        }

        Vector2 frontWorld = RobotGroundPath.CellToFeetWorld(mgr, frontCell, rb.feetYOffset);
        return FindWallNotifyFromOverlaps(Physics2D.OverlapCircleAll(frontWorld, 0.65f), rb.transform);
    }

    private static IDestructibleWallNotify FindWallNotifyFromOverlaps(
        Collider2D[] hits,
        Transform robotTransform)
    {
        if (hits == null)
        {
            return null;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            Transform hitTransform = hit.transform;

            if (hitTransform == robotTransform || hitTransform.IsChildOf(robotTransform))
            {
                continue;
            }

            IDestructibleWallNotify wall = hit.GetComponentInParent<IDestructibleWallNotify>();

            if (wall != null)
            {
                return wall;
            }
        }

        return null;
    }

    private void ResetMovementState(Robot2D rb)
    {
        activePath = null;
        pathIndex = 0;
        chargeActive = false;
        lockedRowY = int.MinValue;
    }
}
