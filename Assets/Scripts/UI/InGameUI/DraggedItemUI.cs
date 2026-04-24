using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DraggedItemUI : MonoBehaviour
{
    [Header("Refs")]
    private Canvas rootCanvas; // 主Canvas
    private Image draggedIconImage; // 跟随鼠标的图标

    public bool IsDragging { get; private set; } // bool锁，是否正在拖拽物品
    public InventoryItem draggedItem { get; private set; } // 拖拽时暂存的物品

    private RectTransform selfRt; // 自身的Rect

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        draggedIconImage = GetComponent<Image>();
        selfRt = transform as RectTransform;
        HideItem(); // 隐藏拖拽时的物品图标
    }

    private void Update()
    {
        if (!IsDragging) return;

        // 让图标跟随鼠标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            Input.mousePosition,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out var mousePosition
        );
        selfRt.anchoredPosition = mousePosition;

        if (Input.GetMouseButtonDown(1)) // 右键按下
        {
            RotateDraggedItem(); // 旋转物品
        }
    }

    public void BeginDrag(InventoryItem item) // 开始拖拽
    {
        draggedItem = item; // 暂存该物品
        IsDragging = true; // 开启bool锁
        ShowItem();
    }

    public void EndDrag() // 结束拖拽
    {
        HideItem();
        draggedItem = null; // 清空暂存的物品
        IsDragging = false;
    }

    private void HideItem()
    {
        draggedIconImage.enabled = false; // 禁用图标（使其不可见）
        selfRt.sizeDelta = new Vector2(0, 0);
    }

    private void ShowItem()
    {
        draggedIconImage.enabled=true;
        draggedIconImage.sprite=draggedItem.ItemData.itemIcon;
        Vector2 ItemSize=new Vector2(draggedItem.ItemData.backpackItemData.imageSize.x , draggedItem.ItemData.backpackItemData.imageSize.y);
        selfRt.sizeDelta=  ItemSize*draggedItem.ItemData.backpackItemData.pixelAmount;
    }
    private void RotateDraggedItem() // 顺时针旋转物品
    {

    }


}
