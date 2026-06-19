using UnityEngine;

/// <summary>
/// 符纸落地后飞向目标怪物（参考 MoleStolenItemFly）。
/// </summary>
[DisallowMultipleComponent]
public class TalismanTargetFly : MonoBehaviour
{
    private Transform flyTarget;
    private float flyDuration;
    private float elapsedTime;
    private Vector3 startPosition;

    public static void Begin(GameObject flyer, Transform target, float duration)
    {
        if (flyer == null || target == null)
        {
            return;
        }

        TalismanTargetFly fly = flyer.GetComponent<TalismanTargetFly>();
        if (fly == null)
        {
            fly = flyer.AddComponent<TalismanTargetFly>();
        }

        fly.Initialize(target, duration);
    }

    private void Initialize(Transform target, float duration)
    {
        flyTarget = target;
        flyDuration = Mathf.Max(0.01f, duration);
        elapsedTime = 0f;
        startPosition = transform.position;
    }

    private void Update()
    {
        if (flyTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / flyDuration);
        transform.position = Vector3.Lerp(startPosition, flyTarget.position, progress);

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
