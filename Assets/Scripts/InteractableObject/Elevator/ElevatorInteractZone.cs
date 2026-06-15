using UnityEngine;

/// <summary>
/// 单层电梯呼叫区：进入后显示选层 UI，按 E 确认（同 LootArea 流程）。
/// </summary>
public class ElevatorInteractZone : PlayerSensorTarget
{
    [SerializeField] private ElevatorFloor floor;
    [SerializeField] private ElevatorController elevatorController;
    [SerializeField] private Transform uiAnchor;

    public ElevatorFloor Floor => floor;
    public Transform UiAnchor => uiAnchor != null ? uiAnchor : transform;

    protected override void Awake()
    {
        base.Awake();

        if (elevatorController == null)
        {
            elevatorController = GetComponentInParent<ElevatorController>();
        }
    }

    public override void Interact()
    {
        if (elevatorController == null || elevatorController.IsMoving)
        {
            return;
        }

        if (elevatorController.IsUiOpen && elevatorController.InteractionFloor == floor)
        {
            elevatorController.ConfirmSelectionViaUi();
            return;
        }

        base.Interact();
        elevatorController.NotifyZoneEntered(this);
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<Player>() == null)
        {
            return;
        }

        base.OnTriggerEnter2D(collision);

        if (elevatorController != null && !elevatorController.IsMoving)
        {
            elevatorController.NotifyZoneEntered(this);
        }
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<Player>() == null)
        {
            return;
        }

        base.OnTriggerExit2D(collision);
        elevatorController?.NotifyZoneExited(this);
    }

    public void SetElevatorController(ElevatorController controller)
    {
        elevatorController = controller;
    }
}
