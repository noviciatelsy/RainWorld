using UnityEngine;

public interface IToyCarAttractable
{
    /// <summary>
    /// 被玩具车吸引。
    /// </summary>
    /// <param name="myToyCarPosition">
    /// 当前玩具车所在的世界坐标。
    /// </param>
    void AttractToToyCar(Vector2 myToyCarPosition);
}