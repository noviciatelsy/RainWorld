using UnityEngine;

/// <summary>
/// 蜗牛动画总控：移动 squash 等。
/// </summary>
public class SnailAni : MonoBehaviour
{
    [SerializeField] private Snail2D snail;
    [SerializeField] private SnailMoveAni moveAni;

    private void Awake()
    {
        if (snail == null)
        {
            snail = GetComponent<Snail2D>();
        }

        if (moveAni == null)
        {
            moveAni = GetComponentInChildren<SnailMoveAni>(true);
        }
    }

    public void RefreshMoveBaseScale()
    {
        if (moveAni != null)
        {
            moveAni.RefreshBaseScale();
        }
    }
}
