using System.Collections.Generic;
using UnityEngine;

public class MoleCaveManager : MonoBehaviour
{
    public static MoleCaveManager Instance { get; private set; }

    private List<MoleCave> allCaves = new List<MoleCave>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterCave(MoleCave cave)
    {
        if (!allCaves.Contains(cave))
        {
            allCaves.Add(cave);
        }
    }

    public void UnregisterCave(MoleCave cave)
    {
        if (allCaves.Contains(cave))
        {
            allCaves.Remove(cave);
        }
    }

    /// <summary>
    /// 寻找距离指定位置最近，且在图结构中拥有至少一个连通出口的有效洞穴
    /// </summary>
    /// <param name="searchPos">鼹鼠当前的位置</param>
    /// <returns>找到的有效洞穴，若无则返回 null</returns>
    public MoleCave FindClosestValidCave(Vector2 searchPos)
    {
        MoleCave bestCave = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < allCaves.Count; i++)
        {
            MoleCave cave = allCaves[i];

            // 补充2核心：必须保证这个 cave 拥有可到达的连通关系，否则跳过
            if (cave.connectedCaves == null || cave.connectedCaves.Count == 0)
                continue;

            float dist = Vector2.Distance(searchPos, cave.Position);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestCave = cave;
            }
        }

        return bestCave;
    }

    /// <summary>
    /// 获取与指定洞穴相连通的所有目标洞穴列表（图的邻接节点查询）
    /// </summary>
    public List<MoleCave> GetLinkedCaves(MoleCave srcCave)
    {
        if (srcCave == null) return null;
        return srcCave.connectedCaves;
    }

    /// <summary>
    /// 供外部动态构建无向图边的 API
    /// </summary>
    public void ConnectTwoCaves(MoleCave a, MoleCave b)
    {
        if (a != null && b != null)
        {
            a.AddConnection(b);
        }
    }
}