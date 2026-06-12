using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/MoleAmulet", fileName = "ItemEffectData_MoleAmulet")]
public class ItemEffectDataSO_MoleAmulet : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        MoleAmuletPassiveEffect moleAmuletPassiveEffect = player.GetComponentInChildren<MoleAmuletPassiveEffect>();
        if (moleAmuletPassiveEffect != null)
        {
            moleAmuletPassiveEffect.EnableEffect();
        }
    }

    public override void Unsubscribe()
    {
        base.Subscribe(player);
        MoleAmuletPassiveEffect moleAmuletPassiveEffect = player.GetComponentInChildren<MoleAmuletPassiveEffect>();
        if (moleAmuletPassiveEffect != null)
        {
            moleAmuletPassiveEffect.DisableEffect();
        }
    }
}
