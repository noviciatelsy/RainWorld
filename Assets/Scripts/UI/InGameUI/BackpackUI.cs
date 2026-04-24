using UnityEngine;

public class BackpackUI : MonoBehaviour
{
    [SerializeField] private GameData itemImagePrefab;
    private ItemSlotUI[] itemSlots;
    private InventoryPlayer inventoryPlayer;

    private void Awake()
    {
        itemSlots = GetComponentsInChildren<ItemSlotUI>();
        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i].BindItemIndex(i);
        }
    }

    private void OnEnable()
    {
        GetInventoryPlayer(PlayerManager.Instance.CurrentPlayer);
        PlayerManager.Instance.OnPlayerRegistered += GetInventoryPlayer;
        UpdateItemImages();
        if (inventoryPlayer != null)
            inventoryPlayer.onInventoryChange += UpdateItemImages;
    }

    private void OnDisable()
    {
        PlayerManager.Instance.OnPlayerRegistered -= GetInventoryPlayer;
        if (inventoryPlayer != null)
            inventoryPlayer.onInventoryChange -= UpdateItemImages;
    }

    private void UpdateItemImages()
    {

    }

    private void GetInventoryPlayer(Player player)
    {
        Player currentPlayer = player;
        if (currentPlayer != null)
        {
            inventoryPlayer = currentPlayer.GetComponent<InventoryPlayer>();
            foreach (ItemSlotUI itemSlotUI in itemSlots)
            {
                itemSlotUI.SetInventory(inventoryPlayer);
            }
        }
    }
}
