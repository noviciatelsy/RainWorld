using System.Collections.Generic;
using UnityEngine;

public class WolfSpiderUtilityAI : IMonsterAI
{
    private readonly WolfSpider2D owner;

    private WolfSpiderIntent lastIssuedIntent;
    private bool hasIssuedIntent;

    private Transform currentPrey;
    private Vector2 lastKnownPreyPosition;
    private bool hasLastKnownPreyPosition;
    private float aggroMemoryTimer;

    private float idleTimer;
    private float perceptionTimer;
    private float pathPickTimer;
    private Vector2 lastPathGoal;
    private float postAttackRecoveryTimer;

    private Transform lastDebugPrey;
    private WolfSpiderBehavior lastDebugBehavior = WolfSpiderBehavior.Idle;

    private const float JumpTargetLockThreshold = 0.12f;
    private const float JumpTargetLockThresholdSqr = JumpTargetLockThreshold * JumpTargetLockThreshold;
    private const float PreyGoalChangeThresholdSqr = 1.5f * 1.5f;
    private const int MaxPathNodeChecks = 6;
    private const int RefractSampleCount = 10;
    private const int IdleRingSampleCount = 10;

    public WolfSpiderUtilityAI(WolfSpider2D owner)
    {
        this.owner = owner;
        idleTimer = owner.idleJumpInterval;
        perceptionTimer = 0f;
        pathPickTimer = 0f;
        lastIssuedIntent = new WolfSpiderIntent
        {
            behaviorState = WolfSpiderBehavior.Idle,
            jumpTarget = owner.Position,
            focusTarget = null
        };
    }

    public IIntent Evaluate(MonsterBase ownerBase)
    {
        WolfSpider2D spider = ownerBase as WolfSpider2D;

        if (spider == null)
        {
            return lastIssuedIntent;
        }

        UpdatePerception(spider);

        if (spider.drawDebugGizmos)
        {
            UpdateDebugState(spider);
        }

        if (postAttackRecoveryTimer > 0f)
        {
            postAttackRecoveryTimer -= Time.fixedDeltaTime;
        }

        if (spider.ConsumeJumpTargetRejected())
        {
            HandleJumpTargetRejected(spider);
        }

        if (spider.IsJumping)
        {
            return GetHeldIntent(spider);
        }

        if (spider.IsCoolingDown || postAttackRecoveryTimer > 0f)
        {
            lastIssuedIntent = CreateIdleIntent(spider, spider.Position);
            spider.CurrentBehavior = WolfSpiderBehavior.Idle;
            hasIssuedIntent = true;
            return lastIssuedIntent;
        }

        WolfSpiderBehavior behavior = DecideBehavior(spider);
        spider.CurrentBehavior = behavior;

        switch (behavior)
        {
            case WolfSpiderBehavior.Attack:
                lastIssuedIntent = BuildAttackIntent(spider);
                break;

            case WolfSpiderBehavior.Hunt:
                lastIssuedIntent = BuildHuntIntent(spider);
                break;

            default:
                lastIssuedIntent = BuildIdleIntent(spider);
                break;
        }

        hasIssuedIntent = true;
        LogBehaviorChange(spider, behavior);
        return lastIssuedIntent;
    }

    public void NotifyAttackPerformed()
    {
        postAttackRecoveryTimer = owner.attackStiffDuration;
        idleTimer = owner.idleJumpInterval;
        pathPickTimer = 0f;
    }

    private void HandleJumpTargetRejected(WolfSpider2D spider)
    {
        lastIssuedIntent = CreateIdleIntent(spider, spider.Position);
        hasIssuedIntent = true;
        pathPickTimer = 0f;
        idleTimer = 0f;
        spider.LogDebug("落点无效，重新选择跳跃目标。");
    }

    private IIntent GetHeldIntent(WolfSpider2D spider)
    {
        if (!hasIssuedIntent)
        {
            lastIssuedIntent = BuildIdleIntent(spider);
            hasIssuedIntent = true;
        }

        return lastIssuedIntent;
    }

