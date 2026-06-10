public interface IIgnitable
{
    /// <summary>
    /// 被火源点燃。
    /// 具体的燃烧逻辑由实现该接口的物体自行处理。
    /// </summary>
    void Ignite();
}