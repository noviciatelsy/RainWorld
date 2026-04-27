using System.Collections.Generic;
using UnityEngine;

public class FlyMotor : IMonsterMotor
{
    private Fly2D owner;

    private List<Vector2> path;
    private int index;

    public FlyMotor(Fly2D owner)
    {
        this.owner = owner;
    }

    public void Execute(MonsterBase owner, IIntent intent)
    {
        if (intent is not FlyMoveIntent move)
            return;
        Fly2D fly = owner as Fly2D;

        // 只在没有路径 或 到达时 才重建
        if (path == null || owner.Arrived)
        {
            path = TileMapGuideManager.Instance.FindPath(owner.Position, move.target);

            if (path == null || path.Count == 0)
                return;

            index = 0;

            owner.CurrentTarget = move.target;
            owner.Arrived = false;

            owner.DebugPath = path;
            owner.DebugTarget = move.target;
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