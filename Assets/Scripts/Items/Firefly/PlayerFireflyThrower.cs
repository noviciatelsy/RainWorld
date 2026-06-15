using UnityEngine;

public class PlayerFireflyThrower : MonoBehaviour
{
    [SerializeField] private PlayerFireflySpawner fireflySpawner;
    [SerializeField] private Transform throwSpawnPosition;
    [SerializeField] private float throwSpeed = 4f;
    [SerializeField] private float upwardRatio = 0.7f;

    private PlayerControl playerControl;

    private void Awake()
    {
        playerControl = GetComponentInParent<PlayerControl>();

        if (fireflySpawner == null)
        {
            fireflySpawner = GetComponentInParent<PlayerFireflySpawner>();
        }
    }

    public bool TryThrowFirefly()
    {
        if (fireflySpawner == null || throwSpawnPosition == null)
        {
            Debug.LogWarning("PlayerFireflyThrower 缺少 spawner 或投掷出生点。", this);
            return false;
        }

        float horizontalDirection = playerControl != null && playerControl.facingDir < 0 ? -1f : 1f;
        Vector2 throwDirection = new Vector2(horizontalDirection, upwardRatio).normalized;
        fireflySpawner.SpawnThrownFly(throwSpawnPosition.position, throwDirection * throwSpeed);
        return true;
    }
}
