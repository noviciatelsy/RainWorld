using UnityEngine;

public class BatUtilityAI : IMonsterAI
{
    private readonly Bat2D owner;

    private BatIntent lastIssuedIntent;
    private bool hasIssuedIntent;

    private Transform currentPrey;
    private Vector2 lastKnownPreyPosition;
    private bool hasLastKnownPreyPosition;
    private float aggroMemoryTimer;

    private float idleTimer;
    private float huntPathPickTimer;
    private float perceptionTimer;
    private Vector2 lastPathGoal;
    private float postAttackRecoveryTimer;

    private Transform lastDebugPrey;
    private BatBehavior lastDebugBehavior = BatBehavior.Idle;

    private Vector2 currentHuntPoint;
    private bool hasHuntPoint;

    private EnemyAttractionSource currentPreySource = EnemyAttractionSource.None;

    private Vector2 toyCarChasePoint;
    private bool hasToyCarChasePoint;
    private bool usesSectorHuntPrey;

    private const float MoveTargetLockThresholdSqr = 0.12f * 0.12f;
    private const float PreyGoalChangeThresholdSqr = 1.5f * 1.5f;

    public BatUtilityAI(Bat2D owner)
    {
        this.owner = owner;
        idleTimer = owner.idleMoveInterval;
        huntPathPickTimer = 0f;
        lastIssuedIntent = CreateIdleIntent(owner, owner.Position);
    }

    public IIntent Evaluate(MonsterBase ownerBase)
    {
        Bat2D bat = ownerBase as Bat2D;

        if (bat == null)
        {
            return lastIssuedIntent;
        }

        UpdatePerception(bat);

        if (bat.drawDebugGizmos)
        {
            UpdateDebugState(bat);
        }

        if (TryBuildRepellentFleeIntent(bat, out BatIntent repellentFleeIntent))
        {
            lastIssuedIntent = repellentFleeIntent;
            return lastIssuedIntent;
        }

        if (postAttackRecoveryTimer > 0f)
        {
            postAttackRecoveryTimer -= Time.fixedDeltaTime;
        }

        if (bat.IsInAttackSequence)
        {
            if (RepellentAvoidance.IsInsideAnyZone(bat.Position))
            {
                lastIssuedIntent = CreateFleeIntent(
                    bat,
                    RepellentAvoidance.GetFleePointAwayFromAll(bat.Position));
                hasIssuedIntent = true;
                bat.CurrentBehavior = BatBehavior.Idle;
                return lastIssuedIntent;
            }

            if (!hasIssuedIntent || lastIssuedIntent.behaviorState != BatBehavior.Attack)
            {
                lastIssuedIntent = BuildAttackIntent(bat);
                hasIssuedIntent = true;
            }

            bat.CurrentBehavior = BatBehavior.Attack;
            return lastIssuedIntent;
        }

        if (bat.IsCoolingDown || postAttackRecoveryTimer > 0f)
        {
            lastIssuedIntent = CreateIdleIntent(bat, bat.Position);
            bat.CurrentBehavior = BatBehavior.Idle;
            hasIssuedIntent = true;
            return lastIssuedIntent;
        }

        BatBehavior behavior = DecideBehavior(bat);
        bat.CurrentBehavior = behavior;

        switch (behavior)
        {
            case BatBehavior.Attack:
                lastIssuedIntent = BuildAttackIntent(bat);
                break;

            case BatBehavior.Hunt:
                lastIssuedIntent = BuildHuntIntent(bat);
                break;

            default:
                lastIssuedIntent = BuildIdleIntent(bat);
                break;
        }

        hasIssuedIntent = true;
        LogBehaviorChange(bat, behavior);
        return lastIssuedIntent;
    }

    public void NotifyAttackPerformed()
    {
        postAttackRecoveryTimer = owner.attackStiffDuration;
        idleTimer = owner.idleMoveInterval;
        huntPathPickTimer = 0f;
        usesSectorHuntPrey = false;
        hasHuntPoint = false;
        owner.HuntPathUnreachable = false;
    }

    public void NotifyRepelledByMosquitoCoil(Vector2 coilPosition)
    {
        hasHuntPoint = false;
        huntPathPickTimer = 0f;
        usesSectorHuntPrey = false;
        idleTimer = 0f;
    }

    public void NotifyRepelledByTorch(Vector2 torchPosition)
    {
        hasHuntPoint = false;
        huntPathPickTimer = 0f;
        usesSectorHuntPrey = false;
        idleTimer = 0f;
        hasIssuedIntent = false;
    }

