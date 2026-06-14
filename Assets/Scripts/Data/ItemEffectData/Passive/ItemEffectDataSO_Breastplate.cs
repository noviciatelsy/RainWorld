using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Breastplate", fileName = "ItemEffectData_Breastplate")]
public class ItemEffectDataSO_Breastplate : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        BreastplatePassiveEffect breastplatePassiveEffect=player.GetComponentInChildren<BreastplatePassiveEffect>();
        if(playerVitals != null&&playerControl!=null&&breastplatePassiveEffect!=null)
        {
            playerVitals.AddDefense(4);
            playerControl.ReduceMoveSpeed(0.25f);
            breastplatePassiveEffect.EnableEffect();
        }
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        PlayerVitals playerVitals = player.GetComponent<PlayerVitals>();
        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        BreastplatePassiveEffect breastplatePassiveEffect = player.GetComponentInChildren<BreastplatePassiveEffect>();
        if (playerVitals != null && playerControl != null && breastplatePassiveEffect != null)
        {
            playerVitals.ReduceDefense(4);
            playerControl.AddMoveSpeed(0.25f);
            breastplatePassiveEffect.DisableEffect();
        }
    }

}
