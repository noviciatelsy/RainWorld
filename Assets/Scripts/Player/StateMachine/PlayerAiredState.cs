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
            if (playerControl.groundDetected)
            // 如果下落至地面
            {

                stateMachine.ChangeState(playerControl.idleState);
                // 切换至待机状态
            }
        }

    }
}
