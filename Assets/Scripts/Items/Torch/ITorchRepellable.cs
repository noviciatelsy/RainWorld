using UnityEngine;

public interface ITorchRepellable
{
    /// <summary>
    /// 被火把驱赶。
    /// </summary>
    /// <param name="myTorchPosition">
    /// 火把当前所在的世界坐标。
    /// 敌人根据这个位置计算逃跑方向。
    /// </param>
    void FleeFromTorch(Vector2 myTorchPosition);
}