using UnityEngine;

/// <summary>
/// 蜗牛吃掉道具时销毁，不修改 PickableObject 本身。
/// </summary>
internal static class SnailPickableHelper
{
    public static void Consume(PickableObject pickable)
    {
        if (pickable == null)
        {
            return;
        }

        Collider2D[] colliders = pickable.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        Rigidbody2D body = pickable.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.simulated = false;
        }

        Object.Destroy(pickable.gameObject);
    }
}
