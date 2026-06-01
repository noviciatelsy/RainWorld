using System.Collections.Generic;
using UnityEngine;

public class BatMotor : IMonsterMotor
{
    private enum BatAttackPhase
    {
        None,
        Lunge,
        Strike,
        Retreat
    }

    private readonly Bat2D owner;

    private List<Vector2> path;
    private int pathIndex;
    private float cooldownTimer;

    private BatAttackPhase attackPhase = BatAttackPhase.None;
    private Vector2 attackAnchor;
    private Vector2 lungeTarget;
    private Transform attackFocus;
    private bool strikePerformed;
    private float strikeHoldTimer;

    public BatMotor(Bat2D owner)
    {
        this.owner = owner;
    }

    public void Execute(MonsterBase ownerBase, IIntent intent)
    {
        Bat2D bat = ownerBase as Bat2D;

        if (bat == null || intent is not BatIntent batIntent)
        {
            return;
        }

        UpdateCooldown(bat);
        bat.CurrentBehavior = batIntent.behaviorState;

        if (batIntent.behaviorState == BatBehavior.Attack || attackPhase != BatAttackPhase.None)
        {
            ExecuteAttack(bat, batIntent);
            return;
        }

        if (bat.IsCoolingDown)
        {
            return;
        }

        ExecuteFlight(bat, batIntent);
    }

    private void ExecuteAttack(Bat2D bat, BatIntent intent)
    {
        if (attackPhase == BatAttackPhase.None)
        {
            BeginAttackSequence(bat, intent);
        }

        switch (attackPhase)
        {
            case BatAttackPhase.Lunge:
                TickLunge(bat);
                break;

            case BatAttackPhase.Strike:
                TickStrike(bat, intent);
                break;

            case BatAttackPhase.Retreat:
                TickRetreat(bat);
                break;
        }
    }

    private void BeginAttackSequence(Bat2D bat, BatIntent intent)
    {
        attackPhase = BatAttackPhase.Lunge;
        strikePerformed = false;
        attackFocus = intent.focusTarget;

        bat.IsInAttackSequence = true;
        bat.IsAttacking = true;
        bat.Arrived = true;
        bat.CurrentTarget = bat.Position;

        attackAnchor = bat.Position;
        lungeTarget = ComputeLungeTarget(bat, intent);

        bat.UpdateFacingToward(lungeTarget);
    }

    private Vector2 ComputeLungeTarget(Bat2D bat, BatIntent intent)
    {
        Vector2 direction = bat.LastMoveDirection;

        if (intent.focusTarget != null)
        {
            direction = (Vector2)intent.focusTarget.position - attackAnchor;

            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                float preyDist = Vector2.Distance(attackAnchor, intent.focusTarget.position);
                float lungeDist = Mathf.Min(bat.attackLungeDistance, preyDist - 0.15f);
                lungeDist = Mathf.Max(0.2f, lungeDist);
                return attackAnchor + direction * lungeDist;
            }
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        return attackAnchor + direction.normalized * bat.attackLungeDistance;
    }

    private void TickLunge(Bat2D bat)
    {
        float threshold = bat.attackPhaseArriveThreshold;

        bat.Transform.position = Vector2.MoveTowards(
            bat.Position,
            lungeTarget,
            bat.attackLungeSpeed * Time.fixedDeltaTime
        );

        bat.SetLastMoveDirection(lungeTarget - bat.Position);
        bat.UpdateFacingToward(lungeTarget);

        if (Vector2.Distance(bat.Position, lungeTarget) > threshold)
        {
            return;
        }

        bat.Transform.position = lungeTarget;
        attackPhase = BatAttackPhase.Strike;
    }

    private void TickStrike(Bat2D bat, BatIntent intent)
    {
        if (!strikePerformed)
        {
            Transform focus = attackFocus != null ? attackFocus : intent.focusTarget;
            bat.PerformAttack(focus);
            strikePerformed = true;
            strikeHoldTimer = bat.attackStrikeHoldDuration;
            return;
        }

        strikeHoldTimer -= Time.fixedDeltaTime;

        if (strikeHoldTimer > 0f)
        {
            return;
        }

        attackPhase = BatAttackPhase.Retreat;
        bat.UpdateFacingToward(attackAnchor);
    }

    private void TickRetreat(Bat2D bat)
    {
        float threshold = bat.attackPhaseArriveThreshold;

        bat.Transform.position = Vector2.MoveTowards(
            bat.Position,
            attackAnchor,
            bat.attackRetreatSpeed * Time.fixedDeltaTime
        );

        bat.SetLastMoveDirection(attackAnchor - bat.Position);
        bat.UpdateFacingToward(attackAnchor);

        if (Vector2.Distance(bat.Position, attackAnchor) > threshold)
        {
            return;
        }

        bat.Transform.position = attackAnchor;
        EndAttackSequence(bat);
    }

    private void EndAttackSequence(Bat2D bat)
    {
        attackPhase = BatAttackPhase.None;
        strikePerformed = false;
        attackFocus = null;

        bat.IsAttacking = false;
        bat.IsInAttackSequence = false;
        bat.IsCoolingDown = true;
        bat.Arrived = true;
        cooldownTimer = bat.attackStiffDuration;
        bat.NotifyAttackPerformed();
    }

    private void ExecuteFlight(Bat2D bat, BatIntent intent)
    {
        if (path == null || bat.Arrived || bat.TargetChanged(intent.moveTarget))
        {
            RebuildPath(bat, intent.moveTarget);
        }

        if (path == null || path.Count == 0)
        {
            bat.Arrived = true;
            return;
        }

        MoveAlongPath(bat);
    }

    private void RebuildPath(Bat2D bat, Vector2 target)
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        if (mgr == null)
        {
            path = null;
            pathIndex = 0;
            return;
        }

        path = mgr.FindPath(bat.Position, target);

        if (bat.drawDebugGizmos)
        {
            bat.DebugPath = path;
            bat.DebugTarget = target;
        }

        if (path == null || path.Count == 0)
        {
            path = null;
            pathIndex = 0;
            bat.Arrived = true;
            return;
        }

        pathIndex = 0;
        bat.CurrentTarget = target;
        bat.Arrived = false;
    }

    private void MoveAlongPath(Bat2D bat)
    {
        Vector2 waypoint = path[pathIndex];
        Vector2 previousPosition = bat.Position;

        bat.Transform.position = Vector2.MoveTowards(
            bat.Position,
            waypoint,
            bat.moveSpeed * Time.fixedDeltaTime
        );

        Vector2 moveDir = bat.Position - previousPosition;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            bat.SetLastMoveDirection(moveDir);
            bat.UpdateFacingToward(waypoint);
        }

        float threshold = bat.arriveThreshold;

        if (Vector2.Distance(bat.Position, waypoint) >= threshold)
        {
            return;
        }

        pathIndex++;

        if (pathIndex >= path.Count)
        {
            bat.Arrived = true;
            path = null;
            pathIndex = 0;
        }
    }

    private void UpdateCooldown(Bat2D bat)
    {
        if (!bat.IsCoolingDown)
        {
            return;
        }

        cooldownTimer -= Time.fixedDeltaTime;

        if (cooldownTimer <= 0f)
        {
            bat.IsCoolingDown = false;
        }
    }
}
