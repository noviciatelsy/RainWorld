using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HudQuickItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIconImage;

    public InventoryItem itemInSlot; 

    private RectTransform itemIconRect;

    private void Awake()
    {
        if (itemIconImage != null)
        {
            itemIconImage.raycastTarget = false;
            itemIconImage.enabled = false;

            itemIconRect = itemIconImage.transform as RectTransform;
        }
    }


    public void UpdateItem(InventoryItem itemInSlot)
    {
        this.itemInSlot = itemInSlot;

        if (itemIconImage == null)
        {
            return;
        }

        if (itemInSlot != null && itemInSlot.ItemData != null)
        {
            itemIconImage.enabled = true;
            itemIconImage.sprite = itemInSlot.ItemData.itemIcon;

            UpdateIconScaleByBackpackItemData(itemInSlot.ItemData);
        }
        else
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;

            ResetIconScale();
        }
    }

    private void UpdateIconScaleByBackpackItemData(ItemDataSO itemData)
    {
        if (itemIconRect == null)
        {
            return;
        }

        if (itemData == null || itemData.backpackItemData == null)
        {
            ResetIconScale();
            return;
        }

        Vector2Int imageSize = itemData.backpackItemData.imageSize;

        if (imageSize.x <= 0 || imageSize.y <= 0)
        {
            ResetIconScale();
            return;
        }

        float width = imageSize.x;
        float height = imageSize.y;

        float maxSide = Mathf.Max(width, height);

        float scaleX = width / maxSide;
        float scaleY = height / maxSide;

        itemIconRect.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private void ResetIconScale()
    {
        if (itemIconRect == null)
        {
            return;
        }

        itemIconRect.localScale = Vector3.one;
    }


}
