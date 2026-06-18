using UnityEngine;

/// <summary>
/// 玩家从上方踩上后禁用平台碰撞体，并通知 BoomController。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class BoomStompPlatform : MonoBehaviour
{
    [SerializeField] private BoomController boomController;
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private string platformLayerName = "Platform";
    [SerializeField] private float minStompDownSpeed = 0.35f;
    [SerializeField] private float topContactTolerance = 0.12f;

    private bool isTriggered;

    public bool IsTriggered => isTriggered;

    private void Awake()
    {
        if (platformCollider == null)
        {
            platformCollider = GetComponent<Collider2D>();
        }

        SetupOneWayPlatform();
    }

    public void Bind(BoomController controller)
    {
        boomController = controller;
    }

    private void SetupOneWayPlatform()
    {
        if (platformCollider == null)
        {
            return;
        }

        platformCollider.isTrigger = false;
        platformCollider.usedByEffector = true;

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
        TryTrigger(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryTrigger(collision);
    }

    private void TryTrigger(Collision2D collision)
    {
        if (isTriggered || collision == null || platformCollider == null)
        {
            return;
        }

        Collider2D other = collision.collider;

        if (other == null || other.GetComponentInParent<Player>() == null)
        {
            return;
        }

        if (collision.relativeVelocity.y > -minStompDownSpeed)
        {
            return;
        }

        if (other.bounds.min.y < platformCollider.bounds.max.y - topContactTolerance)
        {
            return;
        }

        TriggerPlatform();
    }

    private void TriggerPlatform()
    {
        if (isTriggered)
        {
            return;
        }

        isTriggered = true;

        PlatformEffector2D effector = GetComponent<PlatformEffector2D>();

        if (effector != null)
        {
            effector.enabled = false;
        }

        platformCollider.enabled = false;

        if (boomController == null)
        {
            boomController = GetComponentInParent<BoomController>();
        }

        boomController?.ActivateFromPlatform(this);
    }
}
