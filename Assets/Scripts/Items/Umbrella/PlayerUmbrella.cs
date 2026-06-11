using UnityEngine;

public class PlayerUmbrella : MonoBehaviour
{
    [Header("References")]
    private PlayerControl playerControl;
    // 玩家控制脚本

    private Rigidbody2D playerRigidbody;
    // 玩家刚体


    [Header("Umbrella Settings")]
    [SerializeField] private float fallingGravityMultiplier = 0.2f;
    // 开伞并且玩家正在下落时使用的额外重力倍率


    [SerializeField] private float normalGravityMultiplier = 1f;
    // 未触发缓降时使用的额外重力倍率

    public bool IsUmbrellaOpen { get; private set; }
    // 雨伞是否处于打开状态


    private void Awake()
    {
        playerControl = GetComponentInParent<PlayerControl>();

        if (playerRigidbody == null && playerControl != null)
        {
            playerRigidbody = playerControl.rb;
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponentInParent<Rigidbody2D>();
        }
    }


    private void Update()
    {
        UpdateUmbrellaGravity();
    }


    /// <summary>
    /// 开伞。
    /// 开伞后，只有玩家正在下落时才会触发缓降。
    /// </summary>
    public void OpenUmbrella()
    {
        IsUmbrellaOpen = true;

        UpdateUmbrellaGravity();
    }


    /// <summary>
    /// 关伞。
    /// 关伞后，额外重力倍率恢复为 1。
    /// </summary>
    public void CloseUmbrella()
    {
        IsUmbrellaOpen = false;

        ResetBonusGravityMultiplier();
    }


    public void ToggleUmbrella()
    {
        if (IsUmbrellaOpen)
        {
            CloseUmbrella();
        }
        else
        {
            OpenUmbrella();
        }
    }


    /// <summary>
    /// 根据玩家当前 y 方向速度更新额外重力倍率。
    /// 
    /// y 速度 >= 0：
    ///     正在上升，或者没有下落。
    ///     此时保持倍率为 1，避免跳得更高。
    /// 
    /// y 速度 < 0：
    ///     正在下落。
    ///     此时降低倍率，产生缓降效果。
    /// </summary>
    private void UpdateUmbrellaGravity()
    {
        if (playerControl == null || playerRigidbody == null)
        {
            return;
        }

        if (!IsUmbrellaOpen)
        {
            ResetBonusGravityMultiplier();
            return;
        }

        if (playerRigidbody.velocity.y >= 0f)
        {
            playerControl.SetBonusGravityMultiplier(
                normalGravityMultiplier);

            return;
        }

        playerControl.SetBonusGravityMultiplier(
            fallingGravityMultiplier);
    }


    /// <summary>
    /// 将额外重力倍率恢复为普通状态。
    /// </summary>
    private void ResetBonusGravityMultiplier()
    {
        if (playerControl == null)
        {
            return;
        }

        playerControl.SetBonusGravityMultiplier(
            normalGravityMultiplier);
    }
}