    public void ForcePerceptionRefresh()
    {
        perceptionTimer = 0f;
        hasHuntPoint = false;
        huntPathPickTimer = 0f;
        usesSectorHuntPrey = false;
        idleTimer = 0f;
    }

    private bool TryBuildRepellentFleeIntent(Bat2D bat, out BatIntent fleeIntent)
    {
        fleeIntent = default;

        if (!RepellentAvoidance.HasActiveZones())
        {
            return false;
        }

        if (!RepellentAvoidance.IsInsideAnyZone(bat.Position))
        {
            return false;
        }

        Vector2 fleeTarget = RepellentAvoidance.GetFleePointAwayFromAll(bat.Position);
        fleeIntent = CreateFleeIntent(bat, fleeTarget);
        bat.CurrentBehavior = BatBehavior.Idle;
        hasIssuedIntent = true;
        return true;
    }

    private BatIntent CreateFleeIntent(Bat2D bat, Vector2 fleeTarget)
    {
        bat.DebugPickReason = "RepellentFlee";
        bat.DebugTarget = fleeTarget;

        return new BatIntent
        {
            behaviorState = BatBehavior.Idle,
            moveTarget = fleeTarget,
            focusTarget = null
        };
    }

    private void UpdatePerception(Bat2D bat)
    {
        perceptionTimer -= Time.fixedDeltaTime;
        float detectRadiusSqr = bat.detectRadius * bat.detectRadius;

        if (currentPrey != null)
        {
            if (!PlayerInvisibilityPerception.IsPlayerDetectable(currentPrey)
                && currentPrey.GetComponentInParent<Player>() != null)
            {
                currentPrey = null;
                currentPreySource = EnemyAttractionSource.None;
                hasHuntPoint = false;
                aggroMemoryTimer = 0f;
            }
            else if (currentPrey.gameObject.activeInHierarchy)
            {
                lastKnownPreyPosition = currentPrey.position;

                if (((Vector2)currentPrey.position - bat.Position).sqrMagnitude <= detectRadiusSqr)
                {
                    hasLastKnownPreyPosition = true;
                    aggroMemoryTimer = bat.aggroMemoryDuration;
                }
            }
            else
            {
                currentPrey = null;
                currentPreySource = EnemyAttractionSource.None;
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
                currentPreySource = EnemyAttractionSource.None;
            }

            return;
        }

        perceptionTimer = bat.perceptionInterval;

        EnemyAttractionCapabilities capabilities =
            EnemyAttractionCapabilities.MeatBait
            | EnemyAttractionCapabilities.ToyCar
            | EnemyAttractionCapabilities.Fly
            | EnemyAttractionCapabilities.Snail
            | EnemyAttractionCapabilities.Player;

        if (EnemyAttractionUtility.TryResolveTarget(
                bat.Position,
                bat.detectRadius,
                capabilities,
                null,
                out EnemyAttractionTarget attraction)
            && attraction.Transform != null)
        {
            ApplyDetectedPrey(bat, attraction.Transform, attraction.Source);
            return;
        }

        int hitCount = bat.OverlapPreyNonAlloc(out Collider2D[] hits);
        Transform bestOther = null;
        float bestOtherDistSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            float distSqr = ((Vector2)hit.transform.position - bat.Position).sqrMagnitude;

            if (distSqr > detectRadiusSqr)
            {
                continue;
            }

            if (bat.IsOtherPreyCollider(hit))
            {
                if (distSqr < bestOtherDistSqr)
                {
                    bestOtherDistSqr = distSqr;
                    bestOther = hit.transform;
                }
            }
        }

        if (bestOther != null)
        {
            ApplyDetectedPrey(bat, bestOther, EnemyAttractionSource.None);
            return;
        }

        if (ShouldKeepToyCarChaseWithoutPerception(bat))
        {
            aggroMemoryTimer = bat.aggroMemoryDuration;
            return;
        }

        if (aggroMemoryTimer > 0f)
        {
            aggroMemoryTimer -= bat.perceptionInterval;
            return;
        }

