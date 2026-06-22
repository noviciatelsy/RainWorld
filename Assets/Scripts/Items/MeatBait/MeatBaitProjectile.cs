using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MeatBaitProjectile : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D meatBaitRigidbody;
    // ????????? Rigidbody2D

    private Collider2D meatBaitCollider;
    // ????????? Collider2D

    private Transform effectCenter;
    // ????????????
    // ?????????????????


    [Header("Flight Settings")]
    [SerializeField] private float spriteRotationOffset = -90f;
    // ??????????????????
    // ??????????????????????? -90

    [SerializeField] private float minimumDirectionSpeed = 0.05f;
    // ??????????????????????????????
    // ????????? 0 ?????????????

    [SerializeField] private float maximumFlightDuration = 10f;
    // ???????????????????????????
    // ??????????????????????????


    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayerMask;
    // ??????????????????????

    [SerializeField] private float embedDistance = 0.08f;
    // ????????????????????????????

    [SerializeField] private float stuckDuration = 10f;
    // ?????????????????


    [Header("Wave Light Effect")]
    [SerializeField] private float effectRadius = 4f;
    [SerializeField] private WaveLightEffect waveLightEffectPrefab;
    [SerializeField] private int effectCenterAlpha = 40;
    [SerializeField] private int effectWaveStartAlpha = 40;
    [SerializeField] private Color effectColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float effectWavePeriod = 1f;
    [SerializeField] private float effectWaveExpandDuration = 1.5f;


    public bool IsAttracting => isStuck;

    public Vector2 AttractionCenter => GetEffectCenterPosition();

    /// <summary>
    /// 怪物啃食时调用：播放音效并销毁肉饵。
    /// </summary>
    public void ConsumeByEnemy()
    {
        AudioManager.Instance?.PlaySFX(ItemAudioPaths.MonsterEatMeatBait, randomPitch: false);
        Destroy(gameObject);
    }

    public static bool TryConsumeFromTransform(Transform preyTransform)
    {
        if (preyTransform == null)
        {
            return false;
        }

        MeatBaitProjectile meatBait = preyTransform.GetComponentInParent<MeatBaitProjectile>();
        if (meatBait == null)
        {
            return false;
        }

        meatBait.ConsumeByEnemy();
        return true;
    }


    private bool hasBeenInitialized;
    // ?????????????????????

    private bool isStuck;
    // ???????????????

    private float currentStateElapsedTime;
    // ????????????????????????????

    private Vector2 lastFlightDirection = Vector2.right;

    private WaveLightEffect activeWaveLightEffect;


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
    /// ??????????
    /// </summary>
    /// <param name="myInitialVelocity">
    /// ???????????
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
    /// ????????????????????????
    /// ???????? X ?????????????????????
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

        int collisionLayer =
            myCollision.collider.gameObject.layer;

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

        // ??? Rigidbody2D ??????
        // 1. ?????????????
        // 2. ?????????????????
        // 3. ???? Collider2D ???????????????
        meatBaitRigidbody.simulated = false;

        MeatBaitRegistry.Register(this);
        SpawnWaveLightEffect();
    }


    private void OnDestroy()
    {
        MeatBaitRegistry.Unregister(this);
        ClearWaveLightEffect();
    }


    private void SpawnWaveLightEffect()
    {
        if (waveLightEffectPrefab == null)
        {
            Debug.LogWarning($"{nameof(MeatBaitProjectile)}: waveLightEffectPrefab 未配置。", this);
            return;
        }

        ClearWaveLightEffect();

        Transform attachPoint = effectCenter != null ? effectCenter : transform;
        activeWaveLightEffect = Instantiate(waveLightEffectPrefab, attachPoint);
        activeWaveLightEffect.transform.localPosition = Vector3.zero;
        activeWaveLightEffect.transform.localRotation = Quaternion.identity;
        activeWaveLightEffect.PlayAttached(
            effectRadius,
            0f,
            effectCenterAlpha,
            effectWaveStartAlpha,
            effectColor,
            effectWavePeriod,
            effectWaveExpandDuration);
    }


    private void ClearWaveLightEffect()
    {
        if (activeWaveLightEffect == null)
        {
            return;
        }

        activeWaveLightEffect.StopEffect();
        Destroy(activeWaveLightEffect.gameObject);
        activeWaveLightEffect = null;
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
            effectRadius
        );
    }
}