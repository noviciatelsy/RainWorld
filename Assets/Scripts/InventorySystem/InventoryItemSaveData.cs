using System;

[Serializable]
public class InventoryItemSaveData
{
    public string itemSaveID = "";
    public string runtimeSaveID = "";

    public int topLeftX = 0;
    public int topLeftY = 0;

    public ItemRotateState rotateState = ItemRotateState.Rotate0;
}