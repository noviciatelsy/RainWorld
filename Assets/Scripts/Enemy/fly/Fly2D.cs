using System.Collections.Generic;
using UnityEngine;

public class Fly2D : MonsterBase
{
    public float moveSpeed = 3f;

    //private FlyUtilityAI ai;
    //private FlyMotor motor;


    protected override void Init()
    {
        ai = new FlyUtilityAI(this);
        motor = new FlyMotor(this);
    }


    void OnDrawGizmos()
    {
        // =========================
        // 1. 目标点
        // =========================
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(DebugTarget, 0.2f);

        // ===================}======
        // 2. 路径线
        // =========================
        if (DebugPath == null || DebugPath.Count < 2) return;

        Gizmos.color = Color.green;

        for (int i = 0; i < DebugPath.Count - 1; i++)
        {
            Gizmos.DrawLine(DebugPath[i], DebugPath[i + 1]);
        }

        // =========================
        // 3. 路径点
        // =========================
        Gizmos.color = Color.yellow;

        foreach (var p in DebugPath)
        {
            Gizmos.DrawSphere(p, 0.08f);
        }

        // =========================
        // 4. 当前所在点
        // =========================
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.12f);
    }
}
