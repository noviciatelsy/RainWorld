using UnityEngine;

public class BackpackUI : MonoBehaviour
{
    [SerializeField] private InventoryGridUI playerInventoryGrid;
    private DraggedItemUI draggedItemUI;

    private InventoryPlayer inventoryPlayer;

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

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCurrentPlayerChanged += HandleCurrentPlayerChanged;

            HandleCurrentPlayerChanged(PlayerManager.Instance.CurrentPlayer);
        }
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCurrentPlayerChanged -= HandleCurrentPlayerChanged;
        }

        if (draggedItemUI != null && draggedItemUI.IsDragging)
        {
            draggedItemUI.TryReturnToSource();
        }

        if (playerInventoryGrid != null)
        {
            playerInventoryGrid.ClearInventoryBinding();
        }

        inventoryPlayer = null;
    }

    private void HandleCurrentPlayerChanged(Player player)
    {
        if (player == null)
        {
            inventoryPlayer = null;

            if (playerInventoryGrid != null)
            {
                playerInventoryGrid.ClearInventoryBinding();
            }

            return;
        }

        inventoryPlayer = player.GetComponent<InventoryPlayer>();

        if (playerInventoryGrid != null)
        {
            playerInventoryGrid.SetInventory(inventoryPlayer);
        }
    }
}