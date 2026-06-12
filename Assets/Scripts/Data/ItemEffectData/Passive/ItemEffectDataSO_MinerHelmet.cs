using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/MinerHelmet", fileName = "ItemEffectData_MinerHelmet")]
public class ItemEffectDataSO_MinerHelmet : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        MinerHelmetPassiveEffect minerHelmetPassiveEffect=player.GetComponentInChildren<MinerHelmetPassiveEffect>();
        if(minerHelmetPassiveEffect != null )
        {
            minerHelmetPassiveEffect.EnableEffect();
        }
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        MinerHelmetPassiveEffect minerHelmetPassiveEffect = player.GetComponentInChildren<MinerHelmetPassiveEffect>();
        if (minerHelmetPassiveEffect != null)
        {
            minerHelmetPassiveEffect.DisableEffect();
        }
    }
}
