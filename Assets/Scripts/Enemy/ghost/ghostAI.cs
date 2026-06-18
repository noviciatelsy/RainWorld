using UnityEngine;

public struct GhostIntent : IIntent
{
    public Transform target;
}

public class ghostAI : IMonsterAI
{
    private Transform player;

    public IIntent Evaluate(MonsterBase owner)
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        if (player == null || !PlayerInvisibilityPerception.IsPlayerDetectable(player))
        {
            return null;
        }

        return new GhostIntent
        {
            target = player
        };
    }
}
