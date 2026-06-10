using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定义一个黑暗房间区域。
///
/// 玩家进入时启用黑暗遮罩，离开时关闭。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class DarkRoomZone : MonoBehaviour
{
    [SerializeField] private bool isDark=false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Player>() == null)
        {
            return;
        }
        
        if(InGameUI.Instance.darknessMaskController != null)
        {
            if(isDark)
            {
                InGameUI.Instance.darknessMaskController.SetDarknessActive(true);
            }
            else
            {
                InGameUI.Instance.darknessMaskController.SetDarknessActive(false);
            }
        }
    }

}