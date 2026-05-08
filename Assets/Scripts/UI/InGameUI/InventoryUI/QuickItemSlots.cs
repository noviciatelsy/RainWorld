using System;
using UnityEngine;

public class QuickItemSlots : MonoBehaviour
{
    private QuickItemSlotUI[] quickItemSlotUIs;
    private InventoryPlayer playerInventory;

    private void Awake()
    {
        quickItemSlotUIs = GetComponentsInChildren<QuickItemSlotUI>(true);

        Array.Sort(
            quickItemSlotUIs,
            (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex())
        );

        for (int i = 0; i < quickItemSlotUIs.Length; i++)
        {
            quickItemSlotUIs[i].Bind(this, i);
        }
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

        playerInventory = newPlayerInventory;

        if (playerInventory != null)
        {
            playerInventory.onQuickItemsChange += UpdateQuickItemSlots;
        }

        UpdateQuickItemSlots();
    }

    public void ClearQuickItem(int quickSlotIndex)
    {
        if (playerInventory == null)
        {
            return;
        }

        playerInventory.ClearQuickItem(quickSlotIndex);
    }

    private void UpdateQuickItemSlots()
    {
        for (int i = 0; i < quickItemSlotUIs.Length; i++)
        {
            InventoryItem itemInSlot = null;

            if (playerInventory != null && i < playerInventory.quickItemSlotList.Count)
            {
                itemInSlot = playerInventory.GetQuickItem(i);
            }

            quickItemSlotUIs[i].UpdateItem(itemInSlot);
        }
    }
}