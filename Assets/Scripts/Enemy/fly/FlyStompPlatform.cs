using UnityEngine;

/// <summary>
/// Fly 顶部踩踏碰撞：玩家从上方踩中后令 Fly 进入 stun 并掉落为道具。
/// </summary>
[DisallowMultipleComponent]
public class FlyStompPlatform : MonoBehaviour
{
    [SerializeField] private Fly2D fly;
    [SerializeField] private Collider2D stompCollider;
    [SerializeField] private float minStompDownSpeed = 0.5f;
    [SerializeField] private float stompBounceImpulse = 0f;

    private void Awake()
    {
        if (fly == null)
        {
            fly = GetComponentInParent<Fly2D>();
        }

        if (stompCollider == null)
        {
            stompCollider = GetComponent<Collider2D>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStomp(collision.collider, collision.relativeVelocity);
    }

    private void TryStomp(Collider2D other, Vector2 relativeVelocity)
    {
        if (fly == null || !fly.CanBeStomped || other == null)
        {
            return;
        }

        Player player = other.GetComponentInParent<Player>();

        if (player == null)
        {
            return;
        }

        if (relativeVelocity.y > -minStompDownSpeed)
        {
            return;
        }

        if (stompCollider != null && other.bounds.min.y < stompCollider.bounds.center.y)
        {
            return;
        }

        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if (playerControl != null)
        {
            playerControl.ApplyStompBounce(stompBounceImpulse);
        }

        bool facingRight = playerControl == null || playerControl.facingDir >= 0;
        fly.EnterStunAndDropAsItem(facingRight);
        EnemyIntelligenceUnlockUtility.TryUnlockByName(EnemyIntelligenceNames.FlyCleverUse);
    }
}
