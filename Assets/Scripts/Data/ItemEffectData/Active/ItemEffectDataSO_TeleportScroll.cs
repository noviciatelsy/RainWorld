using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Setup/ItemEffect Data/TeleportScroll", fileName = "ItemEffectData_TeleportScroll")]
public class ItemEffectDataSO_TeleportScroll : ItemEffectDataSO
{
    [SerializeField] private Vector3 targetPosition = Vector3.zero;

    public override bool MainUse(InventoryItem item, InventoryPlayer inventoryPlayer)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        if (inventoryPlayer == null)
        {
            Debug.LogWarning("回城卷轴使用失败：inventoryPlayer 为空。");
            return false;
        }

        if (player == null)
        {
            Debug.LogWarning("回城卷轴使用失败：player 为空。");
            return false;
        }

        PlayerControl playerControl = player.GetComponent<PlayerControl>();

        if (playerControl == null)
        {
            Debug.LogWarning("回城卷轴使用失败：找不到 PlayerControl。");
            return false;
        }

        LoseRandomItem(item, inventoryPlayer);

        if (GlobalUI.Instance == null || GlobalUI.Instance.fadeScreenUI == null)
        {
            Debug.LogWarning("回城卷轴使用失败：找不到 GlobalUI 或 fadeScreenUI。");
            return false;
        }
        AudioManager.Instance.PlaySFX("UseItemTeleportScrollSFX");
        GlobalUI.Instance.fadeScreenUI.PlayRoomSwitchFade(() =>
        {
            PlayerManager.Instance.SetPendingPlayerShowUpPosition(targetPosition, playerControl.facingDir);
            SceneSwitchManager.Instance.SwitchToScene(SceneType.Base);
        });

        return true;
    }

    private void LoseRandomItem(InventoryItem itemItself, InventoryPlayer inventoryPlayer)
    {
        if (inventoryPlayer == null)
        {
            return;
        }

        List<InventoryItem> candidateItems = GetLoseCandidateItems(itemItself, inventoryPlayer);

        if (candidateItems.Count <= 0)
        {
            Debug.Log("回城卷轴没有可丢失的物品。");
            return;
        }

        int randomIndex = Random.Range(0, candidateItems.Count);
        InventoryItem itemToLose = candidateItems[randomIndex];

        if (itemToLose == null || itemToLose.ItemData == null)
        {
            return;
        }

        // 如果丢失的是当前手持物品，先取消手持
        if (inventoryPlayer.GetHoldingItem() == itemToLose)
        {
            inventoryPlayer.ClearHoldingItem();
        }

        // 如果丢失的是快捷栏里的物品，清掉快捷栏引用
        inventoryPlayer.ClearQuickItem(itemToLose);

        bool removed = inventoryPlayer.RemoveItem(itemToLose);

        if (removed)
        {
            inventoryPlayer.ValidateQuickItems(null);
            inventoryPlayer.ValidateHoldingItem(null);

            Debug.Log($"回城卷轴发动：随机丢失了 {itemToLose.ItemData.itemDisplayName}。");
        }
    }

    private List<InventoryItem> GetLoseCandidateItems(InventoryItem itemItself, InventoryPlayer inventoryPlayer)
    {
        List<InventoryItem> result = new List<InventoryItem>();

        if (inventoryPlayer == null)
        {
            return result;
        }

        for (int i = 0; i < inventoryPlayer.inventoryItems.Count; i++)
        {
            InventoryItem item = inventoryPlayer.inventoryItems[i];

            if (!CanLoseItem(item, itemItself))
            {
                continue;
            }

            result.Add(item);
        }

        return result;
    }

    private bool CanLoseItem(InventoryItem item, InventoryItem itemItself)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        // 只排除“正在被使用的那个 InventoryItem 实例”
        // 不排除同类 ItemData 的其他回城卷轴
        if (item == itemItself)
        {
            return false;
        }

        return true;
    }
}