    private void UpdatePerception(WolfSpider2D spider)
    {
        perceptionTimer -= Time.fixedDeltaTime;
        float detectRadiusSqr = spider.detectRadius * spider.detectRadius;

        if (currentPrey != null)
        {
            if (currentPrey.gameObject.activeInHierarchy)
            {
                lastKnownPreyPosition = currentPrey.position;

                if (((Vector2)currentPrey.position - spider.Position).sqrMagnitude <= detectRadiusSqr)
                {
                    hasLastKnownPreyPosition = true;
                    aggroMemoryTimer = spider.aggroMemoryDuration;

                    if (perceptionTimer > 0f)
                    {
                        return;
                    }
                }
            }
            else
            {
                currentPrey = null;
            }
        }

        if (perceptionTimer > 0f)
        {
            if (aggroMemoryTimer > 0f)
            {
                aggroMemoryTimer -= Time.fixedDeltaTime;
            }

            if (aggroMemoryTimer <= 0f)
            {
                currentPrey = null;
            }

            return;
        }

        perceptionTimer = spider.perceptionInterval;

        int hitCount = spider.OverlapPreyNonAlloc(out Collider2D[] hits);
        Transform bestFly = null;
        Transform bestPlayer = null;
        float bestFlyDistSqr = float.MaxValue;
        float bestPlayerDistSqr = float.MaxValue;

        spider.DebugColliderHitCount = hitCount;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            float distSqr = ((Vector2)hit.transform.position - spider.Position).sqrMagnitude;

            if (distSqr > detectRadiusSqr)
            {
                continue;
            }

            if (spider.IsFlyCollider(hit))
            {
                if (distSqr < bestFlyDistSqr)
                {
                    bestFlyDistSqr = distSqr;
                    bestFly = hit.transform;
                }

                continue;
            }

            if (spider.IsPlayerCollider(hit))
            {
                if (distSqr < bestPlayerDistSqr)
                {
                    bestPlayerDistSqr = distSqr;
                    bestPlayer = hit.transform;
                }
            }
        }

        Fly2D closestFly = FlyRegistry.FindClosest(spider.Position, spider.detectRadius, out float flyDistSqr);
        spider.DebugFlyScanCount = FlyRegistry.ActiveCount;

        if (closestFly != null && (bestFly == null || flyDistSqr < bestFlyDistSqr))
        {
            bestFly = closestFly.transform;
        }

        Transform detected = bestFly != null ? bestFly : bestPlayer;

        if (detected != null)
        {
            if (currentPrey != detected)
            {
                spider.LogDebug(
                    $"发现目标: {detected.name} ({(bestFly != null ? "Fly" : "Player")})"
                );
            }

            currentPrey = detected;
            lastKnownPreyPosition = detected.position;
            hasLastKnownPreyPosition = true;
            aggroMemoryTimer = spider.aggroMemoryDuration;
            return;
        }

        if (aggroMemoryTimer > 0f)
        {
            aggroMemoryTimer -= spider.perceptionInterval;
            return;
        }

        if (currentPrey != null)
        {
            spider.LogDebug("目标丢失，仇恨计时结束，回到 Idle。");
        }

