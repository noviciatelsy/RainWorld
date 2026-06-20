using UnityEngine;

public class WolfSpiderMotor : IMonsterMotor
{
    private readonly WolfSpider2D owner;

    private Vector2 jumpStart;
    private Vector2 jumpTarget;
    private Vector2 jumpArcNormal;
    private float jumpProgress;
    private float jumpDuration;
    private float jumpElapsed;
    private float cooldownTimer;

    private const float JumpArriveThreshold = 0.05f;
    private const float MinJumpSlack = 0.02f;
    private const float MaxJumpTime = 2.5f;

    private float attackAnimTimer;

    public WolfSpiderMotor(WolfSpider2D owner)
    {
        this.owner = owner;
    }

    public void Execute(MonsterBase ownerBase, IIntent intent)
    {
        WolfSpider2D spider = ownerBase as WolfSpider2D;

        if (spider == null || intent is not WolfSpiderIntent spiderIntent)
        {
            return;
        }

        if (!spider.Arrived && !spider.IsJumping)
        {
            spider.Arrived = true;
        }

        UpdateCooldown(spider);
        UpdateAttackAnim(spider);
        spider.TickPostLandJumpCooldown(Time.fixedDeltaTime);
        spider.CurrentBehavior = spiderIntent.behaviorState;

        if (spider.IsCoolingDown)
        {
            return;
        }

        if (spider.IsJumping)
        {
            TickJump(spider);
            return;
        }

        if (spiderIntent.behaviorState == WolfSpiderBehavior.Attack)
        {
            ExecuteAttack(spider, spiderIntent);
            return;
        }

        if (ShouldBeginJump(spider, spiderIntent))
        {
            Vector2 arcNormal = WolfSpiderSurfaceProbe.ResolveJumpArcNormal(
                spider.Position,
                spiderIntent.jumpTarget,
                spider.CurrentSurfaceNormal,
                spider.surfaceSnapMaxDistance);

            BeginJump(spider, spiderIntent, arcNormal);
            TickJump(spider);
        }
    }

    private void ExecuteAttack(WolfSpider2D spider, WolfSpiderIntent intent)
    {
        spider.Arrived = true;
        spider.CurrentTarget = spider.Position;
        spider.DebugArcSamples.Clear();
        spider.PerformAttack(intent.focusTarget);
        spider.SetPerformingAttackAnim(true);
        attackAnimTimer = Mathf.Max(0.05f, spider.attackAnimDuration);
        spider.NotifyAttackStarted();
        spider.IsCoolingDown = true;
        cooldownTimer = Mathf.Max(attackAnimTimer, spider.attackInterval);
        spider.NotifyAttackPerformed();
    }

    private bool ShouldBeginJump(WolfSpider2D spider, WolfSpiderIntent intent)
    {
        if (spider.IsPostLandJumpCooldown)
        {
            return false;
        }

        if (!spider.Arrived)
        {
            return false;
        }

        if (intent.behaviorState == WolfSpiderBehavior.Attack)
        {
            return false;
        }

        float distanceSqr = (spider.Position - intent.jumpTarget).sqrMagnitude;
        float snapTolSqr = spider.surfaceSnapMaxDistance * spider.surfaceSnapMaxDistance;

        if (distanceSqr <= snapTolSqr || distanceSqr <= JumpArriveThreshold * JumpArriveThreshold)
        {
            return false;
        }

        float minJump = spider.minJumpDist + MinJumpSlack;

        if (distanceSqr < minJump * minJump)
        {
            spider.Arrived = true;
            return false;
        }

        return true;
    }

