using System.Collections.Generic;
using UnityEngine;

public class DroppedLiquid : MonoBehaviour
{
    private PickableObject pickableObject;

    [SerializeField] private string EnemyLayerName = "Enemy"; // 敌人Layer名称
    [SerializeField] private string PlayerSensorTargetLayerName = "PlayerSensorTarget"; // 玩家感应目标Layer名称
    [SerializeField] private float ContactRadius = 1f; // 液体接触检测半径

    // 根据Layer名称生成并缓存的LayerMask
    private int liquidContactLayerMask;

    private void Awake()
    {
        pickableObject = GetComponent<PickableObject>();

        if (pickableObject == null)
        {
            Debug.LogError(
                $"{name} 没有找到 PickableObject 组件，DroppedLiquid 无法监听物品落地事件。",
                this);

            enabled = false;
            return;
        }

        // 检查填写的Layer名称是否存在
        int enemyLayerIndex = LayerMask.NameToLayer(EnemyLayerName);
        int playerSensorTargetLayerIndex = LayerMask.NameToLayer(PlayerSensorTargetLayerName);

        if (enemyLayerIndex == -1)
        {
            Debug.LogError(
                $"没有找到名为“{EnemyLayerName}”的Layer，请检查Tags and Layers设置。",
                this);

            enabled = false;
            return;
        }

        if (playerSensorTargetLayerIndex == -1)
        {
            Debug.LogError(
                $"没有找到名为“{PlayerSensorTargetLayerName}”的Layer，请检查Tags and Layers设置。",
                this);

            enabled = false;
            return;
        }

        // 将Layer字符串名称转换为Physics2D检测所需的LayerMask
        liquidContactLayerMask = LayerMask.GetMask(
            EnemyLayerName,
            PlayerSensorTargetLayerName);
    }

    private void OnEnable()
    {
        if (pickableObject == null)
        {
            return;
        }

        pickableObject.onItemStop += ContactLiquidTargets;
    }

    private void OnDisable()
    {
        if (pickableObject == null)
        {
            return;
        }

        pickableObject.onItemStop -= ContactLiquidTargets;
    }

    private void ContactLiquidTargets()
    {
        TriggerContactLiquidTargets();
    }

    private void TriggerContactLiquidTargets()
    {
        AudioManager.Instance.PlaySFX("UseItemSplashSFX");
        Vector2 liquidPosition = transform.position;
        bool isWater = IsWaterPickable(pickableObject);

        Collider2D[] detectedColliders = Physics2D.OverlapCircleAll(
            liquidPosition,
            ContactRadius,
            liquidContactLayerMask);

        HashSet<MonoBehaviour> triggeredTargets = new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider = detectedColliders[i];

            if (isWater)
            {
                MonoBehaviour waterBehaviour =
                    FindInterfaceBehaviourInParents<IActivatedByWater>(detectedCollider);

                if (waterBehaviour != null && triggeredTargets.Add(waterBehaviour))
                {
                    (waterBehaviour as IActivatedByWater)?.ActivateByWater();
                }
            }

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<IContactWithLiquid>(detectedCollider);

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            IContactWithLiquid liquidTarget =
                interfaceBehaviour as IContactWithLiquid;

            liquidTarget?.ContactWithLiquid();
        }
    }

    private static bool IsWaterPickable(PickableObject pickable)
    {
        if (pickable == null || pickable.ItemData == null)
        {
            return false;
        }

        return pickable.ItemData.itemEffectData is ItemEffectDataSO_Water;
    }

    /// <summary>
    /// 从碰撞体自身及其父物体上，
    /// 寻找第一个实现指定接口的 MonoBehaviour。
    /// </summary>
    private MonoBehaviour FindInterfaceBehaviourInParents<T>(
        Collider2D myCollider) where T : class
    {
        MonoBehaviour[] parentBehaviours =
            myCollider.GetComponentsInParent<MonoBehaviour>();

        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            MonoBehaviour currentBehaviour = parentBehaviours[i];

            if (currentBehaviour is T)
            {
                return currentBehaviour;
            }
        }

        return null;
    }
}