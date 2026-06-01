using System.Collections.Generic;
using UnityEngine;

public class MainMenuFlyingItemSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemListDataSO itemDataBase;
    [SerializeField] private MainMenuFlyingItem flyingItemPrefab;
    [SerializeField] private RectTransform flyingItemRoot;
    [SerializeField] private MainMenuFlyingItemPhysicsManager physicsManager;
    [SerializeField] private List<MainMenuItemThrowPoint> throwPoints = new List<MainMenuItemThrowPoint>();

    [Header("Item Size")]
    [SerializeField] private Vector2 cellSize = new Vector2(65f, 65f);

    [Header("Spawn Control")]
    [SerializeField] private bool spawnOnEnable = true;
    [SerializeField] private int maxAliveItemCount = 40;

    private readonly List<float> nextSpawnTimes = new List<float>();

    private void OnEnable()
    {
        if (spawnOnEnable)
        {
            ResetSpawnTimers();
        }
    }

    private void Update()
    {
        if (itemDataBase == null || flyingItemPrefab == null || flyingItemRoot == null || physicsManager == null)
        {
            return;
        }

        if (physicsManager.AliveItemCount >= maxAliveItemCount)
        {
            return;
        }

        EnsureTimerCount();

        float currentTime = Time.unscaledTime;

        for (int i = 0; i < throwPoints.Count; i++)
        {
            MainMenuItemThrowPoint throwPoint = throwPoints[i];

            if (throwPoint == null)
            {
                continue;
            }

            if (currentTime < nextSpawnTimes[i])
            {
                continue;
            }

            SpawnOneItem(throwPoint);

            nextSpawnTimes[i] = currentTime + throwPoint.RollSpawnInterval();
        }
    }

    private void SpawnOneItem(MainMenuItemThrowPoint throwPoint)
    {
        ItemDataSO itemData = RollRandomItemData();

        if (itemData == null)
        {
            return;
        }

        MainMenuFlyingItem flyingItem = Instantiate(flyingItemPrefab, flyingItemRoot);

        Vector2 spawnPosition = flyingItemRoot.InverseTransformPoint(throwPoint.transform.position);

        Vector2 initialVelocity = throwPoint.RollInitialVelocity();
        float initialAngle = throwPoint.RollInitialAngle();
        float initialAngularVelocity = throwPoint.RollInitialAngularVelocity(initialVelocity);

        flyingItem.Setup
        (
            itemData,
            cellSize,
            spawnPosition,
            initialVelocity,
            initialAngle,
            initialAngularVelocity
        );

        physicsManager.RegisterItem(flyingItem);
    }

    private ItemDataSO RollRandomItemData()
    {
        if (itemDataBase == null || itemDataBase.itemList == null || itemDataBase.itemList.Length <= 0)
        {
            return null;
        }

        List<ItemDataSO> candidates = new List<ItemDataSO>();

        for (int i = 0; i < itemDataBase.itemList.Length; i++)
        {
            ItemDataSO itemData = itemDataBase.itemList[i];

            if (itemData == null)
            {
                continue;
            }

            if (itemData.itemIcon == null)
            {
                continue;
            }

            if (itemData.backpackItemData == null)
            {
                continue;
            }

            candidates.Add(itemData);
        }

        if (candidates.Count <= 0)
        {
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void ResetSpawnTimers()
    {
        EnsureTimerCount();

        float currentTime = Time.unscaledTime;

        for (int i = 0; i < throwPoints.Count; i++)
        {
            if (throwPoints[i] == null)
            {
                nextSpawnTimes[i] = currentTime;
                continue;
            }

            nextSpawnTimes[i] = currentTime + throwPoints[i].RollSpawnInterval();
        }
    }

    private void EnsureTimerCount()
    {
        while (nextSpawnTimes.Count < throwPoints.Count)
        {
            nextSpawnTimes.Add(Time.unscaledTime);
        }

        while (nextSpawnTimes.Count > throwPoints.Count)
        {
            nextSpawnTimes.RemoveAt(nextSpawnTimes.Count - 1);
        }
    }
}