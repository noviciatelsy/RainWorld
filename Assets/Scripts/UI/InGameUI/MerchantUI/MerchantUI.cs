using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MerchantUI : MonoBehaviour
{
    [Header("Grids")]
    [SerializeField] private InventoryGridUI playerInventoryGrid;
    private PlayerBackpack playerBackpack;

    [Header("Refs")]
    private DraggedItemUI draggedItemUI;
    private InventoryPlayer playerInventory;
    private GoodsShelfUI goodsShelfUI;

    private void Awake()
    {
        if (draggedItemUI == null)
        {
            InGameUI inGameUI = GetComponentInParent<InGameUI>();

            if (inGameUI != null)
            {
                draggedItemUI = inGameUI.draggedItemUI;
            }
        }
        playerBackpack = GetComponentInChildren<PlayerBackpack>();
        goodsShelfUI= GetComponentInChildren<GoodsShelfUI>();

    }

    public void Open()
    {

        Player currentPlayer = PlayerManager.Instance != null ? PlayerManager.Instance.CurrentPlayer : null;

        if (currentPlayer == null)
        {
            return;
        }

        playerInventory = currentPlayer.GetComponent<InventoryPlayer>();

        if (playerInventory == null)
        {
            return;
        }

        gameObject.SetActive(true);

        playerInventoryGrid.SetInventory(playerInventory);
        playerBackpack.SetInventory(playerInventory);
        goodsShelfUI.SetInventory(playerInventory);

    }

    public void Close()
    {
        if (draggedItemUI != null && draggedItemUI.IsDragging)
        {
            draggedItemUI.TryDropItem();
        }

        if (playerInventoryGrid != null)
        {
            playerInventoryGrid.SetInventory(null);
        }


        playerBackpack.SetInventory(null);
        goodsShelfUI.SetInventory(null);
        playerInventory = null;


        gameObject.SetActive(false);
    }
}