        currentPrey = null;
    }

    private void UpdateDebugState(WolfSpider2D spider)
    {
        spider.DebugHasPrey = currentPrey != null || (aggroMemoryTimer > 0f && hasLastKnownPreyPosition);
        spider.DebugAggroTimer = aggroMemoryTimer;
        spider.DebugPreyIsFly = currentPrey != null && currentPrey.GetComponentInParent<Fly2D>() != null;

        if (currentPrey != null)
        {
            spider.DebugPreyName = currentPrey.name;
            spider.DebugPreyPosition = currentPrey.position;
            lastDebugPrey = currentPrey;
            return;
        }

        if (aggroMemoryTimer > 0f && hasLastKnownPreyPosition)
        {
            spider.DebugPreyName = lastDebugPrey != null ? $"{lastDebugPrey.name}(记忆)" : "LastKnown";
            spider.DebugPreyPosition = lastKnownPreyPosition;
            return;
        }

        spider.DebugPreyName = "None";
        spider.DebugPreyPosition = spider.Position;
    }

    private void LogBehaviorChange(WolfSpider2D spider, WolfSpiderBehavior behavior)
    {
        if (!spider.enableDebugLog || behavior == lastDebugBehavior)
        {
            return;
        }

        lastDebugBehavior = behavior;

        switch (behavior)
        {
            case WolfSpiderBehavior.Hunt:
                spider.LogDebug(
                    $"进入 Hunt，目标 {spider.DebugPreyName}，落点 {lastIssuedIntent.jumpTarget}，原因: {spider.DebugPickReason}"
                );
                break;
            case WolfSpiderBehavior.Attack:
                spider.LogDebug($"进入 Attack，原地咬 {spider.DebugPreyName}");
                break;
            case WolfSpiderBehavior.Idle:
                spider.LogDebug("进入 Idle");
                break;
        }
    }

    private WolfSpiderBehavior DecideBehavior(WolfSpider2D spider)
    {
        if (postAttackRecoveryTimer > 0f)
        {
            return WolfSpiderBehavior.Idle;
        }

        if (currentPrey != null)
        {
            if (CanAttack(spider, currentPrey.position))
            {
                return WolfSpiderBehavior.Attack;
            }

            return WolfSpiderBehavior.Hunt;
        }

        if (aggroMemoryTimer > 0f && hasLastKnownPreyPosition)
        {
            if (CanAttack(spider, lastKnownPreyPosition))
            {
                return WolfSpiderBehavior.Attack;
            }

            return WolfSpiderBehavior.Hunt;
        }

        return WolfSpiderBehavior.Idle;
    }

    private bool CanAttack(WolfSpider2D spider, Vector2 preyPosition)
    {
        float attackRangeSqr = spider.attackRange * spider.attackRange;

        if ((preyPosition - spider.Position).sqrMagnitude > attackRangeSqr)
        {
            return false;
        }

        return WolfSpiderSurfaceProbe.HasLineOfSight(spider.Position, preyPosition);
    }

    private WolfSpiderIntent BuildAttackIntent(WolfSpider2D spider)
    {
        return new WolfSpiderIntent
        {
            behaviorState = WolfSpiderBehavior.Attack,
            jumpTarget = spider.Position,
            focusTarget = currentPrey
        };
    }

    private WolfSpiderIntent BuildHuntIntent(WolfSpider2D spider)
    {
        if (!spider.Arrived)
        {
            return hasIssuedIntent && lastIssuedIntent.behaviorState == WolfSpiderBehavior.Hunt
                ? lastIssuedIntent
                : CreateHuntIntent(spider, lastIssuedIntent.jumpTarget);
        }

        if (ShouldKeepCurrentJumpTarget(spider, WolfSpiderBehavior.Hunt))
        {
            return lastIssuedIntent;
        }

        Vector2 preyPosition = GetPreyPosition(spider);

        if (pathPickTimer > 0f && !HasPreyGoalChanged(preyPosition))
        {
            pathPickTimer -= Time.fixedDeltaTime;
            return lastIssuedIntent;
        }

        pathPickTimer = spider.pathPickInterval;
        lastPathGoal = preyPosition;

        Vector2 jumpTarget = PickJumpTarget(spider, preyPosition, WolfSpiderBehavior.Hunt);
        return CreateHuntIntent(spider, jumpTarget);
    }

    private WolfSpiderIntent CreateHuntIntent(WolfSpider2D spider, Vector2 jumpTarget)
    {
        return new WolfSpiderIntent
        {
            behaviorState = WolfSpiderBehavior.Hunt,
            jumpTarget = jumpTarget,
            focusTarget = currentPrey
        };
    }

    private WolfSpiderIntent BuildIdleIntent(WolfSpider2D spider)
    {
        if (!spider.Arrived)
        {
            return hasIssuedIntent ? lastIssuedIntent : CreateIdleIntent(spider, spider.Position);
        }

        if (ShouldKeepCurrentJumpTarget(spider, WolfSpiderBehavior.Idle))
        {
            idleTimer -= Time.fixedDeltaTime;
            return lastIssuedIntent;
        }

        idleTimer -= Time.fixedDeltaTime;

        if (idleTimer > 0f)
        {
            return hasIssuedIntent && lastIssuedIntent.behaviorState == WolfSpiderBehavior.Idle
                ? lastIssuedIntent
                : CreateIdleIntent(spider, spider.Position);
        }

        idleTimer = spider.idleJumpInterval;
        pathPickTimer = spider.pathPickInterval;

        Vector2 jumpTarget = PickJumpTarget(
            spider,
            GetRandomIdleGoal(spider),
            WolfSpiderBehavior.Idle
        );

        return CreateIdleIntent(spider, jumpTarget);
    }

    private bool HasPreyGoalChanged(Vector2 preyPosition)
    {
        return (preyPosition - lastPathGoal).sqrMagnitude > PreyGoalChangeThresholdSqr;
    }

    private bool ShouldKeepCurrentJumpTarget(WolfSpider2D spider, WolfSpiderBehavior expectedBehavior)
    {
        if (!hasIssuedIntent || lastIssuedIntent.behaviorState != expectedBehavior)
        {
            return false;
        }

        return (lastIssuedIntent.jumpTarget - spider.Position).sqrMagnitude > JumpTargetLockThresholdSqr;
    }

    private WolfSpiderIntent CreateIdleIntent(WolfSpider2D spider, Vector2 jumpTarget)
    {
        return new WolfSpiderIntent
        {
            behaviorState = WolfSpiderBehavior.Idle,
            jumpTarget = jumpTarget,
            focusTarget = null
        };
    }

    private Vector2 GetPreyPosition(WolfSpider2D spider)
    {
        if (currentPrey != null)
        {
            return currentPrey.position;
        }

        if (hasLastKnownPreyPosition)
        {
            return lastKnownPreyPosition;
        }

        return spider.Position;
    }

    private Vector2 GetRandomIdleGoal(WolfSpider2D spider)
    {
        Bounds bounds = spider.activityBounds;
        Vector2 center = bounds.center;
        Vector2 extents = bounds.extents;

        for (int i = 0; i < 8; i++)
        {
            Vector2 candidate = new Vector2(
                center.x + Random.Range(-extents.x, extents.x),
                center.y + Random.Range(-extents.y, extents.y)
            );

            if (bounds.Contains(candidate))
            {
                return candidate;
            }
        }

        return center;
    }

    private Vector2 PickJumpTarget(WolfSpider2D spider, Vector2 goal, WolfSpiderBehavior behavior)
    {
        if (TryPathBasedJump(spider, goal, out Vector2 pathJumpTarget))
        {
            spider.DebugPickReason = "Path";
            spider.DebugTarget = pathJumpTarget;
            CacheArcDebug(spider, pathJumpTarget);
            return pathJumpTarget;
        }

        Vector2 bias = goal - spider.Position;

        if (TryRefractJump(spider, goal, bias, behavior, out Vector2 refractJumpTarget))
        {
            spider.DebugPickReason = "Refract";
            spider.DebugTarget = refractJumpTarget;
            CacheArcDebug(spider, refractJumpTarget);
            return refractJumpTarget;
        }

        if (behavior == WolfSpiderBehavior.Idle)
        {
            SurfaceSnapResult idleSample = WolfSpiderSurfaceProbe.SampleSurfaceInRing(
                spider.Position,
                spider.minJumpDist,
                spider.maxJumpDist,
                Random.insideUnitCircle,
                IdleRingSampleCount,
                spider.surfaceSnapMaxDistance,
                spider.visualSurfaceOffset,
                deterministic: false
            );

            if (idleSample.success
                && spider.activityBounds.Contains(idleSample.point)
                && IsCandidateJumpValid(spider, idleSample.point))
            {
                spider.DebugPickReason = "IdleRing";
                spider.DebugTarget = idleSample.point;
                CacheArcDebug(spider, idleSample.point);
                return idleSample.point;
            }
        }

        spider.DebugPickReason = "Stay";
        return spider.Position;
    }

    private void CacheArcDebug(WolfSpider2D spider, Vector2 jumpTarget)
    {
        if (!spider.drawDebugGizmos)
        {
            spider.DebugArcSamples.Clear();
            return;
        }

        WolfSpiderSurfaceProbe.FillArcSamples(
            spider.Position,
            jumpTarget,
            spider.arcHeight,
            spider.CurrentSurfaceNormal,
            spider.DebugArcSamples
        );
    }

    private bool TryPathBasedJump(WolfSpider2D spider, Vector2 goal, out Vector2 jumpTarget)
    {
        jumpTarget = spider.Position;

        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return false;
        }

        List<Vector2> path = mgr.FindPath(spider.Position, goal);

        if (spider.drawDebugGizmos)
        {
            spider.DebugPath = path;
        }

        if (path == null || path.Count == 0)
        {
            return false;
        }

        Vector2 bestPoint = spider.Position;
        float bestScore = float.MinValue;
        int step = Mathf.Max(1, path.Count / MaxPathNodeChecks);

        for (int i = path.Count - 1; i >= 0; i -= step)
        {
            SurfaceSnapResult snap = WolfSpiderSurfaceProbe.SnapToSurface(
                path[i],
                spider.surfaceSnapMaxDistance,
                spider.visualSurfaceOffset,
                spider.Position
            );

            if (!snap.success || !IsCandidateJumpValid(spider, snap.point))
            {
                continue;
            }

            float towardGoal = Vector2.Dot(
                (snap.point - spider.Position).normalized,
                (goal - spider.Position).normalized
            );
            float score = (snap.point - spider.Position).sqrMagnitude + towardGoal * 2f;

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = snap.point;
            }
        }

        if (bestScore <= float.MinValue)
        {
            return false;
        }

        jumpTarget = bestPoint;
        return true;
    }

    private bool TryRefractJump(
        WolfSpider2D spider,
        Vector2 goal,
        Vector2 bias,
        WolfSpiderBehavior behavior,
        out Vector2 jumpTarget)
    {
        jumpTarget = spider.Position;

        SurfaceSnapResult sample = WolfSpiderSurfaceProbe.SampleSurfaceInRing(
            spider.Position,
            spider.minJumpDist,
            spider.maxJumpDist,
            bias.sqrMagnitude > 0.0001f ? bias : Vector2.right,
            RefractSampleCount,
            spider.surfaceSnapMaxDistance,
            spider.visualSurfaceOffset,
            deterministic: behavior == WolfSpiderBehavior.Hunt
        );

        if (!sample.success)
        {
            return false;
        }

        if (behavior == WolfSpiderBehavior.Idle && !spider.activityBounds.Contains(sample.point))
        {
            return false;
        }

        if (!IsCandidateJumpValid(spider, sample.point))
        {
            return false;
        }

        jumpTarget = sample.point;
        return true;
    }

    private bool IsCandidateJumpValid(WolfSpider2D spider, Vector2 candidate)
    {
        return WolfSpiderSurfaceProbe.IsValidJumpTarget(
            spider.Position,
            candidate,
            spider.minJumpDist,
            spider.maxJumpDist,
            spider.arcHeight,
            spider.CurrentSurfaceNormal
        );
    }
}
