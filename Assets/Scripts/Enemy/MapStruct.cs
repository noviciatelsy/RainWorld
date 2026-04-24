using UnityEngine;

public struct Edge
{
    public Vector2 a;   // 起点（世界坐标）
    public Vector2 b;   // 终点（世界坐标）

    public int loopId;  // 属于哪个封闭轮廓（关键）

    public Vector2 Dir => (b - a).normalized; // 方向向量
}

public struct Node
{
    public Vector2Int cell;
    public Vector2 worldPos;
}