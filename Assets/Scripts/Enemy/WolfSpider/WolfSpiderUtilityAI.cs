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

    private Transform lastDebugPrey;
    private WolfSpiderBehavior lastDebugBehavior = WolfSpiderBehavior.Idle;

    private const float JumpTargetLockThreshold = 0.12f;

    public WolfSpiderUtilityAI(WolfSpider2D owner)
    {
        this.owner = owner;
        idleTimer = owner.idleJumpInterval;
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
        UpdateDebugState(spider);

        if (spider.IsJumping || spider.IsCoolingDown)
        {
            return GetHeldIntent(spider);
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(spider.Position, spider.detectRadius);
        Transform bestFly = null;
        Transform bestPlayer = null;
        float bestFlyDist = float.MaxValue;
        float bestPlayerDist = float.MaxValue;

        spider.DebugColliderHitCount = hits.Length;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            if (spider.IsFlyCollider(hit))
            {
                float dist = Vector2.Distance(spider.Position, hit.transform.position);

                if (dist < bestFlyDist)
                {
                    bestFlyDist = dist;
                    bestFly = hit.transform;
                }

                continue;
            }

            if (spider.IsPlayerCollider(hit))
            {
                float dist = Vector2.Distance(spider.Position, hit.transform.position);

                if (dist < bestPlayerDist)
                {
                    bestPlayerDist = dist;
                    bestPlayer = hit.transform;
                }
            }
        }

        Transform bestFlyByComponent = FindClosestFlyByComponent(spider, ref bestFlyDist, out int flyCount);
        spider.DebugFlyScanCount = flyCount;

        if (bestFlyByComponent != null && (bestFly == null || bestFlyDist > Vector2.Distance(spider.Position, bestFlyByComponent.position)))
        {
            bestFly = bestFlyByComponent;
            bestFlyDist = Vector2.Distance(spider.Position, bestFlyByComponent.position);
        }

        Transform detected = bestFly != null ? bestFly : bestPlayer;

        if (detected != null)
        {
            if (currentPrey != detected)
            {
                spider.LogDebug(
                    $"发现目标: {detected.name} ({(bestFly != null ? "Fly" : "Player")}), 距离 {Vector2.Distance(spider.Position, detected.position):F2}"
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
            aggroMemoryTimer -= Time.fixedDeltaTime;
            return;
        }

        if (currentPrey != null)
        {
            spider.LogDebug("目标丢失，仇恨计时结束，回到 Idle。");
        }

        currentPrey = null;
    }

    private Transform FindClosestFlyByComponent(WolfSpider2D spider, ref float bestFlyDist, out int flyCount)
    {
        Fly2D[] flies = Object.FindObjectsOfType<Fly2D>();
        flyCount = flies.Length;
        Transform closest = null;

        for (int i = 0; i < flies.Length; i++)
        {
            Fly2D fly = flies[i];

            if (fly == null)
            {
                continue;
            }

            float dist = Vector2.Distance(spider.Position, fly.Position);

            if (dist > spider.detectRadius || dist >= bestFlyDist)
            {
                continue;
            }

            bestFlyDist = dist;
            closest = fly.transform;
        }

        return closest;
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
        if (behavior == lastDebugBehavior)
        {
            return;
        }

        lastDebugBehavior = behavior;

        switch (behavior)
        {
            case WolfSpiderBehavior.Hunt:
                spider.LogDebug(
                    $"进入 Hunt，目标 {spider.DebugPreyName} @ {spider.DebugPreyPosition}, 落点 {lastIssuedIntent.jumpTarget}, 原因: {spider.DebugPickReason}"
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
        float distance = Vector2.Distance(spider.Position, preyPosition);

        if (distance > spider.attackRange)
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
        Vector2 preyPosition = GetPreyPosition(spider);

        if (!spider.Arrived)
        {
            return hasIssuedIntent && lastIssuedIntent.behaviorState == WolfSpiderBehavior.Hunt
                ? lastIssuedIntent
                : CreateHuntIntent(spider, preyPosition, lastIssuedIntent.jumpTarget);
        }

        if (ShouldKeepCurrentJumpTarget(spider, WolfSpiderBehavior.Hunt))
        {
            return lastIssuedIntent;
        }

        Vector2 jumpTarget = PickJumpTarget(spider, preyPosition, WolfSpiderBehavior.Hunt);
        return CreateHuntIntent(spider, preyPosition, jumpTarget);
    }

    private WolfSpiderIntent CreateHuntIntent(WolfSpider2D spider, Vector2 preyPosition, Vector2 jumpTarget)
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

        Vector2 jumpTarget = PickJumpTarget(
            spider,
            GetRandomIdleGoal(spider),
            WolfSpiderBehavior.Idle
        );

        return CreateIdleIntent(spider, jumpTarget);
    }

    private bool ShouldKeepCurrentJumpTarget(WolfSpider2D spider, WolfSpiderBehavior expectedBehavior)
    {
        if (!hasIssuedIntent || lastIssuedIntent.behaviorState != expectedBehavior)
        {
            return false;
        }

        float distanceToTarget = Vector2.Distance(spider.Position, lastIssuedIntent.jumpTarget);
        return distanceToTarget > JumpTargetLockThreshold;
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

        for (int i = 0; i < 20; i++)
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
            return pathJumpTarget;
        }

        Vector2 bias = goal - spider.Position;

        if (TryRefractJump(spider, goal, bias, behavior, out Vector2 refractJumpTarget))
        {
            spider.DebugPickReason = "Refract";
            spider.DebugTarget = refractJumpTarget;
            return refractJumpTarget;
        }

        if (behavior == WolfSpiderBehavior.Idle)
        {
            SurfaceSnapResult idleSample = WolfSpiderSurfaceProbe.SampleSurfaceInRing(
                spider.Position,
                spider.minJumpDist,
                spider.maxJumpDist,
                Random.insideUnitCircle,
                32,
                spider.surfaceSnapMaxDistance,
                spider.visualSurfaceOffset,
                deterministic: false
            );

            if (idleSample.success
                && spider.activityBounds.Contains(idleSample.point)
                && WolfSpiderSurfaceProbe.IsValidJumpTarget(
                    spider.Position,
                    idleSample.point,
                    spider.minJumpDist,
                    spider.maxJumpDist,
                    spider.arcHeight))
            {
                spider.DebugPickReason = "IdleRing";
                spider.DebugTarget = idleSample.point;
                return idleSample.point;
            }
        }

        spider.DebugPickReason = "Stay";
        return spider.Position;
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
        spider.DebugPath = path;

        if (path == null || path.Count == 0)
        {
            return false;
        }

        Vector2 bestPoint = spider.Position;
        float bestScore = float.MinValue;

        for (int i = path.Count - 1; i >= 0; i--)
        {
            SurfaceSnapResult snap = WolfSpiderSurfaceProbe.SnapToSurface(
                path[i],
                spider.surfaceSnapMaxDistance,
                spider.visualSurfaceOffset,
                spider.Position
            );

            if (!snap.success)
            {
                continue;
            }

            if (!IsCandidateJumpValid(spider, snap.point))
            {
                continue;
            }

            float towardGoal = Vector2.Dot(
                (snap.point - spider.Position).normalized,
                (goal - spider.Position).normalized
            );
            float score = Vector2.Distance(spider.Position, snap.point) + towardGoal * 2f;

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
        WolfSpiderSurfaceProbe.FillArcSamples(spider.Position, jumpTarget, spider.arcHeight, spider.DebugArcSamples);
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
            36,
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
        WolfSpiderSurfaceProbe.FillArcSamples(spider.Position, jumpTarget, spider.arcHeight, spider.DebugArcSamples);
        return true;
    }

    private bool IsCandidateJumpValid(WolfSpider2D spider, Vector2 candidate)
    {
        return WolfSpiderSurfaceProbe.IsValidJumpTarget(
            spider.Position,
            candidate,
            spider.minJumpDist,
            spider.maxJumpDist,
            spider.arcHeight
        );
    }
}
