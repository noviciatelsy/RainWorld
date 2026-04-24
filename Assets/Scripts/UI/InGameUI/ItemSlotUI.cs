using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image slotStateImage; // 显示该格可用状态的Image
    private int slotIndex; // UI的槽位序号
    public InventoryItem itemInSlot;// 槽内物品的Inventory_Item对象
    private InventoryBase inventory;

    public void BindItemIndex(int index)
    {
        slotIndex = index;
    }

    public void SetInventory(InventoryBase inventory)
    {
        this.inventory = inventory;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void SetSlotState(SlotState slotState)
    {
        switch (slotState)
        {
            case SlotState.None:
                break;

            case SlotState.EnablePlace:
                break;

            case SlotState.DisablePlace:
                break;

            case SlotState.EnableReplace:
                break;
        }
    }
}
