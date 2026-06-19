using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TalismanProjectile : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D talismanRigidbody;
    private BoxCollider2D talismanCollider;
    private Transform detectionCenter;

    [Header("Flight Settings")]
    [SerializeField] private float gravityScale = 0.35f;
    [SerializeField] private bool rotateWithVelocity = true;
    [SerializeField] private float spriteRotationOffset = -90f;
    [SerializeField] private float minimumDirectionSpeed = 0.05f;
    [SerializeField] private float maximumFlightDuration = 8f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayerMask;

    [Header("Exterminate Detection")]
    [SerializeField] private LayerMask exterminableMonsterLayerMask;
    [SerializeField] private float exterminateRadius = 16f;
    [SerializeField] private float homingDuration = 0.5f;

    private bool hasBeenInitialized;
    private bool hasTriggered;
    private float currentFlightElapsedTime;
    private Vector2 lastFlightDirection = Vector2.right;

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

    public void Initialize(Vector2 myInitialVelocity)
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
            float initialRotation = CalculateRotationFromDirection(lastFlightDirection);
            talismanRigidbody.SetRotation(initialRotation);
        }

        SetRigidbodyVelocity(myInitialVelocity);
    }

    private void UpdateFlightRotation()
    {
        Vector2 currentVelocity = GetRigidbodyVelocity();
        float minimumSpeedSqr = minimumDirectionSpeed * minimumDirectionSpeed;

        if (currentVelocity.sqrMagnitude < minimumSpeedSqr)
        {
            return;
        }

        lastFlightDirection = currentVelocity.normalized;
        float targetRotation = CalculateRotationFromDirection(lastFlightDirection);
        talismanRigidbody.MoveRotation(targetRotation);
    }

    private float CalculateRotationFromDirection(Vector2 myDirection)
    {
        float directionAngle = Mathf.Atan2(myDirection.y, myDirection.x) * Mathf.Rad2Deg;
        return directionAngle + spriteRotationOffset;
    }

    private void OnCollisionEnter2D(Collision2D myCollision)
    {
        if (!hasBeenInitialized || hasTriggered)
        {
            return;
        }

        int collisionLayer = myCollision.collider.gameObject.layer;
        if (!IsLayerInMask(collisionLayer, groundLayerMask))
        {
            return;
        }

        TriggerTalismanEffect();
    }

    private void TriggerTalismanEffect()
    {
        if (hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        SetRigidbodyVelocity(Vector2.zero);
        talismanRigidbody.angularVelocity = 0f;
        talismanRigidbody.simulated = false;

        if (talismanCollider != null)
        {
            talismanCollider.enabled = false;
        }

        Vector2 centerPosition = GetDetectionCenterPosition();
        ITalismanExterminable nearestTarget = FindNearestExterminableTarget(centerPosition);

        if (nearestTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        nearestTarget.ExterminateByTalisman(centerPosition);

        MonoBehaviour targetBehaviour = nearestTarget as MonoBehaviour;
        if (targetBehaviour != null)
        {
            TalismanTargetFly.Begin(gameObject, targetBehaviour.transform, homingDuration);
            return;
        }

        Destroy(gameObject);
    }

    private ITalismanExterminable FindNearestExterminableTarget(Vector2 centerPosition)
    {
        Collider2D[] detectedColliders =
            Physics2D.OverlapCircleAll(
                centerPosition,
                exterminateRadius,
                exterminableMonsterLayerMask);

        ITalismanExterminable nearestTarget = null;
        float nearestDistanceSqr = float.MaxValue;
        HashSet<MonoBehaviour> visitedTargets = new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<ITalismanExterminable>(
                    detectedColliders[i]);

            if (interfaceBehaviour == null || !visitedTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            ITalismanExterminable exterminableMonster =
                interfaceBehaviour as ITalismanExterminable;

            if (exterminableMonster == null)
            {
                continue;
            }

            float distanceSqr =
                (interfaceBehaviour.transform.position - (Vector3)centerPosition).sqrMagnitude;

            if (distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            nearestDistanceSqr = distanceSqr;
            nearestTarget = exterminableMonster;
        }

        return nearestTarget;
    }

    private MonoBehaviour FindInterfaceBehaviourInParents<T>(Collider2D myCollider)
        where T : class
    {
        MonoBehaviour[] parentBehaviours = myCollider.GetComponentsInParent<MonoBehaviour>();

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            MonoBehaviour currentBehaviour = parentBehaviours[i];
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

    private bool IsLayerInMask(int myLayer, LayerMask myLayerMask)
    {
        int layerValue = 1 << myLayer;
        return (myLayerMask.value & layerValue) != 0;
    }

    private Vector2 GetRigidbodyVelocity()
    {
        return talismanRigidbody.velocity;
    }

    private void SetRigidbodyVelocity(Vector2 myVelocity)
    {
        talismanRigidbody.velocity = myVelocity;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centerPosition = detectionCenter != null
            ? detectionCenter.position
            : transform.position;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(centerPosition, exterminateRadius);
    }
}
