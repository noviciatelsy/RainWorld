using UnityEngine;

public class BackpackUI : MonoBehaviour
{
    [SerializeField] private InventoryGridUI playerInventoryGrid;
    private DraggedItemUI draggedItemUI;

    private InventoryPlayer playerInventory;

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

    public void Open()
    {
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
        gameObject.SetActive(true);
        playerInventoryGrid.SetInventory(playerInventory);
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
        playerInventory = null;
        gameObject.SetActive(false);
    }

}