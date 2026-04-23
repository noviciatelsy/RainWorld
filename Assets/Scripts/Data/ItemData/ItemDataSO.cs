using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ItemDataSO : ScriptableObject
{
    public string saveID { get; private set; }

    [Header("Item Details")]
    public string itemDisplayName;
    public string itemDescription;
    public Sprite itemIcon;
    public ItemEffectDataSO effectData;
    public BackpackItemDataSO backpackItemData;
    public ItemType itemType; // 物品类型
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Merchant details")]
    public int itembuyPrice = 0; // 物品购买价格
    public int itemSellPrice = 0; // 物品售出价格

    private void OnValidate()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this); // ScriptableObject 资源在工程里的路径
        saveID = AssetDatabase.AssetPathToGUID(path); // 把这个 ScriptableObject 资源在工程里的 GUID，存进 saveID 这个字符串字段里
#endif
    }

}