        currentPrey = null;
        currentPreySource = EnemyAttractionSource.None;
        usesSectorHuntPrey = false;
        hasHuntPoint = false;
    }

    private void ApplyDetectedPrey(Bat2D bat, Transform detected, EnemyAttractionSource source)
    {
        if (detected != null
            && detected.GetComponentInParent<Player>() != null
            && !PlayerInvisibilityPerception.IsPlayerDetectable(detected))
        {
            return;
        }

        bool preyChanged = currentPrey != detected || currentPreySource != source;

        if (preyChanged)
        {
            string preyType = GetPreyTypeLabel(source, detected);
            bat.LogDebug($"发现目标: {detected.name} ({preyType})");
            idleTimer = 0f;
            huntPathPickTimer = 0f;
            usesSectorHuntPrey = false;
            hasHuntPoint = false;

            if (source != EnemyAttractionSource.ToyCar)
            {
                hasToyCarChasePoint = false;
            }

            if (source == EnemyAttractionSource.Player
                || detected != null && detected.GetComponentInParent<Player>() != null)
            {
                EnemyBatAudioEmitter audioEmitter = bat.GetComponent<EnemyBatAudioEmitter>();
                if (audioEmitter != null)
                {
                    audioEmitter.PlaySpotPlayer();
                }

                if (bat is BatKing2D)
                {
                    EnemyIntelligenceUnlockUtility.TryUnlockByName(EnemyIntelligenceNames.BatKingTerritory);
                }
            }
        }

        currentPrey = detected;
        currentPreySource = source;
        lastKnownPreyPosition = detected.position;
        hasLastKnownPreyPosition = true;
        aggroMemoryTimer = bat.aggroMemoryDuration;
        usesSectorHuntPrey = !IsHuntOnlyPrey() && IsSectorHuntPrey(detected);
    }

    private bool IsSectorHuntPrey(Transform prey)
    {
        if (prey == null)
        {
            return false;
        }

        if (prey.GetComponentInParent<Player>() != null)
        {
            return true;
        }

        if (prey.GetComponentInParent<Fly2D>() != null)
        {
            return true;
        }

        if (prey.GetComponentInParent<Snail2D>() != null)
        {
            return true;
        }

        Collider2D col = prey.GetComponent<Collider2D>();
        return col != null && owner.IsFlyCollider(col);
    }

    private static string GetPreyTypeLabel(EnemyAttractionSource source, Transform detected)
    {
        switch (source)
        {
            case EnemyAttractionSource.MeatBait:
                return "MeatBait";
            case EnemyAttractionSource.ToyCar:
                return "ToyCar";
            case EnemyAttractionSource.Fly:
                return "Fly";
            case EnemyAttractionSource.Snail:
                return "Snail";
            case EnemyAttractionSource.Player:
                return "Player";
            default:
                if (detected != null && detected.GetComponentInParent<Fly2D>() != null)
                {
                    return "Fly";
                }

                if (detected != null && detected.GetComponentInParent<Snail2D>() != null)
                {
                    return "Snail";
                }

                if (detected != null && detected.GetComponentInParent<Player>() != null)
                {
                    return "Player";
                }

                return "Other";
        }
    }

    private void UpdateDebugState(Bat2D bat)
    {
        bat.DebugHasPrey = currentPrey != null || (aggroMemoryTimer > 0f && hasLastKnownPreyPosition);
        bat.DebugAggroTimer = aggroMemoryTimer;
        bat.DebugPreyIsFly = currentPrey != null && currentPrey.GetComponentInParent<Fly2D>() != null;

        if (currentPrey != null)
        {
            bat.DebugPreyName = currentPrey.name;
            bat.DebugPreyPosition = currentPrey.position;
            lastDebugPrey = currentPrey;
            return;
        }

        if (aggroMemoryTimer > 0f && hasLastKnownPreyPosition)
        {
            bat.DebugPreyName = lastDebugPrey != null ? $"{lastDebugPrey.name}(记忆)" : "LastKnown";
            bat.DebugPreyPosition = lastKnownPreyPosition;
            return;
        }

        bat.DebugPreyName = "None";
        bat.DebugPreyPosition = bat.Position;
    }

    private void LogBehaviorChange(Bat2D bat, BatBehavior behavior)
    {
        if (!bat.enableDebugLog || behavior == lastDebugBehavior)
        {
            return;
        }

        lastDebugBehavior = behavior;

        switch (behavior)
        {
            case BatBehavior.Hunt:
                bat.LogDebug(
                    $"进入 Hunt，目标 {bat.DebugPreyName}，航点 {lastIssuedIntent.moveTarget}，原因: {bat.DebugPickReason}"
                );
                break;
            case BatBehavior.Attack:
                bat.LogDebug($"进入 Attack，撕咬 {bat.DebugPreyName}");
                break;
            case BatBehavior.Idle:
                bat.LogDebug("进入 Idle");
                break;
        }
    }

    private BatBehavior DecideBehavior(Bat2D bat)
    {
        if (RepellentAvoidance.IsInsideAnyZone(bat.Position))
        {
            return BatBehavior.Idle;
        }

        if (postAttackRecoveryTimer > 0f)
        {
            return BatBehavior.Idle;
        }

        if (currentPrey != null)
        {
            Vector2 preyPos = GetRawPreyPosition(bat);
            if (RepellentAvoidance.IsInsideAnyZone(preyPos))
            {
                return BatBehavior.Idle;
            }

            if (CanAttack(bat, preyPos))
            {
                return BatBehavior.Attack;
            }

            return BatBehavior.Hunt;
        }

        if (aggroMemoryTimer > 0f && hasLastKnownPreyPosition)
        {
            if (CanAttack(bat, lastKnownPreyPosition))
            {
                return BatBehavior.Attack;
            }

            return BatBehavior.Hunt;
        }

        return BatBehavior.Idle;
    }

    private bool CanAttack(Bat2D bat, Vector2 preyPosition)
    {
        if (IsHuntOnlyPrey())
        {
            return false;
        }

        if (RepellentAvoidance.IsInsideAnyZone(bat.Position)
            || RepellentAvoidance.IsInsideAnyZone(preyPosition))
        {
            return false;
        }

        if (ShouldUseSectorHunt())
        {
            return bat.CanAttackPosition(preyPosition);
        }

        float attackRangeSqr = bat.attackRange * bat.attackRange;
        return (preyPosition - bat.Position).sqrMagnitude <= attackRangeSqr;
    }

    private BatIntent BuildAttackIntent(Bat2D bat)
    {
        return new BatIntent
        {
            behaviorState = BatBehavior.Attack,
            moveTarget = bat.Position,
            focusTarget = currentPrey
        };
    }

    private BatIntent BuildHuntIntent(Bat2D bat)
    {
        if (bat.HuntPathUnreachable)
        {
            huntPathPickTimer = Mathf.Max(huntPathPickTimer, bat.idleMoveInterval);
        }

        if (hasIssuedIntent && lastIssuedIntent.behaviorState == BatBehavior.Hunt)
        {
            if (!bat.Arrived)
            {
                return lastIssuedIntent;
            }

            huntPathPickTimer -= Time.fixedDeltaTime;

            if (huntPathPickTimer > 0f)
            {
                if (bat.HuntPathUnreachable)
                {
                    bat.DebugPickReason = "HuntBlockedStay";
                    bat.DebugTarget = bat.Position;
                    return CreateHuntIntent(bat, bat.Position);
                }

                return lastIssuedIntent;
            }
        }

        bat.HuntPathUnreachable = false;

        Vector2 preyPosition = GetRawPreyPosition(bat);
        lastPathGoal = preyPosition;

        Vector2 moveTarget;

        if (currentPreySource == EnemyAttractionSource.ToyCar)
        {
            if (ShouldPickNewToyCarChasePoint(bat, preyPosition))
            {
                toyCarChasePoint = preyPosition;
                hasToyCarChasePoint = true;
            }

            moveTarget = hasToyCarChasePoint ? toyCarChasePoint : preyPosition;
            bat.DebugPickReason = "HuntToyCarCommitted";
        }
        else if (ShouldUseSectorHunt())
        {
            currentHuntPoint = bat.PickRandomHuntSectorPoint(preyPosition);
            hasHuntPoint = true;
            moveTarget = currentHuntPoint;
            bat.DebugPickReason = "HuntSector";
        }
        else
        {
            if (RepellentAvoidance.IsInsideAnyZone(preyPosition))
            {
                moveTarget = RepellentAvoidance.GetFleePointAwayFromAll(bat.Position);
                bat.DebugPickReason = "RepellentFleeFromPrey";
            }
            else
            {
                moveTarget = preyPosition;
                bat.DebugPickReason = "HuntPrey";
            }
        }

        bat.DebugTarget = moveTarget;

        if (!BatFlightPath.CanAttemptPath(bat, moveTarget))
        {
            bat.DebugPickReason = "TooFarOrBlocked";
            moveTarget = bat.Position;
            bat.HuntPathUnreachable = true;
            huntPathPickTimer = bat.idleMoveInterval;
        }
        else
        {
            huntPathPickTimer = bat.pathPickInterval;
        }

        return CreateHuntIntent(bat, moveTarget);
    }

    private bool ShouldPickNewToyCarChasePoint(Bat2D bat, Vector2 preyPosition)
    {
        if (!hasToyCarChasePoint)
        {
            return true;
        }

        if (bat.Arrived)
        {
            return true;
        }

        float detectRadiusSqr = bat.detectRadius * bat.detectRadius;

        if (((Vector2)preyPosition - bat.Position).sqrMagnitude <= detectRadiusSqr
            && HasPreyGoalChanged(preyPosition))
        {
            return true;
        }

        return false;
    }

    private bool ShouldKeepToyCarChaseWithoutPerception(Bat2D bat)
    {
        if (currentPreySource != EnemyAttractionSource.ToyCar)
        {
            return false;
        }

        if (!ToyCarRegistry.HasActiveCar())
        {
            hasToyCarChasePoint = false;
            return false;
        }

        if (currentPrey != null && currentPrey.gameObject.activeInHierarchy)
        {
            return true;
        }

        return hasToyCarChasePoint && !bat.Arrived;
    }

    private BatIntent BuildIdleIntent(Bat2D bat)
    {
        if (!bat.Arrived)
        {
            if (hasIssuedIntent && lastIssuedIntent.behaviorState == BatBehavior.Idle)
            {
                return lastIssuedIntent;
            }

            return CreateIdleIntent(bat, bat.Position);
        }

        bool isStayIntent = hasIssuedIntent
            && lastIssuedIntent.behaviorState == BatBehavior.Idle
            && (lastIssuedIntent.moveTarget - bat.Position).sqrMagnitude <= MoveTargetLockThresholdSqr;

        if (idleTimer <= 0f)
        {
            idleTimer = bat.idleMoveInterval;

            Vector2 moveTarget = PickRandomIdleGoal(bat);
            bat.DebugTarget = moveTarget;

            if (!BatFlightPath.CanAttemptPath(bat, moveTarget))
            {
                bat.DebugPickReason = "IdleTooFar";
                return CreateIdleIntent(bat, bat.Position);
            }

            bat.DebugPickReason = "IdleGoal";
            return CreateIdleIntent(bat, moveTarget);
        }

        if (!isStayIntent && ShouldKeepCurrentMoveTarget(bat, BatBehavior.Idle))
        {
            idleTimer -= Time.fixedDeltaTime;
            return lastIssuedIntent;
        }

        idleTimer -= Time.fixedDeltaTime;
        return lastIssuedIntent;
    }

    private bool HasPreyGoalChanged(Vector2 preyPosition)
    {
        return (preyPosition - lastPathGoal).sqrMagnitude > PreyGoalChangeThresholdSqr;
    }

    private bool ShouldKeepCurrentMoveTarget(Bat2D bat, BatBehavior expectedBehavior)
    {
        if (!hasIssuedIntent || lastIssuedIntent.behaviorState != expectedBehavior)
        {
            return false;
        }

        return (lastIssuedIntent.moveTarget - bat.Position).sqrMagnitude > MoveTargetLockThresholdSqr;
    }

    private BatIntent CreateHuntIntent(Bat2D bat, Vector2 moveTarget)
    {
        return new BatIntent
        {
            behaviorState = BatBehavior.Hunt,
            moveTarget = moveTarget,
            focusTarget = currentPrey
        };
    }

    private BatIntent CreateIdleIntent(Bat2D bat, Vector2 moveTarget)
    {
        return new BatIntent
        {
            behaviorState = BatBehavior.Idle,
            moveTarget = moveTarget,
            focusTarget = null
        };
    }

    private Vector2 GetRawPreyPosition(Bat2D bat)
    {
        if (currentPrey != null)
        {
            return currentPrey.position;
        }

        if (hasLastKnownPreyPosition)
        {
            return lastKnownPreyPosition;
        }

        return bat.Position;
    }

    private bool ShouldUseSectorHunt()
    {
        if (IsHuntOnlyPrey())
        {
            return false;
        }

        return usesSectorHuntPrey;
    }

    private bool IsHuntOnlyPrey()
    {
        return currentPreySource == EnemyAttractionSource.MeatBait
            || currentPreySource == EnemyAttractionSource.ToyCar;
    }

    /// <summary>
    /// 随机游荡目标（仅几何选点；A* 由 BatMotor 单次执行，同 Fly）。
    /// </summary>
    private Vector2 PickRandomIdleGoal(Bat2D bat)
    {
        Bounds bounds = bat.activityBounds;

        if (bounds.size.sqrMagnitude < 0.01f)
        {
            bounds = new Bounds(bat.Position, new Vector3(14f, 10f, 1f));
        }

        for (int i = 0; i < 8; i++)
        {
            Vector2 candidate;

            if (i < 4)
            {
                candidate = new Vector2(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y));
            }
            else
            {
                Vector2 offset = Random.insideUnitCircle
                    * Random.Range(bat.idleWanderRadiusMin, bat.idleWanderRadiusMax);
                candidate = bat.Position + offset;
            }

            if (RepellentAvoidance.IsInsideAnyZone(candidate))
            {
                continue;
            }

            if (bounds.Contains(candidate) && BatFlightPath.CanAttemptPath(bat, candidate))
            {
                return candidate;
            }
        }

        return bat.Position;
    }
}
