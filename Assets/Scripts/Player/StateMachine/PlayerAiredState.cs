using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAiredState : PlayerBaseState
{
    public PlayerAiredState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }

    private float groundDetectDelay = 0.2f;
    private float groundDetectDelayTimer;

    public override void Enter()
    {
        base.Enter();
        groundDetectDelayTimer = 0;
    }

    public override void Update()
    {
        base.Update();

        if (TryEnterClimbState())
        // 如果人物处于可攀爬区域，并且有竖直方向输入
        {
            return;
        }

        if (TryEnterSwimState())
        {
            return;
        }

        if (mainInput.Player.Jump.WasPerformedThisFrame()) // 如果人物在空中按下跳跃键
        {
            if (playerControl.TryConsumeDoubleJump())
            {
                stateMachine.ChangeState(playerControl.jumpState);
                // 消耗二段跳机会，并重新进入跳跃状态
                AudioManager.Instance.PlaySFX("PlayerDoubleJumpSFX");
                return;
            }
        }

        groundDetectDelayTimer += Time.deltaTime;

        if (playerControl.moveInput.x != 0)
        // 如果有横向移动输入
        {
            playerControl.SetVelocity(playerControl.moveInput.x * playerControl.moveSpeed * playerControl.inAirMoveMultiplier, rb.velocity.y);
            // 获取相应方向横向速度
        }
        else
        {
            playerControl.SetVelocity(0, rb.velocity.y);
            // 获取相应方向横向速度
        }

        if (groundDetectDelayTimer > groundDetectDelay)
        {
            if (playerControl.IsGroundedForLanding())
            // 如果下落至地面（起跳后短暂忽略电梯地面，避免立刻切回 Idle）
            {
                stateMachine.ChangeState(playerControl.idleState);
                // 切换至待机状态
                return;
            }
        }
    }
}
