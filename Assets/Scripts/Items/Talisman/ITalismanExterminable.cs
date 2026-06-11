using UnityEngine;

public interface ITalismanExterminable
{
    /// <summary>
    /// 被符纸消灭。
    /// </summary>
    /// <param name="myTalismanPosition">
    /// 符纸触发消灭效果时的世界坐标。
    /// </param>
    void ExterminateByTalisman(Vector2 myTalismanPosition);
}