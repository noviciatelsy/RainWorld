using UnityEngine;

/// <summary>
/// 鼹鼠爷爷：识别区域内丢弃的宝物，吸收达到数量后永久进入开心状态。
/// </summary>
[DisallowMultipleComponent]
public class MoleParentController : MonoBehaviour
{
    [Header("Treasure Detection")]
    [Tooltip("宝物识别半径")]
    [SerializeField] private float treasureDetectRadius = 8f;

    [Tooltip("触发开心状态所需吸收的宝物数量")]
    [SerializeField] private int requiredTreasureCount = 1;

    [Tooltip("可识别的宝物 ItemData")]
    [SerializeField] private ItemDataSO[] targetTreasures;

    [Tooltip("是否接受所有 ItemType.Treasure")]
    [SerializeField] private bool acceptAnyTreasureType = true;

    [SerializeField] private float detectInterval = 0.25f;

    [Header("Happy Landing")]
    [Tooltip("开心动画结束后根节点的世界坐标")]
    [SerializeField] private Vector2 happyLandingWorldPosition;

    [Header("References")]
    [SerializeField] private MoleParentAni moleParentAni;

    private int absorbedTreasureCount;
    private float detectTimer;

    public bool IsHappy => moleParentAni != null && moleParentAni.IsHappy;

    private void Awake()
    {
        if (moleParentAni == null)
        {
            moleParentAni = GetComponent<MoleParentAni>();
        }
    }

    private void Update()
    {
        if (IsHappy || moleParentAni != null && moleParentAni.IsPlayingHappySequence)
        {
            return;
        }

        detectTimer -= Time.deltaTime;

        if (detectTimer > 0f)
        {
            return;
        }

        detectTimer = Mathf.Max(0.05f, detectInterval);
        ScanAndAbsorbTreasures();
    }

    private void ScanAndAbsorbTreasures()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, treasureDetectRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            PickableObject pickable = hit.GetComponentInParent<PickableObject>();

            if (!IsValidDiscardedTreasure(pickable))
            {
                continue;
            }

            AbsorbTreasure(pickable);

            if (absorbedTreasureCount >= requiredTreasureCount)
            {
                TriggerPermanentHappyState();
                return;
            }
        }
    }

    private void AbsorbTreasure(PickableObject treasure)
    {
        if (treasure == null)
        {
            return;
        }

        Destroy(treasure.gameObject);
        absorbedTreasureCount++;
    }

    private void TriggerPermanentHappyState()
    {
        if (moleParentAni == null)
        {
            return;
        }

        moleParentAni.EnterPermanentHappyState(happyLandingWorldPosition);
    }

    private bool IsValidDiscardedTreasure(PickableObject pickable)
    {
        if (pickable == null || pickable.ItemData == null)
        {
            return false;
        }

        if (!pickable.IsSettledOnGround)
        {
            return false;
        }

        if (acceptAnyTreasureType && pickable.ItemData.itemType == ItemType.Treasure)
        {
            return true;
        }

        if (targetTreasures == null || targetTreasures.Length == 0)
        {
            return false;
        }

        ItemDataSO itemData = pickable.ItemData;

        for (int i = 0; i < targetTreasures.Length; i++)
        {
            if (targetTreasures[i] == itemData)
            {
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, treasureDetectRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(happyLandingWorldPosition, 0.2f);
    }
#endif
}
