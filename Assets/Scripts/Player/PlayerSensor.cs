using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家交互感应器：进入 PlayerSensorTarget 区域后按 Interact(E) 触发交互。
/// </summary>
public class PlayerSensor : MonoBehaviour
{
    private readonly List<PlayerSensorTarget> targetsInRange = new List<PlayerSensorTarget>();
    private MainInput mainInput;

    private void Awake()
    {
        mainInput = InputManager.Instance.mainInput;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerSensorTarget target = other.GetComponent<PlayerSensorTarget>();
        if (target == null || targetsInRange.Contains(target))
        {
            return;
        }

        targetsInRange.Add(target);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerSensorTarget target = other.GetComponent<PlayerSensorTarget>();
        if (target == null)
        {
            return;
        }

        targetsInRange.Remove(target);
    }

    private void Update()
    {
        if (mainInput == null)
        {
            return;
        }

        if (ElevatorInputGate.IsBlocking)
        {
            return;
        }

        if (!mainInput.Player.Interact.WasPerformedThisFrame())
        {
            return;
        }

        for (int i = targetsInRange.Count - 1; i >= 0; i--)
        {
            PlayerSensorTarget target = targetsInRange[i];
            if (target == null)
            {
                targetsInRange.RemoveAt(i);
                continue;
            }

            target.Interact();
        }
    }
}
