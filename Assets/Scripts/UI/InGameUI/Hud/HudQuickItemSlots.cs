using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HudQuickItemSlots : MonoBehaviour
{
    private HudQuickItemSlotUI[] hudQuickItemSlotUIs;
    private InventoryPlayer playerInventory;

    private void Awake()
    {
        hudQuickItemSlotUIs = GetComponentsInChildren<HudQuickItemSlotUI>(true);

        Array.Sort(
            hudQuickItemSlotUIs,
            (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex())
        );
    }

    private void OnEnable()
    {
        TrySubscribeToPlayer(PlayerManager.Instance.TryGetCurrentPlayer());
        PlayerManager.Instance.OnPlayerRegistered += TrySubscribeToPlayer;
    }

    private void OnDisable()
    {
        PlayerManager.Instance.OnPlayerRegistered -= TrySubscribeToPlayer;
        if (playerInventory != null)
        {
            playerInventory.onQuickItemsChange -= UpdateQuickItemSlots;
        }
    }

    private void TrySubscribeToPlayer(Player player)
    {
        if (player == null)
        {
            return;
        }
        InventoryPlayer playerInventory= player.GetComponent<InventoryPlayer>();
        if(playerInventory != null)
        {
            if(this.playerInventory != null)
            {
                this.playerInventory.onQuickItemsChange -= UpdateQuickItemSlots;
            }
            this.playerInventory = playerInventory;
            this.playerInventory.onQuickItemsChange += UpdateQuickItemSlots;
            UpdateQuickItemSlots();
        }
    }

    private void UpdateQuickItemSlots()
    {
        for (int i = 0; i < hudQuickItemSlotUIs.Length; i++)
        {
            InventoryItem itemInSlot = null;

            if (playerInventory != null && i < playerInventory.quickItemSlotList.Count)
            {
                itemInSlot = playerInventory.GetQuickItem(i);
            }

            hudQuickItemSlotUIs[i].UpdateItem(itemInSlot);
        }
    }
}
