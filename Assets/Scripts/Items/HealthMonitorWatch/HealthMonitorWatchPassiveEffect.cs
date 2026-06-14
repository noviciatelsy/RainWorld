using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthMonitorWatchPassiveEffect : MonoBehaviour
{
    private PlayerVitals playerVitals;
    private void Awake()
    {
        playerVitals = GetComponentInParent<PlayerVitals>();
    }


    public void EnableEffect()
    {
        ApplyEffect();
    }


    public void DisableEffect()
    {

        RemoveEffect();
    }



    private void ApplyEffect()
    {
        playerVitals.ReduceHungerIncreaseAmount(0.1f);
    }


    private void RemoveEffect()
    {
        playerVitals.AddHungerIncreaseAmount(0.1f);
    }

}
