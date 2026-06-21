using UnityEngine;

/// <summary>
/// 水中移动状态：物理力由 PlayerWaterPhysics 施加，本状态只处理输入语义与切换。
/// </summary>
public class PlayerSwimState : PlayerBaseState
{
    public PlayerSwimState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl)
        : base(stateMachine, animBoolName, playerControl)
    {
    }

    public override void Enter()
    {
        base.Enter();
        playerControl.ResetDoubleJump();
    }

    public override void Update()
    {
        base.Update();

        if (IsCurrentState() == false)
        {
            return;
        }

        if (TryEnterClimbState())
        {
            return;
        }

        if (mainInput.Player.Jump.WasPerformedThisFrame())
        {
            if (playerControl.TryJumpFromWaterSurface())
            {
                return;
            }

            playerControl.TrySwimBoost();
        }

        if (!playerControl.isInWater)
        {
            ExitToLandOrAir();
        }
    }

    private void ExitToLandOrAir()
    {
        if (playerControl.IsGroundedForLanding())
        {
            stateMachine.ChangeState(playerControl.idleState);
            return;
        }

        stateMachine.ChangeState(playerControl.fallState);
    }
}
