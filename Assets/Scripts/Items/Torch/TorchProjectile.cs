using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TorchProjectile : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D torchRigidbody;
    // ????????? Rigidbody2D

    private Collider2D torchCollider;
    // ????????? Collider2D

    private Transform effectCenter;
    // ????????????????
    // ??????????????????
    // ??????????????????????????

    [Header("Flight Settings")]
    [SerializeField] private float spriteRotationOffset = -90f;
    // ??????????????????
    // ?????????????????????? -90

    [SerializeField] private float minimumDirectionSpeed = 0.05f;
    // ??????????????????????????????
    // ????????? 0 ?????????????

    [SerializeField] private float maximumFlightDuration = 10f;
    // ???????????????????????????
    // ?????????????????????????

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayerMask;
    // ?????????????????????

    [SerializeField] private float embedDistance = 0.08f;
    // ????????????????????????????

    [SerializeField] private float stuckDuration = 3f;
    // ?????????????????


    [Header("Ignition Detection")]
    [SerializeField] private LayerMask ignitableLayerMask;
    // ????????????? Layer

    [SerializeField] private float ignitionRadius = 1.2f;
    // ???????


    [Header("Enemy Repel Detection")]
    [SerializeField] private LayerMask repellableEnemyLayerMask;
    // ??????????????????? Layer

    [SerializeField] private float repelRadius = 3f;
    // ???????

    [SerializeField] private float repelDetectInterval = 0.25f;
    // ??????????????????????


    private bool hasBeenInitialized;
    // ?????????????????????

    private bool isStuck;
    // ???????????????

    private float currentStateElapsedTime;
    // ????????????????????????????

    private Vector2 lastFlightDirection = Vector2.right;
    // ???????????????????
    // ???????????????????

    private float repelDetectTimer;

    public bool IsRepelActive => isStuck;

    public Vector2 RepelCenterPosition => GetEffectCenterPosition();

    public float RepelRadius => repelRadius;

    public bool IsPointInsideRepelRadius(Vector2 worldPoint)
    {
        float radius = Mathf.Max(0f, repelRadius);
        return (worldPoint - RepelCenterPosition).sqrMagnitude < radius * radius;
    }


    private void Awake()
    {
        torchRigidbody = GetComponent<Rigidbody2D>();
        torchCollider = GetComponent<Collider2D>();
        effectCenter = transform;
    }


    private void OnDisable()
    {
        TorchRegistry.Unregister(this);
    }


    private void OnDestroy()
    {
        TorchRegistry.Unregister(this);
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
    /// ?????????
    /// </summary>
    /// <param name="myInitialVelocity">
    /// ?????????
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
    /// ???????????????????????
    /// ??????? X ?????????????????????
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
    /// ?????????? 2D ???????
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
    /// ???????????g?????????
    /// </summary>
    private void StickIntoGround(Collision2D myCollision)
    {
        if (isStuck)
        {
            return;
        }

        isStuck = true;
        currentStateElapsedTime = 0f;
        repelDetectTimer = 0f;

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

        // ??? Rigidbody2D ??????
        // 1. ?????????????
        // 2. ?????????????????
        // 3. ???? Collider2D ???????????????
        torchRigidbody.simulated = false;

        TorchRegistry.Register(this);
        TriggerIgnitionDetection();
        TriggerEnemyRepelDetection();
    }


    /// <summary>
    /// ????????????????????????????
    /// ??????????????????????????
    /// </summary>
    private void TriggerIgnitionDetection()
    {
        Vector2 detectionCenter = GetEffectCenterPosition();

        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll(
                detectionCenter,
                ignitionRadius,
                ignitableLayerMask);

        // ???????????????? Collider2D??
        // ??? HashSet ?????????????????????????
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
    /// ???????????????????????????
    /// ??????????????????????????
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
    /// ???????????????????????
    /// ????????????????? MonoBehaviour??
    ///
    /// ????????? Collider2D ????????????????
    /// ??????????????????????????
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
        repelDetectTimer -= Time.deltaTime;
        if (repelDetectTimer <= 0f)
        {
            repelDetectTimer = Mathf.Max(0.05f, repelDetectInterval);
            TriggerEnemyRepelDetection();
        }

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