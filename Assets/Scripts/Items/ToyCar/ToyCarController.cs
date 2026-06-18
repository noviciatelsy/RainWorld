using UnityEngine;


public class ToyCarController : MonoBehaviour{
    [Header("References")]
    private Rigidbody2D toyCarRigidbody;
    // ?????????? Rigidbody2D

    private Collider2D toyCarCollider;
    // ?????????? Collider2D

    [SerializeField] private Transform visualRoot;
    // ????????
    // ?????? Animator ?????????
    // ?????????????????????? Rigidbody ??????

    [SerializeField] private Transform frontCheck;
    // ???????
    // ????????????????????????

    private Transform attractionCenter;
    // ???????????


    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.5f;
    // ????????????

    [SerializeField] private bool spriteFacesRightByDefault = true;
    // ???????????????

    [SerializeField] private float reverseCooldown = 0.12f;
    // ??????
    // ??????????????????????

    [SerializeField] private bool freezeRotationOnAwake = true;
    // ????? Awake ?????????
    // ??????????????????????????????


    [Header("Front Ground Check")]
    [SerializeField] private LayerMask groundLayerMask;
    // Ground ??

    [SerializeField] private float frontCheckDistance = 0.08f;
    // ????????????

    [SerializeField] private bool useRaycastFrontCheck = true;


    [Header("Life Time")]
    [SerializeField] private float lifeDuration = 10f;
    // ??????????


    private bool hasBeenInitialized;
    // ???????????

    private int moveDirection = 1;
    // ??????????
    // 1 ???
    // -1 ???

    private float currentLifeTime;
    // ??????????

    private float nextAllowedReverseTime;
    // ?????????????????

    private float frontCheckOriginalLocalX;
    // ????????????? X ?????????

    private Vector3 visualRootOriginalScale;
    // ??????????????

    private AudioSource toyCarSFXAudioSource;

    public bool IsAttracting => hasBeenInitialized;

    public Vector2 AttractionCenter => GetAttractionCenterPosition();


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
        toyCarSFXAudioSource= AudioManager.Instance.PlayLoopSFX("ToyCarSFX");
    }


    private void Update()
    {
        if (!hasBeenInitialized)
        {
            return;
        }

        UpdateLifeTime();
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
    /// ???????????
    /// </summary>
    /// <param name="myMoveDirection">
    /// ??????????
    /// ??????? 0 ???????????? 0 ???????
    /// </param>
    public void Initialize(int myMoveDirection)
    {
        hasBeenInitialized = true;

        currentLifeTime = 0f;

        moveDirection =
            myMoveDirection >= 0 ? 1 : -1;

        UpdateFacingVisual();
        UpdateFrontCheckPosition();
        ApplyHorizontalMovement();

        ToyCarRegistry.Register(this);
    }


    private void OnDestroy()
    {
        ToyCarRegistry.Unregister(this);
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
        toyCarSFXAudioSource.Stop();
        Destroy(gameObject);
    }


    /// <summary>
    /// ???? Rigidbody2D ?????????????????????????
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
    /// ?????????????? Ground??
    /// ???????????
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
    /// ????????
    /// ???????????????????????????????? Ground??
    /// ??????????????????????????
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