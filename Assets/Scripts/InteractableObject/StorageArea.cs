using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageArea : PlayerSensorTarget
{
    private InventoryBase storageInventory;

    protected override void Awake()
    {
        base.Awake();
        storageInventory = GetComponent<InventoryBase>();
    }
    public override void Interact()
    {
        base.Interact();
        if (storageInventory != null)
        {
            InGameUI.Instance.ToggleStorageUI(storageInventory);
        }
    }
}
