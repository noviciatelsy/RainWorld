using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerGroundedState
{
    public PlayerMoveState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }

    private AudioSource footStepsAudioSource;

    public override void Enter()
    {
        base.Enter();
        footStepsAudioSource = AudioManager.Instance.PlayLoopSFX("PlayerFootStepsSFX");
    }

    public override void Update()
    {
        base.Update();
        if (IsCurrentState() == false)
        {
            return;
        }

        if (playerControl.moveInput.x == 0 || playerControl.wallDetected)
        // 如果人物无移动输入或接触到墙
        {
            stateMachine.ChangeState(playerControl.idleState);
            // 切换至待机状态
            //return;
        }

        playerControl.SetVelocity(
            playerControl.moveInput.x * playerControl.moveSpeed,
            playerControl.GetVerticalVelocityWithoutKnockback());
        // x方向按照输入移动，y方向保持原速度
    }

    public override void Exit()
    {
        base.Exit();
        if (footStepsAudioSource != null)
        {
            footStepsAudioSource.Stop();
        }
    }
}
