using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/InvisibleCloak", fileName = "ItemEffectData_InvisibleCloak")]
public class ItemEffectDataSO_InvisibleCloak : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        InvisibleCloakPassiveEffect invisibleCloakPassiveEffect = player.GetComponentInChildren<InvisibleCloakPassiveEffect>();
        if (invisibleCloakPassiveEffect != null)
        {
            invisibleCloakPassiveEffect.EnableEffect();
        }
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        InvisibleCloakPassiveEffect invisibleCloakPassiveEffect = player.GetComponentInChildren<InvisibleCloakPassiveEffect>();
        if (invisibleCloakPassiveEffect != null)
        {
            invisibleCloakPassiveEffect.DisableEffect();
        }
    }

  
}
