using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbState : PlayerBaseState
{
    public PlayerClimbState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }
}
