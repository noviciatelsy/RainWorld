using UnityEngine;
using UnityEngine.Events;

public class MinerHelmetPassiveEffect : MonoBehaviour
{
    public bool hasHeadProtection {  get; private set; }
    // 是否拥有头部保护

    private int equippedCount;
    // 当前已经装备的矿工头盔数量
    // 注意：
    // 这个数量可以大于 1
    // 但实际效果只会开启一次


    public int EquippedCount
    {
        get
        {
            return equippedCount;
        }
    }


    /// <summary>
    /// 开启矿工头盔效果。
    /// 
    /// 每装备一个矿工头盔都会调用一次。
    /// 但是只有从 0 个变成 1 个时，才会真正开启效果。
    /// </summary>
    public void EnableEffect()
    {
        equippedCount++;

        if (equippedCount > 1)
        {
            // 已经有至少一个矿工头盔在生效了。
            // 重复装备时，只记录数量，不重复开启效果。
            return;
        }

        ApplyEffect();
    }


    /// <summary>
    /// 关闭矿工头盔效果。
    /// 
    /// 每移除一个矿工头盔都会调用一次。
    /// 但是只有从 1 个变成 0 个时，才会真正关闭效果。
    /// </summary>
    public void DisableEffect()
    {
        if (equippedCount <= 0)
        {
            // 防止外部系统误调用关闭，导致数量变成负数。
            equippedCount = 0;
            return;
        }

        equippedCount--;

        if (equippedCount > 0)
        {
            // 还有其他矿工头盔正在装备。
            // 此时不能关闭头部保护，也不能关闭灯光。
            return;
        }

        RemoveEffect();
    }


    /// <summary>
    /// 强制清空矿工头盔效果。
    /// 
    /// 适合玩家死亡、读档、清空全部被动道具时调用。
    /// </summary>
    public void ForceClearEffect()
    {
        equippedCount = 0;

        RemoveEffect();
    }


    /// <summary>
    /// 真正应用矿工头盔效果。
    /// 只会在装备数量从 0 变成 1 时调用。
    /// </summary>
    private void ApplyEffect()
    {
        hasHeadProtection = true;

        EnableLightEffect();
    }


    /// <summary>
    /// 真正移除矿工头盔效果。
    /// 只会在装备数量从 1 变成 0 时调用。
    /// </summary>
    private void RemoveEffect()
    {
        hasHeadProtection = false;
        DisableLightEffect();
    }



    private void EnableLightEffect()
    {
        DarknessRevealSource darknessRevealSource=GetComponentInParent<DarknessRevealSource>();
        if(darknessRevealSource!=null)
        {
            darknessRevealSource.AddRadius(2);
        }
    }


    private void DisableLightEffect()
    {
        DarknessRevealSource darknessRevealSource = GetComponentInParent<DarknessRevealSource>();
        if (darknessRevealSource != null)
        {
            darknessRevealSource.ReduceRadius(2);
        }
    }
}