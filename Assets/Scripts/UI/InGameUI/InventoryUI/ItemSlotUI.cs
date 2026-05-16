using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlotUI : BaseItemSlotUI
{
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
        inGameUI.itemToolTip.ShowItemToolTip(true, rect, itemInSlot);
    }

    protected override void HideItemToolTip()
    {
        base.HideItemToolTip();
        inGameUI.itemToolTip.ShowItemToolTip(false, rect, itemInSlot);
    }
}
