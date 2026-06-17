using UnityEngine;

/// <summary>
/// 饮品飞向机器人（参考 MoleStolenItemFly）：直接移动 Transform，不依赖 Rigidbody2D。
/// </summary>
[DisallowMultipleComponent]
public class RobotDrinkFly : MonoBehaviour
{
    private Transform flyTarget;
    private RobotDrinkCollector collector;
    private PickableObject pickable;
    private float flySpeed;
    private float arriveDistance;

    public static bool TryBegin(
        PickableObject targetPickable,
        Transform targetTransform,
        RobotDrinkCollector ownerCollector,
        float speed,
        float arriveDist)
    {
        if (targetPickable == null || targetTransform == null || ownerCollector == null)
        {
            return false;
        }

        if (targetPickable.GetComponent<RobotDrinkFly>() != null)
        {
            return false;
        }

        PreparePickableForFly(targetPickable);

        RobotDrinkFly fly = targetPickable.gameObject.AddComponent<RobotDrinkFly>();
        fly.pickable = targetPickable;
        fly.flyTarget = targetTransform;
        fly.collector = ownerCollector;
        fly.flySpeed = speed;
        fly.arriveDistance = arriveDist;
        return true;
    }

    private static void PreparePickableForFly(PickableObject pickableObject)
    {
        pickableObject.enabled = false;

        Collider2D[] colliders = pickableObject.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody2D rb = pickableObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            return;
        }

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
    }

    private void Update()
    {
        if (flyTarget == null || collector == null || pickable == null)
        {
            Destroy(this);
            return;
        }

        if (collector.Robot != null && collector.Robot.IsDrinkFrozen)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            flyTarget.position,
            flySpeed * Time.deltaTime
        );

        float arriveSqr = arriveDistance * arriveDistance;
        if ((transform.position - flyTarget.position).sqrMagnitude > arriveSqr)
        {
            return;
        }

        collector.CompleteCollect(pickable);
        Destroy(this);
    }
}
