using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DraggedItemUI : MonoBehaviour
{
    //[Header("Refs")]
    //private Canvas rootCanvas; // 主Canvas
    //private Image dragIcon; // 跟随鼠标的图标

    //public bool IsDragging { get; private set; } // bool锁，是否正在拖拽物品
    //public InventoryItem dragItem { get; private set; } // 拖拽时暂存的物品

    //private RectTransform selfRt; // 自身的Rect

    //private void Awake()
    //{
    //    rootCanvas = GetComponentInParent<Canvas>();
    //    selfRt = transform as RectTransform;
    //    Hide(); // 隐藏拖拽时的物品图标
    //}

    //private void Update()
    //{
    //    if (!IsDragging) return;

    //    // 让图标跟随鼠标
    //    RectTransformUtility.ScreenPointToLocalPointInRectangle(
    //        rootCanvas.transform as RectTransform,
    //        Input.mousePosition,
    //        rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
    //        out var mousePosition
    //    );
    //    selfRt.anchoredPosition = mousePosition;

    //    if (Input.GetMouseButtonDown(1)) // 右键按下
    //    {
    //        RotateDraggedItem(); // 旋转物品
    //    }
    //}

    //public void Begin(InventoryItem item) // 开始拖拽
    //{
    //    dragItem = item; // 暂存该物品
    //    IsDragging = true; // 开启bool锁
    //    dragIcon.enabled = true; // 启用Image

    //    // 显示图标与堆叠数
    //    dragIcon.sprite = item?.ItemData?.itemIcon;
        
    //}

    //public void SwapAndKeep(Inventory_Item newItem)
    //{
    //    // 用于放到已有物品上 , 被顶出来的物品继续拖拽
    //    Begin(newItem);
    //}

    //public void End() // 结束拖拽
    //{
    //    Hide();
    //    dragItem = null; // 清空暂存的物品
    //    IsDragging = false;
    //}

    //private void Hide()
    //{
    //    dragIcon.enabled = false; // 禁用图标（使其不可见）
    //    selfRt.sizeDelta = new Vector2(0, 0);

    //private void RotateDraggedItem()
    //{

    //}

    //public void PutItemBack()
    //{
    //    if (!IsDragging) return;

    //    int total = dragItem.stackSize; // 手里一共多少
    //    //int leftover = inventory.DepositItemAmount(dragItem.ItemData, total);

    //    //if (leftover == 0)
    //    //{
    //    //    End(); // 全塞进去了，结束拖拽
    //    //}
    //    //else
    //    //{

    //    //    // 塞不下的，丢在地上生成实体
    //    //    GameObject itemDropped = Instantiate(
    //    //        pickupableObject,
    //    //        inventory.gameObject.transform.position,
    //    //        Quaternion.identity
    //    //    );
    //    //    itemDropped.GetComponent<Object_ItemPickup>()
    //    //               .SetupDropedObject(dragItem.ItemData, leftover);

    //    //    End();
    //    //}
    //    GameObject itemDropped = Instantiate(
    //            pickupableObject,
    //            inventory.gameObject.transform.position,
    //            Quaternion.identity
    //        );
    //    itemDropped.GetComponent<Object_ItemPickup>()
    //               .SetupObject(dragItem.ItemData, total);

    //    End();
    //}


    //private void UpdateCountText()
    //{
    //    if (countText == null) return;
    //    countText.text = (dragItem != null && dragItem.stackSize > 1)
    //        ? dragItem.stackSize.ToString()
    //        : "";
    //}


}
