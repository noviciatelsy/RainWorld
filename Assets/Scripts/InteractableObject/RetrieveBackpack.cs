using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetrieveBackpack : PlayerSensorTarget
{
    private InventoryBase retrieveInventory;

    protected override void Awake()
    {
        base.Awake();
        retrieveInventory = GetComponent<InventoryBase>();
        retrieveInventory.LoadData();
    }
    private void OnEnable()
    {
        SaveManager.Instance.OnGameRunDataOverwrite += retrieveInventory.LoadData;
    }

    private void OnDisable()
    {
        SaveManager.Instance.OnGameRunDataOverwrite -= retrieveInventory.LoadData;
    }
    public override void Interact()
    {
        base.Interact();
        InGameUI.Instance.ToggleRetrieveUI(retrieveInventory);
    }

}
