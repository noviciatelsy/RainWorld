/// <summary>
/// 电梯 UI 打开/移动中，或密码门 UI 激活时，阻止其它系统抢占交互键或地图快捷键。
/// </summary>
public static class ElevatorInputGate
{
    private static bool elevatorBlocking;
    private static bool passwordDoorInputBlocking;
    private static bool passwordDoorUiBlocking;

    public static bool IsBlocking => elevatorBlocking || passwordDoorInputBlocking;

    /// <summary>密码门 UI 显示时阻止 M 键打开/关闭地图。</summary>
    public static bool IsMapBlocking => passwordDoorUiBlocking;

    public static void SetBlocking(bool blocking)
    {
        elevatorBlocking = blocking;
    }

    public static void SetPasswordDoorInputBlocking(bool blocking)
    {
        passwordDoorInputBlocking = blocking;
    }

    public static void SetPasswordDoorUiBlocking(bool blocking)
    {
        passwordDoorUiBlocking = blocking;
    }
}
