using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenuFlyingItemPhysicsManager : MonoBehaviour
{
    [Header("References")]
    private RectTransform simulationRoot;

    [Header("World Physics")]
    [SerializeField] private float gravity = 1500f;
    [SerializeField] private int solverIterations = 2;
    [SerializeField] private float maxDeltaTime = 0.033f;
    [SerializeField] private float destroyMargin = 450f;

    [Header("Item vs Item")]
    [SerializeField] private float itemRestitution = 0.82f;
    [SerializeField] private float itemPositionCorrection = 0.8f;
    [SerializeField] private float angularImpulseFactor = 0.0025f;
    [SerializeField] private float tangentSpinFactor = 0.0012f;

    [Header("Mouse Collision")]
    [SerializeField] private bool enableMouseCollision = true;
    [SerializeField] private float mouseRadius = 36f;
    [SerializeField] private float mouseRestitution = 0.75f;
    [SerializeField] private float mouseVelocityTransfer = 0.65f;
    [SerializeField] private float mousePositionCorrection = 0.75f;

    [Header("Mouse Collision Limit")]
    [SerializeField] private float mouseCollisionCooldown = 0.12f;

    [SerializeField] private bool enableMouseImpulseLimit = true;

    [Tooltip("低于这个冲量时完全不限制，用来保留普通鼠标碰撞手感。")]
    [SerializeField] private float mouseImpulseFreeLimit = 260f;

    [Tooltip("鼠标碰撞最终冲量的软上限。")]
    [SerializeField] private float maxMouseImpulse = 460f;

    [Tooltip("超过自由区间后，冲量靠近上限的速度。越小越早变软，越大越接近硬限制。")]
    [SerializeField] private float mouseImpulseLimitSoftness = 140f;

    [Header("Button Collision")]
    [SerializeField] private RectTransform[] buttonCollisionRects;
    [SerializeField] private float buttonRestitution = 0.78f;
    [SerializeField] private float buttonPositionCorrection = 0.85f;

    private readonly List<MainMenuFlyingItem> flyingItems = new List<MainMenuFlyingItem>();
    private readonly Dictionary<MainMenuFlyingItem, float> nextMouseCollisionTimes = new Dictionary<MainMenuFlyingItem, float>();

    private Vector2 mouseLocalPosition;
    private Vector2 previousMouseLocalPosition;
    private Vector2 mouseVelocity;
    private bool hasMousePosition;

    public int AliveItemCount
    {
        get
        {
            return flyingItems.Count;
        }
    }

    private void Awake()
    {
        simulationRoot=GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (simulationRoot == null)
        {
            return;
        }

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, maxDeltaTime);

        UpdateMouseState(deltaTime);

        for (int i = flyingItems.Count - 1; i >= 0; i--)
        {
            if (flyingItems[i] == null)
            {
                flyingItems.RemoveAt(i);
                continue;
            }

            flyingItems[i].Tick(deltaTime, gravity);
        }

        for (int i = 0; i < solverIterations; i++)
        {
            SolveItemItemCollisions();
            SolveMouseCollisions();
            SolveButtonCollisions();
        }

        DestroyOutsideItems();
    }

    public void RegisterItem(MainMenuFlyingItem item)
    {
        if (item == null)
        {
            return;
        }

        if (!flyingItems.Contains(item))
        {
            flyingItems.Add(item);
        }

        if (!nextMouseCollisionTimes.ContainsKey(item))
        {
            nextMouseCollisionTimes.Add(item, 0f);
        }
    }

    public void UnregisterItem(MainMenuFlyingItem item)
    {
        if (item == null)
        {
            return;
        }

        flyingItems.Remove(item);
        nextMouseCollisionTimes.Remove(item);
    }

    private void SolveItemItemCollisions()
    {
        for (int i = 0; i < flyingItems.Count; i++)
        {
            MainMenuFlyingItem itemA = flyingItems[i];

            if (itemA == null)
            {
                continue;
            }

            for (int j = i + 1; j < flyingItems.Count; j++)
            {
                MainMenuFlyingItem itemB = flyingItems[j];

                if (itemB == null)
                {
                    continue;
                }

                float radiusSum = itemA.CollisionRadius + itemB.CollisionRadius;

                if ((itemB.Position - itemA.Position).sqrMagnitude > radiusSum * radiusSum)
                {
                    continue;
                }

                if (!TryGetItemCollision(itemA, itemB, out Vector2 normal, out float penetration))
                {
                    continue;
                }

                Vector2 contactPoint = (itemA.Position + itemB.Position) * 0.5f;

                float totalInverseMass = itemA.InverseMass + itemB.InverseMass;

                if (totalInverseMass <= Mathf.Epsilon)
                {
                    continue;
                }

                Vector2 correction = normal * penetration * itemPositionCorrection / totalInverseMass;

                itemA.MoveBy(-correction * itemA.InverseMass);
                itemB.MoveBy(correction * itemB.InverseMass);

                Vector2 relativeVelocity = itemB.Velocity - itemA.Velocity;
                float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

                if (velocityAlongNormal > 0f)
                {
                    continue;
                }

                float impulseStrength = -(1f + itemRestitution) * velocityAlongNormal;
                impulseStrength /= totalInverseMass;

                Vector2 impulse = impulseStrength * normal;

                itemA.ApplyImpulse(-impulse, contactPoint, angularImpulseFactor);
                itemB.ApplyImpulse(impulse, contactPoint, angularImpulseFactor);

                Vector2 tangent = new Vector2(-normal.y, normal.x);
                float tangentSpeed = Vector2.Dot(relativeVelocity, tangent);

                itemA.AddAngularVelocity(-tangentSpeed * tangentSpinFactor);
                itemB.AddAngularVelocity(tangentSpeed * tangentSpinFactor);

                itemA.PlayCollisionSquash(impulseStrength);
                itemB.PlayCollisionSquash(impulseStrength);
            }
        }
    }

    private void SolveMouseCollisions()
    {
        if (!enableMouseCollision || !hasMousePosition)
        {
            return;
        }

        for (int i = 0; i < flyingItems.Count; i++)
        {
            MainMenuFlyingItem item = flyingItems[i];

            if (item == null)
            {
                continue;
            }

            float radiusSum = item.CollisionRadius + mouseRadius;

            if ((item.Position - mouseLocalPosition).sqrMagnitude > radiusSum * radiusSum)
            {
                continue;
            }

            Vector2 closestPoint = item.GetClosestPointOnShape(mouseLocalPosition);
            bool mouseInsideItem = item.ContainsPoint(mouseLocalPosition);

            Vector2 normal;

            if (mouseInsideItem)
            {
                normal = item.Position - mouseLocalPosition;
            }
            else
            {
                normal = closestPoint - mouseLocalPosition;
            }

            if (normal.sqrMagnitude <= 0.0001f)
            {
                normal = item.Position - mouseLocalPosition;
            }

            if (normal.sqrMagnitude <= 0.0001f)
            {
                normal = Vector2.up;
            }

            normal.Normalize();

            float distance = mouseInsideItem ? 0f : Vector2.Distance(mouseLocalPosition, closestPoint);
            float penetration = mouseRadius - distance;

            if (mouseInsideItem)
            {
                penetration = mouseRadius;
            }

            if (penetration <= 0f)
            {
                continue;
            }

            float currentTime = Time.unscaledTime;

            if (!nextMouseCollisionTimes.TryGetValue(item, out float nextMouseCollisionTime))
            {
                nextMouseCollisionTimes.Add(item, 0f);
                nextMouseCollisionTime = 0f;
            }

            if (currentTime < nextMouseCollisionTime)
            {
                // 冷却期内完全不处理鼠标碰撞。
                // 不推出、不加冲量、不限制速度，只是单纯禁止短时间内再次触发。
                continue;
            }

            item.MoveBy(normal * penetration * mousePositionCorrection);

            Vector2 relativeVelocity = item.Velocity - mouseVelocity;
            float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

            float impulseStrength = 0f;

            if (velocityAlongNormal < 0f)
            {
                impulseStrength = -(1f + mouseRestitution) * velocityAlongNormal;
            }

            Vector2 mousePush = Vector2.Dot(mouseVelocity, normal) * normal * mouseVelocityTransfer;
            Vector2 impulse = normal * impulseStrength + mousePush;

            // 只限制鼠标碰撞最终冲量。
            // 普通碰撞不受影响，只有鼠标快速撞击时会被压住。
            impulse = SoftLimitMouseImpulse(impulse);

            item.ApplyImpulse(impulse, closestPoint, angularImpulseFactor);
            item.PlayCollisionSquash(impulse.magnitude);

            nextMouseCollisionTimes[item] = currentTime + mouseCollisionCooldown;
        }
    }

    private Vector2 SoftLimitMouseImpulse(Vector2 impulse)
    {
        if (!enableMouseImpulseLimit)
        {
            return impulse;
        }

        float impulseMagnitude = impulse.magnitude;

        if (impulseMagnitude <= mouseImpulseFreeLimit)
        {
            return impulse;
        }

        if (maxMouseImpulse <= mouseImpulseFreeLimit)
        {
            return Vector2.ClampMagnitude(impulse, maxMouseImpulse);
        }

        float extraImpulse = impulseMagnitude - mouseImpulseFreeLimit;
        float maxExtraImpulse = maxMouseImpulse - mouseImpulseFreeLimit;

        float limitedExtraImpulse = maxExtraImpulse * (1f - Mathf.Exp(-extraImpulse / mouseImpulseLimitSoftness));

        float limitedMagnitude = mouseImpulseFreeLimit + limitedExtraImpulse;
        limitedMagnitude = Mathf.Min(limitedMagnitude, maxMouseImpulse);

        return impulse.normalized * limitedMagnitude;
    }

    private void SolveButtonCollisions()
    {
        if (buttonCollisionRects == null)
        {
            return;
        }

        for (int buttonIndex = 0; buttonIndex < buttonCollisionRects.Length; buttonIndex++)
        {
            RectTransform buttonRect = buttonCollisionRects[buttonIndex];

            if (buttonRect == null || !buttonRect.gameObject.activeInHierarchy)
            {
                continue;
            }

            Rect localRect = GetLocalRectInSimulationRoot(buttonRect);

            for (int itemIndex = 0; itemIndex < flyingItems.Count; itemIndex++)
            {
                MainMenuFlyingItem item = flyingItems[itemIndex];

                if (item == null)
                {
                    continue;
                }

                if (!TryGetItemRectCollision(item, localRect, out Vector2 normal, out float penetration, out Vector2 contactPoint))
                {
                    continue;
                }

                item.MoveBy(normal * penetration * buttonPositionCorrection);

                float velocityAlongNormal = Vector2.Dot(item.Velocity, normal);

                if (velocityAlongNormal < 0f)
                {
                    float impulseStrength = -(1f + buttonRestitution) * velocityAlongNormal;
                    Vector2 impulse = normal * impulseStrength;

                    item.ApplyImpulse(impulse, contactPoint, angularImpulseFactor);
                    item.PlayCollisionSquash(impulseStrength);

                    MainMenuButtonCollisionFeedback feedback = buttonRect.GetComponent<MainMenuButtonCollisionFeedback>();

                    if (feedback != null)
                    {
                        feedback.PlayBounce(normal, impulseStrength);
                    }
                }
            }
        }
    }

    private bool TryGetItemCollision
    (
        MainMenuFlyingItem itemA,
        MainMenuFlyingItem itemB,
        out Vector2 bestNormal,
        out float bestPenetration
    )
    {
        bestNormal = Vector2.zero;
        bestPenetration = float.PositiveInfinity;

        List<List<Vector2>> shapesA = itemA.GetWorldShapes();
        List<List<Vector2>> shapesB = itemB.GetWorldShapes();

        bool hasCollision = false;

        for (int i = 0; i < shapesA.Count; i++)
        {
            for (int j = 0; j < shapesB.Count; j++)
            {
                if (TryGetPolygonCollision(shapesA[i], shapesB[j], out Vector2 normal, out float penetration))
                {
                    hasCollision = true;

                    if (penetration < bestPenetration)
                    {
                        bestPenetration = penetration;
                        bestNormal = normal;
                    }
                }
            }
        }

        if (!hasCollision)
        {
            return false;
        }

        Vector2 direction = itemB.Position - itemA.Position;

        if (Vector2.Dot(direction, bestNormal) < 0f)
        {
            bestNormal = -bestNormal;
        }

        return true;
    }

    private bool TryGetPolygonCollision
    (
        List<Vector2> polygonA,
        List<Vector2> polygonB,
        out Vector2 bestNormal,
        out float bestPenetration
    )
    {
        bestNormal = Vector2.zero;
        bestPenetration = float.PositiveInfinity;

        if (!CheckPolygonAxes(polygonA, polygonB, ref bestNormal, ref bestPenetration))
        {
            return false;
        }

        if (!CheckPolygonAxes(polygonB, polygonA, ref bestNormal, ref bestPenetration))
        {
            return false;
        }

        Vector2 centerA = GetPolygonCenter(polygonA);
        Vector2 centerB = GetPolygonCenter(polygonB);

        if (Vector2.Dot(centerB - centerA, bestNormal) < 0f)
        {
            bestNormal = -bestNormal;
        }

        return true;
    }

    private bool CheckPolygonAxes
    (
        List<Vector2> sourcePolygon,
        List<Vector2> targetPolygon,
        ref Vector2 bestNormal,
        ref float bestPenetration
    )
    {
        for (int i = 0; i < sourcePolygon.Count; i++)
        {
            Vector2 a = sourcePolygon[i];
            Vector2 b = sourcePolygon[(i + 1) % sourcePolygon.Count];

            Vector2 edge = b - a;

            if (edge.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            Vector2 axis = new Vector2(-edge.y, edge.x).normalized;

            ProjectPolygon(sourcePolygon, axis, out float minA, out float maxA);
            ProjectPolygon(targetPolygon, axis, out float minB, out float maxB);

            float overlap = Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);

            if (overlap <= 0f)
            {
                return false;
            }

            if (overlap < bestPenetration)
            {
                bestPenetration = overlap;
                bestNormal = axis;
            }
        }

        return true;
    }

    private bool TryGetItemRectCollision
    (
        MainMenuFlyingItem item,
        Rect rect,
        out Vector2 normal,
        out float penetration,
        out Vector2 contactPoint
    )
    {
        normal = Vector2.zero;
        penetration = 0f;
        contactPoint = item.Position;

        Vector2 rectCenter = rect.center;
        Vector2 rectHalfSize = rect.size * 0.5f;

        Vector2 delta = item.Position - rectCenter;

        float coarseOverlapX = rectHalfSize.x + item.CollisionRadius - Mathf.Abs(delta.x);
        float coarseOverlapY = rectHalfSize.y + item.CollisionRadius - Mathf.Abs(delta.y);

        if (coarseOverlapX <= 0f || coarseOverlapY <= 0f)
        {
            return false;
        }

        bool hasActualHit = false;
        Vector2 contactSum = Vector2.zero;
        int contactCount = 0;

        List<List<Vector2>> itemShapes = item.GetWorldShapes();

        for (int shapeIndex = 0; shapeIndex < itemShapes.Count; shapeIndex++)
        {
            List<Vector2> shape = itemShapes[shapeIndex];

            for (int pointIndex = 0; pointIndex < shape.Count; pointIndex++)
            {
                Vector2 point = shape[pointIndex];

                if (rect.Contains(point))
                {
                    hasActualHit = true;
                    contactSum += point;
                    contactCount++;
                }
            }
        }

        Vector2[] rectCorners = new Vector2[]
        {
            new Vector2(rect.xMin, rect.yMin),
            new Vector2(rect.xMin, rect.yMax),
            new Vector2(rect.xMax, rect.yMax),
            new Vector2(rect.xMax, rect.yMin)
        };

        for (int i = 0; i < rectCorners.Length; i++)
        {
            if (item.ContainsPoint(rectCorners[i]))
            {
                hasActualHit = true;
                contactSum += rectCorners[i];
                contactCount++;
            }
        }

        if (!hasActualHit)
        {
            return false;
        }

        if (coarseOverlapX < coarseOverlapY)
        {
            normal = new Vector2(NonZeroSign(delta.x), 0f);
            penetration = coarseOverlapX;
        }
        else
        {
            normal = new Vector2(0f, NonZeroSign(delta.y));
            penetration = coarseOverlapY;
        }

        if (contactCount > 0)
        {
            contactPoint = contactSum / contactCount;
        }

        return true;
    }

    private Rect GetLocalRectInSimulationRoot(RectTransform targetRect)
    {
        Vector3[] worldCorners = new Vector3[4];
        targetRect.GetWorldCorners(worldCorners);

        Vector2 min = simulationRoot.InverseTransformPoint(worldCorners[0]);
        Vector2 max = min;

        for (int i = 1; i < worldCorners.Length; i++)
        {
            Vector2 localPoint = simulationRoot.InverseTransformPoint(worldCorners[i]);

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        return new Rect(min, max - min);
    }

    private void UpdateMouseState(float deltaTime)
    {
        if (!enableMouseCollision)
        {
            return;
        }

        Vector2 screenPosition;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            hasMousePosition = false;
            return;
        }

        screenPosition = Mouse.current.position.ReadValue();
#else
        screenPosition = Input.mousePosition;
#endif

        Camera eventCamera = null;

        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            simulationRoot,
            screenPosition,
            eventCamera,
            out Vector2 localPosition
        );

        if (!success)
        {
            hasMousePosition = false;
            return;
        }

        if (!hasMousePosition)
        {
            previousMouseLocalPosition = localPosition;
            mouseVelocity = Vector2.zero;
            hasMousePosition = true;
        }
        else
        {
            mouseVelocity = (localPosition - previousMouseLocalPosition) / Mathf.Max(deltaTime, 0.0001f);
            previousMouseLocalPosition = localPosition;
        }

        mouseLocalPosition = localPosition;
    }



    private void DestroyOutsideItems()
    {
        Rect rootRect = simulationRoot.rect;

        for (int i = flyingItems.Count - 1; i >= 0; i--)
        {
            MainMenuFlyingItem item = flyingItems[i];

            if (item == null)
            {
                flyingItems.RemoveAt(i);
                continue;
            }

            Vector2 position = item.Position;

            bool outside =
                position.x < rootRect.xMin - destroyMargin ||
                position.x > rootRect.xMax + destroyMargin ||
                position.y < rootRect.yMin - destroyMargin ||
                position.y > rootRect.yMax + destroyMargin;

            if (outside)
            {
                flyingItems.RemoveAt(i);
                nextMouseCollisionTimes.Remove(item);
                Destroy(item.gameObject);
            }
        }
    }

    private static void ProjectPolygon(List<Vector2> polygon, Vector2 axis, out float min, out float max)
    {
        float firstProjection = Vector2.Dot(polygon[0], axis);

        min = firstProjection;
        max = firstProjection;

        for (int i = 1; i < polygon.Count; i++)
        {
            float projection = Vector2.Dot(polygon[i], axis);

            if (projection < min)
            {
                min = projection;
            }

            if (projection > max)
            {
                max = projection;
            }
        }
    }

    private static Vector2 GetPolygonCenter(List<Vector2> polygon)
    {
        Vector2 sum = Vector2.zero;

        for (int i = 0; i < polygon.Count; i++)
        {
            sum += polygon[i];
        }

        return sum / polygon.Count;
    }

    private static float NonZeroSign(float value)
    {
        return value >= 0f ? 1f : -1f;
    }
}