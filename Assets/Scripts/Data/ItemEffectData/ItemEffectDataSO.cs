using UnityEngine;

public class ItemEffectDataSO : ScriptableObject
{
    [TextArea] public string effectDescription; // 效果描述
    [TextArea] public string holdingHintMessage="长按左键丢弃该物品";

    protected Player player;

    public virtual void Subscribe(Player player)
    {
        this.player = player; // 获取player
    }

    public virtual void Unsubscribe()
    {
        player = null; // 还原player
    }


    public virtual void StartHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        GlobalUI.Instance.hintMessageUI.ShowLongTimeMessage(holdingHintMessage);
    }


    public virtual void EndHoldingItem(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        GlobalUI.Instance.hintMessageUI.StopLongTimeMessage();
    }


    public virtual bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        return false;
    }


    public virtual bool SecondaryUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        inventoryPlayer.DropItem(item.ItemData);
        return true;
    }
}