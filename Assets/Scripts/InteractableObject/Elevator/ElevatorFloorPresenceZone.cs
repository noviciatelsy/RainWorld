using UnityEngine;

/// <summary>
/// 单层电梯存在区：玩家进入（比 InteractZone 更大的一圈）时，通知控制器将平台瞬移到该层。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ElevatorFloorPresenceZone : MonoBehaviour
{
    [SerializeField] private ElevatorController elevatorController;
    [SerializeField] private ElevatorFloor floor;
    [Tooltip("若指定 InteractZone，Awake 时按该 Collider 外扩生成本 Trigger 尺寸")]
    [SerializeField] private ElevatorInteractZone sourceInteractZone;
    [SerializeField] private float expandPadding = 1.5f;
    [SerializeField] private Vector2 fallbackSize = new Vector2(14f, 10f);

    public ElevatorFloor Floor => floor;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (elevatorController == null)
        {
            elevatorController = GetComponentInParent<ElevatorController>();
        }

        ApplyColliderSize();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryNotify(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryNotify(other);
    }

    public void Configure(
        ElevatorController controller,
        ElevatorFloor targetFloor,
        ElevatorInteractZone source,
        float padding,
        Vector2 defaultSize)
    {
        elevatorController = controller;
        floor = targetFloor;
        sourceInteractZone = source;
        expandPadding = padding;
        fallbackSize = defaultSize;
        ApplyColliderSize();
    }

    private void ApplyColliderSize()
    {
        if (!TryGetComponent(out BoxCollider2D self))
        {
            return;
        }

        self.isTrigger = true;

        if (sourceInteractZone != null && sourceInteractZone.TryGetComponent(out BoxCollider2D source))
        {
            self.offset = source.offset;
            self.size = source.size + Vector2.one * expandPadding * 2f;
            return;
        }

        self.offset = Vector2.zero;
        self.size = fallbackSize;
    }

    private void TryNotify(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() == null || elevatorController == null)
        {
            return;
        }

        if (elevatorController.IsMoving)
        {
            return;
        }

        PlayerControl playerControl = other.GetComponentInParent<PlayerControl>();
        if (playerControl != null && playerControl.IsOnMovingElevator())
        {
            return;
        }

        elevatorController.NotifyPlayerFloorPresence(floor);
    }
}
