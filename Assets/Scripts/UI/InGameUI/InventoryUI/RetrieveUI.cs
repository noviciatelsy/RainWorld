using UnityEngine;

public class RetrieveUI : MonoBehaviour
{
    [Header("Grids")]
    [SerializeField] private InventoryGridUI playerInventoryGrid;
    [SerializeField] private InventoryGridUI lostInventoryGrid;

    [Header("Refs")]
    private DraggedItemUI draggedItemUI;

    private InventoryPlayer playerInventory;
    private InventoryBase currentLostInventory;

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

    public void Open(InventoryBase lostInventory)
    {
        if (lostInventory == null)
        {
            Debug.LogWarning("打开 RetrieveUI 失败：lostInventory 为空。");
            return;
        }

        Player currentPlayer = PlayerManager.Instance != null ? PlayerManager.Instance.CurrentPlayer : null;

        if (currentPlayer == null)
        {
            Debug.LogWarning("打开 RetrieveUI 失败：当前没有玩家。");
            return;
        }

        playerInventory = currentPlayer.GetComponent<InventoryPlayer>();

        if (playerInventory == null)
        {
            Debug.LogWarning("打开 RetrieveUI 失败：玩家身上没有 InventoryPlayer。");
            return;
        }

        currentLostInventory = lostInventory;

        gameObject.SetActive(true);

        playerInventoryGrid.SetInventory(playerInventory);
        lostInventoryGrid.SetInventory(currentLostInventory);
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

        if (lostInventoryGrid != null)
        {
            lostInventoryGrid.ClearInventoryBinding();
        }

        playerInventory = null;
        currentLostInventory = null;

        gameObject.SetActive(false);
    }
}