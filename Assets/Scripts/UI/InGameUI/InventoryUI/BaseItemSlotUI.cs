using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BaseItemSlotUI : MonoBehaviour, IPointerDownHandler,IPointerEnterHandler,IPointerExitHandler
{
    [SerializeField] private Image slotStateImage; // 显示该格可用状态的Image

    [Header("State Colors")]
    [SerializeField] private Color enablePlaceColor = new Color(0.45f, 1f, 0.55f, 0.45f);
    [SerializeField] private Color enableReplaceColor = new Color(1f, 0.85f, 0.25f, 0.55f);
    [SerializeField] private Color disablePlaceColor = new Color(1f, 0.25f, 0.25f, 0.55f);

    private int slotIndex; // UI的槽位序号
    public InventoryItem itemInSlot; // 槽内物品的Inventory_Item对象

    private InventoryGridUI ownerGridUI;

    protected virtual void Awake()
    {
        if (slotStateImage != null)
        {
            slotStateImage.raycastTarget = false;

            RectTransform stateRt = slotStateImage.transform as RectTransform;

            if (stateRt != null)
            {
                stateRt.anchorMin = Vector2.zero;
                stateRt.anchorMax = Vector2.one;
                stateRt.offsetMin = Vector2.zero;
                stateRt.offsetMax = Vector2.zero;
            }

            // 保证状态图层在当前格子的最上面
            slotStateImage.transform.SetAsLastSibling();

            HideSlotState();
        }
    }

    public void BindItemIndex(int index)
    {
        slotIndex = index;
    }

    public void SetOwnerGridUI(InventoryGridUI ownerGridUI)
    {
        this.ownerGridUI = ownerGridUI;
    }

    public void SetItemInSlot(InventoryItem item)
    {
        itemInSlot = item;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (ownerGridUI == null)
        {
            return;
        }

        ownerGridUI.OnSlotPointerDown(slotIndex, eventData);
    }

    public void SetSlotState(SlotState slotState)
    {
        if (slotStateImage == null)
        {
            return;
        }

        switch (slotState)
        {
            case SlotState.None:
                HideSlotState();
                break;

            case SlotState.EnablePlace:
                ShowSlotState(enablePlaceColor);
                break;

            case SlotState.EnableReplace:
                ShowSlotState(enableReplaceColor);
                break;

            case SlotState.DisablePlace:
                ShowSlotState(disablePlaceColor);
                break;
        }
    }

    private void ShowSlotState(Color color)
    {
        slotStateImage.gameObject.SetActive(true);
        slotStateImage.enabled = true;
        slotStateImage.color = color;
    }

    private void HideSlotState()
    {
        slotStateImage.enabled = false;
        slotStateImage.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
    }
}