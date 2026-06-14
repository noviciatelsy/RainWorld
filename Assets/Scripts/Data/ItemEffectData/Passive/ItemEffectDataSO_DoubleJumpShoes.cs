using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/DoubleJumpShoes", fileName = "ItemEffectData_DoubleJumpShoes")]
public class ItemEffectDataSO_DoubleJumpShoes : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        DoubleJumpShoesPassiveEffect doubleJumpShoesPassiveEffect=player.GetComponentInChildren<DoubleJumpShoesPassiveEffect>();
        if(doubleJumpShoesPassiveEffect != null )
        {
            doubleJumpShoesPassiveEffect.EnableEffect();
        }
        
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        DoubleJumpShoesPassiveEffect doubleJumpShoesPassiveEffect = player.GetComponentInChildren<DoubleJumpShoesPassiveEffect>();
        if (doubleJumpShoesPassiveEffect != null)
        {
            doubleJumpShoesPassiveEffect.DisableEffect();
        }
    }
}
