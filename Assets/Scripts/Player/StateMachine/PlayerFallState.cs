using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallState : PlayerAiredState
{
    public PlayerFallState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }

    private float coyoteTime = 0.15f;
    private float coyoteTimer = 0;

    public override void Enter()
    {
        base.Enter();
        coyoteTimer = 0;
    }

    public override void Update()
    {
        base.Update();
        coyoteTimer += Time.deltaTime;
        if (coyoteTimer < coyoteTime)
        {
            if (mainInput.Player.Jump.WasPerformedThisFrame()) // 如果人物按下跳跃键
            {
                stateMachine.ChangeState(playerControl.jumpState); // 切换至跳跃状态
            }
        }
    }
}
