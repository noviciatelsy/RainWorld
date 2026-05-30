using UnityEngine;

public class WolfSpiderMotor : IMonsterMotor
{
    private readonly WolfSpider2D owner;

    private Vector2 jumpStart;
    private Vector2 jumpTarget;
    private Vector2 jumpArcNormal;
    private float jumpProgress;
    private float jumpDuration;
    private float cooldownTimer;

    private const float JumpArriveThreshold = 0.05f;

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

        UpdateCooldown(spider);
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
            if (!WolfSpiderSurfaceProbe.IsValidJumpTarget(
                spider.Position,
                spiderIntent.jumpTarget,
                spider.minJumpDist,
                spider.maxJumpDist,
                spider.arcHeight,
                spider.CurrentSurfaceNormal))
            {
                spider.NotifyJumpTargetRejected();
                spider.Arrived = true;
                return;
            }

            BeginJump(spider, spiderIntent);
            TickJump(spider);
        }
    }

    private void ExecuteAttack(WolfSpider2D spider, WolfSpiderIntent intent)
    {
        spider.Arrived = true;
        spider.CurrentTarget = spider.Position;
        spider.DebugArcSamples.Clear();
        spider.PerformAttack(intent.focusTarget);
        spider.IsCoolingDown = true;
        cooldownTimer = spider.attackStiffDuration;
        spider.NotifyAttackPerformed();
    }

    private bool ShouldBeginJump(WolfSpider2D spider, WolfSpiderIntent intent)
    {
        if (!spider.Arrived)
        {
            return false;
        }

        if (intent.behaviorState == WolfSpiderBehavior.Attack)
        {
            return false;
        }

        float distanceSqr = (spider.Position - intent.jumpTarget).sqrMagnitude;
        return distanceSqr > JumpArriveThreshold * JumpArriveThreshold;
    }

    private void BeginJump(WolfSpider2D spider, WolfSpiderIntent intent)
    {
        jumpStart = spider.Position;
        jumpTarget = intent.jumpTarget;
        jumpArcNormal = spider.CurrentSurfaceNormal.sqrMagnitude > 0.0001f
            ? spider.CurrentSurfaceNormal.normalized
            : Vector2.up;
        jumpProgress = 0f;

        float distance = Vector2.Distance(jumpStart, jumpTarget);
        jumpDuration = Mathf.Max(0.08f, distance / Mathf.Max(0.01f, spider.moveSpeed));

        spider.IsJumping = true;
        spider.Arrived = false;
        spider.CurrentTarget = jumpTarget;
        spider.DebugTarget = jumpTarget;

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

        if ((jumpTarget - jumpStart).sqrMagnitude > 0.0001f)
        {
            spider.FaceToward(jumpTarget);
        }
    }

    private void TickJump(WolfSpider2D spider)
    {
        if (jumpDuration <= 0f)
        {
            Land(spider);
            return;
        }

        jumpProgress += Time.fixedDeltaTime / jumpDuration;
        float t = Mathf.Clamp01(jumpProgress);

        Vector2 flatPosition = Vector2.Lerp(jumpStart, jumpTarget, t);
        float heightOffset = Mathf.Sin(t * Mathf.PI) * spider.arcHeight;
        Vector2 nextPosition = flatPosition + jumpArcNormal * heightOffset;

        if (!IsAirPositionClear(spider, nextPosition))
        {
            Land(spider, flatPosition);
            return;
        }

        spider.Transform.position = nextPosition;

        if (t >= 1f - 0.0001f || (spider.Position - jumpTarget).sqrMagnitude <= JumpArriveThreshold * JumpArriveThreshold)
        {
            Land(spider);
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

    private void Land(WolfSpider2D spider)
    {
        Land(spider, jumpTarget);
    }

    private void Land(WolfSpider2D spider, Vector2 landHint)
    {
        SurfaceSnapResult snap = WolfSpiderSurfaceProbe.SnapToFloorSurface(
            landHint,
            spider.surfaceSnapMaxDistance,
            spider.visualSurfaceOffset
        );

        if (!snap.success)
        {
            snap = WolfSpiderSurfaceProbe.SnapToSurface(
                landHint,
                spider.surfaceSnapMaxDistance,
                spider.visualSurfaceOffset,
                jumpStart
            );
        }

        if (snap.success)
        {
            spider.Transform.position = snap.point;
            spider.ApplySurfaceOrientation(snap.normal);
        }
        else
        {
            spider.Transform.position = landHint;
        }

        spider.IsJumping = false;
        spider.Arrived = true;
        jumpProgress = 0f;
        spider.DebugArcSamples.Clear();
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
