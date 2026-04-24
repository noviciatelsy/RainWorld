using UnityEngine;
public enum MoveIntentType
{
    SurfaceMove
}

public struct MoveIntent
{
    public MoveIntentType type;
    public bool clockwise;
}

public class SurfaceWalkerUtilityAI : IMonsterAI
{
    public object Evaluate(MonsterBase owner)
    {
        float cw = Score(owner, true);
        float ccw = Score(owner, false);

        return new MoveIntent
        {
            type = MoveIntentType.SurfaceMove,
            clockwise = cw >= ccw
        };
    }

    float Score(MonsterBase owner, bool clockwise)
    {
        var mgr = TileMapGuideManager.Instance;

        int next = mgr.GetNextIndex(owner.EdgeIndex, clockwise);
        Edge e = mgr.GetEdge(next);

        float dist = Vector2.Distance(
            owner.Position,
            (e.a + e.b) * 0.5f
        );

        float baseWeight = 1.0f;

        return baseWeight + (1f / (1f + dist));
    }
}