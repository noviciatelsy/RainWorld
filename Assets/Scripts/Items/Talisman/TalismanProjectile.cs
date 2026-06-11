using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TalismanProjectile : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D talismanRigidbody;
    // 符纸自身的 Rigidbody2D

    private BoxCollider2D talismanCollider;
    // 符纸自身的 Collider2D

    private Transform detectionCenter;
    // 消灭检测中心
  

    [Header("Flight Settings")]
    [SerializeField] private float gravityScale = 0.35f;
    // 符纸受到的重力比例
    // 比火把小一些，所以飞行轨迹更轻、更飘

    [SerializeField] private bool rotateWithVelocity = true;
    // 是否根据飞行速度调整符纸朝向

    [SerializeField] private float spriteRotationOffset = -90f;
    // 贴图朝向修正角度
    //
    // 默认认为符纸图片在 Rotation = 0 时朝向右方
    // 如果你的符纸图片默认朝上，可以尝试设为 -90

    [SerializeField] private float minimumDirectionSpeed = 0.05f;
    // 速度低于该值时，不再刷新符纸朝向

    [SerializeField] private float maximumFlightDuration = 8f;
    // 符纸没有碰到地面时的最大存在时间
    // 防止符纸飞出地图后永远不消失


    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayerMask;
    // 能够触发符纸消灭检测的地面层


    [Header("Exterminate Detection")]
    [SerializeField] private LayerMask exterminableMonsterLayerMask;
    // 可被符纸消灭的怪物所在 Layer

    [SerializeField] private float exterminateRadius = 16f;
    // 符纸落地后的消灭检测半径


    private bool hasBeenInitialized;
    // 是否已经被投掷器初始化

    private bool hasTriggered;
    // 是否已经触发过落地检测

    private float currentFlightElapsedTime;
    // 已经飞行的时间

    private Vector2 lastFlightDirection = Vector2.right;
    // 最近一次有效飞行方向


    private void Awake()
    {
        talismanRigidbody = GetComponent<Rigidbody2D>();
        talismanCollider = GetComponent<BoxCollider2D>();
        detectionCenter = transform;

    }


    private void Update()
    {
        if (!hasBeenInitialized || hasTriggered)
        {
            return;
        }

        currentFlightElapsedTime += Time.deltaTime;

        if (maximumFlightDuration <= 0f)
        {
            return;
        }

        if (currentFlightElapsedTime < maximumFlightDuration)
        {
            return;
        }

        Destroy(gameObject);
    }


    private void FixedUpdate()
    {
        if (!hasBeenInitialized || hasTriggered)
        {
            return;
        }

        if (!rotateWithVelocity)
        {
            return;
        }

        UpdateFlightRotation();
    }


    /// <summary>
    /// 初始化符纸。
    /// </summary>
    /// <param name="myInitialVelocity">
    /// 初始投掷速度。
    /// </param>
    /// <param name="myOwnerColliders">
    /// 需要忽略碰撞的投掷者碰撞体。
    /// </param>
    public void Initialize(
        Vector2 myInitialVelocity)
    {
        hasBeenInitialized = true;
        hasTriggered = false;
        currentFlightElapsedTime = 0f;

        talismanRigidbody.bodyType = RigidbodyType2D.Dynamic;
        talismanRigidbody.simulated = true;
        talismanRigidbody.gravityScale = gravityScale;

        SetRigidbodyVelocity(Vector2.zero);
        talismanRigidbody.angularVelocity = 0f;

        if (myInitialVelocity.sqrMagnitude > 0.0001f)
        {
            lastFlightDirection = myInitialVelocity.normalized;

            float initialRotation =
                CalculateRotationFromDirection(lastFlightDirection);

            talismanRigidbody.SetRotation(initialRotation);
        }

        SetRigidbodyVelocity(myInitialVelocity);
    }


    /// <summary>
    /// 根据当前速度调整符纸朝向。
    /// </summary>
    private void UpdateFlightRotation()
    {
        Vector2 currentVelocity =
            GetRigidbodyVelocity();

        float minimumSpeedSqr =
            minimumDirectionSpeed * minimumDirectionSpeed;

        if (currentVelocity.sqrMagnitude < minimumSpeedSqr)
        {
            return;
        }

        lastFlightDirection = currentVelocity.normalized;

        float targetRotation =
            CalculateRotationFromDirection(lastFlightDirection);

        talismanRigidbody.MoveRotation(targetRotation);
    }


    private float CalculateRotationFromDirection(Vector2 myDirection)
    {
        float directionAngle =
            Mathf.Atan2(myDirection.y, myDirection.x)
            * Mathf.Rad2Deg;

        return directionAngle + spriteRotationOffset;
    }


    private void OnCollisionEnter2D(Collision2D myCollision)
    {
        if (!hasBeenInitialized || hasTriggered)
        {
            return;
        }

        int collisionLayer =
            myCollision.collider.gameObject.layer;

        if (!IsLayerInMask(collisionLayer, groundLayerMask))
        {
            return;
        }

        TriggerTalismanEffect();
    }


    /// <summary>
    /// 触发符纸落地效果。
    /// 只会执行一次。
    /// </summary>
    private void TriggerTalismanEffect()
    {
        if (hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        SetRigidbodyVelocity(Vector2.zero);
        talismanRigidbody.angularVelocity = 0f;

        TriggerExterminateDetection();

        Destroy(gameObject);
    }


    /// <summary>
    /// 在圆形范围内检测可被符纸消灭的怪物。
    /// </summary>
    private void TriggerExterminateDetection()
    {
        Vector2 centerPosition =
            GetDetectionCenterPosition();

        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll(
                centerPosition,
                exterminateRadius,
                exterminableMonsterLayerMask);

        HashSet<MonoBehaviour> triggeredTargets =
            new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider =
                detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<ITalismanExterminable>(
                    detectedCollider);

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            ITalismanExterminable exterminableMonster =
                interfaceBehaviour as ITalismanExterminable;

            exterminableMonster?.ExterminateByTalisman(
                centerPosition);
        }
    }


    /// <summary>
    /// 从碰撞体自身及其父物体上寻找实现指定接口的脚本。
    /// 
    /// 这样可以支持：
    /// EnemyRoot
    /// └── HitBox
    ///     └── Collider2D
    ///
    /// 接口脚本挂在 EnemyRoot 上，
    /// Collider2D 挂在子物体上。
    /// </summary>
    private MonoBehaviour FindInterfaceBehaviourInParents<T>(
        Collider2D myCollider)
        where T : class
    {
        MonoBehaviour[] parentBehaviours =
            myCollider.GetComponentsInParent<MonoBehaviour>();

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            MonoBehaviour currentBehaviour =
                parentBehaviours[i];

            if (currentBehaviour is T)
            {
                return currentBehaviour;
            }
        }

        return null;
    }


    private Vector2 GetDetectionCenterPosition()
    {
        if (detectionCenter != null)
        {
            return detectionCenter.position;
        }

        return transform.position;
    }


    private bool IsLayerInMask(
        int myLayer,
        LayerMask myLayerMask)
    {
        int layerValue =
            1 << myLayer;

        return (myLayerMask.value & layerValue) != 0;
    }


    /// <summary>
    /// 获取 Rigidbody2D 的速度。
    /// 
    /// 如果晴将使用 Unity 6，可以把这里改成：
    /// return talismanRigidbody.linearVelocity;
    /// 
    /// 如果使用 Unity 2022 / 2023，velocity 更常见。
    /// </summary>
    private Vector2 GetRigidbodyVelocity()
    {
        return talismanRigidbody.velocity;
    }


    /// <summary>
    /// 设置 Rigidbody2D 的速度。
    /// 
    /// 如果晴将使用 Unity 6，可以把这里改成：
    /// talismanRigidbody.linearVelocity = myVelocity;
    /// </summary>
    private void SetRigidbodyVelocity(Vector2 myVelocity)
    {
        talismanRigidbody.velocity = myVelocity;
    }


    private void OnDrawGizmosSelected()
    {
        Vector3 centerPosition;

        if (detectionCenter != null)
        {
            centerPosition = detectionCenter.position;
        }
        else
        {
            centerPosition = transform.position;
        }

        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            centerPosition,
            exterminateRadius);
    }
}