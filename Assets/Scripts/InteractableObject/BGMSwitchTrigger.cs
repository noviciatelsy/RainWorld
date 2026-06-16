using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BGMSwitchTrigger : MonoBehaviour
{
    [SerializeField] private string BGMToSwitch;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player == null)
        {
            return;
        }

        if (BGMToSwitch=="")
        {
            return;
        }

        AudioManager.Instance.PlayBGM(BGMToSwitch);
    }
}
