using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TorchProjectile : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D torchRigidbody;
    // 火把自身的 Rigidbody2D

    private Collider2D torchCollider;
    // 火把自身的 Collider2D

    private Transform effectCenter;
    // 点燃和驱赶检测的中心
    // 建议放在火把燃烧端的位置
    // 如果没有设置，则使用火把自身位置

    [Header("Flight Settings")]
    [SerializeField] private float spriteRotationOffset = -90f;
    // 火把贴图的朝向修正角度
    // 如果贴图默认朝上，通常需要设为 -90

    [SerializeField] private float minimumDirectionSpeed = 0.05f;
    // 速度低于这个数值时，不再刷新飞行朝向
    // 避免速度接近 0 时产生不稳定旋转

    [SerializeField] private float maximumFlightDuration = 10f;
    // 火把一直没有碰到地面时的最大存活时间
    // 防止火把飞出地图后永远留在场景中

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayerMask;
    // 能够让火把嵌入并停下来的地面层

    [SerializeField] private float embedDistance = 0.08f;
    // 碰到地面后继续向飞行方向嵌入的距离

    [SerializeField] private float stuckDuration = 3f;
    // 火把嵌入地面后保留的时间


    [Header("Ignition Detection")]
    [SerializeField] private LayerMask ignitableLayerMask;
    // 可点燃物体所在的 Layer

    [SerializeField] private float ignitionRadius = 1.2f;
    // 点燃检测半径


    [Header("Enemy Repel Detection")]
    [SerializeField] private LayerMask repellableEnemyLayerMask;
    // 可被火把驱赶的敌人所在的 Layer

    [SerializeField] private float repelRadius = 3f;
    // 驱赶检测半径


    private bool hasBeenInitialized;
    // 火把是否已经由投掷器初始化

    private bool isStuck;
    // 火把是否已经嵌入地面

    private float currentStateElapsedTime;
    // 当前飞行状态或嵌入状态已经经过的时间

    private Vector2 lastFlightDirection = Vector2.right;
    // 最近一次有效的飞行方向
    // 在碰撞瞬间用于确定嵌入方向


    private void Awake()
    {
        torchRigidbody = GetComponent<Rigidbody2D>();
        torchCollider = GetComponent<Collider2D>();
        effectCenter = transform;
    }


    private void Update()
    {
        if (!hasBeenInitialized)
        {
            return;
        }

        currentStateElapsedTime += Time.deltaTime;

        if (isStuck)
        {
            UpdateStuckLifetime();
        }
        else
        {
            UpdateFlightLifetime();
        }
    }


    private void FixedUpdate()
    {
        if (!hasBeenInitialized || isStuck)
        {
            return;
        }

        UpdateFlightRotation();
    }


    /// <summary>
    /// 初始化火把。
    /// </summary>
    /// <param name="myInitialVelocity">
    /// 火把的初始速度
    public void Initialize(Vector2 myInitialVelocity)
    {
        hasBeenInitialized = true;
        isStuck = false;
        currentStateElapsedTime = 0f;


        torchRigidbody.velocity = Vector2.zero;
        torchRigidbody.angularVelocity = 0f;

        if (myInitialVelocity.sqrMagnitude > 0.0001f)
        {
            lastFlightDirection = myInitialVelocity.normalized;

            float initialRotation = CalculateRotationFromDirection(
                lastFlightDirection);

            torchRigidbody.SetRotation(initialRotation);
        }

        torchRigidbody.velocity = myInitialVelocity;
    }


    /// <summary>
    /// 根据火把当前的飞行速度调整朝向。
    /// 火把的本地 X 轴正方向被视为火把头部方向。
    /// </summary>
    private void UpdateFlightRotation()
    {
        Vector2 currentVelocity = torchRigidbody.velocity;

        float minimumSpeedSqr =
            minimumDirectionSpeed * minimumDirectionSpeed;

        if (currentVelocity.sqrMagnitude < minimumSpeedSqr)
        {
            return;
        }

        lastFlightDirection = currentVelocity.normalized;

        float targetRotation = CalculateRotationFromDirection(
            lastFlightDirection);

        torchRigidbody.MoveRotation(targetRotation);
    }


    /// <summary>
    /// 将方向转换为 2D 旋转角度。
    /// </summary>
    private float CalculateRotationFromDirection(Vector2 myDirection)
    {
        float directionAngle =
            Mathf.Atan2(myDirection.y, myDirection.x)
            * Mathf.Rad2Deg;

        return directionAngle + spriteRotationOffset;
    }


    private void OnCollisionEnter2D(Collision2D myCollision)
    {
        if (!hasBeenInitialized || isStuck)
        {
            return;
        }

        int collisionLayer = myCollision.collider.gameObject.layer;

        if (!IsLayerInMask(collisionLayer, groundLayerMask))
        {

            return;

        }

        StickIntoGround(myCollision);
    }


    /// <summary>
    /// 将火把嵌入地面并停止物理模拟。
    /// </summary>
    private void StickIntoGround(Collision2D myCollision)
    {
        if (isStuck)
        {
            return;
        }

        isStuck = true;
        currentStateElapsedTime = 0f;

        Vector2 embedDirection = lastFlightDirection;

        if (embedDirection.sqrMagnitude < 0.0001f
            && myCollision.contactCount > 0)
        {
            ContactPoint2D contactPoint =
                myCollision.GetContact(0);

            embedDirection = -contactPoint.normal;
        }

        embedDirection.Normalize();

        Vector2 embeddedPosition =
            torchRigidbody.position
            + embedDirection * embedDistance;

        torchRigidbody.velocity = Vector2.zero;
        torchRigidbody.angularVelocity = 0f;

        torchRigidbody.position = embeddedPosition;

        // 关闭 Rigidbody2D 的模拟后：
        // 1. 不再受重力影响
        // 2. 不再因碰撞发生位移
        // 3. 附属 Collider2D 也不再参与物理模拟
        torchRigidbody.simulated = false;

        TriggerIgnitionDetection();
        TriggerEnemyRepelDetection();
    }


    /// <summary>
    /// 在小范围内寻找并点燃所有可点燃目标。
    /// 该方法只会在火把嵌入时调用一次。
    /// </summary>
    private void TriggerIgnitionDetection()
    {
        Vector2 detectionCenter = GetEffectCenterPosition();

        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll(
                detectionCenter,
                ignitionRadius,
                ignitableLayerMask);

        // 一个物体可能拥有多个 Collider2D。
        // 使用 HashSet 避免同一个接口组件被重复调用。
        HashSet<MonoBehaviour> triggeredTargets =
            new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider =
                detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<IIgnitable>(
                    detectedCollider);

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            IIgnitable ignitable =
                interfaceBehaviour as IIgnitable;

            ignitable?.Ignite();
        }
    }


    /// <summary>
    /// 在较大范围内寻找并驱赶所有怕火敌人。
    /// 该方法只会在火把嵌入时调用一次。
    /// </summary>
    private void TriggerEnemyRepelDetection()
    {
        Vector2 detectionCenter = GetEffectCenterPosition();

        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll(
                detectionCenter,
                repelRadius,
                repellableEnemyLayerMask);

        HashSet<MonoBehaviour> triggeredTargets =
            new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider =
                detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<ITorchRepellable>(
                    detectedCollider);

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            ITorchRepellable repellableEnemy =
                interfaceBehaviour as ITorchRepellable;

            repellableEnemy?.FleeFromTorch(detectionCenter);
        }
    }


    /// <summary>
    /// 从碰撞体自身及其父物体上，
    /// 寻找第一个实现指定接口的 MonoBehaviour。
    ///
    /// 这样敌人的 Collider2D 可以放在子物体上，
    /// 而敌人控制脚本可以放在根物体上。
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


    private Vector2 GetEffectCenterPosition()
    {
        if (effectCenter != null)
        {
            return effectCenter.position;
        }

        return torchRigidbody.position;
    }


    private void UpdateFlightLifetime()
    {
        if (maximumFlightDuration <= 0f)
        {
            return;
        }

        if (currentStateElapsedTime < maximumFlightDuration)
        {
            return;
        }

        Destroy(gameObject);
    }


    private void UpdateStuckLifetime()
    {
        if (currentStateElapsedTime < stuckDuration)
        {
            return;
        }

        Destroy(gameObject);
    }


    private bool IsLayerInMask(
        int myLayer,
        LayerMask myLayerMask)
    {
        int layerValue = 1 << myLayer;

        return (myLayerMask.value & layerValue) != 0;
    }


    private void OnDrawGizmosSelected()
    {
        Vector3 centerPosition;

        if (effectCenter != null)
        {
            centerPosition = effectCenter.position;
        }
        else
        {
            centerPosition = transform.position;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            centerPosition,
            ignitionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            centerPosition,
            repelRadius);
    }
}