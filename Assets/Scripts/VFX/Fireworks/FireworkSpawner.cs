using System.Collections;
using UnityEngine;

public class FireworkSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] launchPoints;

    [Header("Firework Variants")]
    [SerializeField] private FireworkVariantDataSO[] variants;

    [Header("Auto Launch Settings")]
    [SerializeField] private bool autoStartOnStart = false;

    [Tooltip("开启发射后，是否先快速发射一轮。关闭后，每个点位会先等待一次发射间隔。")]
    [SerializeField] private bool launchImmediatelyWhenStarted = true;

    [Tooltip("开启发射后，第一轮发射前的随机延迟。用于错开发射点，避免所有点位同时发射。")]
    [SerializeField] private Vector2 firstLaunchDelayRange = new Vector2(0f, 1f);

    [Tooltip("每个发射点两轮烟花之间的随机间隔。")]
    [SerializeField] private Vector2 launchIntervalRange = new Vector2(2f, 4f);

    [Tooltip("每一轮从同一个发射点发射的烟花数量范围。")]
    [SerializeField] private Vector2Int fireworkCountPerRoundRange = new Vector2Int(1, 3);

    [Tooltip("如果一轮发射数量大于 1，则同一轮内两发烟花之间的随机间隔。")]
    [SerializeField] private Vector2 fireworkIntervalInRoundRange = new Vector2(0.1f, 0.35f);

    [Header("Debug")]
    [SerializeField] private bool toggleLaunchWithKey = true;
    [SerializeField] private KeyCode launchKey = KeyCode.F;

    private Coroutine[] launchPointCoroutines;
    private bool isLaunchingFireworks;

    public bool IsLaunchingFireworks => isLaunchingFireworks;

    private void Start()
    {
        if (autoStartOnStart)
        {
            StartLaunchingFireworks();
        }
    }

    private void Update()
    {
        if (toggleLaunchWithKey == false)
        {
            return;
        }

        if (Input.GetKeyDown(launchKey))
        {
            if (isLaunchingFireworks)
            {
                StopLaunchingFireworks();
            }
            else
            {
                StartLaunchingFireworks();
            }
        }
    }

    private void OnDisable()
    {
        StopLaunchingFireworks();
    }

    public void StartLaunchingFireworks()
    {
        if (isLaunchingFireworks)
        {
            return;
        }

        if (variants == null || variants.Length == 0)
        {
            Debug.LogWarning("FireworkSpawner 没有配置任何烟花变体。");
            return;
        }

        isLaunchingFireworks = true;

        if (launchPoints == null || launchPoints.Length == 0)
        {
            launchPointCoroutines = new Coroutine[1];
            launchPointCoroutines[0] = StartCoroutine(LaunchPointRoutine(null));
            return;
        }

        launchPointCoroutines = new Coroutine[launchPoints.Length];

        for (int i = 0; i < launchPoints.Length; i++)
        {
            Transform currentLaunchPoint = launchPoints[i];

            if (currentLaunchPoint == null)
            {
                Debug.LogWarning($"FireworkSpawner 的 launchPoints 中第 {i} 个点位为空，已跳过。");
                continue;
            }

            launchPointCoroutines[i] = StartCoroutine(LaunchPointRoutine(currentLaunchPoint));
        }
    }

    public void StopLaunchingFireworks()
    {
        isLaunchingFireworks = false;

        if (launchPointCoroutines == null)
        {
            return;
        }

        for (int i = 0; i < launchPointCoroutines.Length; i++)
        {
            if (launchPointCoroutines[i] != null)
            {
                StopCoroutine(launchPointCoroutines[i]);
                launchPointCoroutines[i] = null;
            }
        }

        launchPointCoroutines = null;
    }

    public void LaunchRandomFirework()
    {
        Transform selectedLaunchPoint = GetRandomLaunchPoint();
        Vector3 spawnPosition = GetLaunchPosition(selectedLaunchPoint);

        LaunchRandomFireworkAtPosition(spawnPosition);
    }

    public void LaunchRandomFireworkFromPoint(Transform myLaunchPoint)
    {
        Vector3 spawnPosition = GetLaunchPosition(myLaunchPoint);
        LaunchRandomFireworkAtPosition(spawnPosition);
    }

    public void LaunchRandomFireworkAtPosition(Vector3 mySpawnPosition)
    {
        if (variants == null || variants.Length == 0)
        {
            Debug.LogWarning("FireworkSpawner 没有配置任何烟花变体。");
            return;
        }

        FireworkVariantDataSO selectedVariant = variants[Random.Range(0, variants.Length)];

        LaunchFirework(selectedVariant, mySpawnPosition);
    }

    public void LaunchFirework(FireworkVariantDataSO myVariant, Vector3 mySpawnPosition)
    {
        if (myVariant == null)
        {
            Debug.LogWarning("尝试发射烟花，但传入的 FireworkVariantDataSO 为空。");
            return;
        }

        if (myVariant.RocketPrefab == null)
        {
            Debug.LogWarning("烟花变体没有配置 RocketPrefab。");
            return;
        }

        if (myVariant.ExplosionPrefab == null)
        {
            Debug.LogWarning("烟花变体没有配置 ExplosionPrefab。");
            return;
        }

        FireworkRocket newRocket = Instantiate(myVariant.RocketPrefab, mySpawnPosition, Quaternion.identity);
        newRocket.Setup(myVariant, mySpawnPosition);
    }

    private IEnumerator LaunchPointRoutine(Transform myLaunchPoint)
    {
        if (launchImmediatelyWhenStarted)
        {
            float firstDelay = GetRandomFloat(firstLaunchDelayRange);
            yield return new WaitForSeconds(firstDelay);
        }
        else
        {
            float firstDelay = GetRandomFloat(launchIntervalRange);
            yield return new WaitForSeconds(firstDelay);
        }

        while (isLaunchingFireworks)
        {
            yield return LaunchFireworkRound(myLaunchPoint);

            float nextRoundDelay = GetRandomFloat(launchIntervalRange);
            yield return new WaitForSeconds(nextRoundDelay);
        }
    }

    private IEnumerator LaunchFireworkRound(Transform myLaunchPoint)
    {
        int fireworkCount = GetRandomInt(fireworkCountPerRoundRange);

        for (int i = 0; i < fireworkCount; i++)
        {
            if (isLaunchingFireworks == false)
            {
                yield break;
            }

            LaunchRandomFireworkFromPoint(myLaunchPoint);

            bool hasNextFireworkInThisRound = i < fireworkCount - 1;

            if (hasNextFireworkInThisRound)
            {
                float interval = GetRandomFloat(fireworkIntervalInRoundRange);
                yield return new WaitForSeconds(interval);
            }
        }
    }

    private Transform GetRandomLaunchPoint()
    {
        if (launchPoints == null || launchPoints.Length == 0)
        {
            return null;
        }

        int validPointCount = GetValidLaunchPointCount();

        if (validPointCount <= 0)
        {
            return null;
        }

        int targetValidIndex = Random.Range(0, validPointCount);
        int currentValidIndex = 0;

        for (int i = 0; i < launchPoints.Length; i++)
        {
            if (launchPoints[i] == null)
            {
                continue;
            }

            if (currentValidIndex == targetValidIndex)
            {
                return launchPoints[i];
            }

            currentValidIndex++;
        }

        return null;
    }

    private int GetValidLaunchPointCount()
    {
        if (launchPoints == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < launchPoints.Length; i++)
        {
            if (launchPoints[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private Vector3 GetLaunchPosition(Transform myLaunchPoint)
    {
        if (myLaunchPoint != null)
        {
            return myLaunchPoint.position;
        }

        return transform.position;
    }

    private float GetRandomFloat(Vector2 myRange)
    {
        float min = Mathf.Min(myRange.x, myRange.y);
        float max = Mathf.Max(myRange.x, myRange.y);

        min = Mathf.Max(0f, min);
        max = Mathf.Max(0f, max);

        return Random.Range(min, max);
    }

    private int GetRandomInt(Vector2Int myRange)
    {
        int min = Mathf.Min(myRange.x, myRange.y);
        int max = Mathf.Max(myRange.x, myRange.y);

        min = Mathf.Max(1, min);
        max = Mathf.Max(1, max);

        return Random.Range(min, max + 1);
    }
}