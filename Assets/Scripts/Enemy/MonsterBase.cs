using UnityEngine;

public abstract class MonsterBase : MonoBehaviour
{
    public Transform Transform => transform;
    public Vector2 Position => transform.position;

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
        MoveIntent intent = ai.Evaluate(this);
        motor.Execute(this, intent);
    }
}