using System.Collections.Generic;
using System;

[Serializable]
public class InventorySaveData
{
    public string inventorySaveID = "";

    public int columnCount = 7;
    public int maxInventorySize = 56;

    public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();

    public void EnsureDataValid()
    {
        if (items == null)
        {
            items = new List<InventoryItemSaveData>();
        }
    }

    public void Clear()
    {
        if (items == null)
        {
            items = new List<InventoryItemSaveData>();
        }

        items.Clear();
    }
}