using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 机器人饮品吸附：注册目标 + 扫描 detectBounds（参考 MoleTreasureCollector），
/// 由 RobotDrinkFly 将道具飞向机器人后销毁。
/// </summary>
[DisallowMultipleComponent]
public class RobotDrinkCollector : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("饮品吸附检测范围（仅 Size 有效，Center 运行时随机器人更新）")]
    public Bounds detectBounds;

    [Tooltip("可吸附的饮品 ItemData（牛奶、咖啡、水等）")]
    public ItemDataSO[] targetDrinks;

    [Header("Collect")]
    [Tooltip("吸附点，默认机器人 Transform")]
    public Transform absorbPoint;

    public float flySpeed = 12f;
    public float collectArriveDistance = 0.12f;
    [Tooltip("落地通知时在落点周围搜索饮品的半径")]
    public float dropSearchRadius = 1.5f;

    [SerializeField] private Robot2D robot;

    private PickableObject registeredTarget;
    private readonly Collider2D[] overlapBuffer = new Collider2D[32];

    public Robot2D Robot => robot;

    private void Awake()
    {
        if (robot == null)
        {
            robot = GetComponent<Robot2D>();
        }

        if (absorbPoint == null)
        {
            absorbPoint = transform;
        }

        if (detectBounds.size.sqrMagnitude < 0.01f)
        {
            detectBounds = new Bounds(Vector3.zero, new Vector3(6f, 3f, 0.1f));
        }

        EnsureTargetDrinks();
    }

    public Bounds GetDetectBoundsWorld()
    {
        Vector3 center = robot != null ? (Vector3)robot.Position : transform.position;
        return new Bounds(center, detectBounds.size);
    }

    public void OnLiquidContact()
    {
        Vector2 searchOrigin = robot != null ? robot.Position : (Vector2)transform.position;
        TryRegisterDrinkAt(searchOrigin, dropSearchRadius);
        TryCollectRegisteredOrScanned();
    }

    public void NotifyMilkDropped(Vector2 milkPosition)
    {
        TryRegisterDrinkAt(milkPosition, dropSearchRadius);
        TryCollectRegisteredOrScanned();
    }

    public void RegisterDrink(PickableObject pickable)
    {
        if (!IsValidDrinkPickable(pickable))
        {
            return;
        }

        registeredTarget = pickable;
    }

    public void TryRegisterDrinkAt(Vector2 worldPosition, float searchRadius)
    {
        PickableObject drink = FindDrinkAt(worldPosition, searchRadius);
        if (drink != null)
        {
            RegisterDrink(drink);
        }
    }

    public PickableObject ResolveCollectTarget()
    {
        if (IsValidDrinkPickable(registeredTarget))
        {
            return registeredTarget;
        }

        registeredTarget = null;
        return FindDrinkInDetectBounds();
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
        robot?.EnterDrinkFrozenState();
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
        if (robot != null && robot.IsDrinkFrozen)
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
        RobotDrinkFly.TryBegin(
            target,
            flyTarget,
            this,
            flySpeed,
            collectArriveDistance);
    }

    public PickableObject FindDrinkAt(Vector2 worldPosition, float searchRadius)
    {
        if (targetDrinks == null || targetDrinks.Length == 0)
        {
            return null;
        }

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
            if (!IsValidDrinkPickable(pickable))
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

    private PickableObject FindDrinkInDetectBounds()
    {
        if (targetDrinks == null || targetDrinks.Length == 0)
        {
            return null;
        }

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
            if (!IsValidDrinkPickable(pickable))
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

    public bool IsTargetDrink(PickableObject pickable)
    {
        if (pickable == null || pickable.ItemData == null)
        {
            return false;
        }

        return IsTargetDrinkData(pickable.ItemData);
    }

    public bool IsTargetDrinkData(ItemDataSO itemData)
    {
        if (itemData == null || targetDrinks == null)
        {
            return false;
        }

        for (int i = 0; i < targetDrinks.Length; i++)
        {
            if (targetDrinks[i] == itemData)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsValidDrinkPickable(PickableObject pickable)
    {
        if (!IsTargetDrink(pickable))
        {
            return false;
        }

        if (!pickable.IsSettledOnGround)
        {
            return false;
        }

        if (pickable.GetComponent<RobotDrinkFly>() != null)
        {
            return false;
        }

        return true;
    }

    private void EnsureTargetDrinks()
    {
        if (targetDrinks != null && targetDrinks.Length > 0)
        {
            return;
        }

        List<ItemDataSO> resolved = new List<ItemDataSO>();
        ItemDataSO[] allItems = Resources.FindObjectsOfTypeAll<ItemDataSO>();

        for (int i = 0; i < allItems.Length; i++)
        {
            ItemDataSO item = allItems[i];
            if (item == null || resolved.Contains(item))
            {
                continue;
            }

            string itemName = item.name;
            if (itemName.Contains("Milk") || itemName.Contains("Coffee") || itemName.Contains("Water"))
            {
                resolved.Add(item);
            }
        }

        if (resolved.Count > 0)
        {
            targetDrinks = resolved.ToArray();
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawDetectBoundsGizmo(1f);
    }

    public void DrawDetectBoundsGizmo(float alpha)
    {
        Bounds worldBounds = Application.isPlaying
            ? GetDetectBoundsWorld()
            : new Bounds(transform.position, detectBounds.size);

        Gizmos.color = new Color(0.35f, 0.55f, 1f, alpha * 0.85f);
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
    }
}
