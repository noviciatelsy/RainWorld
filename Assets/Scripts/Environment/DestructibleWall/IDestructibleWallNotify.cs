/// <summary>
/// 可破坏墙壁受击/破坏通知接口。
/// </summary>
public interface IDestructibleWallNotify
{
    void NotifyWallDestroy(bool permanentDestroy = false);
}
