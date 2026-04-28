using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ghostMotor : IMonsterMotor
{
    public void Execute(MonsterBase owner, IIntent intent)
    {
        if (intent is not ChaseIntent chase)
            return;

        Ghost enemy = owner as Ghost;
        if (enemy == null || chase.target == null)
            return;

        Vector2 dir = ((Vector2)chase.target.position - enemy.Position).normalized;
        //Debug.Log("onchase" + enemy.transform.position);
        enemy.transform.position += (Vector3)(dir * enemy.moveSpeed * Time.fixedDeltaTime);
    }
}
