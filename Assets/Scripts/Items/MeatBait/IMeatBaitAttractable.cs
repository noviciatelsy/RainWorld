using UnityEngine;

public interface IMeatBaitAttractable
{
    /// <summary>
    /// 被肉饵吸引。
    /// </summary>
    /// <param name="myMeatBaitPosition">
    /// 当前肉饵所在的世界坐标。
    /// </param>
    void AttractToMeatBait(Vector2 myMeatBaitPosition);
}