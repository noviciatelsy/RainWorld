using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbState : PlayerBaseState
{
    private float lastClimbAnimYValue = 1f;
    // 记录上一次攀爬动画方向
    // 1 表示向上爬动画，-1 表示向下爬动画

    public PlayerClimbState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerControl.DisableGravity();
        // 进入攀爬状态时，不再受到重力影响

        playerControl.SetVelocity(0, 0);
        // 刚进入攀爬时先停住，避免继承跳跃/下落速度导致角色滑出绳子

        anim.speed = 1f;
        // 确保刚进入攀爬状态时动画是正常播放的

        if (Mathf.Abs(playerControl.moveInput.y) > playerControl.climbInputDeadZone)
        {
            lastClimbAnimYValue = Mathf.Sign(playerControl.moveInput.y);
        }

        anim.SetFloat("yVelocity", lastClimbAnimYValue);
    }

    public override void Update()
    {
        if (mainInput.Player.Jump.WasPerformedThisFrame()) // 如果人物按下跳跃键
        {
            stateMachine.ChangeState(playerControl.jumpState); // 切换至跳跃状态
            return;
        }

        if (playerControl.isInRopeArea == false)
        // 如果人物攀爬移动导致离开可攀爬区域
        {
            playerControl.SetVelocity(rb.velocity.x, 0);
            // 离开绳子时清掉竖直速度，避免把攀爬速度带进下落状态

            stateMachine.ChangeState(playerControl.dropPlatformState);
            // 切换至跳下平台/下落状态

            return;
        }

        float xInput = playerControl.moveInput.x;
        float yInput = playerControl.moveInput.y;

        if (Mathf.Abs(xInput) <= playerControl.climbInputDeadZone)
        {
            xInput = 0;
        }

        if (Mathf.Abs(yInput) <= playerControl.climbInputDeadZone)
        {
            yInput = 0;
        }

        float xVelocity = xInput * playerControl.climbHorizontalSpeed;
        float yVelocity = yInput * playerControl.climbVerticalSpeed;

        playerControl.SetVelocity(xVelocity, yVelocity);
        // 攀爬时，x方向和y方向都由输入直接控制

        bool hasHorizontalInput = xInput != 0;
        bool hasVerticalInput = yInput != 0;
        bool hasAnyDirectionInput = hasHorizontalInput || hasVerticalInput;

        if (hasVerticalInput)
        {
            lastClimbAnimYValue = Mathf.Sign(yInput);
        }

        anim.SetFloat("yVelocity", lastClimbAnimYValue);
        // 这里不要直接传 rb.velocity.y
        // 因为只有水平输入时，rb.velocity.y 是 0，会导致混合树停在中间
        // 我们要保留上一次向上/向下爬的动画方向

        if (hasAnyDirectionInput)
        {
            anim.speed = 1f;
            // 只要有任意方向输入，就播放攀爬动画
        }
        else
        {
            anim.speed = 0f;
            // 没有任何方向输入时，暂停在当前攀爬动画帧
        }
    }

    public override void Exit()
    {
        anim.speed = 1f;
        // 离开攀爬状态时必须恢复动画速度
        // 否则之后待机、移动、跳跃动画都会被暂停

        playerControl.EnableGravity();
        // 离开攀爬状态后恢复重力

        base.Exit();
    }
}