namespace Simulation
{
    /// <summary>
    /// 世界实体类型枚举。
    /// 用于 WorldGrid 的空间查询：根据类型从 Cell 中取出对应的实体 ID。
    /// </summary>
    public enum EntityType
    {
        Tree,
        Building,
        Road,
        Worker,
    }
}
