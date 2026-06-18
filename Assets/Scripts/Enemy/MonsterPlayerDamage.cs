using UnityEngine;

/// <summary>
/// 怪物对玩家造成伤害的统一入口，走 PlayerVitals.TakeDamage（含防御、无敌帧、受伤音效）。
/// </summary>
public static class MonsterPlayerDamage
{
    public static bool TryDealDamage(Transform target, float damageAmount)
    {
        if (target == null)
        {
            return false;
        }

        return TryDealDamage(ResolveVitals(target), damageAmount);
    }

    public static bool TryDealDamage(Player player, float damageAmount)
    {
        if (player == null)
        {
            return false;
        }

        PlayerVitals vitals = player.GetComponent<PlayerVitals>();
        if (vitals == null)
        {
            vitals = player.GetComponentInChildren<PlayerVitals>();
        }

        return TryDealDamage(vitals, damageAmount);
    }

    public static bool TryDealDamage(PlayerVitals vitals, float damageAmount)
    {
        if (vitals == null || vitals.IsDead || damageAmount <= 0f)
        {
            return false;
        }

        float healthBefore = vitals.CurrentHealth;
        vitals.TakeDamage(damageAmount);
        return vitals.CurrentHealth < healthBefore;
    }

    public static PlayerVitals ResolveVitals(Component component)
    {
        if (component == null)
        {
            return null;
        }

        Player player = component.GetComponentInParent<Player>();
        if (player != null)
        {
            PlayerVitals vitals = player.GetComponent<PlayerVitals>();
            if (vitals != null)
            {
                return vitals;
            }

            return player.GetComponentInChildren<PlayerVitals>();
        }

        return component.GetComponentInParent<PlayerVitals>();
    }
}
