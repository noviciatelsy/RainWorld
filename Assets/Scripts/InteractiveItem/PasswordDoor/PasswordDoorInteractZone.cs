using UnityEngine;

/// <summary>
/// 密码门触发区：进入后由 PasswordDoor 显示 UI，按 E 进入输入模式。
/// </summary>
[DisallowMultipleComponent]
public class PasswordDoorInteractZone : PlayerSensorTarget
{
    [SerializeField] private PasswordDoor passwordDoor;
    [SerializeField] private Transform uiAnchor;

    public Transform UiAnchor => uiAnchor != null ? uiAnchor : transform;

    protected override void Awake()
    {
        if (passwordDoor == null)
        {
            passwordDoor = GetComponentInParent<PasswordDoor>();
        }
    }

    public override void Interact()
    {
        passwordDoor?.OnInteract(this);
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<Player>() == null)
        {
            return;
        }

        passwordDoor?.NotifyPlayerEntered(this, collision.transform.position);
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<Player>() == null)
        {
            return;
        }

        passwordDoor?.NotifyPlayerExited(this);
    }

    public void SetPasswordDoor(PasswordDoor door)
    {
        passwordDoor = door;
    }
}
