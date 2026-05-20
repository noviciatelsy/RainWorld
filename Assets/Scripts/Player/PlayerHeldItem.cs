using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHeldItem : MonoBehaviour
{
    private MainInput mainInput;
    private InventoryPlayer inventoryPlayer;

    [Header("HoldItem")]
    [SerializeField] private SpriteRenderer holdingItemSprite;
    [SerializeField] private float baseScaleMultiplier = 4;

    private void Awake()
    {
        inventoryPlayer = GetComponent<InventoryPlayer>();

        if (InputManager.Instance != null)
        {
            mainInput = InputManager.Instance.mainInput;
        }
    }

    private void OnEnable()
    {
        SubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    private void SubscribeInput()
    {
        if (mainInput == null)
        {
            return;
        }

        mainInput.Player.EquipQuickItem_1.performed += OnEquipQuickItem1Performed;
        mainInput.Player.EquipQuickItem_2.performed += OnEquipQuickItem2Performed;
        mainInput.Player.EquipQuickItem_3.performed += OnEquipQuickItem3Performed;
        mainInput.Player.EquipQuickItem_4.performed += OnEquipQuickItem4Performed;

        mainInput.Player.UseHoldingItem.performed += OnUseHoldingItemPerformed;
    }

    private void UnsubscribeInput()
    {
        if (mainInput == null)
        {
            return;
        }

        mainInput.Player.EquipQuickItem_1.performed -= OnEquipQuickItem1Performed;
        mainInput.Player.EquipQuickItem_2.performed -= OnEquipQuickItem2Performed;
        mainInput.Player.EquipQuickItem_3.performed -= OnEquipQuickItem3Performed;
        mainInput.Player.EquipQuickItem_4.performed -= OnEquipQuickItem4Performed;

        mainInput.Player.UseHoldingItem.performed -= OnUseHoldingItemPerformed;
    }

    private void OnEquipQuickItem1Performed(InputAction.CallbackContext context)
    {
        TryHoldQuickItem(0);
    }

    private void OnEquipQuickItem2Performed(InputAction.CallbackContext context)
    {
        TryHoldQuickItem(1);
    }

    private void OnEquipQuickItem3Performed(InputAction.CallbackContext context)
    {
        TryHoldQuickItem(2);
    }

    private void OnEquipQuickItem4Performed(InputAction.CallbackContext context)
    {
        TryHoldQuickItem(3);
    }

    private void OnUseHoldingItemPerformed(InputAction.CallbackContext context)
    {
        if (inventoryPlayer == null)
        {
            return;
        }

        inventoryPlayer.UseHoldingItem();
    }

    private void TryHoldQuickItem(int quickSlotIndex)
    {
        if (inventoryPlayer == null)
        {
            return;
        }

        inventoryPlayer.TryHoldQuickItem(quickSlotIndex);
    }

    public void StartHoldingItem(ItemDataSO itemToHold)
    {
        if (holdingItemSprite == null)
        {
            return;
        }

        if (itemToHold == null || itemToHold.itemIcon == null)
        {
            EndHoldingItem();
            return;
        }

        holdingItemSprite.sprite = itemToHold.itemIcon;
        holdingItemSprite.enabled = true;

        BackpackItemDataSO backpackItemData = itemToHold.backpackItemData;

        if (backpackItemData == null)
        {
            holdingItemSprite.transform.localScale = Vector3.one;
            return;
        }

        float width = Mathf.Max(1, backpackItemData.imageSize.x);
        float height = Mathf.Max(1, backpackItemData.imageSize.y);

        float bonusScaleMultiplier=1;
        if (width >= height)
        {
            bonusScaleMultiplier = Mathf.Lerp(height,width, 0.5f);
        }
        else
        {
            bonusScaleMultiplier = Mathf.Lerp(width, height, 0.5f);
        }
        if(width==1&&height==1)
        {
            bonusScaleMultiplier = 2;
        }

        float safeBaseScaleMultiplier = Mathf.Max(1f, baseScaleMultiplier);

        holdingItemSprite.transform.localScale = new Vector3
        (
            1 / (bonusScaleMultiplier * safeBaseScaleMultiplier),
            1 / (bonusScaleMultiplier * safeBaseScaleMultiplier),
            1f
        );

        //holdingItemSprite.transform.localScale = new Vector3
        //(
        //  1 / safeBaseScaleMultiplier,
        // 1 / safeBaseScaleMultiplier,
        // 1f
        //);

    }

    public void EndHoldingItem()
    {
        if (holdingItemSprite == null)
        {
            return;
        }

        holdingItemSprite.enabled = false;
        holdingItemSprite.sprite = null;
        holdingItemSprite.transform.localScale = Vector3.one;
    }
}