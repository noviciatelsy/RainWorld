using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Torch", fileName = "ItemEffectData_Torch")]
public class ItemEffectDataSO_Torch : ItemEffectDataSO
{
    public override void StartHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.StartHoldingItem(item, inventoryPlayer);
        DarknessRevealSource darknessRevealSource=player.GetComponent<DarknessRevealSource>();
        if (darknessRevealSource != null )
        {
            darknessRevealSource.AddRadius(2);
        }

    }

    public override void EndHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.EndHoldingItem(item, inventoryPlayer);
        DarknessRevealSource darknessRevealSource = player.GetComponent<DarknessRevealSource>();
        if (darknessRevealSource != null)
        {
            darknessRevealSource.RemoveRadius(2);
        }
    }

    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        PlayerTorchThrower playerTorchThrower=player.GetComponentInChildren<PlayerTorchThrower>();
        if (playerTorchThrower != null )
        {
            playerTorchThrower.TryThrowTorch();
            return true;
        }
        return false;
    }
}
