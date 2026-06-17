using UnityEngine;

public class PlayerFireflySpawner : MonoBehaviour
{
    [SerializeField] private Transform fireflySpawnPosition;
    [SerializeField] private Fly2D fireflyPrefab;
    [SerializeField] private ItemDataSO fireflyItemData;
    [SerializeField] private PickableObject pickableObjectPrefab;

    public void SpawnFireFly()
    {
        if (fireflySpawnPosition == null)
        {
            Debug.LogWarning("PlayerFireflySpawner 缺少 fireflySpawnPosition。", this);
            return;
        }

        SpawnFlyAt(fireflySpawnPosition.position, null);
    }

    public void SpawnThrownFly(Vector2 worldPosition, Vector2 initialVelocity)
    {
        SpawnFlyAt(worldPosition, initialVelocity);
    }

    private Fly2D SpawnFlyAt(Vector2 worldPosition, Vector2? initialVelocity)
    {
        if (fireflyPrefab == null)
        {
            Debug.LogWarning("PlayerFireflySpawner 缺少 fireflyPrefab。", this);
            return null;
        }

        if (!FlySpawnUtility.TryResolveSpawn(
                worldPosition,
                out Vector2 spawnPosition,
                out Vector2 initialTarget))
        {
            Debug.LogWarning(
                $"PlayerFireflySpawner: 附近没有可飞行的生成点，已取消生成。位置={worldPosition}",
                this);
            return null;
        }

        Fly2D fly = Instantiate(fireflyPrefab, spawnPosition, Quaternion.identity);
        fly.ConfigureDropItem(fireflyItemData, pickableObjectPrefab);

        if (initialVelocity.HasValue)
        {
            fly.InitializeThrown(initialVelocity.Value);
        }
        else
        {
            fly.InitializeAsFly(initialTarget);
        }

        return fly;
    }
}
