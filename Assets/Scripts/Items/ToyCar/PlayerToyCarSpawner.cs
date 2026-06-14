using UnityEngine;

public class PlayerToyCarSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToyCarController toyCarPrefab;
    // 玩具车预制体

    [SerializeField] private Transform toyCarSpawnPosition;
    // 玩具车生成位置
    // 建议作为玩家子物体，放在玩家前方

    private PlayerControl playerControl;
    // 玩家控制脚本
    // 用来读取 facingDir


    private void Awake()
    {
        if (playerControl == null)
        {
            playerControl =
                GetComponentInParent<PlayerControl>();
        }
    }


    public bool TrySpawnToyCar()
    {

        if (toyCarPrefab == null
            || toyCarSpawnPosition == null)
        {
            Debug.LogWarning
            (
                "PlayerToyCarSpawner 缺少玩具车预制体或生成位置。",
                this
            );

            return false;
        }

        int spawnDirection =
            GetSpawnDirection();

        ToyCarController newToyCar =
            Instantiate
            (
                toyCarPrefab,
                toyCarSpawnPosition.position,
                Quaternion.identity
            );

        newToyCar.Initialize
        (
            spawnDirection
        );

        return true;
    }


    private int GetSpawnDirection()
    {
        if (playerControl != null)
        {
            return playerControl.facingDir;
        }

        if (transform.localScale.x >= 0f)
        {
            return 1;
        }

        return -1;
    }
}