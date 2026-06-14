using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Sneaker", fileName = "ItemEffectData_Sneaker")]
public class ItemEffectDataSO_Sneaker : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if ( playerControl != null )
        {   
            playerControl.AddMoveSpeed(0.25f);
        }
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        if (playerControl != null)
        {
            playerControl.ReduceMoveSpeed(0.25f);
        }
    }
}
