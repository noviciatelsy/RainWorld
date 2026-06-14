using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Setup/ItemEffect Data/HealthMonitorWatch", fileName = "ItemEffectData_HealthMonitorWatch")]
public class ItemEffectDataSO_HealthMonitorWatch : ItemEffectDataSO
{
    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        HealthMonitorWatchPassiveEffect healthMonitorWatchPassiveEffect =player.GetComponentInChildren<HealthMonitorWatchPassiveEffect>();
        if (healthMonitorWatchPassiveEffect != null )
        {
            healthMonitorWatchPassiveEffect.EnableEffect();
        }
      
    }

    public override void Unsubscribe()
    {
        base.Subscribe(player);
        HealthMonitorWatchPassiveEffect healthMonitorWatchPassiveEffect = player.GetComponentInChildren<HealthMonitorWatchPassiveEffect>();
        if (healthMonitorWatchPassiveEffect != null)
        {
            healthMonitorWatchPassiveEffect.DisableEffect();
        }
    }
}
