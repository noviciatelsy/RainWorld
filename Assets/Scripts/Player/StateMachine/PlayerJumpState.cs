using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerAiredState
{
    public PlayerJumpState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerControl.PrepareDoubleJump();
        // 第一次进入跳跃状态时，准备二段跳机会
        // 如果这是二段跳重新进入 JumpState，因为 hasPreparedDoubleJump 已经是 true，所以不会重复刷新次数

        float xVelocity = rb.velocity.x;

        if (Mathf.Abs(playerControl.moveInput.x) < 0.01f)
        {
            xVelocity = 0f;
        }

        playerControl.SetVelocity(xVelocity, playerControl.jumpForce);
        // 在y方向获得jumpForce大小的速度

        playerControl.jumpBufferTimer = -999f;
    }

    public override void Update()
    {
        base.Update();

        if (IsCurrentState() == false)
        {
            return;
        }

        if (mainInput.Player.Jump.WasPerformedThisFrame()) // 如果人物按下跳跃键
        {
            playerControl.jumpBufferTimer = Time.time; // 记录跳跃缓存时间
        }
    }
}
