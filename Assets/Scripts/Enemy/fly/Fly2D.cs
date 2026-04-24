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
}