using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Idle 沿平地来回巡逻；Charge 固定水平冲刺，遇边缘/墙面即停，撞可破坏墙时触发碎裂。
/// </summary>
public class RobotMotor : IMonsterMotor
{
    private const float MinChargeTravelBeforeHit = 0.25f;
    private const float MinWallProbeDistance = 1.15f;

    private readonly Robot2D robot;
    private readonly Collider2D[] wallProbeBuffer = new Collider2D[8];

    private List<Vector2> activePath;
    private int pathIndex;
    private bool chargeDamageDealt;
    private bool chargeWallBroken;
    private Vector2 chargeStartPosition;

    private float lockedFeetY;
    private int lockedRowY = int.MinValue;
    private bool lockedOnPlatform;
    private RobotGroundPath.RobotSurfaceSupport lockedSurface;
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

        EnemyRobotAudioEmitter audioEmitter = rb.GetComponent<EnemyRobotAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.StopDashLoop();
        }

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
        if (!RobotGroundPath.TryResolveSurfaceSupport(rb.Position, rb.feetYOffset, out lockedSurface))
        {
            return false;
        }

        lockedRowY = lockedSurface.RowY;
        lockedFeetY = lockedSurface.FeetY;
        lockedOnPlatform = lockedSurface.IsPlatform;
        return true;
    }

    private bool TryAdvanceSurface(Robot2D rb, float worldX)
    {
        if (!RobotGroundPath.TryQueryStandPoint(
                new Vector2(worldX, lockedFeetY),
                rb.feetYOffset,
                lockedFeetY,
                out RobotGroundPath.RobotSurfaceSupport stand))
        {
            return false;
        }

        lockedSurface = stand;
        lockedRowY = stand.RowY;
        lockedFeetY = stand.FeetY;
        lockedOnPlatform = stand.IsPlatform;
        return true;
    }

    private void BeginCharge(Robot2D rb, Transform chargeTarget)
    {
        chargeDamageDealt = false;
        chargeWallBroken = false;
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

        float walkableDist = RobotGroundPath.ProbeSurfaceDistance(
            rb.Position,
            lockedSurface,
            chargeDir,
            rb.chargeDistance,
            rb.feetYOffset
        );

        float wallDist = ProbeDestructibleWallDistance(rb, rb.chargeDistance);
        float travelDist = Mathf.Min(rb.chargeDistance, walkableDist, wallDist);
        chargeEndX = rb.Position.x + chargeDir * travelDist;

        rb.Arrived = false;
        rb.CurrentTarget = new Vector2(chargeEndX, lockedFeetY);
        rb.DebugTarget = rb.CurrentTarget;

        EnemyRobotAudioEmitter audioEmitter = rb.GetComponent<EnemyRobotAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.StartDashLoop();
        }
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

        if (!TryAdvanceSurface(rb, nextX))
        {
            if (!chargeWallBroken)
            {
                TryBreakDestructibleWallOnChargeImpact(rb);
            }

            FinishCharge(rb);
            return;
        }

        if (TryHitDestructibleWallOnChargeSegment(rb, rb.Position.x, nextX, out float wallStopX))
        {
            chargeWallBroken = true;
            nextX = wallStopX;
            TryAdvanceSurface(rb, nextX);
            rb.Transform.position = new Vector3(nextX, lockedFeetY, rb.Transform.position.z);
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
        if (chargeActive && !chargeDamageDealt && !chargeWallBroken)
        {
            TryBreakDestructibleWallOnChargeImpact(rb);
        }

        rb.Arrived = true;
        chargeActive = false;
        chargeWallBroken = false;
        activePath = null;
        pathIndex = 0;

        EnemyRobotAudioEmitter audioEmitter = rb.GetComponent<EnemyRobotAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.StopDashLoop();
        }
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

        if (!TryAdvanceSurface(rb, nextX))
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
        float targetY = lockedFeetY;

        if (!lockedOnPlatform && targetY > rb.Position.y + 0.02f)
        {
            targetY = rb.Position.y;
        }

        rb.Transform.position = new Vector3(rb.Position.x, targetY, rb.Transform.position.z);
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

            EnemyRobotAudioEmitter audioEmitter = rb.GetComponent<EnemyRobotAudioEmitter>();
            if (audioEmitter != null)
            {
                audioEmitter.PlayHit();
            }
        }
    }

    private float ProbeDestructibleWallDistance(Robot2D rb, float maxDistance)
    {
        if (chargeDir == 0 || maxDistance <= 0f)
        {
            return maxDistance;
        }

        int layerMask = rb.destructibleWallLayer.value;

        if (layerMask == 0)
        {
            return maxDistance;
        }

        float halfHeight = Mathf.Max(0.35f, rb.chargeDestructibleWallProbeHalfHeight);
        float bodyCenterY = lockedFeetY + halfHeight;
        Vector2 castOrigin = new Vector2(rb.Position.x - chargeDir * 0.08f, bodyCenterY);
        Vector2 castDirection = new Vector2(chargeDir, 0f);
        Vector2 castSize = new Vector2(0.5f, halfHeight * 2f);
        float castDistance = maxDistance + 0.2f;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            castOrigin,
            castSize,
            0f,
            castDirection,
            castDistance,
            layerMask);

        IDestructibleWallNotify wall = FindWallNotifyFromRaycastHits(hits, rb.transform);

        if (wall == null)
        {
            return maxDistance;
        }

        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null)
            {
                continue;
            }

            DestructibleWall destructibleWall = hits[i].collider.GetComponentInParent<DestructibleWall>();

            if (destructibleWall == null || destructibleWall.IsDestroyed)
            {
                continue;
            }

            closestDistance = Mathf.Min(closestDistance, hits[i].distance);
        }

        if (closestDistance >= float.MaxValue)
        {
            return maxDistance;
        }

        return Mathf.Clamp(closestDistance - 0.08f, 0f, maxDistance);
    }

    private bool TryHitDestructibleWallOnChargeSegment(
        Robot2D rb,
        float segmentStartX,
        float segmentEndX,
        out float stopX)
    {
        stopX = segmentEndX;

        if (chargeDir == 0 || chargeWallBroken)
        {
            return false;
        }

        float segmentLength = Mathf.Abs(segmentEndX - segmentStartX);

        if (segmentLength < 0.0001f)
        {
            return false;
        }

        if (Mathf.Abs(segmentEndX - chargeStartPosition.x) < MinChargeTravelBeforeHit
            && Mathf.Abs(segmentStartX - chargeStartPosition.x) < MinChargeTravelBeforeHit)
        {
            return false;
        }

        int layerMask = rb.destructibleWallLayer.value;

        if (layerMask == 0)
        {
            return false;
        }

        float halfHeight = Mathf.Max(0.35f, rb.chargeDestructibleWallProbeHalfHeight);
        float bodyCenterY = lockedFeetY + halfHeight;
        Vector2 castOrigin = new Vector2(segmentStartX - chargeDir * 0.06f, bodyCenterY);
        Vector2 castDirection = new Vector2(chargeDir, 0f);
        Vector2 castSize = new Vector2(0.5f, halfHeight * 2f);
        float castDistance = segmentLength + 0.18f;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            castOrigin,
            castSize,
            0f,
            castDirection,
            castDistance,
            layerMask);

        IDestructibleWallNotify wall = FindWallNotifyFromRaycastHits(hits, rb.transform);

        if (wall == null)
        {
            wall = FindWallNotifyFromOverlapBuffer(
                Physics2D.OverlapCircleNonAlloc(
                    new Vector2(segmentEndX + chargeDir * 0.08f, bodyCenterY),
                    Mathf.Max(0.45f, halfHeight),
                    wallProbeBuffer,
                    layerMask),
                rb.transform);
        }

        if (wall == null)
        {
            return false;
        }

        wall.NotifyWallDestroy();
        chargeWallBroken = true;

        float closestDistance = float.MaxValue;

        if (hits != null)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null)
                {
                    continue;
                }

                DestructibleWall destructibleWall = hits[i].collider.GetComponentInParent<DestructibleWall>();

                if (destructibleWall == null)
                {
                    continue;
                }

                closestDistance = Mathf.Min(closestDistance, hits[i].distance);
            }
        }

        float stopOffset = closestDistance < float.MaxValue
            ? Mathf.Max(0.04f, closestDistance - 0.1f)
            : Mathf.Max(0.04f, segmentLength - 0.1f);
        stopX = segmentStartX + chargeDir * stopOffset;

        if (chargeDir > 0)
        {
            stopX = Mathf.Clamp(stopX, segmentStartX, segmentEndX);
        }
        else
        {
            stopX = Mathf.Clamp(stopX, segmentEndX, segmentStartX);
        }

        return true;
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
            chargeWallBroken = true;
        }
    }

    private IDestructibleWallNotify FindDestructibleWallAhead(Robot2D rb)
    {
        int layerMask = rb.destructibleWallLayer.value;
        float probeDistance = Mathf.Max(MinWallProbeDistance, rb.chargeDestructibleWallProbeDistance);
        float halfHeight = Mathf.Max(0.35f, rb.chargeDestructibleWallProbeHalfHeight);
        float bodyCenterY = lockedFeetY + halfHeight;
        Vector2 castOrigin = new Vector2(rb.Position.x, bodyCenterY);
        Vector2 castDirection = new Vector2(chargeDir, 0f);
        Vector2 castSize = new Vector2(0.55f, halfHeight * 2f);

        IDestructibleWallNotify wall = FindWallNotifyFromRaycastHits(
            Physics2D.BoxCastAll(
                castOrigin,
                castSize,
                0f,
                castDirection,
                probeDistance,
                layerMask),
            rb.transform);

        if (wall != null)
        {
            return wall;
        }

        Vector2 overlapCenter = castOrigin + castDirection * (probeDistance * 0.5f);
        Vector2 overlapSize = new Vector2(probeDistance + 0.35f, halfHeight * 2f);

        wall = FindWallNotifyFromOverlapBuffer(
            Physics2D.OverlapBoxNonAlloc(
                overlapCenter,
                overlapSize,
                0f,
                wallProbeBuffer,
                layerMask),
            rb.transform);

        if (wall != null)
        {
            return wall;
        }

        return FindWallNotifyAlongChargeDirection(rb, probeDistance, halfHeight, layerMask);
    }

    private IDestructibleWallNotify FindWallNotifyAlongChargeDirection(
        Robot2D rb,
        float probeDistance,
        float halfHeight,
        int layerMask)
    {
        float bodyCenterY = lockedFeetY + halfHeight;
        int sampleCount = 4;

        for (int i = 1; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            Vector2 samplePoint = new Vector2(
                rb.Position.x + chargeDir * probeDistance * t,
                bodyCenterY);

            IDestructibleWallNotify wall = FindWallNotifyFromOverlapBuffer(
                Physics2D.OverlapCircleNonAlloc(
                    samplePoint,
                    halfHeight,
                    wallProbeBuffer,
                    layerMask),
                rb.transform);

            if (wall != null)
            {
                return wall;
            }
        }

        return FindWallNotifyAtBlockedFrontCell(rb, halfHeight, layerMask);
    }

    private IDestructibleWallNotify FindWallNotifyAtBlockedFrontCell(
        Robot2D rb,
        float halfHeight,
        int layerMask)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null || chargeDir == 0)
        {
            return null;
        }

        float feetY = lockedFeetY;
        int cellX = mgr.WorldToCell(new Vector2(rb.Position.x, feetY)).x;
        Vector2Int frontCell = new Vector2Int(cellX + chargeDir, lockedRowY);
        Vector2 frontWorld = RobotGroundPath.CellToFeetWorld(mgr, frontCell, rb.feetYOffset);
        float bodyCenterY = lockedFeetY + halfHeight;

        IDestructibleWallNotify wall = FindWallNotifyFromOverlapBuffer(
            Physics2D.OverlapCircleNonAlloc(
                new Vector2(frontWorld.x, bodyCenterY),
                Mathf.Max(0.75f, halfHeight + 0.25f),
                wallProbeBuffer,
                layerMask),
            rb.transform);

        if (wall != null)
        {
            return wall;
        }

        return FindWallNotifyFromOverlapBuffer(
            Physics2D.OverlapCircleNonAlloc(
                frontWorld,
                0.85f,
                wallProbeBuffer,
                layerMask),
            rb.transform);
    }

    private static IDestructibleWallNotify FindWallNotifyFromRaycastHits(
        RaycastHit2D[] hits,
        Transform robotTransform)
    {
        if (hits == null)
        {
            return null;
        }

        IDestructibleWallNotify closestWall = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;

            if (hitCollider == null)
            {
                continue;
            }

            IDestructibleWallNotify wall = FindWallNotifyOnCollider(hitCollider, robotTransform);

            if (wall == null || hits[i].distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hits[i].distance;
            closestWall = wall;
        }

        return closestWall;
    }

    private IDestructibleWallNotify FindWallNotifyFromOverlapBuffer(
        int hitCount,
        Transform robotTransform)
    {
        if (hitCount <= 0)
        {
            return null;
        }

        for (int i = 0; i < hitCount; i++)
        {
            IDestructibleWallNotify wall = FindWallNotifyOnCollider(wallProbeBuffer[i], robotTransform);

            if (wall != null)
            {
                return wall;
            }
        }

        return null;
    }

    private static IDestructibleWallNotify FindWallNotifyOnCollider(
        Collider2D hit,
        Transform robotTransform)
    {
        if (hit == null)
        {
            return null;
        }

        Transform hitTransform = hit.transform;

        if (hitTransform == robotTransform || hitTransform.IsChildOf(robotTransform))
        {
            return null;
        }

        DestructibleWall destructibleWall = hit.GetComponentInParent<DestructibleWall>();

        if (destructibleWall == null || destructibleWall.IsDestroyed)
        {
            return null;
        }

        return destructibleWall;
    }

    private void ResetMovementState(Robot2D rb)
    {
        activePath = null;
        pathIndex = 0;
        chargeActive = false;
        chargeWallBroken = false;
        lockedRowY = int.MinValue;
        lockedOnPlatform = false;
        lockedSurface = default;
    }
}
