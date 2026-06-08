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
        storageInventory.LoadData();
    }
    private void OnEnable()
    {
        SaveManager.Instance.OnGameRunDataOverwrite += storageInventory.LoadData;
    }

    private void OnDisable()
    {
        SaveManager.Instance.OnGameRunDataOverwrite -= storageInventory.LoadData;
    }
    private void OnDestroy()
    {
        storageInventory.SaveData();
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
