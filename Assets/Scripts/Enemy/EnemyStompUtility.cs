using UnityEngine;

public static class EnemyStompUtility
{
    public static bool TryStompFromCollision(
        EnemyStompReceiver receiver,
        Collision2D collision,
        Collider2D stompCollider,
        float minStompDownSpeed = 0.5f)
    {
        if (receiver == null || receiver.IsStomped || collision == null)
        {
            return false;
        }

        Collider2D other = collision.collider;

        if (other == null)
        {
            return false;
        }

        Player player = other.GetComponentInParent<Player>();

        if (player == null)
        {
            return false;
        }

        if (collision.relativeVelocity.y > -minStompDownSpeed)
        {
            return false;
        }

        if (stompCollider != null && other.bounds.min.y < stompCollider.bounds.center.y)
        {
            return false;
        }

        return receiver.TryApplyStomp(player, stompCollider, collision.relativeVelocity);
    }

    public static float GetLocalSpriteTopY(Transform anchor, float fallbackY = 0.35f)
    {
        if (anchor == null)
        {
            return fallbackY;
        }

        float maxY = float.MinValue;
        bool found = false;

        SpriteRenderer[] renderers = anchor.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null || renderer.sprite == null || !renderer.enabled)
            {
                continue;
            }

            Bounds localBounds = renderer.sprite.bounds;
            Vector3 localTop = new Vector3(localBounds.center.x, localBounds.max.y, 0f);
            Vector3 worldTop = renderer.transform.TransformPoint(localTop);
            Vector3 anchorLocalTop = anchor.InverseTransformPoint(worldTop);

            if (!found || anchorLocalTop.y > maxY)
            {
                maxY = anchorLocalTop.y;
                found = true;
            }
        }

        return found ? maxY : fallbackY;
    }
}
