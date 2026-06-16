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

        Vector2 spawnPosition = FlySpawnUtility.ResolveSpawnPosition(worldPosition);
        Fly2D fly = Instantiate(fireflyPrefab, spawnPosition, Quaternion.identity);
        fly.ConfigureDropItem(fireflyItemData, pickableObjectPrefab);

        if (initialVelocity.HasValue)
        {
            fly.InitializeThrown(initialVelocity.Value);
        }
        else
        {
            fly.InitializeAsFly();
        }

        return fly;
    }
}
