using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MeatBaitProjectile : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D meatBaitRigidbody;
    // 肉饵自身的 Rigidbody2D

    private Collider2D meatBaitCollider;
    // 肉饵自身的 Collider2D

    private Transform effectCenter;
    // 吸引检测的中心
    // 默认使用肉饵自身位置


    [Header("Flight Settings")]
    [SerializeField] private float spriteRotationOffset = -90f;
    // 肉饵贴图的朝向修正角度
    // 如果贴图默认朝上，通常可以设为 -90

    [SerializeField] private float minimumDirectionSpeed = 0.05f;
    // 速度低于这个数值时，不再刷新飞行朝向
    // 避免速度接近 0 时产生不稳定旋转

    [SerializeField] private float maximumFlightDuration = 10f;
    // 肉饵一直没有碰到地面时的最大存活时间
    // 防止肉饵飞出地图后永远留在场景中


    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayerMask;
    // 能够让肉饵嵌入并停下来的地面层

    [SerializeField] private float embedDistance = 0.08f;
    // 碰到地面后继续向飞行方向嵌入的距离

    [SerializeField] private float stuckDuration = 10f;
    // 肉饵嵌入地面后保留的时间


    [Header("Attraction Detection")]
    [SerializeField] private LayerMask attractableMonsterLayerMask;
    // 可被肉饵吸引的怪物所在 Layer

    [SerializeField] private float attractionRadius = 4f;
    // 肉饵吸引检测半径


    private bool hasBeenInitialized;
    // 肉饵是否已经由投掷器初始化

    private bool isStuck;
    // 肉饵是否已经嵌入地面

    private float currentStateElapsedTime;
    // 当前飞行状态或嵌入状态已经经过的时间

    private Vector2 lastFlightDirection = Vector2.right;
    // 最近一次有效飞行方向
    // 在碰撞瞬间用于确定嵌入方向


    private void Awake()
    {
        meatBaitRigidbody = GetComponent<Rigidbody2D>();
        meatBaitCollider = GetComponent<Collider2D>();
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
    /// 初始化肉饵。
    /// </summary>
    /// <param name="myInitialVelocity">
    /// 肉饵的初始速度。
    /// </param>
    public void Initialize(Vector2 myInitialVelocity)
    {
        hasBeenInitialized = true;
        isStuck = false;
        currentStateElapsedTime = 0f;

        meatBaitRigidbody.simulated = true;

        meatBaitRigidbody.velocity = Vector2.zero;
        meatBaitRigidbody.angularVelocity = 0f;

        if (myInitialVelocity.sqrMagnitude > 0.0001f)
        {
            lastFlightDirection = myInitialVelocity.normalized;

            float initialRotation =
                CalculateRotationFromDirection(lastFlightDirection);

            meatBaitRigidbody.SetRotation(initialRotation);
        }

        meatBaitRigidbody.velocity = myInitialVelocity;
    }


    /// <summary>
    /// 根据肉饵当前飞行速度调整朝向。
    /// 肉饵的本地 X 轴正方向被视为肉饵头部方向。
    /// </summary>
    private void UpdateFlightRotation()
    {
        Vector2 currentVelocity =
            meatBaitRigidbody.velocity;

        float minimumSpeedSqr =
            minimumDirectionSpeed * minimumDirectionSpeed;

        if (currentVelocity.sqrMagnitude < minimumSpeedSqr)
        {
            return;
        }

        lastFlightDirection =
            currentVelocity.normalized;

        float targetRotation =
            CalculateRotationFromDirection(lastFlightDirection);

        meatBaitRigidbody.MoveRotation(targetRotation);
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

        int collisionLayer =
            myCollision.collider.gameObject.layer;

        if (!IsLayerInMask(collisionLayer, groundLayerMask))
        {
            return;
        }

        StickIntoGround(myCollision);
    }


    /// <summary>
    /// 将肉饵嵌入地面并停止物理模拟。
    /// </summary>
    private void StickIntoGround(Collision2D myCollision)
    {
        if (isStuck)
        {
            return;
        }

        isStuck = true;
        currentStateElapsedTime = 0f;

        Vector2 embedDirection =
            lastFlightDirection;

        if (embedDirection.sqrMagnitude < 0.0001f
            && myCollision.contactCount > 0)
        {
            ContactPoint2D contactPoint =
                myCollision.GetContact(0);

            embedDirection =
                -contactPoint.normal;
        }

        embedDirection.Normalize();

        Vector2 embeddedPosition =
            meatBaitRigidbody.position
            + embedDirection * embedDistance;

        meatBaitRigidbody.velocity = Vector2.zero;
        meatBaitRigidbody.angularVelocity = 0f;

        meatBaitRigidbody.position =
            embeddedPosition;

        // 关闭 Rigidbody2D 的模拟后：
        // 1. 不再受重力影响
        // 2. 不再因碰撞发生位移
        // 3. 附属 Collider2D 也不再参与物理模拟
        meatBaitRigidbody.simulated = false;

        TriggerAttractionDetection();
    }


    /// <summary>
    /// 在圆形范围内寻找所有可被肉饵吸引的怪物。
    /// 该方法只会在肉饵嵌入时调用一次。
    /// </summary>
    private void TriggerAttractionDetection()
    {
        Vector2 detectionCenter =
            GetEffectCenterPosition();

        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll
            (
                detectionCenter,
                attractionRadius,
                attractableMonsterLayerMask
            );

        // 一个怪物可能拥有多个 Collider2D。
        // 使用 HashSet 避免同一个接口组件被重复调用。
        HashSet<MonoBehaviour> triggeredTargets =
            new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider =
                detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<IMeatBaitAttractable>
                (
                    detectedCollider
                );

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            IMeatBaitAttractable attractableMonster =
                interfaceBehaviour as IMeatBaitAttractable;

            attractableMonster?.AttractToMeatBait
            (
                detectionCenter
            );
        }
    }


    /// <summary>
    /// 从碰撞体自身及其父物体上，
    /// 寻找第一个实现指定接口的 MonoBehaviour。
    ///
    /// 这样怪物的 Collider2D 可以放在子物体上，
    /// 而怪物控制脚本可以放在根物体上。
    /// </summary>
    private MonoBehaviour FindInterfaceBehaviourInParents<T>
    (
        Collider2D myCollider
    )
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

        return meatBaitRigidbody.position;
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


    private bool IsLayerInMask
    (
        int myLayer,
        LayerMask myLayerMask
    )
    {
        int layerValue =
            1 << myLayer;

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

        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere
        (
            centerPosition,
            attractionRadius
        );
    }
}