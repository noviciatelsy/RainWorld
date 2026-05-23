using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoodsShelfUI : MonoBehaviour
{
    private InventoryPlayer playerInventory;
    private DraggedItemUI draggedItemUI;

    private void Awake()
    {
        if (draggedItemUI == null)
        {
            InGameUI inGameUI = GetComponentInParent<InGameUI>();

            if (inGameUI != null)
            {
                draggedItemUI = inGameUI.draggedItemUI;
            }
        }
    }

    public void SetInventory(InventoryPlayer playerInventory)
    {
        this.playerInventory = playerInventory;
    }

    public void TryBuyItem(ItemDataSO itemToBuy)
    {
        if(!playerInventory.MoneyCanAfford(itemToBuy.itemBuyPrice))
        {
            return;
        }
        if(draggedItemUI.IsDragging)
        {
            return;
        }
        playerInventory.ReduceMoney(itemToBuy.itemBuyPrice);
        InventoryItem item=new InventoryItem(itemToBuy);
        draggedItemUI.BeginDrag(item);
    }

    public void TrySellItem()
    {
        if (!draggedItemUI.IsDragging)
        {
            return;
        }
        playerInventory.AddMoney(draggedItemUI.draggedItem.ItemData.itemSellPrice);
        draggedItemUI.EndDrag();
    }
}
