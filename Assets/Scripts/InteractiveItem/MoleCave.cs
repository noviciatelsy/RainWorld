using System.Collections.Generic;
using UnityEngine;

public class MoleCave : MonoBehaviour
{
    [Header("���������")]
    [Tooltip("�ö�Ѩ��Ͻ������ Idle ������߳����η�Χ")]
    public Bounds activityBounds;

    [Header("ͼ�ṹ����ͨ�Ķ�Ѩ")]
    [Tooltip("�뵱ǰ��Ѩ��ͨ��������Ѩ�б�������ͼ�ıߣ�")]
    public List<MoleCave> connectedCaves = new List<MoleCave>();

    public Vector2 Position => transform.position;

    private void OnEnable()
    {
        if (MoleCaveManager.Instance != null)
        {
            MoleCaveManager.Instance.RegisterCave(this);
        }
    }

    private void Awake()
    {
        if (MoleCaveManager.Instance != null)
        {
            MoleCaveManager.Instance.RegisterCave(this);
        }
    }

    private void OnDisable()
    {
        if (MoleCaveManager.Instance != null)
        {
            MoleCaveManager.Instance.UnregisterCave(this);
        }
    }

    private void OnDestroy()
    {
        if (MoleCaveManager.Instance != null)
        {
            MoleCaveManager.Instance.UnregisterCave(this);
        }
    }

    /// <summary>
    /// �� Inspector ���ֶ�����˫�����ӵĸ�������
    /// </summary>
    public void AddConnection(MoleCave other)
    {
        if (other == null || other == this) return;

        if (!connectedCaves.Contains(other))
            connectedCaves.Add(other);

        if (!other.connectedCaves.Contains(this))
            other.connectedCaves.Add(this);
    }

    // ==========================================
    // �༭�����ӻ� (Gizmos)
    // ==========================================
    private void OnDrawGizmos()
    {
        // 1. ���ƻ��Χ������ (��ɫ)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(activityBounds.center, activityBounds.size);

        // 2. ���ƶ�Ѩ���ĵ� (��ɫ��)
        Gizmos.color = new Color(0.6f, 0.2f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.3f);

        // 3. ��������ͼ����ͨ�� (��ɫ)
        if (connectedCaves == null) return;
        Gizmos.color = Color.cyan;
        foreach (var neighbor in connectedCaves)
        {
            if (neighbor != null)
            {
                // ���� ID С�ڶԷ�ʱ���ƣ�����˫�����ظ����Ƶ�����ɫ����
                if (this.GetInstanceID() < neighbor.GetInstanceID())
                {
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
                }
            }
        }
    }
}