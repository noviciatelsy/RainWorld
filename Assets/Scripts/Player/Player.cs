using UnityEngine;

public class Player : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerManager.Instance.RegisterPlayer(this);
        
    }

    private void OnDisable()
    {
        PlayerManager.Instance.UnregisterPlayer(this);
    }

}
