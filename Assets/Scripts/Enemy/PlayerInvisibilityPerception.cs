using UnityEngine;

/// <summary>
/// 怪物感知玩家时检查隐身斗篷状态（读取 InvisibleCloakPassiveEffect，不修改 Player 脚本）。
/// </summary>
public static class PlayerInvisibilityPerception
{
    public static bool IsPlayerDetectable(Player player)
    {
        if (player == null)
        {
            return false;
        }

        InvisibleCloakPassiveEffect cloak =
            player.GetComponentInChildren<InvisibleCloakPassiveEffect>();

        return cloak == null || !cloak.isInvisible;
    }

    public static bool IsPlayerDetectable(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return false;
        }

        Player player = targetTransform.GetComponentInParent<Player>();
        if (player == null)
        {
            return true;
        }

        return IsPlayerDetectable(player);
    }

    public static bool IsPlayerDetectable(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        Player player = collider.GetComponentInParent<Player>();
        if (player == null)
        {
            return true;
        }

        return IsPlayerDetectable(player);
    }

    public static bool TryFindDetectablePlayer(
        Vector2 origin,
        float radius,
        LayerMask playerLayerMask,
        out Player player)
    {
        player = null;

        if (radius <= 0f || playerLayerMask.value == 0)
        {
            return false;
        }

        Collider2D hit = Physics2D.OverlapCircle(origin, radius, playerLayerMask);
        if (hit == null)
        {
            return false;
        }

        player = hit.GetComponentInParent<Player>();
        if (player == null)
        {
            return false;
        }

        if (!IsPlayerDetectable(player))
        {
            player = null;
            return false;
        }

        return true;
    }
}
