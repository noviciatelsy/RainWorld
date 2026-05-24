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

        if (goods != null)
        {
            goods.SetItemData(itemData);
        }

        if (itemCostText != null)
        {
            itemCostText.text = itemData != null ? itemData.itemBuyPrice.ToString() : string.Empty;
        }
    }
}