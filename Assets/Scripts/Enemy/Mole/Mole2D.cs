using UnityEngine;

public class Mole2D : MonsterBase
{
    [Header("鼹鼠属性配置")]
    public float moveSpeed = 2.5f;
    public float playerCheckRadius = 5f; // 玩家检测范围
    public LayerMask playerLayer;        // 玩家图层

    [Header("当前状态数据（由 AI 与 Motor 维护）")]
    public int idleArrivalCount = 0;     // Idle 状态到达目标点计数 (0~3)
    public float stealTimer = 0f;        // Steal 状态的 3 秒持续计时器
    public MoleCave currentHomeCave;     // 鼹鼠当前所属/关联的洞穴节点

    protected override void Init()
    {
        // 1. 初始化你的接口实现类
        ai = new MoleUtilityAI(this);
        motor = new MoleMotor(this);

        // 2. 结合全局图管理器，初始化鼹鼠的起始洞穴
        if (MoleCaveManager.Instance != null)
        {
            currentHomeCave = MoleCaveManager.Instance.FindClosestValidCave(Position);
            if (currentHomeCave != null)
            {
                // 初始位置强制对齐到所属洞穴
                transform.position = currentHomeCave.Position;
            }
            else
            {
                Debug.LogWarning("场景中未找到任何配置了连通关系的有效 MoleCave！");
            }
        }

        idleArrivalCount = 0;
        stealTimer = 0f;
    }
}