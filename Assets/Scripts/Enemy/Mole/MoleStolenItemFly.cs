using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 被偷道具从玩家飞向鼹鼠，到达后销毁。
/// </summary>
[DisallowMultipleComponent]
public class MoleStolenItemFly : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flySpeed = 10f;
    [SerializeField] private float arriveDistance = 0.08f;

    private Transform target;

    public static MoleStolenItemFly Spawn(
        Sprite icon,
        Vector3 worldStart,
        Transform flyTarget,
        float speed,
        float displayScale)
    {
        if (icon == null || flyTarget == null)
        {
            return null;
        }

        GameObject instance = new GameObject("MoleStolenItemFly");
        instance.transform.position = worldStart;

        SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
        renderer.sprite = icon;
        renderer.sortingOrder = 20;

        float safeScale = Mathf.Max(0.01f, displayScale);
        instance.transform.localScale = new Vector3(safeScale, safeScale, 1f);

        MoleStolenItemFly fly = instance.AddComponent<MoleStolenItemFly>();
        fly.spriteRenderer = renderer;
        fly.flySpeed = speed;
        fly.target = flyTarget;
        return fly;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            flySpeed * Time.deltaTime
        );

        if ((transform.position - target.position).sqrMagnitude <= arriveDistance * arriveDistance)
        {
            Destroy(gameObject);
        }
    }
}
