using TMPro;
using UnityEngine;

public class ItemToolTip : BaseToolTip
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private TextMeshProUGUI itemEffect;
    [SerializeField] private TextMeshProUGUI equipInfo;
    [SerializeField] private TextMeshProUGUI setQuickItemInfo;
    [SerializeField] private TextMeshProUGUI cancelQuickItemInfo;
    [SerializeField] private TextMeshProUGUI consumableItemInfo;
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

        if (itemData.itemEffectData != null)
        {
            itemEffect.gameObject.SetActive(true);
            itemEffect.text = itemData.itemEffectData.effectDescription;
        }
        else
        {
            itemEffect.gameObject.SetActive(false);
        }

        equipInfo.gameObject.SetActive(true);
        setQuickItemInfo.gameObject.SetActive(true);

        if (itemData.itemType == ItemType.Active)
        {
            consumableItemInfo.gameObject.SetActive(true);
            ActiveItemDataSO activeItemData = itemData as ActiveItemDataSO;
            if (activeItemData.isConsumable)
            {
                consumableItemInfo.text = "消耗品";
            }
            else
            {
                consumableItemInfo.text = "非消耗品";
            }
        }
        else
        {
            setQuickItemInfo.gameObject.SetActive(false);
        }

        cancelQuickItemInfo.gameObject.SetActive(false);

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


        if (itemData.itemEffectData != null)
        {
            itemEffect.gameObject.SetActive(true);
            itemEffect.text = itemData.itemEffectData.effectDescription;
        }
        else
        {
            itemEffect.gameObject.SetActive(false);
        }

        equipInfo.gameObject.SetActive(false);
        setQuickItemInfo.gameObject.SetActive(false);

        cancelQuickItemInfo.gameObject.SetActive(true);

        consumableItemInfo.gameObject.SetActive(true);
        if (itemData.itemType == ItemType.Active)
        {
            consumableItemInfo.gameObject.SetActive(true);
            ActiveItemDataSO activeItemData = itemData as ActiveItemDataSO;
            if (activeItemData.isConsumable)
            {
                consumableItemInfo.text = "消耗品";
            }
            else
            {
                consumableItemInfo.text = "非消耗品";
            }
        }
        else
        {
            setQuickItemInfo.gameObject.SetActive(false);
        }


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


        if (itemData.itemEffectData != null)
        {
            itemEffect.gameObject.SetActive(true);
            itemEffect.text = itemData.itemEffectData.effectDescription;
        }
        else
        {
            itemEffect.gameObject.SetActive(false);
        }

        equipInfo.gameObject.SetActive(false);
        setQuickItemInfo.gameObject.SetActive(false);

        if (itemData.itemType == ItemType.Active)
        {
            consumableItemInfo.gameObject.SetActive(true);
            ActiveItemDataSO activeItemData = itemData as ActiveItemDataSO;
            if (activeItemData.isConsumable)
            {
                consumableItemInfo.text = "消耗品";
            }
            else
            {
                consumableItemInfo.text = "非消耗品";
            }
        }
        else
        {
            consumableItemInfo.gameObject.SetActive(false);
        }

        cancelQuickItemInfo.gameObject.SetActive(false);



        sellMoney.text = "售出价格:" + itemData.itemSellPrice;
        sellMoney.gameObject.SetActive(true);
    }

    public void HideItemToolTip()
    {
        base.ShowToolTip(false, null);
    }
}
