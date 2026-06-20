using System.Collections.Generic;
using UnityEngine;

public class MoleCaveManager : MonoBehaviour
{
    public static MoleCaveManager Instance { get; private set; }

    private List<MoleCave> allCaves = new List<MoleCave>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RefreshAllCaves();
    }

    /// <summary>
    /// ɨ�賡�������ж�Ѩ��ע�ᣨ���� Awake ˳����©ע�ᣩ��
    /// </summary>
    public void RefreshAllCaves()
    {
        allCaves.Clear();

        MoleCave[] caves = Object.FindObjectsOfType<MoleCave>(true);

        for (int i = 0; i < caves.Length; i++)
        {
            RegisterCave(caves[i]);
        }
    }

    public static bool CaveHasConnections(MoleCave cave)
    {
        if (cave == null || cave.connectedCaves == null)
        {
            return false;
        }

        for (int i = 0; i < cave.connectedCaves.Count; i++)
        {
            if (cave.connectedCaves[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    public void RegisterCave(MoleCave cave)
    {
        if (cave != null && !allCaves.Contains(cave))
        {
            allCaves.Add(cave);
        }
    }

    public void UnregisterCave(MoleCave cave)
    {
        if (cave != null && allCaves.Contains(cave))
        {
            allCaves.Remove(cave);
        }
    }

    /// <summary>
    /// Ѱ�Ҿ���ָ��λ�����������ͼ�ṹ��ӵ������һ����ͨ���ڵ���Ч��Ѩ��
    /// </summary>
    public MoleCave FindClosestValidCave(Vector2 searchPos, float feetYOffset = RobotGroundPath.DefaultFeetYOffset)
    {
        MoleCave bestCave = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < allCaves.Count; i++)
        {
            MoleCave cave = allCaves[i];

            if (!CaveHasConnections(cave))
            {
                continue;
            }

            float dist = Vector2.Distance(searchPos, cave.GetMoleFeetPosition(feetYOffset));

            if (dist < minDistance)
            {
                minDistance = dist;
                bestCave = cave;
            }
        }

        return bestCave;
    }

    /// <summary>
    /// 不要求连通关系，用于在场景里兜底找洞。
    /// </summary>
    public MoleCave FindClosestCave(Vector2 searchPos, float feetYOffset = RobotGroundPath.DefaultFeetYOffset)
    {
        MoleCave bestCave = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < allCaves.Count; i++)
        {
            MoleCave cave = allCaves[i];

            if (cave == null)
            {
                continue;
            }

            float dist = Vector2.Distance(searchPos, cave.GetMoleFeetPosition(feetYOffset));

            if (dist < minDistance)
            {
                minDistance = dist;
                bestCave = cave;
            }
        }

        return bestCave;
    }

    public List<MoleCave> GetLinkedCaves(MoleCave srcCave)
    {
        if (srcCave == null)
        {
            return null;
        }

        return srcCave.connectedCaves;
    }

    /// <summary>
    /// 从源洞窟的相邻连通列表中随机选一个（不含自身）。
    /// 若未手动配置 connectedCaves，则回退为场景中任意其他鼹鼠洞。
    /// </summary>
    public MoleCave GetRandomAdjacentCave(MoleCave sourceCave)
    {
        if (sourceCave == null)
        {
            return null;
        }

        List<MoleCave> validCaves = CollectValidDestinations(sourceCave, sourceCave.connectedCaves);
        if (validCaves.Count > 0)
        {
            return validCaves[Random.Range(0, validCaves.Count)];
        }

        List<MoleCave> fallbackCaves = new List<MoleCave>();
        for (int i = 0; i < allCaves.Count; i++)
        {
            MoleCave cave = allCaves[i];
            if (cave != null && cave != sourceCave)
            {
                fallbackCaves.Add(cave);
            }
        }

        if (fallbackCaves.Count == 0)
        {
            return null;
        }

        return fallbackCaves[Random.Range(0, fallbackCaves.Count)];
    }

    private static List<MoleCave> CollectValidDestinations(MoleCave sourceCave, List<MoleCave> candidates)
    {
        List<MoleCave> validCaves = new List<MoleCave>();
        if (candidates == null || candidates.Count == 0)
        {
            return validCaves;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            MoleCave cave = candidates[i];
            if (cave != null && cave != sourceCave)
            {
                validCaves.Add(cave);
            }
        }

        return validCaves;
    }

    public void ConnectTwoCaves(MoleCave a, MoleCave b)
    {
        if (a != null && b != null)
        {
            a.AddConnection(b);
        }
    }
}
