using System.Collections.Generic;
using UnityEngine;

public class Fly2D : MonoBehaviour
{
    public float moveSpeed = 3f;

    public Transform Transform => transform;
    public Vector2 Position => transform.position;

    public Vector2 CurrentTarget;

    private FlyUtilityAI ai;
    private FlyMotor motor;

    public bool Arrived;   // 新增
    public List<Vector2> DebugPath;
    public Vector2 DebugTarget;

    void Start()
    {
        ai = new FlyUtilityAI(this);
        motor = new FlyMotor(this);
    }

    void FixedUpdate()
    {
        FlyIntent intent = ai.Evaluate();
        motor.Execute(intent);
    }

    public bool HasReachedTarget()
    {
        return Vector2.Distance(Position, CurrentTarget) < 0.1f;
    }

    public bool TargetChanged(Vector2 newTarget)
    {
        return Vector2.Distance(CurrentTarget, newTarget) > 0.1f;
    }

    void OnDrawGizmos()
    {
        // =========================
        // 1. 目标点
        // =========================
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(DebugTarget, 0.2f);

        // =========================
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