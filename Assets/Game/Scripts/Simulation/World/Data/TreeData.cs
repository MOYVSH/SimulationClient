namespace Simulation
{
    /// <summary>
    /// 树实体数据。
    ///
    /// 当前为最小骨架，P1 实体层会补充：
    ///   - State（GrowthState 枚举：Sapling / Mature / Marked / Stump）
    ///   - HP（血量，用于砍伐逻辑）
    ///   - Size（当前大小，影响视觉效果）
    ///   - GrowthTimer（生长计时器）
    ///
    /// 注意区分两个「TypeId」：
    ///   - IWorldEntity.TypeId（本类返回 EntityTypes.Tree）：实体分类，用于仓储索引
    ///   - TreeTypeId（本类字段）：树种配置 ID，指向 TreeTypeData，决定外观/产量
    ///   两者语义不同，不要混用。
    ///
    /// TreeTypeId 当前暂用 treeInstance.prototypeIndex，P1 树种配置完善后改为映射表。
    /// </summary>
    public class TreeData : IWorldEntity
    {
        public int Id { get; set; }
        public GridPos Position { get; set; }
        public int TypeId => EntityTypes.Tree;

        /// <summary>树种配置 ID，引用 TreeTypeData ScriptableObject。</summary>
        public int TreeTypeId;

        public TreeData() { }

        public TreeData(GridPos position, int treeTypeId)
        {
            Position = position;
            TreeTypeId = treeTypeId;
        }
    }
}
