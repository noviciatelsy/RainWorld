using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSwitchTrigger : MonoBehaviour
{
    [SerializeField] private SceneType targetScene=SceneType.None;
    [SerializeField] private Vector3 targetPosition=Vector3.zero;
    [SerializeField] private int playerFacingDirection=1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player== null)
        {
            return;
        }

        if(targetScene == SceneType.None)
        {
            return;
        }

        GlobalUI.Instance.fadeScreenUI.PlayRoomSwitchFade(() =>
        {
            PlayerManager.Instance.SetPendingPlayerShowUpPosition(targetPosition,playerFacingDirection);
            SceneSwitchManager.Instance.SwitchToScene(targetScene);
        });
    }

    private void OnValidate()
    {
        if(playerFacingDirection==1)
        {
            targetPosition=new Vector3(transform.position.x-0.25f,transform.position.y,transform.position.z);
        }
        else if(playerFacingDirection==-1)
        {
            targetPosition = new Vector3(transform.position.x + 0.25f, transform.position.y, transform.position.z);
        }
        else
        {
            targetPosition = transform.position;
        }

    }
}