    private void BeginJump(WolfSpider2D spider, WolfSpiderIntent intent, Vector2 arcNormal)
    {
        jumpStart = spider.Position;
        jumpTarget = intent.jumpTarget;
        jumpArcNormal = arcNormal;
        jumpProgress = 0f;
        jumpElapsed = 0f;

        float distance = Vector2.Distance(jumpStart, jumpTarget);
        jumpDuration = Mathf.Max(0.08f, distance / Mathf.Max(0.01f, spider.moveSpeed));

        spider.IsJumping = true;
        spider.Arrived = false;
        spider.CurrentTarget = jumpTarget;
        spider.DebugTarget = jumpTarget;
        spider.NotifyJumpStarted();

        if (spider.drawDebugGizmos)
        {
            WolfSpiderSurfaceProbe.FillArcSamples(
                jumpStart,
                jumpTarget,
                spider.arcHeight,
                jumpArcNormal,
                spider.DebugArcSamples
            );
        }

        Vector2? progressGoal = intent.focusTarget != null
            ? (Vector2?)intent.focusTarget.position
            : null;

        spider.PrepareJumpVisual(jumpTarget, jumpStart, progressGoal);
    }

    private void TickJump(WolfSpider2D spider)
    {
        jumpElapsed += Time.fixedDeltaTime;

        if (jumpDuration <= 0f || jumpElapsed >= MaxJumpTime)
        {
            FinishJump(spider, jumpTarget, forced: true);
            return;
        }

        jumpProgress += Time.fixedDeltaTime / jumpDuration;
        float t = Mathf.Clamp01(jumpProgress);

        Vector2 flatPosition = Vector2.Lerp(jumpStart, jumpTarget, t);
        float heightOffset = Mathf.Sin(t * Mathf.PI) * spider.arcHeight;
        Vector2 nextPosition = flatPosition + jumpArcNormal * heightOffset;

        if (!IsAirPositionClear(spider, nextPosition))
        {
            FinishJump(spider, flatPosition, forced: false);
            return;
        }

        spider.Transform.position = nextPosition;
        spider.SetJumpVisualProgress(t);

        if (t >= 1f - 0.0001f)
        {
            FinishJump(spider, jumpTarget, forced: false);
            return;
        }

        float snapTolSqr = spider.surfaceSnapMaxDistance * spider.surfaceSnapMaxDistance;

        if ((spider.Position - jumpTarget).sqrMagnitude <= snapTolSqr)
        {
            FinishJump(spider, jumpTarget, forced: false);
        }
    }

    private bool IsAirPositionClear(WolfSpider2D spider, Vector2 position)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            return true;
        }

        return !mgr.IsSolid(mgr.WorldToCell(position));
    }

    private void FinishJump(WolfSpider2D spider, Vector2 landHint, bool forced)
    {
        Vector2 notifyLandPoint = landHint;
        Vector2 notifyJumpOrigin = jumpStart;
        bool landed = spider.TryCompleteJumpLanding(landHint, jumpStart);

        if (landed)
        {
            notifyLandPoint = spider.Position;
            spider.NotifySuccessfulLanding(notifyLandPoint, notifyJumpOrigin);
        }
        else
        {
            spider.Transform.position = landHint;
        }

        spider.IsJumping = false;
        spider.Arrived = true;
        spider.NotifyJumpEnded();
        spider.ArmPostLandJumpCooldown();

        EnemyWolfSpiderAudioEmitter audioEmitter = spider.GetComponent<EnemyWolfSpiderAudioEmitter>();
        if (audioEmitter != null)
        {
            audioEmitter.PlayLand();
        }

        jumpProgress = 0f;
        jumpElapsed = 0f;
        spider.DebugArcSamples.Clear();

        if (!landed)
        {
            spider.NotifyJumpTargetRejected();
        }
    }

    private void UpdateAttackAnim(WolfSpider2D spider)
    {
        if (!spider.IsPerformingAttackAnim)
        {
            return;
        }

        attackAnimTimer -= Time.fixedDeltaTime;

        if (attackAnimTimer > 0f)
        {
            return;
        }

        spider.SetPerformingAttackAnim(false);
        spider.NotifyAttackAnimEnded();
    }

    private void UpdateCooldown(WolfSpider2D spider)
    {
        if (!spider.IsCoolingDown)
        {
            return;
        }

        cooldownTimer -= Time.fixedDeltaTime;

        if (cooldownTimer <= 0f)
        {
            spider.IsCoolingDown = false;
            cooldownTimer = 0f;
        }
    }
}
