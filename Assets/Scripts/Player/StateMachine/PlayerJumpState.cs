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
        playerControl.SetVelocity(rb.velocity.x, playerControl.jumpForce);
        // 在y方向获得jumpForce大小的速度
        playerControl.jumpBufferTimer = -999f;
    }

    public override void Update()
    {
        base.Update();
        if (mainInput.Player.Jump.WasPerformedThisFrame()) // 如果人物按下跳跃键
        {
            playerControl.jumpBufferTimer = Time.time; // 记录跳跃缓存时间
        }
    }
}
