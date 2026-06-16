using UnityEngine;

/// <summary>
/// 将本体 Collider 上的踩踏碰撞转发给 EnemyStompReceiver（如 Robot 根节点 BoxCollider）。
/// </summary>
[DisallowMultipleComponent]
public class EnemyStompBodyForwarder : MonoBehaviour
{
    [SerializeField] private EnemyStompReceiver stompReceiver;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private float minStompDownSpeed = 0.5f;

    public void Initialize(EnemyStompReceiver receiver, Collider2D collider)
    {
        stompReceiver = receiver;
        bodyCollider = collider;
    }

    private void Awake()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (stompReceiver == null)
        {
            stompReceiver = GetComponentInParent<EnemyStompReceiver>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryForward(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryForward(collision);
    }

    private void TryForward(Collision2D collision)
    {
        if (bodyCollider != null && bodyCollider.isTrigger)
        {
            return;
        }

        EnemyStompUtility.TryStompFromCollision(
            stompReceiver,
            collision,
            bodyCollider,
            minStompDownSpeed);
    }
}
