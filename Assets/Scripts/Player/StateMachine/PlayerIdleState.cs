using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }
    private float jumpBufferTime = 0.15f;
    public override void Enter()
    {
        base.Enter();
        playerControl.SetVelocity(0, rb.velocity.y);
        if (Time.time - playerControl.jumpBufferTimer < jumpBufferTime)
        {
            stateMachine.ChangeState(playerControl.jumpState); // 切换至跳跃状态
            return;
        }
    }

    public override void Update()
    {
        base.Update();
        if (playerControl.moveInput.x == playerControl.facingDir && playerControl.wallDetected)
        // 如果人物在墙边并尝试朝墙方向移动
        {
            return; // 不响应
        }
        playerControl.SetVelocity(0, rb.velocity.y);
        if (playerControl.moveInput.x != 0)
        // 如果人物有移动输入
        {
            stateMachine.ChangeState(playerControl.moveState);
            // 切换至移动状态
            return ;
        }

    }
}
