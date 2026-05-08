using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerItemSlotUI : ItemSlotUI, IPointerClickHandler
{
    private PlayerBackpack playerBackpack;

    protected override void Awake()
    {
        base.Awake();
        playerBackpack = GetComponentInParent<PlayerBackpack>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        if (itemInSlot == null || itemInSlot.ItemData == null)
        {
            return;
        }

        if (itemInSlot.ItemData.itemType != ItemType.Active)
        {
            return;
        }

        if (playerBackpack == null)
        {
            return;
        }
        playerBackpack.TrySetQuickItemToHeldSlot(itemInSlot);
    }
}