using UnityEngine;

public class PlayerTalismanThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TalismanProjectile talismanPrefab;
    // 符纸预制体

    [SerializeField] private Transform talismanSpawnPosition;
    // 符纸生成位置

    [Header("Throw Settings")]
    [SerializeField] private float throwSpeed = 3f;
    // 符纸投掷速度

    [SerializeField] private float upwardRatio = 0.5f;
    // 投掷方向的向上比例
    // 越大飞得越高

    private PlayerControl playerControl;

    private void Awake()
    {
        playerControl = GetComponentInParent<PlayerControl>();
    }

    public bool TryThrowTalisman()
    {
 
        if (talismanPrefab == null
            || talismanSpawnPosition == null)
        {
            Debug.LogWarning(
                "PlayerTalismanThrower 缺少符纸预制体或生成位置。",
                this);

            return false;
        }

        float horizontalDirection = playerControl.facingDir;

        Vector2 throwDirection =
            new Vector2(
                horizontalDirection,
                upwardRatio);

        throwDirection.Normalize();

        TalismanProjectile newTalisman =
            Instantiate(
                talismanPrefab,
                talismanSpawnPosition.position,
                Quaternion.identity);

        Vector2 initialVelocity =
            throwDirection * throwSpeed;

        newTalisman.Initialize(
            initialVelocity);

        return true;
    }


   
}