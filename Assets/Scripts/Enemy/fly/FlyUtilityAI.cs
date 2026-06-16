using UnityEngine;

public struct FlyMoveIntent : IIntent
{
    public Vector2 target;
}

public class FlyUtilityAI : IMonsterAI
{
    private readonly Fly2D owner;

    private float timer;
    private float interval = 2f;

    private Vector2 lastIssuedTarget;

    public FlyUtilityAI(Fly2D owner)
    {
        this.owner = owner;
    }

    public IIntent Evaluate(MonsterBase ownerBase)
    {
        if (ownerBase is Fly2D fly && fly.CurrentState != FlyState.Normal)
        {
            return new FlyMoveIntent { target = fly.Position };
        }

        if (MosquitoCoilAvoidance.IsInsideAnyActiveCoil(owner.Position))
        {
            EnsureFleeTargetOutsideCoil();
            return new FlyMoveIntent
            {
                target = lastIssuedTarget
            };
        }

        timer -= Time.fixedDeltaTime;

        if (owner.Arrived)
        {
            timer = interval;
            lastIssuedTarget = PickRandomTarget();

            return new FlyMoveIntent
            {
                target = lastIssuedTarget
            };
        }

        if (!MosquitoCoilAvoidance.IsInsideAnyActiveCoil(lastIssuedTarget))
        {
            return new FlyMoveIntent
            {
                target = lastIssuedTarget
            };
        }

        if (timer <= 0f)
        {
            timer = interval;
            lastIssuedTarget = PickRandomTarget();
        }

        return new FlyMoveIntent
        {
            target = lastIssuedTarget
        };
    }

    public void NotifyRepelledByMosquitoCoil(Vector2 coilPosition)
    {
        if (!MosquitoCoilAvoidance.IsInsideAnyActiveCoil(owner.Position))
        {
            return;
        }

        if (!MosquitoCoilAvoidance.IsInsideAnyActiveCoil(lastIssuedTarget))
        {
            return;
        }

        lastIssuedTarget = MosquitoCoilAvoidance.GetFleePointFromCoil(
            owner.Position,
            coilPosition,
            MosquitoCoilRegistry.GetRadiusAtCenter(coilPosition));
    }

    private void EnsureFleeTargetOutsideCoil()
    {
        if (!MosquitoCoilAvoidance.IsInsideAnyActiveCoil(lastIssuedTarget))
        {
            return;
        }

        lastIssuedTarget = MosquitoCoilAvoidance.GetFleePointAwayFromAllCoils(owner.Position);
    }

    private Vector2 PickRandomTarget()
    {
        TileMapGuideManager mgr = TileMapGuideManager.Instance;

        for (int i = 0; i < 30; i++)
        {
            Vector2 offset = Random.insideUnitCircle * Random.Range(1f, 10f);
            Vector2 candidate = owner.Position + offset;

            if (MosquitoCoilAvoidance.IsInsideAnyActiveCoil(candidate))
            {
                continue;
            }

            if (mgr == null)
            {
                return candidate;
            }

            var path = mgr.FindPath(owner.Position, candidate);

            if (path != null && path.Count > 1)
            {
                return candidate;
            }
        }

        Vector2 fallback = owner.Position + Random.insideUnitCircle * 2f;
        if (!MosquitoCoilAvoidance.IsInsideAnyActiveCoil(fallback))
        {
            return fallback;
        }

        return MosquitoCoilAvoidance.GetFleePointAwayFromAllCoils(owner.Position);
    }
}
