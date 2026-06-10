using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootArea_Tomb : LootArea
{
    [SerializeField] private ItemDataSO cursedTreasureData;
    [SerializeField] private Ghost ghostPrefab;

    [Header("幽灵生成设置")]
    [SerializeField, Min(0f)] private float minGhostSpawnDistance = 5f;
    [SerializeField, Min(0f)] private float maxGhostSpawnDistance = 9f;

    private bool hasSpawnGhost = false;

    public override void Interact()
    {
        base.Interact();

        // 每个坟墓只会生成一次幽灵
        if (hasSpawnGhost == true)
        {
            return;
        }

        Player player = PlayerManager.Instance.TryGetCurrentPlayer();

        if (player == null)
        {
            Debug.LogWarning("没有找到当前玩家，无法生成幽灵。", this);
            return;
        }

        if (ghostPrefab == null)
        {
            Debug.LogError("LootArea_Tomb 没有设置幽灵预制体。", this);
            return;
        }

        hasSpawnGhost = true;

        SpawnGhostAroundPlayer(player);
    }

    /// <summary>
    /// 在玩家周围的指定距离范围内随机生成幽灵。
    /// </summary>
    private void SpawnGhostAroundPlayer(Player player)
    {
        // 防止最大距离被意外设置得比最小距离还小
        float validMaxDistance = Mathf.Max(
            minGhostSpawnDistance,
            maxGhostSpawnDistance);

        // 获取一个随机方向
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        // 极低概率下随机点可能恰好为零，手动提供一个备用方向
        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }

        float randomDistance = Random.Range(
            minGhostSpawnDistance,
            validMaxDistance);

        Vector2 spawnOffset = randomDirection * randomDistance;
        Vector3 playerPosition = player.transform.position;

        Vector3 spawnPosition = new Vector3(
            playerPosition.x + spawnOffset.x,
            playerPosition.y + spawnOffset.y,
            playerPosition.z);

        Instantiate(
            ghostPrefab,
            spawnPosition,
            ghostPrefab.transform.rotation);
    }

    public override void GenerateLoot()
    {
        inventory.AddItem(cursedTreasureData);
        base.GenerateLoot();
    }

    private void OnValidate()
    {
        minGhostSpawnDistance = Mathf.Max(
            0f,
            minGhostSpawnDistance);

        maxGhostSpawnDistance = Mathf.Max(
            minGhostSpawnDistance,
            maxGhostSpawnDistance);
    }
}