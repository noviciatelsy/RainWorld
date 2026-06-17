using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class PlayerControl : MonoBehaviour
{
    [Header("Collision detection")]
    [SerializeField] private float groundCheckDistance; // ??????????????????????
    [SerializeField] private float wallCheckDistance; // ?????????????????????
    [SerializeField] LayerMask whatIsGround; // ????/???layer
    [SerializeField] private Transform groundCheck; // ??????????
    [SerializeField] private Transform wallCheck; // ?????????

    [Header("Movement details")]
    public float moveSpeed = 3.5f; // ????????
    public float jumpForce =11f; // ???????
    [Range(0, 1)]
    public float inAirMoveMultiplier = 1; // ?????????????

    [Header("DropPlatform")]
    [SerializeField] private Collider2D playerCollider;       // ??????????
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] private LayerMask oneWayPlatformLayer;   // ???????????
    [SerializeField] private float dropIgnoreTime = 0.25f;    // ??????????????

    [Header("Climb details")]
    public float climbHorizontalSpeed = 2.5f; // ???????????????
    public float climbVerticalSpeed = 2.5f; // ????????????????
    public float climbInputDeadZone = 0.1f; // ????????????

    [Header("Elevator")]
    [SerializeField] private float platformJumpGroundIgnoreTime = 0.2f;

    public Player player { get; private set; }
    public Animator anim {  get; private set; }
    public Rigidbody2D rb { get; private set; }
    public MainInput mainInput { get; private set; }
    public Vector2 moveInput { get; private set; }
    public PlayerStateMachine stateMachine { get; private set; }
    private bool facingRight = true; // ???o??
    public int facingDir { get; private set; } = 1;
    public bool groundDetected { get; private set; } // ????????

    public bool wallDetected { get; private set; } // ????????
    public float jumpBufferTimer = -999f;
    private float originalGravityScale;
    private Collider2D currentOneWayPlatform;                 // ??????????
    private bool isDropping;
    private ElevatorPlatform ridingElevator;
    private float elevatorDetachTimer;
    private float platformJumpIgnoreTimer;
    private float inheritedPlatformVelocityY;
    private bool inheritElevatorVelocityInAir;
    private const float ElevatorDetachGrace = 0.2f;

    public float baseGravityMultiplier { get; private set; } = 1;
    public float BonusGravityMultiplier { get; private set; } = 1;

    public bool isInRopeArea {  get; private set; }

    public bool enableDoubleJump {  get; private set; }
    private bool hasPreparedDoubleJump;
    private bool hasUsedDoubleJump;
    #region State Variables
    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerFallState fallState { get; private set; }
    public PlayerDropPlatformState dropPlatformState { get; private set; }
    public PlayerClimbState climbState { get; private set; }
    #endregion


    private void Awake()
    {
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        stateMachine = new PlayerStateMachine();
        mainInput = InputManager.Instance.mainInput;
        originalGravityScale=rb.gravityScale;

        #region State Initialize
        idleState = new PlayerIdleState(stateMachine, "idle", this);
        moveState = new PlayerMoveState(stateMachine, "move", this);
        jumpState = new PlayerJumpState(stateMachine, "jumpFall", this);
        fallState = new PlayerFallState(stateMachine, "jumpFall", this);
        dropPlatformState=new PlayerDropPlatformState(stateMachine,"jumpFall",this);
        climbState = new PlayerClimbState(stateMachine, "climb", this);
        #endregion
    }

    private void Start()
    {
        stateMachine.Initialize(idleState);
    }

    private void OnEnable()
    {
        mainInput.Player.Enable();
        mainInput.Player.Move.performed += OnMovePerformed;
        mainInput.Player.Move.canceled += OnMoveCanceled;
    }

    private void OnDisable()
    {
        mainInput.Player.Disable();
        mainInput.Player.Move.performed -= OnMovePerformed;
        mainInput.Player.Move.canceled -= OnMoveCanceled;
    }

    private void Update()
    {
        stateMachine.UpdateActiveState();

        if (!IsGroundedOnMovingElevator())
        {
            rb.gravityScale = originalGravityScale * baseGravityMultiplier * BonusGravityMultiplier;
        }

        UpdateElevatorDetachTimer();
        UpdatePlatformJumpIgnoreTimer();
        ApplyElevatorAirVelocityInheritance();
    }

    private void FixedUpdate()
    {
        HandleCollisionDetecion();
        UpdateElevatorReference();
        ApplyElevatorGroundPhysicsBeforeStep();
    }

    public void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }


    public void SetVelocity(float xVelocity, float yVelocity, bool yIsJumpImpulse = false)
    {
        if (yIsJumpImpulse)
        {
            ApplyJumpVelocity(xVelocity, yVelocity);
        }
        else if (IsGroundedOnMovingElevator())
        {
            ElevatorPlatform platform = GetElevatorUnderFeet() ?? ridingElevator;
            Vector2 platformVelocity = platform != null ? platform.Velocity : Vector2.zero;
            rb.velocity = new Vector2(xVelocity, platformVelocity.y);
        }
        else
        {
            rb.velocity = new Vector2(xVelocity, yVelocity);
        }

        Handleflip(xVelocity);
    }

    public bool IsGroundedForLanding()
    {
        if (platformJumpIgnoreTimer > 0f)
        {
            return false;
        }

        return groundDetected;
    }

    public void NotifyStandingOnElevator(ElevatorPlatform elevator)
    {
        ridingElevator = elevator;
        elevatorDetachTimer = 0f;
    }

    public void NotifyLeftElevatorPlatform(ElevatorPlatform elevator)
    {
        if (ridingElevator != elevator)
        {
            return;
        }

        elevatorDetachTimer = ElevatorDetachGrace;
    }

    public void SetRidingElevator(ElevatorPlatform elevator)
    {
        NotifyStandingOnElevator(elevator);
    }

    public void ClearRidingElevator(ElevatorPlatform elevator)
    {
        NotifyLeftElevatorPlatform(elevator);
    }

    private void UpdateElevatorDetachTimer()
    {
        if (ridingElevator == null || elevatorDetachTimer <= 0f)
        {
            return;
        }

        elevatorDetachTimer -= Time.deltaTime;
        if (elevatorDetachTimer <= 0f)
        {
            ridingElevator = null;
        }
    }

    private void UpdatePlatformJumpIgnoreTimer()
    {
        if (platformJumpIgnoreTimer <= 0f)
        {
            return;
        }

        platformJumpIgnoreTimer -= Time.deltaTime;
    }

    private bool IsElevatorRiderActive()
    {
        if (ridingElevator == null)
        {
            return false;
        }

        if (elevatorDetachTimer > 0f)
        {
            return true;
        }

        if (platformJumpIgnoreTimer > 0f)
        {
            return true;
        }

        return ridingElevator.HasRider(this) || GetElevatorUnderFeet() == ridingElevator;
    }

    public bool IsOnMovingElevator()
    {
        return ridingElevator != null && ridingElevator.IsMoving && IsElevatorRiderActive();
    }

    public Vector2 GetElevatorVelocity()
    {
        if (!IsElevatorRiderActive())
        {
            return Vector2.zero;
        }

        return ridingElevator.Velocity;
    }

    private void ApplyJumpVelocity(float xVelocity, float jumpImpulse)
    {
        ElevatorPlatform platform = GetElevatorUnderFeet() ?? ridingElevator;
        if (platform != null && platform.IsMoving)
        {
            inheritedPlatformVelocityY = platform.Velocity.y;
            inheritElevatorVelocityInAir = true;
            rb.velocity = new Vector2(xVelocity, jumpImpulse + inheritedPlatformVelocityY);
        }
        else
        {
            inheritElevatorVelocityInAir = false;
            inheritedPlatformVelocityY = 0f;
            rb.velocity = new Vector2(xVelocity, jumpImpulse);
        }

        platformJumpIgnoreTimer = platformJumpGroundIgnoreTime;
        platform?.UnregisterRider(this);
    }

    /// <summary>
    /// 踩弹跳物（如萤火虫）时施加向上速度，效果类似弹簧。
    /// </summary>
    /// <param name="upwardImpulse">向上速度；&lt;= 0 时使用当前 jumpForce。</param>
    public void ApplyStompBounce(float upwardImpulse = 0f)
    {
        float impulse = upwardImpulse > 0f ? upwardImpulse : jumpForce;
        float xVelocity = Mathf.Abs(moveInput.x) > climbInputDeadZone
            ? moveInput.x * moveSpeed
            : rb.velocity.x;

        ApplyJumpVelocity(xVelocity, impulse);
        stateMachine.ChangeState(jumpState);
    }

    private void ApplyElevatorAirVelocityInheritance()
    {
        if (!inheritElevatorVelocityInAir)
        {
            return;
        }

        if (platformJumpIgnoreTimer <= 0f && elevatorDetachTimer <= 0f)
        {
            inheritElevatorVelocityInAir = false;
            inheritedPlatformVelocityY = 0f;
            return;
        }

        ElevatorPlatform platform = ridingElevator;
        if (platform == null || !platform.IsMoving)
        {
            inheritElevatorVelocityInAir = false;
            inheritedPlatformVelocityY = 0f;
            return;
        }

        float platformVy = platform.Velocity.y;
        float relativeVy = rb.velocity.y - inheritedPlatformVelocityY;
        rb.velocity = new Vector2(rb.velocity.x, relativeVy + platformVy);
        inheritedPlatformVelocityY = platformVy;
    }

    private void UpdateElevatorReference()
    {
        if (platformJumpIgnoreTimer > 0f)
        {
            return;
        }

        ElevatorPlatform underFeet = GetElevatorUnderFeet();
        if (underFeet != null)
        {
            ridingElevator = underFeet;
            elevatorDetachTimer = 0f;
            underFeet.RegisterRider(this);
            return;
        }

        if (ridingElevator != null && elevatorDetachTimer <= 0f && !ridingElevator.HasRider(this))
        {
            ridingElevator = null;
        }
    }

    private void ApplyElevatorGroundPhysicsBeforeStep()
    {
        if (!IsGroundedOnMovingElevator())
        {
            return;
        }

        ElevatorPlatform platform = GetElevatorUnderFeet() ?? ridingElevator;
        if (platform == null)
        {
            return;
        }

        rb.gravityScale = 0f;
        Vector2 platformVelocity = platform.Velocity;
        rb.velocity = new Vector2(GetElevatorGroundHorizontalVelocity(), platformVelocity.y);
    }

    private float GetElevatorGroundHorizontalVelocity()
    {
        PlayerBaseState currentState = stateMachine.currentState;

        if (currentState == moveState)
        {
            if (moveInput.x == 0f || wallDetected)
            {
                return 0f;
            }

            return moveInput.x * moveSpeed;
        }

        if (currentState == idleState)
        {
            return 0f;
        }

        return rb.velocity.x;
    }

    public bool IsGroundedOnMovingElevator()
    {
        if (platformJumpIgnoreTimer > 0f || !groundDetected || !IsInGroundedMovementState())
        {
            return false;
        }

        ElevatorPlatform platform = GetElevatorUnderFeet() ?? ridingElevator;
        return platform != null && platform.IsMoving;
    }

    private ElevatorPlatform GetElevatorUnderFeet()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance + 0.1f,
            whatIsGround);

        if (hit.collider == null)
        {
            return null;
        }

        return hit.collider.GetComponentInParent<ElevatorPlatform>();
    }

    private bool IsInGroundedMovementState()
    {
        PlayerBaseState currentState = stateMachine.currentState;
        return currentState == idleState
            || currentState == moveState
            || currentState == climbState;
    }

    public void Handleflip(float xVelocity)
    {
        // ??????????????????
        if (Mathf.Abs(xVelocity) < 0.01f)
        {
            return;
        }
        if (xVelocity > 0 && facingRight == false)
        // ??????????????
        {
            Flip();
        }
        else if (xVelocity < 0 && facingRight == true)
        // ??????????????
        {
            Flip();
        }
    }

    public void Flip()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        // ??????

        facingRight = !facingRight;
        // ???????

        facingDir = facingDir * -1;
        // ??????
    }

    private void HandleCollisionDetecion()
    {
        groundDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
        // ????????????

        wallDetected = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
        // ????????
    }



    public void EnablePlayerControl()
    {
        mainInput.Player.Enable();
    }

    public void DisablePlayerControl()
    {
        mainInput.Player.Disable();
    }

    public void SetBaseGravityMultiplier(float multiplier)
    {
        baseGravityMultiplier= multiplier;
    }

    public void SetBonusGravityMultiplier(float multiplier)
    {
        BonusGravityMultiplier= multiplier;
    }

    public bool TryDropDown()
    {
        // ????????????????????????
        if (isDropping)
        {
            return false;
        }

        // ??????????????????
        currentOneWayPlatform = Physics2D.OverlapBox
        (
            groundCheck.position,
            groundCheckSize,
            0f,
            oneWayPlatformLayer
        );

        if (currentOneWayPlatform != null)
        {

            StartCoroutine(DropDownRoutine(currentOneWayPlatform));
            return true;
        }
        return false;
    }
    public bool CanEnterClimbState()
    {
        if (isInRopeArea == false)
        {
            return false;
        }

        if (Mathf.Abs(moveInput.y) <= climbInputDeadZone)
        {
            return false;
        }

        return true;
    }
    private IEnumerator DropDownRoutine(Collider2D platformCollider)
    {
        isDropping = true;

        // ????????????????????
        Physics2D.IgnoreCollision(playerCollider, platformCollider, true);

        // ????????????????????
        yield return new WaitForSeconds(dropIgnoreTime);

        // ??????
        if (playerCollider != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }

        isDropping = false;
    }

    public void SetInRopeArea(bool inRopeArea)
    {
        isInRopeArea = inRopeArea;
    }

    public void AddMoveSpeed(float amountToAdd)
    {
        // ??????????????????????
        if (amountToAdd <= 0f)
        {
            return;
        }

        moveSpeed += amountToAdd;
    }

    public void ReduceMoveSpeed(float amountToReduce)
    {
        // ??????????????????????
        if (amountToReduce <= 0f)
        {
            return;
        }

        // ??????????????
        moveSpeed = Mathf.Max(0f, moveSpeed - amountToReduce);
    }

    public void AddMoveSpeedTemporarily(float amountToAdd, float time)
    {
        if (amountToAdd <= 0f || time <= 0f)
        {
            return;
        }

        StartCoroutine(AddMoveSpeedTemporarilyCoroutine(amountToAdd, time));
    }

    private IEnumerator AddMoveSpeedTemporarilyCoroutine(float amountToAdd, float time)
    {
        AddMoveSpeed(amountToAdd);

        yield return new WaitForSeconds(time);

        ReduceMoveSpeed(amountToAdd);
    }

    public void ReduceMoveSpeedTemporarily(float amountToReduce, float time)
    {
        if (amountToReduce <= 0f || time <= 0f)
        {
            return;
        }

        StartCoroutine(ReduceMoveSpeedTemporarilyCoroutine(amountToReduce, time));
    }

    private IEnumerator ReduceMoveSpeedTemporarilyCoroutine(float amountToReduce, float time)
    {
        float speedBeforeReduce = moveSpeed;

        ReduceMoveSpeed(amountToReduce);

        // ??????????????????????
        // ????y?????? 3????????? 10??????????? 3
        float actualReducedAmount = speedBeforeReduce - moveSpeed;

        yield return new WaitForSeconds(time);

        AddMoveSpeed(actualReducedAmount);
    }

    public void AddJumpForce(float amountToAdd)
    {
        // ??????????????????????
        if (amountToAdd <= 0f)
        {
            return;
        }

        jumpForce += amountToAdd;
    }

    public void ReduceJumpForce(float amountToReduce)
    {
        // ??????????????????????
        if (amountToReduce <= 0f)
        {
            return;
        }

        // ??????????????????
        jumpForce = Mathf.Max(0f, jumpForce - amountToReduce);
    }

    public void AddJumpForceTemporarily(float amountToAdd, float time)
    {
        if (amountToAdd <= 0f || time <= 0f)
        {
            return;
        }

        StartCoroutine(AddJumpForceTemporarilyCoroutine(amountToAdd, time));
    }

    private IEnumerator AddJumpForceTemporarilyCoroutine(float amountToAdd, float time)
    {
        AddJumpForce(amountToAdd);

        yield return new WaitForSeconds(time);

        ReduceJumpForce(amountToAdd);
    }

    public void ReduceJumpForceTemporarily(float amountToReduce, float time)
    {
        if (amountToReduce <= 0f || time <= 0f)
        {
            return;
        }

        StartCoroutine(ReduceJumpForceTemporarilyCoroutine(amountToReduce, time));
    }

    private IEnumerator ReduceJumpForceTemporarilyCoroutine(float amountToReduce, float time)
    {
        float jumpForceBeforeReduce = jumpForce;

        ReduceJumpForce(amountToReduce);

        // ??????????????????????
        // ????y?????????? 3????????? 10??????????? 3
        float actualReducedAmount = jumpForceBeforeReduce - jumpForce;

        yield return new WaitForSeconds(time);

        AddJumpForce(actualReducedAmount);
    }

    public void EnableDoubleJump(bool enable)
    {
        enableDoubleJump = enable;
    }

    public bool CanDoubleJump()
    {
        if(enableDoubleJump)
        {
            return true;
        }
        return false;
    }

    public void PrepareDoubleJump()
    {
        if (hasPreparedDoubleJump)
        {
            return;
        }

        hasPreparedDoubleJump = true;
        hasUsedDoubleJump = false;
        // ?????????????????????????????????
    }

    public void ResetDoubleJump()
    {
        hasPreparedDoubleJump = false;
        hasUsedDoubleJump = false;
        // ?????????????????????????????????????????????
    }

    public bool TryConsumeDoubleJump()
    {
        if (CanDoubleJump() == false)
        {
            return false;
        }

        if (hasPreparedDoubleJump == false)
        {
            return false;
        }

        if (hasUsedDoubleJump)
        {
            return false;
        }

        hasUsedDoubleJump = true;
        return true;
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + new Vector3(0, -groundCheckDistance));
        Gizmos.DrawLine(wallCheck.position, wallCheck.position + new Vector3(wallCheckDistance * facingDir, 0));
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
