using UnityEngine;

/// <summary>
/// 魔豆藤蔓攀爬区，逻辑与 Rope 一致。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class MagicBeanVineClimbZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerControl playerControl = collision.GetComponentInParent<PlayerControl>();

        if (playerControl == null)
        {
            return;
        }

        playerControl.SetInRopeArea(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerControl playerControl = collision.GetComponentInParent<PlayerControl>();

        if (playerControl == null)
        {
            return;
        }

        playerControl.SetInRopeArea(false);
    }
}
