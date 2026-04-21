using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDropPlatformState : PlayerAiredState
{
    public PlayerDropPlatformState(PlayerStateMachine stateMachine, string animBoolName, PlayerControl playerControl) : base(stateMachine, animBoolName, playerControl)
    {
    }
}
