using System.Collections.Generic;
using UnityEngine;

public class DroppedMilk : MonoBehaviour
{
    private PickableObject pickableObject;

    [SerializeField] private string EnemyLayerName = "Enemy"; // 敌人Layer名称
    [SerializeField] private float AttractRadius = 9f; // 牛奶吸引检测半径

    // 根据Layer名称生成并缓存的LayerMask
    private int enemyLayerMask;

    private void Awake()
    {
        pickableObject = GetComponent<PickableObject>();

        if (pickableObject == null)
        {
            Debug.LogError(
                $"{name} 没有找到 PickableObject 组件，DroppedMilk 无法监听物品落地事件。",
                this);

            enabled = false;
            return;
        }

        // 检查填写的Layer名称是否存在
        int enemyLayerIndex = LayerMask.NameToLayer(EnemyLayerName);

        if (enemyLayerIndex == -1)
        {
            Debug.LogError(
                $"没有找到名为“{EnemyLayerName}”的Layer，请检查Tags and Layers设置。",
                this);

            enabled = false;
            return;
        }

        // 将Layer字符串名称转换为Physics2D检测所需的LayerMask
        enemyLayerMask = LayerMask.GetMask(EnemyLayerName);
    }

    private void OnEnable()
    {
        if (pickableObject == null)
        {
            return;
        }

        pickableObject.onItemStop += AttractMilkTargets;
    }

    private void OnDisable()
    {
        if (pickableObject == null)
        {
            return;
        }

        pickableObject.onItemStop -= AttractMilkTargets;
    }

    private void AttractMilkTargets()
    {
        TriggerAttractMilkTargets();
    }

    private void TriggerAttractMilkTargets()
    {
        Vector2 milkPosition = transform.position;

        Collider2D[] detectedColliders = Physics2D.OverlapCircleAll(
            milkPosition,
            AttractRadius,
            enemyLayerMask);

        HashSet<MonoBehaviour> triggeredTargets = new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider = detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<IAttractedByMilk>(
                    detectedCollider);

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            IAttractedByMilk milkTarget =
                interfaceBehaviour as IAttractedByMilk;

            milkTarget?.AttractedByMilk(milkPosition);
        }
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