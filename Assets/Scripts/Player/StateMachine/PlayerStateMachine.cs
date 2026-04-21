using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine
{
    public PlayerBaseState currentState { get; private set; }
    // 当前状态


    public bool canChangeState;

    public void Initialize(PlayerBaseState startState)
    // 初始化状态
    {
        canChangeState = true;
        currentState = startState; // 当前状态设为初始状态
        currentState.Enter(); // 进入当前状态（初始状态）

    }

    public void ChangeState(PlayerBaseState newState)
    // 切换状态
    {
        if (canChangeState == false)
        {
            return;
        }
        currentState.Exit(); // 退出当前状态（旧状态）
        currentState = newState; // 当前状态设为新状态
        currentState.Enter(); // 进入当前状态（新状态）
    }

    public void UpdateActiveState()
    {
        currentState.Update();
    }

    public void SwitchOffStateMachine()
    {
        canChangeState = false;
    }
}
