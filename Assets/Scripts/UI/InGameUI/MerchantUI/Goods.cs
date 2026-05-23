using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Goods : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler
{
    [SerializeField] private int slotSize = 65;
    [SerializeField] private int spaceSize=0;
    private Image itemImage;
    private RectTransform rect;
    private InGameUI inGameUI;
    private GoodsShelfUI goodsShelfUI;
    private ItemDataSO itemData;

    private void Awake()
    {
        itemImage = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        inGameUI=GetComponentInParent<InGameUI>();
        goodsShelfUI=GetComponentInParent<GoodsShelfUI>();  
    }

    public void SetItemData(ItemDataSO itemData)
    {
        this.itemData = itemData;
        itemImage.sprite=itemData.itemIcon;
        Vector2 imageSize=itemData.backpackItemData.imageSize;
        rect.sizeDelta = new Vector2(slotSize * imageSize.x + spaceSize * (imageSize.x - 1), slotSize * imageSize.y + spaceSize * (imageSize.y - 1));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(itemData==null) return;
        inGameUI.itemToolTip.ShowItemToolTip(true, rect, itemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (itemData==null) return;
        inGameUI.itemToolTip.ShowItemToolTip (false, rect, itemData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(itemData==null) return;
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            return;
        }
        goodsShelfUI.TryBuyItem(itemData);
    }
}
