using UnityEngine;

public class LootUI : MonoBehaviour
{
    [Header("Grids")]
    [SerializeField] private InventoryGridUI playerInventoryGrid;
    [SerializeField] private InventoryGridUI lootInventoryGrid;

    [Header("Refs")]
    private DraggedItemUI draggedItemUI;

    private InventoryPlayer playerInventory;
    private InventoryBase currentLootInventory;

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
    }

    public void Open(InventoryBase lootInventory)
    {
        if (lootInventory == null)
        {
            Debug.LogWarning("打开 LootUI 失败：lootInventory 为空。");
            return;
        }

        Player currentPlayer = PlayerManager.Instance != null ? PlayerManager.Instance.CurrentPlayer : null;

        if (currentPlayer == null)
        {
            Debug.LogWarning("打开 LootUI 失败：当前没有玩家。");
            return;
        }

        playerInventory = currentPlayer.GetComponent<InventoryPlayer>();

        if (playerInventory == null)
        {
            Debug.LogWarning("打开 LootUI 失败：玩家身上没有 InventoryPlayer。");
            return;
        }

        currentLootInventory = lootInventory;

        gameObject.SetActive(true);

        playerInventoryGrid.SetInventory(playerInventory);
        lootInventoryGrid.SetInventory(currentLootInventory);
    }

    public void Close()
    {
        if (draggedItemUI != null && draggedItemUI.IsDragging)
        {
            draggedItemUI.TryReturnToSource();
        }

        if (playerInventoryGrid != null)
        {
            playerInventoryGrid.ClearInventoryBinding();
        }

        if (lootInventoryGrid != null)
        {
            lootInventoryGrid.ClearInventoryBinding();
        }

        playerInventory = null;
        currentLootInventory = null;

        gameObject.SetActive(false);
    }
}