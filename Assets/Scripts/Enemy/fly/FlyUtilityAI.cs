using UnityEngine;

public enum FlyIntentType
{
    RandomWalk
}

public struct FlyIntent
{
    public FlyIntentType type;
    public Vector2 target;
}

public class FlyUtilityAI
{
    private Fly2D owner;

    private float timer = 0f;
    private float interval = 2f;

    private Vector2 lastIssuedTarget;

    public FlyUtilityAI(Fly2D owner)
    {
        this.owner = owner;
    }

    public FlyIntent Evaluate()
    {
        timer -= Time.fixedDeltaTime;

        // 只在时间到或到达时生成新目标
        if (timer <= 0f || owner.Arrived)
        {
            timer = interval;

            Vector2 newTarget = PickRandomTarget();

            lastIssuedTarget = newTarget;

            return new FlyIntent
            {
                type = FlyIntentType.RandomWalk,
                target = newTarget
            };
        }

        //  不再返回 CurrentTarget（这是bug根源）
        return new FlyIntent
        {
            type = FlyIntentType.RandomWalk,
            target = lastIssuedTarget
        };
    }

    Vector2 PickRandomTarget()
    {
        var mgr = TileMapGuideManager.Instance;

        for (int i = 0; i < 10; i++)
        {
            Vector2 offset = Random.insideUnitCircle * Random.Range(1f, 10f);
            Vector2 candidate = owner.Position + offset;

            var path = mgr.FindPath(owner.Position, candidate);

            if (path != null && path.Count > 1)
                return candidate;
        }

        return owner.Position + Random.insideUnitCircle * 2f;
    }
}