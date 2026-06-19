using UnityEngine;

/// <summary>
/// 魔豆：被水吸附后沿 LineRenderer 向上生长到指定目标点。
/// </summary>
[DisallowMultipleComponent]
public class MagicBean : MonoBehaviour, IContactWithLiquid
{
    [Header("Growth")]
    [SerializeField] private MagicBeanVineGrowth vineGrowth;
    [SerializeField] private Transform growTarget;

    [Header("Water")]
    [SerializeField] private MagicBeanWaterCollector waterCollector;

    [Header("Idle Visual")]
    [SerializeField] private GameObject idleVisualRoot;

    private bool isActivated;

    public bool IsActivated => isActivated;

    private void Awake()
    {
        if (vineGrowth == null)
        {
            vineGrowth = GetComponentInChildren<MagicBeanVineGrowth>(true);
        }

        if (waterCollector == null)
        {
            waterCollector = GetComponent<MagicBeanWaterCollector>();
        }

        if (waterCollector == null)
        {
            waterCollector = gameObject.AddComponent<MagicBeanWaterCollector>();
        }

        if (growTarget == null && vineGrowth != null)
        {
            growTarget = vineGrowth.GrowTarget;
        }

        if (idleVisualRoot == null)
        {
            Transform textureRoot = transform.Find("texture");

            if (textureRoot != null)
            {
                idleVisualRoot = textureRoot.gameObject;
            }
        }

        EnsureEnemyLayer();
    }

    private void OnValidate()
    {
        if (vineGrowth == null)
        {
            vineGrowth = GetComponentInChildren<MagicBeanVineGrowth>(true);
        }

        if (waterCollector == null)
        {
            waterCollector = GetComponent<MagicBeanWaterCollector>();
        }
    }

    public void ContactWithLiquid()
    {
        if (isActivated)
        {
            return;
        }

        waterCollector?.OnLiquidContact();
    }

    public void ActivateByWater()
    {
        if (isActivated)
        {
            return;
        }

        isActivated = true;

        if (idleVisualRoot != null)
        {
            idleVisualRoot.SetActive(false);
        }

        if (vineGrowth == null)
        {
            Debug.LogWarning($"[{nameof(MagicBean)}] 未找到 {nameof(MagicBeanVineGrowth)}。", this);
            return;
        }

        vineGrowth.StartGrowth();
    }

    private void EnsureEnemyLayer()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (enemyLayer < 0 || gameObject.layer == enemyLayer)
        {
            return;
        }

        gameObject.layer = enemyLayer;
    }
}
