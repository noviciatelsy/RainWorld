using UnityEngine;

/// <summary>
/// 头部踩踏单向平台：从下可穿过，从上方落下触发踩头。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class EnemyStompPlatform : MonoBehaviour
{
    [SerializeField] private EnemyStompReceiver stompReceiver;
    [SerializeField] private Collider2D stompCollider;
    [SerializeField] private float minStompDownSpeed = 0.5f;
    [SerializeField] private string platformLayerName = "Platform";

    private void Awake()
    {
        if (stompReceiver == null)
        {
            stompReceiver = GetComponentInParent<EnemyStompReceiver>();
        }

        if (stompCollider == null)
        {
            stompCollider = GetComponent<Collider2D>();
        }

        SetupOneWayPlatform();
    }

    private void SetupOneWayPlatform()
    {
        if (stompCollider == null)
        {
            return;
        }

        stompCollider.isTrigger = false;
        stompCollider.usedByEffector = true;

        int platformLayer = LayerMask.NameToLayer(platformLayerName);

        if (platformLayer >= 0)
        {
            gameObject.layer = platformLayer;
        }

        PlatformEffector2D effector = GetComponent<PlatformEffector2D>();

        if (effector == null)
        {
            effector = gameObject.AddComponent<PlatformEffector2D>();
        }

        effector.useOneWay = true;
        effector.useOneWayGrouping = false;
        effector.surfaceArc = 180f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStomp(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryStomp(collision);
    }

    private void TryStomp(Collision2D collision)
    {
        EnemyStompUtility.TryStompFromCollision(
            stompReceiver,
            collision,
            stompCollider,
            minStompDownSpeed);
    }
}
