using UnityEngine;

/// <summary>
/// 鼹鼠偷取结束后的处理接口。
/// </summary>
public interface IMoleStealHandler
{
    /// <summary>
    /// 偷取阶段结束时调用。
    /// </summary>
    /// <returns>是否已受理（例如已安排掉落/飞行动画）</returns>
    bool OnStealFinished(Mole2D mole, InventoryPlayer player);
}
