using UnityEngine;

/// <summary>
/// 对玩家施加击退的统一入口（调用方式参考踩头时的 ApplyStompBounce）。
/// </summary>
public static class PlayerKnockbackUtility
{
    public static bool TryApplyKnockback(Component target, Vector2 impulse)
    {
        PlayerControl control = ResolveControl(target);

        if (control == null)
        {
            return false;
        }

        control.ApplyKnockback(impulse);
        return true;
    }

    /// <summary>
    /// 按 force.x 的绝对值为水平冲量，方向为远离 sourceWorldPosition；force.y 为竖直冲量。
    /// </summary>
    public static bool TryApplyKnockbackFromSource(Component target, Vector2 force, Vector3 sourceWorldPosition)
    {
        if (target == null)
        {
            return false;
        }

        float signX = Mathf.Sign(target.transform.position.x - sourceWorldPosition.x);

        if (Mathf.Approximately(signX, 0f))
        {
            signX = 1f;
        }

        Vector2 impulse = new Vector2(Mathf.Abs(force.x) * signX, force.y);
        return TryApplyKnockback(target, impulse);
    }

    public static PlayerControl ResolveControl(Component component)
    {
        if (component == null)
        {
            return null;
        }

        PlayerControl control = component.GetComponent<PlayerControl>();

        if (control != null)
        {
            return control;
        }

        Player player = component.GetComponentInParent<Player>();

        if (player != null)
        {
            return player.GetComponent<PlayerControl>();
        }

        return component.GetComponentInParent<PlayerControl>();
    }
}
