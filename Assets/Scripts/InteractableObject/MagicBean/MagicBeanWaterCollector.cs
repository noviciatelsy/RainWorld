using UnityEngine;

/// <summary>
/// 魔豆吸水：识别、吸附并销毁水（参考 RobotDrinkCollector）。
/// </summary>
[DisallowMultipleComponent]
public class MagicBeanWaterCollector : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("吸水检测范围（仅 Size 有效，Center 运行时随魔豆更新）")]
    public Bounds detectBounds;

    [Tooltip("可吸附的水 ItemData")]
    public ItemDataSO waterItemData;

    [Header("Collect")]
    [Tooltip("吸水点，默认魔豆 Transform")]
    public Transform absorbPoint;

    public float flySpeed = 12f;
    public float collectArriveDistance = 0.12f;
    [Tooltip("泼水落地时在落点周围搜索水的半径")]
    public float dropSearchRadius = 1.5f;

    [Header("Liquid Splash")]
    [Tooltip("用于被 DroppedLiquid 溅射识别的触发器半径")]
    [SerializeField] private float liquidSensorRadius = 1.2f;

    [SerializeField] private MagicBean magicBean;

    private PickableObject registeredTarget;
    private readonly Collider2D[] overlapBuffer = new Collider2D[32];

    public MagicBean MagicBean => magicBean;

    private void Awake()
    {
        if (magicBean == null)
        {
            magicBean = GetComponent<MagicBean>();
        }

        if (absorbPoint == null)
        {
            absorbPoint = transform;
        }

        if (detectBounds.size.sqrMagnitude < 0.01f)
        {
            detectBounds = new Bounds(Vector3.zero, new Vector3(3f, 3f, 0.1f));
        }

        EnsureLiquidSensorCollider();
        EnsureWaterItemData();
    }

    public Bounds GetDetectBoundsWorld()
    {
        Vector3 center = transform.position;
        return new Bounds(center, detectBounds.size);
    }

    public void OnLiquidContact()
    {
        if (magicBean != null && magicBean.IsActivated)
        {
            return;
        }

        Vector2 searchOrigin = transform.position;
        TryRegisterWaterAt(searchOrigin, dropSearchRadius);
        TryCollectRegisteredOrScanned();
    }

    public void TryRegisterWaterAt(Vector2 worldPosition, float searchRadius)
    {
        PickableObject water = FindWaterAt(worldPosition, searchRadius);

        if (water != null)
        {
            registeredTarget = water;
        }
    }

    public PickableObject ResolveCollectTarget()
    {
        if (IsValidWaterPickable(registeredTarget))
        {
            return registeredTarget;
        }

        registeredTarget = null;
        return FindWaterInDetectBounds();
    }

    public void CompleteCollect(PickableObject pickable)
    {
        if (pickable == null)
        {
            return;
        }

        if (registeredTarget == pickable)
        {
            registeredTarget = null;
        }

        Destroy(pickable.gameObject);
        magicBean?.ActivateByWater();
    }

    public bool IsWithinCollectRange(PickableObject pickable)
    {
        if (pickable == null || absorbPoint == null)
        {
            return false;
        }

        float distSqr = ((Vector2)pickable.transform.position - (Vector2)absorbPoint.position).sqrMagnitude;
        return distSqr <= collectArriveDistance * collectArriveDistance;
    }

    private void FixedUpdate()
    {
        if (magicBean != null && magicBean.IsActivated)
        {
            return;
        }

        TryCollectRegisteredOrScanned();
    }

    private void TryCollectRegisteredOrScanned()
    {
        PickableObject target = ResolveCollectTarget();

        if (target == null)
        {
            return;
        }

        if (IsWithinCollectRange(target))
        {
            CompleteCollect(target);
            return;
        }

        Transform flyTarget = absorbPoint != null ? absorbPoint : transform;
        MagicBeanWaterFly.TryBegin(
            target,
            flyTarget,
            this,
            flySpeed,
            collectArriveDistance);
    }

    public PickableObject FindWaterAt(Vector2 worldPosition, float searchRadius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPosition, searchRadius);
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

            if (!IsValidWaterPickable(pickable))
            {
                continue;
            }

            float distSqr = ((Vector2)pickable.transform.position - worldPosition).sqrMagnitude;

            if (distSqr >= bestDistSqr)
            {
                continue;
            }

            bestDistSqr = distSqr;
            closest = pickable;
        }

        return closest;
    }

    private PickableObject FindWaterInDetectBounds()
    {
        Bounds worldBounds = GetDetectBoundsWorld();
        Vector2 boxSize = new Vector2(
            Mathf.Max(worldBounds.size.x, 0.5f),
            Mathf.Max(worldBounds.size.y, 0.5f)
        );

        int hitCount = Physics2D.OverlapBoxNonAlloc(
            worldBounds.center,
            boxSize,
            0f,
            overlapBuffer
        );

        PickableObject closest = null;
        float bestDistSqr = float.MaxValue;
        Vector2 origin = absorbPoint != null ? (Vector2)absorbPoint.position : (Vector2)transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = overlapBuffer[i];

            if (hit == null)
            {
                continue;
            }

            PickableObject pickable = hit.GetComponentInParent<PickableObject>();

            if (!IsValidWaterPickable(pickable))
            {
                continue;
            }

            if (!IsInsideDetectBounds(pickable.transform.position))
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

    private bool IsInsideDetectBounds(Vector2 point)
    {
        return RobotGroundPath.IsInsideBoundsXY(GetDetectBoundsWorld(), point);
    }

    public bool IsValidWaterPickable(PickableObject pickable)
    {
        if (pickable == null || pickable.ItemData == null || waterItemData == null)
        {
            return false;
        }

        if (pickable.ItemData != waterItemData)
        {
            return false;
        }

        if (!pickable.IsSettledOnGround)
        {
            return false;
        }

        if (pickable.GetComponent<MagicBeanWaterFly>() != null)
        {
            return false;
        }

        return true;
    }

    private void EnsureWaterItemData()
    {
        if (waterItemData != null)
        {
            return;
        }

        ItemDataSO[] allItems = Resources.FindObjectsOfTypeAll<ItemDataSO>();

        for (int i = 0; i < allItems.Length; i++)
        {
            ItemDataSO item = allItems[i];

            if (item != null && item.name.Contains("Water"))
            {
                waterItemData = item;
                return;
            }
        }
    }

    private void EnsureLiquidSensorCollider()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        CircleCollider2D sensor = gameObject.AddComponent<CircleCollider2D>();
        sensor.isTrigger = true;
        sensor.radius = Mathf.Max(0.2f, liquidSensorRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Bounds worldBounds = Application.isPlaying
            ? GetDetectBoundsWorld()
            : new Bounds(transform.position, detectBounds.size);

        Gizmos.color = new Color(0.2f, 0.85f, 0.45f, 0.85f);
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);

        Gizmos.color = new Color(0.35f, 0.75f, 1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, liquidSensorRadius);
    }
}
