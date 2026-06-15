using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/Camera", fileName = "ItemEffectData_Camera")]
public class ItemEffectDataSO_Camera : ItemEffectDataSO
{
    public override void StartHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.StartHoldingItem(item, inventoryPlayer);
        CameraItem cameraItem=player.GetComponentInChildren<CameraItem>();
        if (cameraItem != null)
        {
            cameraItem.OpenPhotographyMode();
        }
    }

    public override void EndHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        base.EndHoldingItem(item, inventoryPlayer);
        CameraItem cameraItem = player.GetComponentInChildren<CameraItem>();
        if (cameraItem != null)
        {
            cameraItem.ClosePhotographyMode();
        }
    }

    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        CameraItem cameraItem = player.GetComponentInChildren<CameraItem>();
        if (cameraItem != null)
        {
            cameraItem.UseCamera();
            return true;
        }
        return false;
    }
}
