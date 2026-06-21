using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鼹鼠偷取结算：偷取结束后延迟从玩家扣除道具，并生成飞向鼹鼠的图标。
/// </summary>
[DisallowMultipleComponent]
public class MoleStealController : MonoBehaviour, IMoleStealHandler
{
    [SerializeField] private Mole2D mole;
    [SerializeField] private Transform itemReceivePoint;

    [Header("Steal Timing")]
    [SerializeField] private float stealFlyDelay = 0.2f;

    [Header("Fly Visual")]
    [SerializeField] private float itemFlySpeed = 10f;
    [SerializeField] private float itemDisplayScale = 0.15f;
    [SerializeField] private Vector2 spawnWorldOffset = new Vector2(0f, 0.6f);

    private Coroutine pendingStealRoutine;

    private void Awake()
    {
        if (mole == null)
        {
            mole = GetComponent<Mole2D>();
        }

        if (itemReceivePoint == null && mole != null)
        {
            itemReceivePoint = mole.transform;
        }
    }

    public bool OnStealFinished(Mole2D owner, InventoryPlayer player)
    {
        if (owner == null || player == null)
        {
            return false;
        }

        if (pendingStealRoutine != null)
        {
            StopCoroutine(pendingStealRoutine);
        }

        pendingStealRoutine = StartCoroutine(StealFlyRoutine(player));
        return true;
    }

    private IEnumerator StealFlyRoutine(InventoryPlayer inventoryPlayer)
    {
        yield return new WaitForSeconds(stealFlyDelay);

        ItemDataSO stolenItem = TryRemoveRandomItem(inventoryPlayer);
        if (stolenItem == null || stolenItem.itemIcon == null)
        {
            pendingStealRoutine = null;
            yield break;
        }

        Vector3 spawnPosition = inventoryPlayer.transform.position + (Vector3)spawnWorldOffset;
        Transform target = itemReceivePoint != null ? itemReceivePoint : mole.transform;

        MoleStolenItemFly.Spawn(
            stolenItem.itemIcon,
            spawnPosition,
            target,
            itemFlySpeed,
            itemDisplayScale
        );

        EnemyIntelligenceUnlockUtility.TryUnlockByName(EnemyIntelligenceNames.MolePrank);

        pendingStealRoutine = null;
    }

    private static ItemDataSO TryRemoveRandomItem(InventoryPlayer inventory)
    {
        if (inventory == null || inventory.inventoryItems == null || inventory.inventoryItems.Count <= 0)
        {
            return null;
        }

        List<InventoryItem> items = inventory.inventoryItems;

        for (int attempt = 0; attempt < items.Count; attempt++)
        {
            int index = Random.Range(0, items.Count);
            InventoryItem item = items[index];

            if (item == null || item.ItemData == null)
            {
                continue;
            }

            ItemDataSO data = item.ItemData;

            if (inventory.holdingItem == item)
            {
                inventory.ClearHoldingItem();
            }

            inventory.ClearQuickItem(item);
            inventory.RemoveItem(item);
            inventory.ValidateQuickItems(null);
            inventory.ValidateHoldingItem(null);
            return data;
        }

        return null;
    }
}
