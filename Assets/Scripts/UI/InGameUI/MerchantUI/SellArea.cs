using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SellArea : MonoBehaviour,IPointerDownHandler
{
    [SerializeField] private RectTransform sellBoard;
    private GoodsShelfUI goodsShelfUI;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            return;
        }
        goodsShelfUI.TrySellItem();
    }
}
