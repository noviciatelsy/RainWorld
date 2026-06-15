/// <summary>
/// 电梯 UI 打开或移动中时，阻止 PlayerSensor 等系统抢占 E 键。
/// </summary>
public static class ElevatorInputGate
{
    public static bool IsBlocking { get; private set; }

    public static void SetBlocking(bool blocking)
    {
        IsBlocking = blocking;
    }
}
