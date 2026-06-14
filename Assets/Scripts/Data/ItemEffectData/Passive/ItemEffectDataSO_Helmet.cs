using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Helmet", fileName = "ItemEffectData_Helmet")]
public class ItemEffectDataSO_Helmet : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if (playerVitals != null && playerControl != null)
        {
            playerVitals.AddDefense(2);
            playerControl.ReduceMoveSpeed(0.125f);
        }
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if (playerVitals != null && playerControl != null)
        {
            playerVitals.ReduceDefense(2);
            playerControl.AddMoveSpeed(0.125f);
        }
    }
}
