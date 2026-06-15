using UnityEngine;

public enum FlyState
{
    Normal,
    Thrown,
    Stunned
}

public class Fly2D : MonsterBase
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Stomp Drop")]
    [SerializeField] private ItemDataSO dropItemData;
    [SerializeField] private PickableObject pickableObjectPrefab;

    [Header("Thrown / Ground")]
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private float thrownSettleSpeed = 0.35f;

    [Header("References")]
    [SerializeField] private FlyWings flyWings;
    [SerializeField] private Rigidbody2D rb;

    public FlyState CurrentState { get; private set; } = FlyState.Normal;
    public bool CanBeStomped => CurrentState == FlyState.Normal;

    private bool behaviorInitialized;

    protected override void Init()
    {
        ai = new FlyUtilityAI(this);
        motor = new FlyMotor(this);
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (flyWings == null)
        {
            flyWings = GetComponent<FlyWings>();
        }
    }

    private void OnEnable()
    {
        FlyRegistry.Register(this);
    }

    private void OnDisable()
    {
        FlyRegistry.Unregister(this);
    }

    protected override void Start()
    {
        base.Start();

        if (!behaviorInitialized)
        {
            InitializeAsFly();
        }
    }

    public void ConfigureDropItem(ItemDataSO itemData, PickableObject pickablePrefab)
    {
        if (itemData != null)
        {
            dropItemData = itemData;
        }

        if (pickablePrefab != null)
        {
            pickableObjectPrefab = pickablePrefab;
        }
    }

    public void InitializeAsFly()
    {
        behaviorInitialized = true;
        CurrentState = FlyState.Normal;
        Arrived = true;
        SetPhysicsMode(true);
        flyWings?.SetFlappingEnabled(true);
    }

    public void InitializeThrown(Vector2 initialVelocity)
    {
        InitializeAsFly();
        CurrentState = FlyState.Thrown;
        SetPhysicsMode(false);
        rb.velocity = initialVelocity;
    }

    protected override void FixedUpdate()
    {
        if (CurrentState == FlyState.Stunned)
        {
            return;
        }

        if (CurrentState == FlyState.Thrown)
        {
            UpdateThrownState();
            return;
        }

        base.FixedUpdate();
    }

    private void UpdateThrownState()
    {
        if (rb == null)
        {
            return;
        }

        if (rb.velocity.sqrMagnitude > thrownSettleSpeed * thrownSettleSpeed)
        {
            return;
        }

        if (!IsGrounded())
        {
            return;
        }

        InitializeAsFly();
    }

    public void EnterStunAndDropAsItem(bool facingRight)
    {
        if (!CanBeStomped)
        {
            return;
        }

        CurrentState = FlyState.Stunned;
        flyWings?.SetFlappingEnabled(false);
        SetPhysicsMode(false);

        SpawnDroppedItem(facingRight);
        Destroy(gameObject);
    }

    private void SpawnDroppedItem(bool facingRight)
    {
        if (pickableObjectPrefab == null || dropItemData == null)
        {
            return;
        }

        PickableObject pickable = Instantiate(
            pickableObjectPrefab,
            transform.position,
            Quaternion.identity
        );
        pickable.SetupObject(dropItemData, facingRight);
    }

    private void SetPhysicsMode(bool kinematic)
    {
        if (rb == null)
        {
            return;
        }

        rb.bodyType = kinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        rb.gravityScale = kinematic ? 0f : 1f;

        if (kinematic)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private bool IsGrounded()
    {
        if (groundLayerMask.value == 0)
        {
            return Physics2D.Raycast(transform.position, Vector2.down, groundCheckRadius + 0.05f);
        }

        return Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayerMask) != null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(DebugTarget, 0.2f);

        if (DebugPath == null || DebugPath.Count < 2)
        {
            return;
        }

        Gizmos.color = Color.green;

        for (int i = 0; i < DebugPath.Count - 1; i++)
        {
            Gizmos.DrawLine(DebugPath[i], DebugPath[i + 1]);
        }

        Gizmos.color = Color.yellow;

        foreach (Vector2 point in DebugPath)
        {
            Gizmos.DrawSphere(point, 0.08f);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.12f);
    }
}
