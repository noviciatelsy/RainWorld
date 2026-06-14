using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoleAmuletPassiveEffect : MonoBehaviour
{
    public bool canUseMoleCave {  get; private set; }

    private int equippedCount;

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
        canUseMoleCave = true;
    }


    private void RemoveEffect()
    {
        canUseMoleCave=false;
    }


}
