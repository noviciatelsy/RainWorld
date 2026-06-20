using UnityEngine;

/// <summary>
/// 鼹鼠宝物感知与兑换：以 treasureDetectRadius 扫描范围内落地宝物，
/// AI 驱动鼹鼠走过去拾取并兑换为护符（敌人11 逻辑）。
/// </summary>
[DisallowMultipleComponent]
public class MoleTreasureCollector : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("宝物识别半径（较广）")]
    public float treasureDetectRadius = 12f;

    [Tooltip("可识别的宝物 ItemData")]
    public ItemDataSO[] targetTreasures;

    [Header("Reward")]
    [Tooltip("兑换后掉落的鼹鼠护符")]
    public ItemDataSO moleAmuletItemData;

    [Tooltip("用于生成护符的 PickableObject 预制体")]
    public PickableObject pickableObjectPrefab;

    [Header("Collect")]
    public float collectArriveDistance = 0.35f;
    public Vector2 amuletDropOffset = new Vector2(0f, 0.4f);

    [SerializeField] private Mole2D mole;

    private PickableObject registeredTarget;

    private void Awake()
    {
        if (mole == null)
        {
            mole = GetComponent<Mole2D>();
        }
    }

    public void RegisterTarget(PickableObject pickable)
    {
        if (!IsValidTreasurePickable(pickable))
        {
            return;
        }

        registeredTarget = pickable;
    }

    public PickableObject ResolveCollectTarget()
    {
        if (IsValidTreasurePickable(registeredTarget))
        {
            return registeredTarget;
        }

        registeredTarget = null;
        return FindNearestTreasure();
    }

    public PickableObject FindNearestTreasure()
    {
        if (targetTreasures == null || targetTreasures.Length == 0)
        {
            return null;
        }

        Vector2 origin = GetScanOrigin();
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, treasureDetectRadius);

        PickableObject closest = null;
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            PickableObject pickable = hit.GetComponentInParent<PickableObject>();
            if (!IsValidTreasurePickable(pickable))
            {
                continue;
            }

            float distSqr = ((Vector2)pickable.transform.position - origin).sqrMagnitude;
            if (distSqr >= bestDistSqr)
            {
                continue;
            }

            bestDistSqr = distSqr;
            closest = pickable;
        }

        return closest;
    }

    public bool IsValidTreasurePickable(PickableObject pickable)
    {
        if (pickable == null || pickable.ItemData == null)
        {
            return false;
        }

        if (!pickable.IsSettledOnGround)
        {
            return false;
        }

        return IsTargetTreasure(pickable.ItemData);
    }

    public bool IsTargetTreasure(ItemDataSO itemData)
    {
        if (itemData == null || targetTreasures == null)
        {
            return false;
        }

        for (int i = 0; i < targetTreasures.Length; i++)
        {
            if (targetTreasures[i] == itemData)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryCollect(PickableObject treasure)
    {
        if (!IsValidTreasurePickable(treasure))
        {
            return false;
        }

        Vector3 dropPosition = transform.position + (Vector3)amuletDropOffset;
        Destroy(treasure.gameObject);

        if (pickableObjectPrefab != null && moleAmuletItemData != null)
        {
            PickableObject amulet = Instantiate(pickableObjectPrefab, dropPosition, Quaternion.identity);
            amulet.SetupObject(moleAmuletItemData, true);
        }

        if (registeredTarget == treasure)
        {
            registeredTarget = null;
        }

        EnemyMoleAudioEmitter audioEmitter = mole != null
            ? mole.GetComponent<EnemyMoleAudioEmitter>()
            : GetComponent<EnemyMoleAudioEmitter>();
        audioEmitter?.PlayGift();

        return true;
    }

    public bool IsWithinCollectRange(PickableObject treasure)
    {
        if (treasure == null)
        {
            return false;
        }

        Vector2 origin = GetScanOrigin();
        float distSqr = ((Vector2)treasure.transform.position - origin).sqrMagnitude;
        return distSqr <= collectArriveDistance * collectArriveDistance;
    }

    public bool IsWithinDetectRange(PickableObject treasure)
    {
        if (treasure == null)
        {
            return false;
        }

        Vector2 origin = GetScanOrigin();
        float distSqr = ((Vector2)treasure.transform.position - origin).sqrMagnitude;
        return distSqr <= treasureDetectRadius * treasureDetectRadius;
    }

    public void TryRegisterTreasureAt(Vector2 worldPosition, float searchRadius)
    {
        if (targetTreasures == null || targetTreasures.Length == 0)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPosition, searchRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            PickableObject pickable = hit.GetComponentInParent<PickableObject>();
            if (IsValidTreasurePickable(pickable))
            {
                RegisterTarget(pickable);
                return;
            }
        }
    }

    public void FixedUpdate()
    {
        if (mole != null && mole.stealTimer > 0f)
        {
            return;
        }

        PickableObject target = ResolveCollectTarget();
        if (target == null)
        {
            return;
        }

        if (IsWithinCollectRange(target))
        {
            TryCollect(target);
        }
    }

    public void ClearRegisteredTarget()
    {
        registeredTarget = null;
    }

    private Vector2 GetScanOrigin()
    {
        return mole != null ? mole.Position : (Vector2)transform.position;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? GetScanOrigin() : (Vector2)transform.position;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
        Gizmos.DrawWireSphere(origin, treasureDetectRadius);

        Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.85f);
        Gizmos.DrawWireSphere(origin, collectArriveDistance);
    }
#endif
}
