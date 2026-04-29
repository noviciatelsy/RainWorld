using UnityEngine;

public class InventoryPlayer : InventoryBase
{
    private Player player;
    [SerializeField] private ItemDataSO test_1;
    [SerializeField] private ItemDataSO test_2;
    [SerializeField] private ItemDataSO test_3;
    [SerializeField] private ItemDataSO test_4;
    [SerializeField] private ItemDataSO test_5; 
    [SerializeField] private ItemDataSO test_6;
    [SerializeField] private ItemDataSO test_7;
    protected override void Awake()
    {
        base.Awake();
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddItem(test_1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AddItem(test_2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AddItem(test_3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            AddItem(test_4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            AddItem(test_5);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            AddItem(test_6);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            AddItem(test_7);
        }

    }

    protected override void OnItemPlaced(InventoryItem item)
    {
        item?.AddItemEffect(player);
    }

    protected override void OnItemRemoved(InventoryItem item)
    {
        item?.RemoveItemEffect();
    }
}