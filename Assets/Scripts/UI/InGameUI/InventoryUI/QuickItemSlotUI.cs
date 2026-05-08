using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickItemSlotUI : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image itemIconImage;

    public InventoryItem itemInSlot; // 槽内物品的Inventory_Item对象

    private QuickItemSlots owner;
    private int quickSlotIndex;

    private void Awake()
    {
        if (itemIconImage != null)
        {
            itemIconImage.raycastTarget = false;
            itemIconImage.enabled = false;
        }
    }

    public void Bind(QuickItemSlots owner, int quickSlotIndex)
    {
        this.owner = owner;
        this.quickSlotIndex = quickSlotIndex;
    }

    public void UpdateItem(InventoryItem itemInSlot)
    {
        this.itemInSlot = itemInSlot;

        if (itemIconImage == null)
        {
            return;
        }

        if (itemInSlot != null && itemInSlot.ItemData != null)
        {
            itemIconImage.enabled = true;
            itemIconImage.sprite = itemInSlot.ItemData.itemIcon;
        }
        else
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 右键快捷栏本身，取消该栏装备
        if (eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        if (owner == null)
        {
            return;
        }

        owner.ClearQuickItem(quickSlotIndex);
    }
}