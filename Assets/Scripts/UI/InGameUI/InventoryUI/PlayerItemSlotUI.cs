using System.Collections;
using System.Collections.Generic;
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
        if(itemInSlot==null)
        {
            return;
        }
        if(eventData.button==PointerEventData.InputButton.Left)
        {
            return;
        }
        if(playerBackpack.holdQuickItemIndex_1)
        {

        }
        else if(playerBackpack.holdQuickItemIndex_2)
        {

        }
        else if (playerBackpack.holdQuickItemIndex_3)
        {

        }
        else if (playerBackpack.holdQuickItemIndex_4)
        {

        }
        else // 直接将物品设为手持
        {
            if(itemInSlot.ItemData.itemType!=ItemType.Active)
            {
                return ;
            }
        }
    }
}
