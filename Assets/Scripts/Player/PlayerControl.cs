using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(PlayerWaterContact))]
[RequireComponent(typeof(PlayerWaterPhysics))]
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
    [SerializeField] private Collider2D playerCollider;
    public Collider2D playerColliderRef => playerCollider;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] private LayerMask oneWayPlatformLayer;   // ???????????
    [SerializeField] private float dropIgnoreTime = 0.25f;    // ??????????????

    [Header("Climb details")]
    public float climbHorizontalSpeed = 2.5f; // ???????????????
    public float climbVerticalSpeed = 2.5f; // ????????????????
    public float climbInputDeadZone = 0.1f; // ????????????

    [Header("Elevator")]
    [SerializeField] private float platformJumpGroundIgnoreTime = 0.2f;

    [Header("Knockback")]
    [Tooltip("击退水平冲量每秒衰减量")]
    [SerializeField] private float knockbackHorizontalDecay = 10f;
    [Tooltip("击退竖直冲量每秒衰减量")]
    [SerializeField] private float knockbackVerticalDecay = 6f;

    [Header("平台碰撞")]
    [SerializeField] private string playerLayerName= "Player";
    [SerializeField] private string platformLayerName = "Platform";

    [Header("Fall limit")]
    [Tooltip("玩家最大下落速度，填正数")]
    [SerializeField] private float maxFallSpeed = 18f;
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
    private float? nextJumpImpulseOverride;
    private float waterSurfaceExitGraceTimer;
    private float inheritedPlatformVelocityY;
    private bool inheritElevatorVelocityInAir;
    private Vector2 knockbackVelocity;
    private const float ElevatorDetachGrace = 0.2f;

    public float baseGravityMultiplier { get; private set; } = 1;
    public float BonusGravityMultiplier { get; private set; } = 1;

    public bool isInRopeArea {  get; private set; }

    public bool isInWater { get; private set; }
    public float waterSubmersion { get; private set; }
    public bool isFullySubmerged { get; private set; }
    public PlayerWaterContact waterContact { get; private set; }
    public PlayerWaterPhysics waterPhysics { get; private set; }

    public bool enableDoubleJump {  get; private set; }
    private bool hasPreparedDoubleJump;
    private bool hasUsedDoubleJump;

    private int playerLayer;
    private int platformLayer;
    #region State Variables
    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerFallState fallState { get; private set; }
    public PlayerDropPlatformState dropPlatformState { get; private set; }
    public PlayerClimbState climbState { get; private set; }
    public PlayerSwimState swimState { get; private set; }
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
        swimState = new PlayerSwimState(stateMachine, "jumpFall", this);
        #endregion

        waterContact = GetComponent<PlayerWaterContact>();
        waterPhysics = GetComponent<PlayerWaterPhysics>();

        playerLayer=LayerMask.NameToLayer(playerLayerName);
        platformLayer = LayerMask.NameToLayer(platformLayerName);

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

        if (isInWater && waterPhysics != null && ShouldUseWaterGravity())
        {
            rb.gravityScale = originalGravityScale
                * baseGravityMultiplier
                * BonusGravityMultiplier
                * waterPhysics.GetGravityMultiplier();
        }
        else if (IsGroundedOnMovingElevator())
        {
            rb.gravityScale = 0f;
        }
        else
        {
            rb.gravityScale = originalGravityScale * baseGravityMultiplier * BonusGravityMultiplier;
        }

        UpdateElevatorDetachTimer();
        UpdatePlatformJumpIgnoreTimer();
        UpdateWaterSurfaceExitGraceTimer();
        ApplyElevatorAirVelocityInheritance();
        ClampFallSpeed();
    }

    private void UpdateWaterSurfaceExitGraceTimer()
    {
        if (waterSurfaceExitGraceTimer <= 0f)
        {
            return;
        }

        waterSurfaceExitGraceTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        DecayKnockback();
        HandleCollisionDetecion();
        UpdateElevatorReference();
        ApplyElevatorGroundPhysicsBeforeStep();
        ClampFallSpeed();
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
        xVelocity += knockbackVelocity.x;
        yVelocity += knockbackVelocity.y;

        if (yIsJumpImpulse)
        {
            ApplyJumpVelocity(xVelocity, yVelocity);
        }
        else if (IsGroundedOnMovingElevator())
        {
            ElevatorPlatform platform = GetElevatorUnderFeet() ?? ridingElevator;
            Vector2 platformVelocity = platform != null ? platform.Velocity : Vector2.zero;
            rb.velocity = new Vector2(xVelocity, platformVelocity.y + knockbackVelocity.y);
        }
        else
        {
            rb.velocity = new Vector2(xVelocity, yVelocity);
        }

        Handleflip(xVelocity);
    }

    public float GetVerticalVelocityWithoutKnockback()
    {
        return rb.velocity.y - knockbackVelocity.y;
    }

    private void DecayKnockback()
    {
        if (knockbackVelocity.sqrMagnitude <= 0.0001f)
        {
            knockbackVelocity = Vector2.zero;
            return;
        }

        knockbackVelocity.x = Mathf.MoveTowards(
            knockbackVelocity.x,
            0f,
            knockbackHorizontalDecay * Time.fixedDeltaTime);
        knockbackVelocity.y = Mathf.MoveTowards(
            knockbackVelocity.y,
            0f,
            knockbackVerticalDecay * Time.fixedDeltaTime);
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

    /// <summary>
    /// 叠加击退冲量；冲量独立衰减，与玩家输入在 SetVelocity 中叠加。
    /// </summary>
    public void ApplyKnockback(Vector2 impulse)
    {
        knockbackVelocity += impulse;

        inheritElevatorVelocityInAir = false;
        inheritedPlatformVelocityY = 0f;
        platformJumpIgnoreTimer = 0f;
        ridingElevator = null;
        elevatorDetachTimer = 0f;

        if (Mathf.Abs(impulse.x) > 0.01f)
        {
            Handleflip(impulse.x);
        }

        if (!groundDetected || impulse.y > 0f)
        {
            stateMachine.ChangeState(impulse.y > 0f ? jumpState : fallState);
        }
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

        // 平台尚未移动前只关重力；Y 速度在电梯 MovePosition 后按本帧 Velocity 刷新。
        rb.gravityScale = 0f;
    }

    /// <summary>
    /// 电梯本帧 MovePosition 之后调用，使用当前帧平台速度，避免不同 fixedDeltaTime 下 Y 速度落后半拍。
    /// </summary>
    public void RefreshElevatorGroundPhysicsAfterPlatform(ElevatorPlatform platform)
    {
        if (platform == null || platformJumpIgnoreTimer > 0f || !IsInGroundedMovementState())
        {
            return;
        }

        if (!platform.IsMoving)
        {
            return;
        }

        if (!platform.HasRider(this)
            && GetElevatorUnderFeet() != platform
            && ridingElevator != platform)
        {
            return;
        }

        if (ridingElevator != platform)
        {
            NotifyStandingOnElevator(platform);
        }

        rb.gravityScale = 0f;
        rb.velocity = new Vector2(GetElevatorGroundHorizontalVelocity(), platform.Velocity.y);
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
        if (isInWater || platformJumpIgnoreTimer > 0f || !IsInGroundedMovementState())
        {
            return false;
        }

        ElevatorPlatform platform = GetElevatorUnderFeet() ?? ridingElevator;
        if (platform == null || !platform.IsMoving || !IsElevatorRiderActive())
        {
            return false;
        }

        return groundDetected || platform.HasRider(this);
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

    public void IgnoreCollisionBetweenPlayerAndPlatform(bool enable)
    {
        Physics2D.IgnoreLayerCollision(playerLayer,platformLayer,enable);
    }

    public void SetInRopeArea(bool inRopeArea)
    {
        isInRopeArea = inRopeArea;
    }

    internal void NotifyWaterContactChanged(bool hasActiveVolume, float rawSubmersion)
    {
        waterSubmersion = rawSubmersion;

        WaterPhysicsSettings settings = waterContact != null
            ? waterContact.ActiveSettings
            : WaterPhysicsSettings.RuntimeFallback;

        if (!isInWater)
        {
            if (hasActiveVolume && rawSubmersion >= settings.enterSubmersionThreshold)
            {
                isInWater = true;
            }
        }
        else if (!hasActiveVolume || rawSubmersion <= settings.exitSubmersionThreshold)
        {
            isInWater = false;
        }

        isFullySubmerged = isInWater && rawSubmersion >= settings.fullSubmersionThreshold;
        TryAutoEnterSwimState();
    }

    public bool CanEnterSwimState()
    {
        if (!isInWater || waterContact == null)
        {
            return false;
        }

        if (waterSurfaceExitGraceTimer > 0f)
        {
            return false;
        }

        WaterPhysicsSettings settings = waterContact.ActiveSettings;
        if (waterSubmersion < settings.enterSubmersionThreshold)
        {
            return false;
        }

        if (stateMachine.currentState == jumpState || stateMachine.currentState == fallState)
        {
            if (GetVerticalVelocityWithoutKnockback() > settings.surfaceJumpReentryVerticalSpeed
                && waterSubmersion < settings.surfaceJumpMaxSubmersion)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsInWaterSurfaceExitGrace()
    {
        return waterSurfaceExitGraceTimer > 0f;
    }

    public bool IsInSwimState()
    {
        return stateMachine.currentState == swimState;
    }

    public bool TrySwimBoost()
    {
        if (waterPhysics == null)
        {
            return false;
        }

        return waterPhysics.TrySwimBoost();
    }

    public bool CanJumpFromWaterSurface()
    {
        if (!isInWater || waterContact == null || !waterContact.HasActiveVolume)
        {
            return false;
        }

        WaterPhysicsSettings settings = waterContact.ActiveSettings;
        if (waterSubmersion < settings.enterSubmersionThreshold)
        {
            return false;
        }

        if (waterSubmersion >= settings.surfaceJumpMaxSubmersion)
        {
            return false;
        }

        return waterContact.DepthBelowSurface <= settings.surfaceJumpMaxDepth;
    }

    public bool TryJumpFromWaterSurface()
    {
        if (!CanJumpFromWaterSurface())
        {
            return false;
        }

        WaterPhysicsSettings settings = waterContact.ActiveSettings;
        nextJumpImpulseOverride = jumpForce * settings.surfaceJumpForceMultiplier;
        waterSurfaceExitGraceTimer = settings.surfaceJumpGraceDuration;
        stateMachine.ChangeState(jumpState);
        jumpBufferTimer = -999f;
        return true;
    }

    internal float ConsumeJumpImpulse()
    {
        float impulse = nextJumpImpulseOverride ?? jumpForce;
        nextJumpImpulseOverride = null;
        return impulse;
    }

    private bool ShouldUseWaterGravity()
    {
        if (!isInWater)
        {
            return false;
        }

        if (IsInSwimState())
        {
            return true;
        }

        return isFullySubmerged;
    }

    public bool ShouldBlockDropPlatform()
    {
        if (!isInWater || waterContact == null)
        {
            return false;
        }

        return waterSubmersion >= waterContact.ActiveSettings.dropPlatformBlockSubmersion;
    }

    private void TryAutoEnterSwimState()
    {
        if (!CanEnterSwimState() || stateMachine.currentState == climbState)
        {
            return;
        }

        if (stateMachine.currentState == swimState)
        {
            return;
        }

        stateMachine.ChangeState(swimState);
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

    private void ClampFallSpeed()
    {
        if (maxFallSpeed <= 0f || rb == null)
        {
            return;
        }

        // 站在向下移动的电梯上时，不限制平台给玩家的速度，避免玩家和平台分离。
        if (IsGroundedOnMovingElevator())
        {
            return;
        }

        Vector2 velocity = rb.velocity;
        float minYVelocity = -maxFallSpeed;

        // Unity 2D 中向下速度是负数，所以低于 -maxFallSpeed 时才需要钳制。
        if (velocity.y >= minYVelocity)
        {
            return;
        }

        rb.velocity = new Vector2(velocity.x, minYVelocity);
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
