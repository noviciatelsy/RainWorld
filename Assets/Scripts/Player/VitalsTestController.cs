using UnityEngine;

public class VitalsTestController : MonoBehaviour
{
    [Header("绑定玩家生命组件")]
    [SerializeField]
    private PlayerVitals playerVitals;

    [Header("测试伤害量")]
    [SerializeField]
    private int testDamage = 10;

    private void Update()
    {
        // 检测键盘大键盘区或小键盘区的 "1" 键
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            if (playerVitals != null)
            {
                // 执行扣血
                playerVitals.ReduceHealth(testDamage);

                // 在控制台打印当前状态方便观察
                Debug.Log($"【测试伤害】玩家受到 {testDamage} 点伤害！" +
                          $"当前血量: {playerVitals.CurrentHealth}/{playerVitals.CurrentMaxHealth} " +
                          $"(当前饥饿度: {playerVitals.CurrentHunger})");
            }
            else
            {
                Debug.LogWarning("【测试警告】未绑定 PlayerVitals 组件，请在 Inspector 中拖拽赋值！");
            }
        }
    }
}