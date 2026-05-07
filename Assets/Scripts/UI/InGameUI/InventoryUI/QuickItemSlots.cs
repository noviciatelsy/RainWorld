using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickItemSlots : MonoBehaviour
{
    private QuickItemSlotUI[] quickItemSlotUIs;
    private InventoryPlayer playerInventory;

    private void Awake()
    {
        quickItemSlotUIs=GetComponentsInChildren<QuickItemSlotUI>();
    }

    public void SetInventory(InventoryPlayer newPlayerInventory)
    {
        if (playerInventory == newPlayerInventory)
        {
            return;
        }

        if (playerInventory != null)
        {
            playerInventory.onQuickItemsChange -= UpdateQuickItemSlots;
        }

        playerInventory=newPlayerInventory;


        if (playerInventory != null)
        {
            playerInventory.onQuickItemsChange += UpdateQuickItemSlots;   
        }

        UpdateQuickItemSlots();
    }

    private void UpdateQuickItemSlots()
    {
        for (int i = 0; i < quickItemSlotUIs.Length; i++)
        {
            InventoryItem itemInSlot=playerInventory.quickItemSlotList[i].itemInSlot;
            quickItemSlotUIs[i].UpdateItem(itemInSlot);  
        }
    }
}
