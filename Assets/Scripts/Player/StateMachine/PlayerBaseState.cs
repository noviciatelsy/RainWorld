using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBaseState
{
    protected PlayerStateMachine stateMachine;

    protected PlayerControl playerControl;
    protected string animBoolName;
    // 状态对应的动画器bool参数名

    protected Animator anim;
    protected Rigidbody2D rb;
    protected MainInput mainInput;

    public PlayerBaseState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl)
    // 构造函数，获取传入所属的状态机和该状态对应的动画器bool参数名
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
        // 进入状态时将动画器对应的bool参数设为true

    }

    public virtual void Update()
    {
        anim.SetFloat("yVelocity", rb.velocity.y);
        // 将每一帧的y方向速度传入动画参数
    }

    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);
        // 退出状态时将动画器对应的bool参数设为false
    }
}
