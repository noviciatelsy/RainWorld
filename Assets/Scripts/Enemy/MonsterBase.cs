using System.Collections.Generic;
using UnityEngine;

public abstract class MonsterBase : MonoBehaviour
{
    public Transform Transform => transform;
    public Vector2 Position => transform.position;

    // =========================
    // 通用移动状态（关键新增）
    // =========================
    public Vector2 CurrentTarget;
    public bool Arrived;

    // Debug
    public List<Vector2> DebugPath;
    public Vector2 DebugTarget;

    // =========================
    // Edge系统（你已有）
    // =========================
    public int EdgeIndex;
    public Edge CurrentEdge;
    public Vector2 Target;
    public bool HasEdge;

    protected IMonsterAI ai;
    protected IMonsterMotor motor;

    protected virtual void Start()
    {
        Init();
    }

    protected abstract void Init();

    protected virtual void FixedUpdate()
    {
        object intent = ai.Evaluate(this);
        motor.Execute(this, intent);
    }

    // =========================
    // 通用函数（关键）
    // =========================
    public bool TargetChanged(Vector2 newTarget)
    {
        return Vector2.Distance(CurrentTarget, newTarget) > 0.1f;
    }

    public bool HasReachedTarget()
    {
        return Vector2.Distance(Position, CurrentTarget) < 0.1f;
    }
}