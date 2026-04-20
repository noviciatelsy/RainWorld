using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public float maxSpeed = 90f;
    public float accelTime = 1f;
    public float decelTime = 1f;
    public float airDecelTime = 1.045f;
    public float airDriveTime = 1.00005f;

    [Header("Jump Constants")]
    public float jumpVelocity = 300f; // 向上为正，逻辑中会取负
    public float maxJumpHold = 0.25f;
    public float coyoteTime = 0.08f;
    public float jumpBufferTime = 0.10f;

    [Header("Gravity Settings")]
    public float jumpGravity = 1100f;      // 长按跳跃/上升重力
    public float shortJumpGravity = 2500f; // 提前松开跳跃重力
    public float fallGravity = 900f;       // 自然下落重力

    private Rigidbody2D rb;
    private Vector2 velocity;
    private bool isGrounded;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private float jumpHoldTimer;
    private bool isJumpPressed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 确保 Unity 的重力不干扰脚本逻辑
        rb.gravityScale = 0;
    }

    void Update()
    {
        // 1. 处理输入缓存 (Jump Buffer)
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }

        // 计时器递减
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
        if (coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        CheckGround();
        HandleHorizontalMovement();
        HandleJumpAndGravity();

        // 应用位移
        rb.velocity = velocity;
    }

    private void HandleHorizontalMovement()
    {
        float inputDir = Input.GetAxisRaw("Horizontal");
        float targetSpeed = inputDir * maxSpeed;

        // 确定加速/减速因子
        float accel;
        if (Mathf.Abs(targetSpeed) > Mathf.Abs(velocity.x))
        {
            accel = accelTime;
        }
        else
        {
            if (isGrounded)
                accel = decelTime;
            else
                accel = (inputDir == 0) ? airDecelTime : airDriveTime;
        }

        // 模拟原脚本中的指数插值：t = 1.0 - pow(base, delta)
        float baseVal = Mathf.Clamp(1.0f - 1.0f / accel, 0.0f, 0.99f);
        float t = 1.0f - Mathf.Pow(baseVal, Time.fixedDeltaTime);

        velocity.x = Mathf.Lerp(velocity.x, targetSpeed, t);
    }

    private void HandleJumpAndGravity()
    {
        // 2. 土狼时间逻辑
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }

        // 3. 执行起跳
        if (jumpBufferTimer > 0 && coyoteTimer > 0)
        {
            DoJump();
        }

        // 4. 重力处理
        if (velocity.y > 0) // 上升阶段
        {
            jumpHoldTimer += Time.fixedDeltaTime;
            // 如果持续按住跳跃键且未超时
            if (Input.GetButton("Jump") && jumpHoldTimer < maxJumpHold)
            {
                velocity.y -= jumpGravity * Time.fixedDeltaTime;
            }
            else
            {
                // 提前松开或超时，使用更大重力（短跳）
                velocity.y -= shortJumpGravity * Time.fixedDeltaTime;
            }
        }
        else // 下落阶段
        {
            if (coyoteTimer > 0)
            {
                velocity.y -= jumpGravity * Time.fixedDeltaTime; // 软重力下落
            }
            else
            {
                velocity.y -= fallGravity * Time.fixedDeltaTime; // 正常重力下落
            }
        }
    }

    private void DoJump()
    {
        // 对应 Godot: velocity.y = JUMP_VELOCITY (Unity 坐标系向上为正)
        velocity.y = jumpVelocity;

        jumpHoldTimer = 0f;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void CheckGround()
    {
        // 简单的地面检测逻辑，你可以根据项目需求替换为 Raycast 或 OverlapBox
        // 这里假设 Y 轴速度极小时在地面，仅作演示
        isGrounded = Mathf.Abs(rb.velocity.y) < 0.01f;
    }
}