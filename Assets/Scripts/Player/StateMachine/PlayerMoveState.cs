using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerGroundedState
{
    public PlayerMoveState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }

    public override void Update()
    {
        base.Update();
        if (playerControl.moveInput.x == 0 || playerControl.wallDetected)
        // 如果人物无移动输入或接触到墙
        {
            stateMachine.ChangeState(playerControl.idleState);
            // 切换至待机状态
        }

        playerControl.SetVelocity(playerControl.moveInput.x * playerControl.moveSpeed, rb.velocity.y);
        // x方向按照输入移动，y方向保持原速度
    }
}
