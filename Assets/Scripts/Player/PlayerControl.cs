using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    [Header("Collision detection")]
    [SerializeField] private float groundCheckDistance; // 实体到地面的距离（用于射线检测）
    [SerializeField] private float wallCheckDistance; // 实体到墙壁的距离（用于射线检测）
    [SerializeField] LayerMask whatIsGround; // 地面/墙壁layer
    [SerializeField] private Transform groundCheck; // 地面检测位置
    [SerializeField] private Transform wallCheck; // 墙壁检测位置

    [Header("Movement details")]
    public float moveSpeed = 3.5f; // 人物移速
    public float jumpForce =11f; // 跳跃力度
    [Range(0, 1)]
    public float inAirMoveMultiplier = 1; // 空中移动速度倍率

    [Header("DropPlatform")]
    [SerializeField] private Collider2D playerCollider;       // 玩家主碰撞体
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] private LayerMask oneWayPlatformLayer;   // 单向平台所在层
    [SerializeField] private float dropIgnoreTime = 0.25f;    // 忽略碰撞持续时间

    [Header("HoldItem")]
    [SerializeField] private SpriteRenderer holdingItemSprite;
    [SerializeField] private int baseScaleMultiplier = 4;
    public Player player { get; private set; }
    public Animator anim {  get; private set; }
    public Rigidbody2D rb { get; private set; }
    public MainInput mainInput { get; private set; }
    public Vector2 moveInput { get; private set; }
    public PlayerStateMachine stateMachine { get; private set; }
    private bool facingRight = true; // 实体朝向
    public int facingDir { get; private set; } = 1;
    public bool groundDetected { get; private set; } // 是否处于地面

    public bool wallDetected { get; private set; } // 是否处于墙壁
    public float jumpBufferTimer = -999f;
    private float originalGravityScale;
    private Collider2D currentOneWayPlatform;                 // 当前脚下的平台
    private bool isDropping;                                  
    #region State Variables
    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerFallState fallState { get; private set; }
    public PlayerDropPlatformState dropPlatformState { get; private set; }
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
        #endregion
    }

    private void Start()
    {
        stateMachine.Initialize(idleState);
    }

    private void OnEnable()
    {
        mainInput.Player.Move.performed += OnMovePerformed;
        mainInput.Player.Move.canceled += OnMoveCanceled;
        mainInput.Enable();
    }

    private void OnDisable()
    {
        mainInput.Player.Move.performed -= OnMovePerformed;
        mainInput.Player.Move.canceled -= OnMoveCanceled;
        mainInput.Disable();
    }

    private void Update()
    {
        stateMachine.UpdateActiveState();
        // 调用当前状态对象的update方法（只响应当前状态的操作）
        // 只对当前状态对象的行为监听
     
    }

    private void FixedUpdate()
    {
        HandleCollisionDetecion(); // 检测是否接触地面/墙壁
    }

    public void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }


    public void SetVelocity(float xVelocity, float yVelocity)
    // 设置实体速度和朝向
    {
        rb.velocity = new Vector2(xVelocity, yVelocity); // 设置rb速度
        Handleflip(xVelocity); // 处理朝向
    }

    public void Handleflip(float xVelocity)
    {
        // 极小速度不参与朝向判断
        if (Mathf.Abs(xVelocity) < 0.01f)
        {
            return;
        }
        if (xVelocity > 0 && facingRight == false)
        // 如果朝向由左改右
        {
            Flip();
        }
        else if (xVelocity < 0 && facingRight == true)
        // 如果朝向由右改左
        {
            Flip();
        }
    }

    public void Flip()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        // 左右翻转

        facingRight = !facingRight;
        // 更新朝向

        facingDir = facingDir * -1;
        // 更像方向
    }

    private void HandleCollisionDetecion()
    {
        groundDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
        // 检测是否接触地面

        wallDetected = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
        // 是否接触墙壁
    }

    public void StartHoldingItem(ItemDataSO itemToHold)
    {
        BackpackItemDataSO backpackItemData=itemToHold.backpackItemData;
        holdingItemSprite.enabled = true;
        holdingItemSprite.transform.localScale = new Vector3(1/baseScaleMultiplier*backpackItemData.imageSize.x,1/ baseScaleMultiplier * backpackItemData.imageSize.y,1);
    }

    public void EndHoldingItem()
    {
        holdingItemSprite.enabled=false;
        holdingItemSprite.transform.localScale=new Vector3(1,1,1);
    }

    public void EnablePlayerControl()
    {
        mainInput.Player.Enable();
    }

    public void DisablePlayerControl()
    {
        mainInput.Player.Disable();
    }

    public void EnableGravity()
    {
        rb.gravityScale = originalGravityScale;
    }

    public void DisableGravity()
    {
        rb.gravityScale = 0;
    }

    public bool TryDropDown()
    {
        // 正在下落过程中，不重复触发
        if (isDropping)
        {
            return false;
        }

        // 先检测脚下是否有单向平台
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

    private IEnumerator DropDownRoutine(Collider2D platformCollider)
    {
        isDropping = true;

        // 临时忽略玩家与当前平台的碰撞
        Physics2D.IgnoreCollision(playerCollider, platformCollider, true);

        // 等待一小段时间，让玩家掉下去
        yield return new WaitForSeconds(dropIgnoreTime);

        // 恢复碰撞
        if (playerCollider != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }

        isDropping = false;
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
