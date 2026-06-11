using UnityEngine;

public class PlayerTorchThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TorchProjectile torchPrefab;
    // 火把预制体

    [SerializeField] private Transform torchSpawnPosition;
    // 火把生成位置

    [Header("Throw Settings")]
    [SerializeField] private float throwSpeed = 10f;
    // 火把的初始投掷速度

    private PlayerControl playerControl;

    private void Awake()
    {
        playerControl = GetComponentInParent<PlayerControl>();
    }

    /// <summary>
    /// 尝试向指定方向投掷火把。
    /// </summary>
    /// <returns>
    /// 是否成功投掷。
    /// </returns>
    public bool TryThrowTorch()
    {
        if (torchPrefab == null
            || torchSpawnPosition == null)
        {
            Debug.LogWarning(
                "PlayerTorchThrower 缺少火把预制体或生成位置。",
                this);

            return false;
        }

        Vector2 normalizedDirection = new Vector2(1 * playerControl.facingDir, 1.5f);

        TorchProjectile newTorch =
            Instantiate(
                torchPrefab,
                torchSpawnPosition.position,
                Quaternion.identity);

        Vector2 initialVelocity =
            normalizedDirection * throwSpeed;

        newTorch.Initialize(
            initialVelocity);
        return true;
    }
}