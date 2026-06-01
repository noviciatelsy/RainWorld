using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuFlyingItem : MonoBehaviour
{
    [Header("References")]
    private RectTransform rectTransform;
    private Image itemImage;

    [Header("Collision Shape")]
    [SerializeField] private float fallbackRectScale = 0.9f;

    [Header("Juicy Feedback")]
    [SerializeField] private bool enableCollisionSquash = false;
    [SerializeField] private float squashDuration = 0.12f;
    [SerializeField] private float squashStrengthMin = 0.03f;
    [SerializeField] private float squashStrengthMax = 0.18f;

    private readonly List<List<Vector2>> localShapes = new List<List<Vector2>>();

    private Coroutine squashCoroutine;

    public Vector2 Velocity { get; private set; }
    public float AngularVelocity { get; private set; }
    public float InverseMass { get; private set; } = 1f;
    public float CollisionRadius { get; private set; }

    public Vector2 Position
    {
        get
        {
            return rectTransform.anchoredPosition;
        }
        private set
        {
            rectTransform.anchoredPosition = value;
        }
    }

    public float RotationZ
    {
        get
        {
            return rectTransform.localEulerAngles.z;
        }
    }


    public void Setup
    (
        ItemDataSO itemData,
        Vector2 cellSize,
        Vector2 spawnPosition,
        Vector2 initialVelocity,
        float initialAngle,
        float initialAngularVelocity
    )
    {

        rectTransform = GetComponent<RectTransform>();
        itemImage = GetComponent<Image>();

        itemImage.sprite = itemData.itemIcon;

        // 飞行物不要挡住按钮点击，按钮碰撞由 MainMenuFlyingItemPhysicsManager 单独处理。
        itemImage.raycastTarget = false;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Vector2Int imageSize = Vector2Int.one;

        if (itemData.backpackItemData != null)
        {
            imageSize = itemData.backpackItemData.imageSize;
        }

        rectTransform.sizeDelta = new Vector2
        (
            imageSize.x * cellSize.x,
            imageSize.y * cellSize.y
        );

        rectTransform.anchoredPosition = spawnPosition;
        rectTransform.localEulerAngles = new Vector3(0f, 0f, initialAngle);
        rectTransform.localScale = Vector3.one;

        Velocity = initialVelocity;
        AngularVelocity = initialAngularVelocity;

        // 惯性与大小无关，所以这里所有物品质量相同。
        InverseMass = 1f;

        BuildLocalShapesFromSprite(itemImage.sprite, rectTransform.sizeDelta);

        CollisionRadius = rectTransform.sizeDelta.magnitude * 0.5f;
    }

    public void Tick(float deltaTime, float gravity)
    {
        Velocity += Vector2.down * gravity * deltaTime;
        Position += Velocity * deltaTime;

        float newRotation = RotationZ + AngularVelocity * deltaTime;
        rectTransform.localEulerAngles = new Vector3(0f, 0f, newRotation);
    }

    public void MoveBy(Vector2 delta)
    {
        Position += delta;
    }

    public void AddVelocity(Vector2 deltaVelocity)
    {
        Velocity += deltaVelocity;
    }

    public void AddAngularVelocity(float deltaAngularVelocity)
    {
        AngularVelocity += deltaAngularVelocity;
    }

    public void ApplyImpulse(Vector2 impulse, Vector2 contactPoint, float angularImpulseFactor)
    {
        Velocity += impulse * InverseMass;

        Vector2 radius = contactPoint - Position;
        float torque = Cross(radius, impulse);

        AngularVelocity += torque * angularImpulseFactor;
    }

    public List<List<Vector2>> GetWorldShapes()
    {
        List<List<Vector2>> worldShapes = new List<List<Vector2>>();

        for (int i = 0; i < localShapes.Count; i++)
        {
            List<Vector2> localShape = localShapes[i];
            List<Vector2> worldShape = new List<Vector2>(localShape.Count);

            for (int j = 0; j < localShape.Count; j++)
            {
                worldShape.Add(LocalToSimulationPoint(localShape[j]));
            }

            worldShapes.Add(worldShape);
        }

        return worldShapes;
    }

    public bool ContainsPoint(Vector2 point)
    {
        List<List<Vector2>> worldShapes = GetWorldShapes();

        for (int i = 0; i < worldShapes.Count; i++)
        {
            if (IsPointInPolygon(point, worldShapes[i]))
            {
                return true;
            }
        }

        return false;
    }

    public Vector2 GetClosestPointOnShape(Vector2 point)
    {
        List<List<Vector2>> worldShapes = GetWorldShapes();

        float bestDistanceSqr = float.PositiveInfinity;
        Vector2 bestPoint = Position;

        for (int i = 0; i < worldShapes.Count; i++)
        {
            List<Vector2> shape = worldShapes[i];

            for (int j = 0; j < shape.Count; j++)
            {
                Vector2 a = shape[j];
                Vector2 b = shape[(j + 1) % shape.Count];

                Vector2 candidate = ClosestPointOnSegment(point, a, b);
                float distanceSqr = (candidate - point).sqrMagnitude;

                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestPoint = candidate;
                }
            }
        }

        return bestPoint;
    }

    public void PlayCollisionSquash(float impulseStrength)
    {
        if (!enableCollisionSquash)
        {
            return;
        }

        if (!isActiveAndEnabled)
        {
            return;
        }

        if (squashCoroutine != null)
        {
            StopCoroutine(squashCoroutine);
        }

        squashCoroutine = StartCoroutine(SquashRoutine(impulseStrength));
    }

    private void BuildLocalShapesFromSprite(Sprite sprite, Vector2 rectSize)
    {
        localShapes.Clear();

        if (sprite == null || sprite.GetPhysicsShapeCount() <= 0)
        {
            AddFallbackRectShape(rectSize);
            return;
        }

        Bounds bounds = sprite.bounds;

        for (int shapeIndex = 0; shapeIndex < sprite.GetPhysicsShapeCount(); shapeIndex++)
        {
            List<Vector2> spritePoints = new List<Vector2>();
            sprite.GetPhysicsShape(shapeIndex, spritePoints);

            if (spritePoints.Count < 3)
            {
                continue;
            }

            List<Vector2> localShape = new List<Vector2>();

            for (int i = 0; i < spritePoints.Count; i++)
            {
                Vector2 spritePoint = spritePoints[i];

                float normalizedX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, spritePoint.x);
                float normalizedY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, spritePoint.y);

                Vector2 localPoint = new Vector2
                (
                    (normalizedX - 0.5f) * rectSize.x,
                    (normalizedY - 0.5f) * rectSize.y
                );

                localShape.Add(localPoint);
            }

            localShapes.Add(localShape);
        }

        if (localShapes.Count <= 0)
        {
            AddFallbackRectShape(rectSize);
        }
    }

    private void AddFallbackRectShape(Vector2 rectSize)
    {
        Vector2 halfSize = rectSize * 0.5f * fallbackRectScale;

        localShapes.Add
        (
            new List<Vector2>
            {
                new Vector2(-halfSize.x, -halfSize.y),
                new Vector2(-halfSize.x, halfSize.y),
                new Vector2(halfSize.x, halfSize.y),
                new Vector2(halfSize.x, -halfSize.y)
            }
        );
    }

    private Vector2 LocalToSimulationPoint(Vector2 localPoint)
    {
        float radians = RotationZ * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        Vector2 rotated = new Vector2
        (
            localPoint.x * cos - localPoint.y * sin,
            localPoint.x * sin + localPoint.y * cos
        );

        return Position + rotated;
    }

    private IEnumerator SquashRoutine(float impulseStrength)
    {
        float punch = Mathf.Clamp
        (
            impulseStrength * 0.002f,
            squashStrengthMin,
            squashStrengthMax
        );

        float timer = 0f;

        while (timer < squashDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / squashDuration);
            float wave = Mathf.Sin(t * Mathf.PI);

            rectTransform.localScale = Vector3.one * (1f + punch * wave);

            yield return null;
        }

        rectTransform.localScale = Vector3.one;
        squashCoroutine = null;
    }

    private static bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            bool intersect =
                ((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x);

            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abSqr = Vector2.Dot(ab, ab);

        if (abSqr <= Mathf.Epsilon)
        {
            return a;
        }

        float t = Vector2.Dot(point - a, ab) / abSqr;
        t = Mathf.Clamp01(t);

        return a + ab * t;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
}