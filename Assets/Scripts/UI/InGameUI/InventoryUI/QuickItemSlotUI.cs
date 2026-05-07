using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickItemSlotUI : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image itemIconImage;
    public InventoryItem itemInSlot; // 槽内物品的Inventory_Item对象

    public void UpdateItem(InventoryItem itemInSlot)
    {
        this.itemInSlot = itemInSlot;
        if(itemInSlot.ItemData!=null)
        {
            itemIconImage.enabled= true;
            itemIconImage.sprite = itemInSlot.ItemData.itemIcon;
        }
        else
        {
            itemIconImage.enabled= false;
        }
    }

    

    public void OnPointerDown(PointerEventData eventData) // 右键按下时，取消将该快捷栏内的物品
    {
        
    }
}
