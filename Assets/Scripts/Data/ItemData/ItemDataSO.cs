using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ItemDataSO : ScriptableObject
{
    public string saveID { get; private set; }

    [Header("Item Details")]

    public string itemDisplayName;
    [TextArea] public string itemDescription;
    public Sprite itemIcon;
    public ItemEffectDataSO itemEffectData;
    public BackpackItemDataSO backpackItemData;
    public ItemType itemType; // 物品类型
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Merchant details")]
    public int itembuyPrice = 0; // 物品购买价格
    public int itemSellPrice = 0; // 物品售出价格

    public string GetItemTypeName()
    {
        switch (itemType)
        {
            case ItemType.Active:
                return "主动道具";
            case ItemType.Passive:
                return "被动道具";
            case ItemType.Treasure:
                return "财宝";
            case ItemType.Note:
                return "未知情报";
            default:
                return "无";
        }

    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this); // ScriptableObject 资源在工程里的路径
        saveID = AssetDatabase.AssetPathToGUID(path); // 把这个 ScriptableObject 资源在工程里的 GUID，存进 saveID 这个字符串字段里
#endif
    }

}

