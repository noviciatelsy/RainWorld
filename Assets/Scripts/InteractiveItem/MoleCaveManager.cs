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
    public MoleCave FindClosestValidCave(Vector2 searchPos)
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
    /// ��Ҫ����ͨ��ϵ�������ڳ����㶵�ס�
    /// </summary>
    public MoleCave FindClosestCave(Vector2 searchPos)
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

            float dist = Vector2.Distance(searchPos, cave.Position);

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

    public void ConnectTwoCaves(MoleCave a, MoleCave b)
    {
        if (a != null && b != null)
        {
            a.AddConnection(b);
        }
    }
}
