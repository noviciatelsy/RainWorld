using System.Collections.Generic;
using UnityEngine;

public class FlyMotor : IMonsterMotor
{
    private Fly2D owner;

    private List<Vector2> path;
    private int index;
    private float pathRetryCooldown;

    private const float PathRetryInterval = 0.25f;

    public FlyMotor(Fly2D owner)
    {
        this.owner = owner;
    }

    public void Execute(MonsterBase owner, IIntent intent)
    {
        if (owner is Fly2D fly && fly.CurrentState != FlyState.Normal)
        {
            return;
        }

        if (intent is not FlyMoveIntent move)
        {
            return;
        }

        Vector2 moveTarget = move.target;

        if (pathRetryCooldown > 0f)
        {
            pathRetryCooldown -= Time.fixedDeltaTime;
        }

        // 只在没有路径、到达或 AI 明确换目标时重建
        if (path == null || owner.Arrived || owner.TargetChanged(moveTarget))
        {
            if (path == null && pathRetryCooldown > 0f)
            {
                return;
            }

            TileMapGuideManager mgr = TileMapGuideManager.Instance;

            if (mgr == null)
            {
                return;
            }

            path = mgr.FindPath(owner.Position, moveTarget);

            if (path == null || path.Count == 0)
            {
                pathRetryCooldown = PathRetryInterval;
                return;
            }

            pathRetryCooldown = 0f;
            index = 0;

            owner.CurrentTarget = moveTarget;
            owner.Arrived = false;

            owner.DebugPath = path;
            owner.DebugTarget = moveTarget;
        }

        Move();
    }

    void Move()
    {
        if (path == null || path.Count == 0)
            return;

        Vector2 target = path[index];

        owner.Transform.position = Vector2.MoveTowards(
            owner.Transform.position,
            target,
            owner.moveSpeed * Time.fixedDeltaTime
        );

        if (Vector2.Distance(owner.Position, target) < 0.05f)
        {
            index++;

            if (index >= path.Count)
            {
                owner.Arrived = true;

                path = null;
                index = 0;
            }
        }
    }
}