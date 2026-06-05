using System.Collections.Generic;
using System;

[Serializable]
public class PlayerInventorySaveData
{
    public InventorySaveData inventorySaveData = new InventorySaveData();

    // 快捷栏里每一格引用的 InventoryItem.runtimeSaveID
    public List<string> quickItemRuntimeIDs = new List<string>();

    // 当前手持物品引用的 InventoryItem.runtimeSaveID
    public string holdingItemRuntimeID = "";

    public int money = 0;

    public void EnsureDataValid()
    {
        if (inventorySaveData == null)
        {
            inventorySaveData = new InventorySaveData();
        }

        inventorySaveData.EnsureDataValid();

        if (quickItemRuntimeIDs == null)
        {
            quickItemRuntimeIDs = new List<string>();
        }
    }
}