using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseToolTip : MonoBehaviour
{
    private RectTransform rect;
    [SerializeField] private Vector2 offset = new Vector2(300, 20); // 提示面板出现位置偏移量
    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public virtual void ShowToolTip(bool show, RectTransform targetRect)
    {
        if (show == false)
        {
            rect.position = new Vector2(9999, 9999); // 如果关闭提示，则将提示移到玩家看不到的位置
            return;
        }
        UpdatePosition(targetRect); // 如果展示提示，则将提示移到目标位置
    }

    private void UpdatePosition(RectTransform targetRect)
    {
        float screenCenterX = Screen.width / 2f; // 屏幕中心X坐标
        float screenTop = Screen.height; // 屏幕顶端高度
        float screenBottom = 0f; // 屏幕底部

        Vector2 targetPosition = targetRect.position; // 技能图标原始位置

        targetPosition.x = targetPosition.x > screenCenterX ? targetPosition.x - offset.x : targetPosition.x + offset.x;
        // 根据技能图标原始位置在屏幕的区域调整提示面板出现位置

        float verticalHalf = rect.sizeDelta.y / 2; // 提示面板长度的一半
        float topY = targetPosition.y + verticalHalf; // 面板上边界Y坐标
        float bottomY = targetPosition.y - verticalHalf; // 面板下边界Y坐标
        if (topY > screenTop) // 如果原上边界超出屏幕范围
        {
            targetPosition.y = screenTop - verticalHalf - offset.y; // 向下偏移
        }
        else if (bottomY < screenBottom) // 如果原下边界超出屏幕范围
        {
            targetPosition.y = screenBottom + verticalHalf + offset.y; // 向上偏移
        }


        rect.position = targetPosition;
    }

}
