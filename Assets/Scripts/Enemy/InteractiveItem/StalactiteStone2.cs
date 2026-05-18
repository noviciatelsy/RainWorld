using UnityEngine;

public class StalactiteStone2 : MonoBehaviour
{
    [Header("伤害设置")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private LayerMask playerLayer;

    private Rigidbody2D rb;
    private bool hasLandedOrHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLandedOrHit) return;

        GameObject hitObj = collision.gameObject;

        // 分支 A：如果砸中了玩家
        if (((1 << hitObj.layer) & playerLayer) != 0)
        {
            Debug.Log("?!");
            PlayerVitals vitals = hitObj.GetComponent<PlayerVitals>();
            if (vitals != null && !vitals.IsDead)
            {
                vitals.ReduceHealth(damageAmount);
                Debug.Log($"【钟乳石】成功砸中玩家！造成 {damageAmount} 点伤害。");
            }

            StopMovement();
            return;
        }

        // 分支 B：没砸中玩家，落到了地形上
        var mgr = TileMapGuideManager.Instance;
        if (mgr != null)
        {
            int nearestEdgeIndex = mgr.FindClosestEdgeIndex(transform.position);
            Edge edge = mgr.GetEdge(nearestEdgeIndex);

            Vector2 dir = (edge.b - edge.a).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);

            if (normal.y > 0.7f)
            {
                StopMovement();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Static; // 变成静态物体，永久留在地面上
                }
                Debug.Log("【钟乳石】安全着陆在朝上的 Edge 地面上。");
            }
        }
    }

    private void StopMovement()
    {
        hasLandedOrHit = true;
        if (rb != null)
        {
            // 修正：使用标准的 velocity
            rb.velocity = Vector2.zero;
        }
    }
}