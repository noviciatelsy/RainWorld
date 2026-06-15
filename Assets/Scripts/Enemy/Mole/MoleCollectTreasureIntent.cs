using UnityEngine;

/// <summary>
/// 鼹鼠收集宝物意图：移动到目标 PickableObject 并兑换为鼹鼠护符。
/// </summary>
public struct MoleCollectTreasureIntent : IIntent
{
    public PickableObject targetPickable;
}
