using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBaseState
{
    protected PlayerStateMachine stateMachine;

    protected PlayerControl playerControl;
    protected string animBoolName;
    // ????????????bool??????

    protected Animator anim;
    protected Rigidbody2D rb;
    protected MainInput mainInput;

    public PlayerBaseState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl)
    // ????????????????????????????????????????bool??????
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
        this.playerControl = playerControl;
        anim = playerControl.anim;
        rb = playerControl.rb;
        mainInput = playerControl.mainInput;
    }

    public virtual void Enter()
    {
        anim.SetBool(animBoolName, true);
        // ????????????????????bool???????true

    }

    public virtual void Update()
    {
        anim.SetFloat("yVelocity", rb.velocity.y);
        // ???????y????????????????
    }

    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);
        // ???????????????????bool???????false
    }

    protected bool IsCurrentState()
    {
        return stateMachine.currentState == this;
    }

    protected bool TryEnterClimbState()
    {
        if (playerControl.CanEnterClimbState())
        {
            stateMachine.ChangeState(playerControl.climbState);
            return true;
        }

        return false;
    }

    protected bool TryEnterSwimState()
    {
        if (playerControl.CanEnterSwimState())
        {
            stateMachine.ChangeState(playerControl.swimState);
            return true;
        }

        return false;
    }
}
