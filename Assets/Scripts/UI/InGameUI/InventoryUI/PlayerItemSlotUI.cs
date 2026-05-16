using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerItemSlotUI : BaseItemSlotUI, IPointerClickHandler
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

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        ShowItemToolTip();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        HideItemToolTip();
    }

    protected override void ShowItemToolTip()
    {
        base.ShowItemToolTip();
        if (itemInSlot == null) return;
        inGameUI.itemToolTip.ShowPlayerItemToolTip(true, rect, itemInSlot);
    }

    protected override void HideItemToolTip()
    {
        base.HideItemToolTip();
        inGameUI.itemToolTip.ShowPlayerItemToolTip(false, rect, itemInSlot);
    }
}