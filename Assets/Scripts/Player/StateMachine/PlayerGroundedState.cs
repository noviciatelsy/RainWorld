using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerGroundedState : PlayerBaseState
{
    public PlayerGroundedState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }

    public override void Update()
    {
        base.Update();

        if (mainInput.Player.Jump.WasPerformedThisFrame()) // 如果人物按下跳跃键
        {
            if (playerControl.moveInput.y >= 0)
            {
                stateMachine.ChangeState(playerControl.jumpState); // 切换至跳跃状态
                return;
            }
            else
            {
                if (playerControl.TryDropDown())
                {
                    stateMachine.ChangeState(playerControl.dropPlatformState); // 切换至跳下平台状态
                    return;
                }

            }
            return;
        }

        if (rb.velocity.y < 0 && playerControl.groundDetected == false)
        // 如果人物向下直接下落，未经过跳跃状态
        {
            stateMachine.ChangeState(playerControl.fallState);
            // 切换至下落状态

            return;
        }

      

      
    }
}
