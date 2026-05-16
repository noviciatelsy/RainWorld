using TMPro;
using UnityEngine;

public class ItemToolTip : BaseToolTip
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private TextMeshProUGUI itemEffect;
    [SerializeField] private TextMeshProUGUI equipInfo;
    [SerializeField] private TextMeshProUGUI SetQuickItemInfo;
    [SerializeField] private TextMeshProUGUI CancelQuickItemInfo;
    [SerializeField] private TextMeshProUGUI sellMoney;

    public void ShowPlayerItemToolTip(bool show, RectTransform targetRect, InventoryItem itemToShow)
    {
        base.ShowToolTip(show, targetRect);
        if (show == false || itemToShow == null)
        {
            return;
        }
        ItemDataSO itemData = itemToShow.ItemData;

        itemName.gameObject.SetActive(true);
        itemName.text = itemData.itemDisplayName;

        itemType.gameObject.SetActive(true);
        itemType.text = itemData.GetItemTypeName();

        itemDescription.gameObject.SetActive(true);
        itemDescription.text = itemData.itemDescription;

        if (itemData.itemType == ItemType.Active || itemData.itemType == ItemType.Passive)
        {
            if(itemData.itemEffectData!=null)
            {
                itemEffect.gameObject.SetActive(true);
                itemEffect.text = itemData.itemEffectData.effectDescription;
            }
            else
            {
                itemEffect.gameObject.SetActive(false);
            }
        }
        else
        {
            itemEffect.gameObject.SetActive(false);
        }

        if (itemData.itemType == ItemType.Active)
        {
            equipInfo.gameObject.SetActive(true);
            SetQuickItemInfo.gameObject.SetActive(true);
        }
        else
        {
            equipInfo.gameObject.SetActive(false);
            SetQuickItemInfo.gameObject.SetActive(false);
        }

        CancelQuickItemInfo.gameObject.SetActive(false);

        sellMoney.text = "售出价格:" + itemData.itemSellPrice;
        sellMoney.gameObject.SetActive(true);
    }

    public void ShowQuickItemToolTip(bool show, RectTransform targetRect, InventoryItem itemToShow)
    {
        base.ShowToolTip(show, targetRect);
        if (show == false || itemToShow == null)
        {
            return;
        }
        ItemDataSO itemData = itemToShow.ItemData;

        itemName.gameObject.SetActive(true);
        itemName.text = itemData.itemDisplayName;

        itemType.gameObject.SetActive(true);
        itemType.text = itemData.GetItemTypeName();

        itemDescription.gameObject.SetActive(true);
        itemDescription.text = itemData.itemDescription;

        if (itemData.itemType == ItemType.Active || itemData.itemType == ItemType.Passive)
        {
            if (itemData.itemEffectData != null)
            {
                itemEffect.gameObject.SetActive(true);
                itemEffect.text = itemData.itemEffectData.effectDescription;
            }
        }
        else
        {
            itemEffect.gameObject.SetActive(false);
        }

        equipInfo.gameObject.SetActive(false);
        SetQuickItemInfo.gameObject.SetActive(false);

        CancelQuickItemInfo.gameObject.SetActive(true);

        sellMoney.gameObject.SetActive(false);
    }

    public void ShowItemToolTip(bool show, RectTransform targetRect, InventoryItem itemToShow)
    {
        base.ShowToolTip(show, targetRect);
        if (show == false || itemToShow == null)
        {
            return;
        }
        ItemDataSO itemData = itemToShow.ItemData;

        itemName.gameObject.SetActive(true);
        itemName.text = itemData.itemDisplayName;

        itemType.gameObject.SetActive(true);
        itemType.text = itemData.GetItemTypeName();

        itemDescription.gameObject.SetActive(true);
        itemDescription.text = itemData.itemDescription;

        if (itemData.itemType == ItemType.Active || itemData.itemType == ItemType.Passive)
        {
            if (itemData.itemEffectData != null)
            {
                itemEffect.gameObject.SetActive(true);
                itemEffect.text = itemData.itemEffectData.effectDescription;
            }
        }
        else
        {
            itemEffect.gameObject.SetActive(false);
        }

        equipInfo.gameObject.SetActive(false);
        SetQuickItemInfo.gameObject.SetActive(false);

        CancelQuickItemInfo.gameObject.SetActive(false);

        sellMoney.text = "售出价格:" + itemData.itemSellPrice;
        sellMoney.gameObject.SetActive(true);
    }

    public void HideItemToolTip()
    {
        base.ShowToolTip(false,null);
    }
}
