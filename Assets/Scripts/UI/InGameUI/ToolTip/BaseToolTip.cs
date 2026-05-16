using UnityEngine;

public class BaseToolTip : MonoBehaviour
{
    private RectTransform rect;
    private Canvas canvas;
    private RectTransform centreRect;
    [SerializeField] private Vector2 offset = new Vector2(300, 20); // 提示面板出现位置偏移量
    [SerializeField] private bool showInMiddle = false;
    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        centreRect=canvas.GetComponent<RectTransform>();
    }

    public virtual void ShowToolTip(bool show, RectTransform targetRect)
    {
        if (show == false)
        {
            rect.position = new Vector2(9999, 9999); // 如果关闭提示，则将提示移到玩家看不到的位置
            return;
        }
        if (showInMiddle)
        {
            UpdatePositionInMiddle(targetRect);
        }
        else
        {
            UpdatePosition(targetRect); // 如果展示提示，则将提示移到目标位置
        }
    }

    private void UpdatePositionInMiddle(RectTransform targetRect)
    {
        float screenTop = Screen.height; // 屏幕顶端高度
        float screenBottom = 0f; // 屏幕底部
        Vector2 targetPosition = targetRect.position;
        targetPosition.x = centreRect.position.x;
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

    private void UpdatePosition(RectTransform targetRect)
    {
        float screenCenterX = Screen.width / 2f; // 屏幕中心X坐标
        float screenTop = Screen.height; // 屏幕顶端高度
        float screenBottom = 0f; // 屏幕底部

        Vector2 targetPosition = targetRect.position;

        targetPosition.x = targetPosition.x > screenCenterX ? targetPosition.x - offset.x : targetPosition.x + offset.x;


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
