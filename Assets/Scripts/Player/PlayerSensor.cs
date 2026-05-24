using System.Collections.Generic;
using UnityEngine;

public class PlayerSensor : MonoBehaviour
{
    private MainInput mainInput;

    // 当前 PlayerSensor 范围内的可交互目标
    private readonly List<PlayerSensorTarget> nearbyTargets = new List<PlayerSensorTarget>();

    private void Awake()
    {
        mainInput=InputManager.Instance.mainInput;
    }

    private void Update()
    {
        if (mainInput.Player.Interact.WasPerformedThisFrame())
        {
            InteractWithNearestTarget();
        }
    }

    private void InteractWithNearestTarget()
    {
        // 清理已经被销毁的目标，避免空引用
        nearbyTargets.RemoveAll(target => target == null);

        if (nearbyTargets.Count == 0)
        {
            return;
        }

        PlayerSensorTarget nearestTarget = null;
        float nearestSqrDistance = float.MaxValue;

        Vector3 sensorPosition = transform.position;

        foreach (PlayerSensorTarget target in nearbyTargets)
        {
            // 用 sqrMagnitude 避免开平方，距离比较时更省一点点性能
            float sqrDistance = (target.transform.position - sensorPosition).sqrMagnitude;

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestTarget = target;
            }
        }

        if (nearestTarget != null)
        {
            nearestTarget.Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerSensorTarget target = collision.GetComponentInParent<PlayerSensorTarget>();

        if (target == null)
        {
            return;
        }

        if (nearbyTargets.Contains(target))
        {
            return;
        }

        nearbyTargets.Add(target);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerSensorTarget target = collision.GetComponentInParent<PlayerSensorTarget>();

        if (target == null)
        {
            return;
        }

        nearbyTargets.Remove(target);
    }
}