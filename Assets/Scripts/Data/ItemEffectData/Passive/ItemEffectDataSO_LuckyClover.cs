using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/LuckyClover", fileName = "ItemEffectData_LuckyClover")]
public class ItemEffectDataSO_LuckyClover : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        PlayerLuck playerLuck=player.GetComponentInChildren<PlayerLuck>();
        if (playerLuck != null)
        {
            playerLuck.AddBonusItemLootAmount(1);
        }
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        PlayerLuck playerLuck = player.GetComponentInChildren<PlayerLuck>();
        if (playerLuck != null)
        {
            playerLuck.ReduceBonusItemLootAmount(1);
        }
    }
}
