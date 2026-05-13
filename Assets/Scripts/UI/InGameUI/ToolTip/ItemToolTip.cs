using System.Collections;
using System.Collections.Generic;
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
        ItemDataSO itemData=itemToShow.ItemData;

        itemName.gameObject.SetActive(true);
        itemName.text=itemData.itemDisplayName;

        itemType.gameObject.SetActive(true);
        
        itemDescription.gameObject.SetActive(true);
    }

}
