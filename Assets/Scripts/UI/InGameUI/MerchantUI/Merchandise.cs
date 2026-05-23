using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Merchandise : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemCostText;
    [SerializeField] private Goods goods;

    private ItemDataSO itemData;

    public void SetItemData(ItemDataSO itemData)
    {
        this.itemData = itemData;
        goods.SetItemData(itemData);
        itemCostText.text=itemData.itemBuyPrice.ToString();
    }
}
