using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct ChaseIntent : IIntent
{
    public Transform target;
}

public class ghostAI : IMonsterAI
{
    private Transform player;
    GameObject Playerobj;

    public IIntent Evaluate(MonsterBase owner)
    {
        if (player == null)
        {
            Playerobj = GameObject.FindGameObjectWithTag("Player");
            if (Playerobj != null)
                player = Playerobj.transform;
        }

        if (player == null)
            return null;

        return new ChaseIntent
        {
            target = player
        };
    }
}
