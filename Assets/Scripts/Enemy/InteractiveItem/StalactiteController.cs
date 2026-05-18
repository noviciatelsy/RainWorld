using UnityEngine;

public class StalactiteController : MonoBehaviour
{
    [Header("子物体引用")]
    [SerializeField] private GameObject stone0;
    [SerializeField] private GameObject stone1;
    [SerializeField] private GameObject stone2;

    [Header("玩家检测设置")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 detectionSize = new Vector2(2f, 10f); // 检测区域的大小
    [SerializeField] private Vector2 detectionOffset = new Vector2(0f, -5f); // 检测区域的偏移（往下延伸）

    private bool isTriggered = false;
    private Rigidbody2D stone2Rb;

    private void Awake()
    {
        // 初始化状态
        if (stone0 != null) stone0.SetActive(true);
        if (stone1 != null) stone1.SetActive(false);
        if (stone2 != null) stone2.SetActive(false);

        // 获取 stone2 的刚体，用于控制下落
        if (stone2 != null)
        {
            stone2Rb = stone2.GetComponent<Rigidbody2D>();
            if (stone2Rb != null)
            {
                stone2Rb.bodyType = RigidbodyType2D.Kinematic; // 初始静止
            }
        }
    }

    private void FixedUpdate()
    {
        if (isTriggered) return;

        // 在下方区域发射一个 Box 检查玩家是否经过
        Vector2 detectCenter = (Vector2)transform.position + detectionOffset;
        Collider2D playerCollider = Physics2D.OverlapBox(detectCenter, detectionSize, 0f, playerLayer);

        if (playerCollider != null)
        {
            TriggerFall();
        }
    }

    private void TriggerFall()
    {
        isTriggered = true;

        // 1. 切换图片的显示隐藏
        if (stone0 != null) stone0.SetActive(false);
        if (stone1 != null) stone1.SetActive(true);
        if (stone2 != null) stone2.SetActive(true);

        // 2. 让 stone2 开始物理下落
        if (stone2Rb != null)
        {
            stone2Rb.bodyType = RigidbodyType2D.Dynamic; // 切换为动态，受重力下落

            // 修正：2D 刚体锁定旋转直接使用 FreezeRotation 即可
            stone2Rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        Debug.Log("【钟乳石】玩家经过，触发下落！");
    }


    // 在编辑器里绘制检测红框，方便你调整检测范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 detectCenter = transform.position + (Vector3)detectionOffset;
        Gizmos.DrawWireCube(detectCenter, new Vector3(detectionSize.x, detectionSize.y, 1f));
    }
}