using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHeldItem : MonoBehaviour
{
    private MainInput mainInput;
    private InventoryPlayer inventoryPlayer;

    [Header("HoldItem")]
    [SerializeField] private SpriteRenderer holdingItemSprite;
    [SerializeField] private float baseScaleMultiplier = 4;

    [Header("Use Input")]
    [SerializeField] private float secondaryUseHoldThreshold = 0.35f;

    private bool isPressingUseHoldingItem;
    private bool hasTriggeredSecondaryUseHoldingItem;
    private float useHoldingItemStartTime;

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
        ResetUseHoldingItemInputState();
    }

    private void Update()
    {
        TryTriggerSecondaryUseByHolding();
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

        mainInput.Player.UseHoldingItem.started += OnUseHoldingItemStarted;
        mainInput.Player.UseHoldingItem.canceled += OnUseHoldingItemCanceled;
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

        mainInput.Player.UseHoldingItem.started -= OnUseHoldingItemStarted;
        mainInput.Player.UseHoldingItem.canceled -= OnUseHoldingItemCanceled;
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

    private void OnUseHoldingItemStarted(InputAction.CallbackContext context)
    {
        isPressingUseHoldingItem = true;
        hasTriggeredSecondaryUseHoldingItem = false;
        useHoldingItemStartTime = Time.unscaledTime;
    }

    private void OnUseHoldingItemCanceled(InputAction.CallbackContext context)
    {
        if (!isPressingUseHoldingItem)
        {
            return;
        }

        bool shouldTriggerMainUse = !hasTriggeredSecondaryUseHoldingItem;

        ResetUseHoldingItemInputState();

        if (!shouldTriggerMainUse)
        {
            return;
        }

        if (inventoryPlayer == null)
        {
            return;
        }

        inventoryPlayer.TryMainUseHoldingItem();
    }

    private void TryTriggerSecondaryUseByHolding()
    {
        if (!isPressingUseHoldingItem)
        {
            return;
        }

        if (hasTriggeredSecondaryUseHoldingItem)
        {
            return;
        }

        if (inventoryPlayer == null)
        {
            return;
        }

        float holdDuration = Time.unscaledTime - useHoldingItemStartTime;

        if (holdDuration < secondaryUseHoldThreshold)
        {
            return;
        }

        hasTriggeredSecondaryUseHoldingItem = true;
        inventoryPlayer.TrySecondaryUseHoldingItem();
    }

    private void ResetUseHoldingItemInputState()
    {
        isPressingUseHoldingItem = false;
        hasTriggeredSecondaryUseHoldingItem = false;
        useHoldingItemStartTime = 0f;
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

        float bonusScaleMultiplier = Mathf.Sqrt(width * height);

        float safeBaseScaleMultiplier = Mathf.Max(1f, baseScaleMultiplier);

        holdingItemSprite.transform.localScale = new Vector3
        (
            1 / (bonusScaleMultiplier * safeBaseScaleMultiplier),
            1 / (bonusScaleMultiplier * safeBaseScaleMultiplier),
            1f
        );
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