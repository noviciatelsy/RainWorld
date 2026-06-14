using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/CloverAmulet", fileName = "ItemEffectData_CloverAmulet")]
public class ItemEffectDataSO_CloverAmulet : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        PlayerLuck playerLuck = player.GetComponentInChildren<PlayerLuck>();
        if (playerLuck != null)
        {
            playerLuck.AddBonusLuck(1);
        }
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        PlayerLuck playerLuck = player.GetComponentInChildren<PlayerLuck>();
        if (playerLuck != null)
        {
            playerLuck.ReduceBonusLuck(1);
        }
    }
}
