using UnityEngine;

public interface IMosquitoCoilRepellable
{
    /// <summary>
    /// 被蚊香驱赶。
    /// </summary>
    /// <param name="myCoilPosition">
    /// 蚊香当前所在的世界坐标。
    /// 怪物可以根据这个位置计算逃离方向。
    /// </param>
    void RepelByMosquitoCoil(Vector2 myCoilPosition);
}