using System.Collections.Generic;
using UnityEngine;


public class ToyCarController : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D toyCarRigidbody;
    // 玩具车自身的 Rigidbody2D

    private Collider2D toyCarCollider;
    // 玩具车自身的 Collider2D

    [SerializeField] private Transform visualRoot;
    // 视觉根节点
    // 建议拖 Animator 子物体进来
    // 用来控制小车朝向，不直接翻转 Rigidbody 根物体

    [SerializeField] private Transform frontCheck;
    // 车头检测点
    // 建议新建一个空物体放在车头前方

    private Transform attractionCenter;
    // 吸引检测中心


    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.5f;
    // 玩具车水平移动速度

    [SerializeField] private bool spriteFacesRightByDefault = true;
    // 玩具车贴图默认是否朝右

    [SerializeField] private float reverseCooldown = 0.12f;
    // 调头冷却
    // 防止车头贴着墙时疯狂左右横跳

    [SerializeField] private bool freezeRotationOnAwake = true;
    // 是否在 Awake 中冻结旋转
    // 一般建议开启，不然小车可能因为碰撞翻车


    [Header("Front Ground Check")]
    [SerializeField] private LayerMask groundLayerMask;
    // Ground 层

    [SerializeField] private float frontCheckDistance = 0.08f;
    // 车头向前检测距离

    [SerializeField] private bool useRaycastFrontCheck = true;
    // 是否使用射线检测车头前方
    // 建议开启


    [Header("Attraction Detection")]
    [SerializeField] private LayerMask attractableEnemyLayerMask;
    // 可被玩具车吸引的敌人所在层

    [SerializeField] private float attractionRadius = 3f;
    // 吸引检测半径

    [SerializeField] private float attractionDetectInterval = 0.5f;
    // 每隔多久检测一次

    [SerializeField] private bool detectImmediatelyOnSpawn = true;
    // 生成后是否立刻检测一次


    [Header("Life Time")]
    [SerializeField] private float lifeDuration = 10f;
    // 玩具车存活时间


    private bool hasBeenInitialized;
    // 是否已经初始化

    private int moveDirection = 1;
    // 当前移动方向
    // 1 为右
    // -1 为左

    private float currentLifeTime;
    // 当前已存活时间

    private float attractionDetectTimer;
    // 吸引检测计时器

    private float nextAllowedReverseTime;
    // 下一次允许调头的时间

    private float frontCheckOriginalLocalX;
    // 车头检测点原始本地 X 位置的绝对值

    private Vector3 visualRootOriginalScale;
    // 视觉根节点原始缩放


    private void Awake()
    {
        toyCarRigidbody = GetComponent<Rigidbody2D>();
        toyCarCollider = GetComponent<Collider2D>();
        attractionCenter = transform;


        visualRootOriginalScale =
            visualRoot.localScale;

        if (frontCheck != null)
        {
            frontCheckOriginalLocalX =
                Mathf.Abs(frontCheck.localPosition.x);
        }

        if (freezeRotationOnAwake)
        {
            toyCarRigidbody.freezeRotation = true;
        }
    }


    private void Start()
    {
        if (!hasBeenInitialized)
        {
            Initialize(1);
        }
    }


    private void Update()
    {
        if (!hasBeenInitialized)
        {
            return;
        }

        UpdateLifeTime();

        UpdateAttractionDetectionTimer();
    }


    private void FixedUpdate()
    {
        if (!hasBeenInitialized)
        {
            return;
        }

        if (useRaycastFrontCheck)
        {
            CheckFrontGroundByRaycast();
        }

        ApplyHorizontalMovement();
    }


    /// <summary>
    /// 初始化玩具车。
    /// </summary>
    /// <param name="myMoveDirection">
    /// 初始移动方向。
    /// 大于等于 0 视为向右，小于 0 视为向左。
    /// </param>
    public void Initialize(int myMoveDirection)
    {
        hasBeenInitialized = true;

        currentLifeTime = 0f;

        moveDirection =
            myMoveDirection >= 0 ? 1 : -1;

        if (detectImmediatelyOnSpawn)
        {
            attractionDetectTimer = 0f;
        }
        else
        {
            attractionDetectTimer =
                attractionDetectInterval;
        }

        UpdateFacingVisual();
        UpdateFrontCheckPosition();
        ApplyHorizontalMovement();
    }


    private void UpdateLifeTime()
    {
        if (lifeDuration <= 0f)
        {
            return;
        }

        currentLifeTime += Time.deltaTime;

        if (currentLifeTime < lifeDuration)
        {
            return;
        }

        Destroy(gameObject);
    }


    private void UpdateAttractionDetectionTimer()
    {
        attractionDetectTimer -= Time.deltaTime;

        if (attractionDetectTimer > 0f)
        {
            return;
        }

        DetectAndAttractEnemies();

        attractionDetectTimer =
            Mathf.Max(0.02f, attractionDetectInterval);
    }


    /// <summary>
    /// 保留 Rigidbody2D 的竖直速度，只控制水平速度。
    /// 这样小车仍然会受到重力影响。
    /// </summary>
    private void ApplyHorizontalMovement()
    {
        Vector2 currentVelocity =
            toyCarRigidbody.velocity;

        currentVelocity.x =
            moveDirection * moveSpeed;

        toyCarRigidbody.velocity =
            currentVelocity;
    }


    /// <summary>
    /// 用车头前方射线检测 Ground。
    /// 检测到时调转方向。
    /// </summary>
    private void CheckFrontGroundByRaycast()
    {
        if (frontCheck == null)
        {
            return;
        }

        Vector2 rayOrigin =
            frontCheck.position;

        Vector2 rayDirection =
            Vector2.right * moveDirection;

        RaycastHit2D hit =
            Physics2D.Raycast
            (
                rayOrigin,
                rayDirection,
                frontCheckDistance,
                groundLayerMask
            );

        if (hit.collider == null)
        {
            return;
        }

        ReverseDirection();
    }


    /// <summary>
    /// 作为保险：
    /// 如果射线没有提前检测到，但车头真实碰到了 Ground，
    /// 也通过碰撞法线判断是否需要调头。
    /// </summary>
    private void OnCollisionEnter2D(Collision2D myCollision)
    {
        if (!hasBeenInitialized)
        {
            return;
        }

        int collisionLayer =
            myCollision.collider.gameObject.layer;

        if (!IsLayerInMask(collisionLayer, groundLayerMask))
        {
            return;
        }

        for (int i = 0; i < myCollision.contactCount; i++)
        {
            ContactPoint2D contactPoint =
                myCollision.GetContact(i);

            Vector2 expectedHeadNormal =
                Vector2.left * moveDirection;

            float normalDot =
                Vector2.Dot
                (
                    contactPoint.normal,
                    expectedHeadNormal
                );

            if (normalDot > 0.5f)
            {
                ReverseDirection();
                return;
            }
        }
    }


    private void ReverseDirection()
    {
        if (Time.time < nextAllowedReverseTime)
        {
            return;
        }

        nextAllowedReverseTime =
            Time.time + reverseCooldown;

        moveDirection *= -1;

        UpdateFacingVisual();
        UpdateFrontCheckPosition();
        ApplyHorizontalMovement();
    }


    private void UpdateFacingVisual()
    {
        if (visualRoot == null)
        {
            return;
        }

        int visualDirection =
            spriteFacesRightByDefault
                ? moveDirection
                : -moveDirection;

        Vector3 newScale =
            visualRootOriginalScale;

        newScale.x =
            Mathf.Abs(visualRootOriginalScale.x)
            * visualDirection;

        visualRoot.localScale =
            newScale;
    }


    private void UpdateFrontCheckPosition()
    {
        if (frontCheck == null)
        {
            return;
        }

        Vector3 localPosition =
            frontCheck.localPosition;

        localPosition.x =
            frontCheckOriginalLocalX * moveDirection;

        frontCheck.localPosition =
            localPosition;
    }


    private void DetectAndAttractEnemies()
    {
        Vector2 centerPosition =
            GetAttractionCenterPosition();

        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll
            (
                centerPosition,
                attractionRadius,
                attractableEnemyLayerMask
            );

        HashSet<MonoBehaviour> triggeredTargets =
            new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider =
                detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<IToyCarAttractable>
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

            IToyCarAttractable attractableEnemy =
                interfaceBehaviour as IToyCarAttractable;

            attractableEnemy?.AttractToToyCar
            (
                centerPosition
            );
        }
    }


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


    private Vector2 GetAttractionCenterPosition()
    {
        if (attractionCenter != null)
        {
            return attractionCenter.position;
        }

        return transform.position;
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

        if (attractionCenter != null)
        {
            centerPosition = attractionCenter.position;
        }
        else
        {
            centerPosition = transform.position;
        }

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere
        (
            centerPosition,
            attractionRadius
        );

        if (frontCheck != null)
        {
            Gizmos.color = Color.red;

            Vector3 rayStart =
                frontCheck.position;

            Vector3 rayEnd =
                rayStart
                + Vector3.right * moveDirection * frontCheckDistance;

            Gizmos.DrawLine
            (
                rayStart,
                rayEnd
            );
        }
    }
}