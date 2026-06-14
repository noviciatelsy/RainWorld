using UnityEngine;

public class PlayerMeatBaitThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeatBaitProjectile meatBaitPrefab;
    // 肉饵预制体

    [SerializeField] private Transform meatBaitSpawnPosition;
    // 肉饵生成位置


    [Header("Throw Settings")]
    [SerializeField] private float throwSpeed = 3f;
    // 肉饵投掷速度

    [SerializeField] private float upwardRatio = 0.6f;
    // 根据玩家朝向投掷时，向上的比例

    private PlayerControl playerControl;

    private void Awake()
    {
        playerControl = GetComponentInParent<PlayerControl>();
    }


    public bool TryThrowMeatBait()
    {

        if (meatBaitPrefab == null
            || meatBaitSpawnPosition == null)
        {
            Debug.LogWarning
            (
                "PlayerMeatBaitThrower 缺少肉饵预制体或生成位置。",
                this
            );

            return false;
        }

        float horizontalDirection =
            playerControl.facingDir >= 0f ? 1f : -1f;

        Vector2 throwDirection =
            new Vector2
            (
                horizontalDirection,
                upwardRatio
            );

        throwDirection.Normalize();

        SpawnMeatBait
        (
            throwDirection
        );

        return true;
    }


    private void SpawnMeatBait(Vector2 myThrowDirection)
    {
        MeatBaitProjectile newMeatBait =
            Instantiate
            (
                meatBaitPrefab,
                meatBaitSpawnPosition.position,
                Quaternion.identity
            );

        Vector2 initialVelocity =
            myThrowDirection * throwSpeed;

        newMeatBait.Initialize
        (
            initialVelocity
        );

    }
}