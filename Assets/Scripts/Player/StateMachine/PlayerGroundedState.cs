using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerGroundedState : PlayerBaseState
{
    public PlayerGroundedState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }

    public override void Enter()
    {
        base.Enter();
        playerControl.ResetDoubleJump();
        // 回到地面状态时，重置二段跳
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

        if (mainInput.Player.Jump.WasPerformedThisFrame()) // 如果人物按下跳跃键
        {
            if (playerControl.moveInput.y >= 0)
            {
                stateMachine.ChangeState(playerControl.jumpState); // 切换至跳跃状态
                return;
            }
            else
            {
                if (playerControl.ShouldBlockDropPlatform())
                {
                    return;
                }

                if (playerControl.TryDropDown())
                {
                    stateMachine.ChangeState(playerControl.dropPlatformState); // 切换至跳下平台状态
                    return;
                }
            }

            return;
        }

        if (rb.velocity.y < 0 && playerControl.groundDetected == false)
        // �����������ֱ�����䣬δ������Ծ״̬
        {
            stateMachine.ChangeState(playerControl.fallState);
            // 切换至下落状态

            return;
        }
    }
}
