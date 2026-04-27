using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : MonsterBase
{
    public float moveSpeed = 2.5f;

    protected override void Init()
    {
        ai = new ghostAI();
        motor = new ghostMotor();
    }
}
