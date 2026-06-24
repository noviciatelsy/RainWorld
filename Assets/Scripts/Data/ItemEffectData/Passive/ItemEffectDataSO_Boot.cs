using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Boot", fileName = "ItemEffectData_Boot")]
public class ItemEffectDataSO_Boot : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();

        if (playerVitals != null && playerControl != null) 
        {
            playerVitals.AddDefense(3);
            playerControl.ReduceJumpForce(0.25f);
        }
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();

        if (playerVitals != null && playerControl != null )
        {
            playerVitals.ReduceDefense(3);
            playerControl.AddJumpForce(0.5f);
        }
    }
}
