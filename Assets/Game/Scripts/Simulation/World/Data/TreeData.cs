namespace Simulation
{
    /// <summary>
    /// 树实体数据。
    ///
    /// 当前为最小骨架，Phase 2（树木系统）会补充：
    ///   - State（GrowthState 枚举：Sapling / Mature / Marked / Stump）
    ///   - HP（血量，用于砍伐逻辑）
    ///   - Size（当前大小，影响视觉效果）
    ///   - GrowthTimer（生长计时器）
    ///
    /// TreeTypeId 的用途：
    ///   引用 ScriptableObject 中的树种配置（TreeTypeData），
    ///   包含树种名称、生长速度、木材产量、预制体路径等。
    ///   这里只存 ID，具体数据通过 TreeTypeDatabase 查询。
    /// </summary>
    public class TreeData : IWorldEntity
    {
        public int Id { get; set; }
        public GridPos Position { get; set; }
        public EntityType EntityType => EntityType.Tree;

        /// <summary>树种类型 ID，引用 TreeTypeData ScriptableObject。</summary>
        public int TreeTypeId;

        public TreeData() { }

        public TreeData(GridPos position, int treeTypeId)
        {
            Position = position;
            TreeTypeId = treeTypeId;
        }
    }
}
