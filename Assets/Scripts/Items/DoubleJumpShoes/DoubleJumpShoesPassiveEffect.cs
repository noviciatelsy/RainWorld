using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleJumpShoesPassiveEffect : MonoBehaviour
{
    private PlayerControl playerControl;

    private int equippedCount;
    private void Awake()
    {
        playerControl = GetComponentInParent<PlayerControl>();
    }


    public void EnableEffect()
    {
        equippedCount++;

        if (equippedCount > 1)
        {
            return;
        }

        ApplyEffect();
    }


    public void DisableEffect()
    {
        if (equippedCount <= 0)
        {
            equippedCount = 0;
            return;
        }

        equippedCount--;

        if (equippedCount > 0)
        {
            return;
        }

        RemoveEffect();
    }


    public void ForceClearEffect()
    {
        equippedCount = 0;

        RemoveEffect();
    }



    private void ApplyEffect()
    {
        playerControl.EnableDoubleJump(true);
    }


    private void RemoveEffect()
    {
        playerControl.EnableDoubleJump(false);
    }

}
