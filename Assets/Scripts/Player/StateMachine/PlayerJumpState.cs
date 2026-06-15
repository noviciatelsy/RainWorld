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
        // ��һ�ν�����Ծ״̬ʱ��׼������������
        // ������Ƕ��������½��� JumpState����Ϊ hasPreparedDoubleJump �Ѿ��� true�����Բ����ظ�ˢ�´���

        float xVelocity = rb.velocity.x;

        if (Mathf.Abs(playerControl.moveInput.x) < 0.01f)
        {
            xVelocity = 0f;
        }

        playerControl.SetVelocity(xVelocity, playerControl.jumpForce, yIsJumpImpulse: true);
        // �� y ������ jumpForce����վ�ڵ����ϻ���ӵ�������ٶ�

        playerControl.jumpBufferTimer = -999f;
    }

    public override void Update()
    {
        base.Update();

        if (IsCurrentState() == false)
        {
            return;
        }

        if (mainInput.Player.Jump.WasPerformedThisFrame()) // ������ﰴ����Ծ��
        {
            playerControl.jumpBufferTimer = Time.time; // ��¼��Ծ����ʱ��
        }
    }
}
