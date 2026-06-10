using System.Collections.Generic;
using UnityEngine;

public class DroppedTreasure : MonoBehaviour
{
    private PickableObject pickableObject;

    [SerializeField] private string MoleLayerName = "Enemy"; // 鼹鼠Layer名称
    [SerializeField] private float AttractRadius = 5f; // 吸引检测半径

    // 根据Layer名称生成并缓存的LayerMask
    private int moleLayerMask;

    private void Awake()
    {
        pickableObject = GetComponent<PickableObject>();

        // 检查填写的Layer名称是否存在
        int moleLayerIndex = LayerMask.NameToLayer(MoleLayerName);

        if (moleLayerIndex == -1)
        {
            Debug.LogError(
                $"没有找到名为“{MoleLayerName}”的Layer，请检查Tags and Layers设置。",
                this);

            enabled = false;
            return;
        }

        // 将Layer字符串名称转换为Physics2D检测所需的LayerMask
        moleLayerMask = LayerMask.GetMask(MoleLayerName);
    }

    private void OnEnable()
    {
        pickableObject.onItemStop += AttractMole;
    }

    private void OnDisable()
    {
        pickableObject.onItemStop -= AttractMole;
    }

    private void AttractMole()
    {
        TriggerAttackMole();
    }

    private void TriggerAttackMole()
    {
        Vector2 treasurePosition = transform.position;

        Collider2D[] detectedColliders = Physics2D.OverlapCircleAll(
            treasurePosition,
            AttractRadius,
            moleLayerMask);

        HashSet<MonoBehaviour> triggeredTargets = new HashSet<MonoBehaviour>();

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            Collider2D detectedCollider = detectedColliders[i];

            MonoBehaviour interfaceBehaviour =
                FindInterfaceBehaviourInParents<IAttractedByTreasure>(
                    detectedCollider);

            if (interfaceBehaviour == null)
            {
                continue;
            }

            if (!triggeredTargets.Add(interfaceBehaviour))
            {
                continue;
            }

            IAttractedByTreasure mole =
                interfaceBehaviour as IAttractedByTreasure;

            mole?.AttractedByTreasure(
                treasurePosition,
                pickableObject);
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