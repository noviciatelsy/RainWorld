using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    [Header("Rope Size")]
    [SerializeField, Min(0.25f)] private float ropeLength = 5f;
    // 绳子总长度
    // Head + Body + Tail 的总长度
    // 同时也是 BoxCollider2D 的 y 方向大小

    [SerializeField, Min(0.001f)] private float ropeWidth = 0.125f;
    // 绳子宽度
    // 同时也是 BoxCollider2D 的 x 方向大小，以及 Body 贴图的宽度

    [SerializeField, Min(0.001f)] private float ropeEndHeight = 0.125f;
    // Head 或 Tail 单独占用的高度
    // 当前你的配置中，Head 高 0.125，Tail 高 0.125，所以二者合计为 0.25

    [Header("References")]
    [SerializeField] private SpriteRenderer ropeHead;
    [SerializeField] private SpriteRenderer ropeBody;
    [SerializeField] private SpriteRenderer ropeTail;

    [SerializeField] private BoxCollider2D boxCollider2D;

    public float RopeLength => ropeLength;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerControl playerControl = collision.GetComponentInParent<PlayerControl>();

        if (playerControl == null)
        {
            return;
        }
        playerControl.SetInRopeArea(true);

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerControl playerControl = collision.GetComponentInParent<PlayerControl>();

        if (playerControl == null)
        {
            return;
        }

        playerControl.SetInRopeArea(false);
    }

    public void SetRopeLength(float newRopeLength)
    {
        ropeLength = Mathf.Max(newRopeLength, GetMinRopeLength());
        ApplyRopeLayout();
    }



    private void ApplyRopeLayout()
    {
        float minRopeLength = GetMinRopeLength();

        if (ropeLength < minRopeLength)
        {
            ropeLength = minRopeLength;
        }

        float bodyHeight = GetBodyHeight();
        // 公式：
        // 当 Length 为 n 时，body 的 h = n - 0.25
        // 这里的 0.25 = ropeEndHeight * 2

        float headTailOffsetY = GetHeadTailOffsetY(bodyHeight);
        // 公式：
        // Head y = h / 2 + 0.0625
        // Tail y = -h / 2 - 0.0625
        // 这里的 0.0625 = ropeEndHeight / 2

        ApplyColliderLayout();
        ApplyBodyLayout(bodyHeight);
        ApplyHeadTailLayout(headTailOffsetY);
    }

    private void ApplyColliderLayout()
    {
        if (boxCollider2D == null)
        {
            return;
        }

        boxCollider2D.isTrigger = true;
        // 绳子区域用于触发攀爬，所以这里强制设为 Trigger

        boxCollider2D.offset = Vector2.zero;
        boxCollider2D.size = new Vector2(ropeWidth, ropeLength);
        // x 方向大小固定为 ropeWidth
        // y 方向大小为绳子总长度 ropeLength
    }

    private void ApplyBodyLayout(float bodyHeight)
    {
        if (ropeBody == null)
        {
            return;
        }

        ropeBody.drawMode = SpriteDrawMode.Tiled;
        // Body 使用平铺模式

        ropeBody.size = new Vector2(ropeWidth, bodyHeight);
        // Body 宽度固定为 0.125
        // Body 高度随 ropeLength 变化

        SetLocalPosition(ropeBody.transform, 0f, 0f);
        // Body 保持在 Rope 中心
    }

    private void ApplyHeadTailLayout(float headTailOffsetY)
    {
        if (ropeHead != null)
        {
            SetLocalPosition(ropeHead.transform, 0f, headTailOffsetY);
        }

        if (ropeTail != null)
        {
            SetLocalPosition(ropeTail.transform, 0f, -headTailOffsetY);
        }
    }

    private void SetLocalPosition(Transform targetTransform, float x, float y)
    {
        Vector3 localPosition = targetTransform.localPosition;

        localPosition.x = x;
        localPosition.y = y;

        targetTransform.localPosition = localPosition;
    }

    private float GetBodyHeight()
    {
        return Mathf.Max(0f, ropeLength - ropeEndHeight * 2f);
    }

    private float GetHeadTailOffsetY(float bodyHeight)
    {
        return bodyHeight * 0.5f + ropeEndHeight * 0.5f;
    }

    private float GetMinRopeLength()
    {
        return ropeEndHeight * 2f;
    }

    private void OnValidate()
    {

        ropeWidth = Mathf.Max(0.001f, ropeWidth);
        ropeEndHeight = Mathf.Max(0.001f, ropeEndHeight);
        ropeLength = Mathf.Max(ropeLength, GetMinRopeLength());

        ApplyRopeLayout();
    }

    private void OnDrawGizmosSelected()
    {
        float validRopeLength = Mathf.Max(ropeLength, GetMinRopeLength());

        Matrix4x4 oldGizmosMatrix = Gizmos.matrix;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(ropeWidth, validRopeLength, 0f));

        Gizmos.matrix = oldGizmosMatrix;
    }
}