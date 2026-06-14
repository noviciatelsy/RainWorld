using UnityEngine;

public class Mole2D : MonsterBase
{
    [Header("������������")]
    public float moveSpeed = 2.5f;
    public float playerCheckRadius = 5f;
    public LayerMask playerLayer;

    [Header("��ǰ״̬���ݣ��� AI �� Motor ά����")]
    public int idleArrivalCount = 0;
    public float stealTimer = 0f;
    public MoleCave currentHomeCave;

    protected override void Init()
    {
        ai = new MoleUtilityAI(this);
        motor = new MoleMotor(this);

        ResolveHomeCave();

        idleArrivalCount = 0;
        stealTimer = 0f;
    }

    private void ResolveHomeCave()
    {
        MoleCaveManager manager = MoleCaveManager.Instance;

        if (manager == null)
        {
            manager = Object.FindObjectOfType<MoleCaveManager>();
        }

        if (manager != null)
        {
            manager.RefreshAllCaves();
        }

        if (currentHomeCave != null)
        {
            transform.position = currentHomeCave.Position;
            return;
        }

        if (manager == null)
        {
            Debug.LogWarning("Mole2D: ������δ�ҵ� MoleCaveManager��");
            return;
        }

        currentHomeCave = manager.FindClosestValidCave(Position);

        if (currentHomeCave == null)
        {
            currentHomeCave = manager.FindClosestCave(Position);
        }

        if (currentHomeCave != null)
        {
            transform.position = currentHomeCave.Position;

            if (!MoleCaveManager.CaveHasConnections(currentHomeCave))
            {
                Debug.LogWarning(
                    $"Mole2D: �Ѱ󶨶�Ѩ��{currentHomeCave.name}�������� connectedCaves Ϊ�ա�"
                    + "���� Inspector ��Ϊ�ö�Ѩָ������һ����ͨ��Ѩ��",
                    currentHomeCave
                );
            }

            return;
        }

        Debug.LogWarning("������δ�ҵ��κ� MoleCave������ô� MoleCave ����Ķ�Ѩ��");
    }
}
