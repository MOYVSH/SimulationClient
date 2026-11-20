namespace Simulation
{
    /// <summary>
    /// 世界实体类型 ID 常量（P0 泛化：从 enum 改为开放 int）。
    ///
    /// 为什么不再用枚举？
    ///   enum 是封闭集合：每加一种实体（矿点、工地、落地物品……）都要改枚举定义，
    ///   并连带修改 Cell 的槽位、WorldData 的 Dictionary 和 switch 分支。
    ///   改成开放 int 后，新增类型只需在这里追加一个常量，
    ///   数据内核（Cell / WorldData / WorldGrid）无需任何改动。
    ///
    /// 约定：
    ///   - 0 保留给「无实体」，与 Cell.OccupantId = 0、IdGenerator 从 1 开始一致
    ///   - TypeId 只回答「实体是什么」，不描述「占几格 / 能否通行」
    ///     占地与通行语义由实体自身数据 + CellFlags 决定
    ///   - 常量值一旦进入存档（P6）就不要再改动，存档按 TypeId 反序列化
    ///
    /// 当前玩法：殖民生存建造（P0 定稿）
    ///   Tree / Rock / Building / Item / ConstructionSite 会写入 WorldGrid；
    ///   Worker 是移动实体，只存在于 WorldData，不写网格；
    ///   Road 暂作保留类型（CellFlags.HasRoad 仍在），P4 道路连通性阶段启用。
    ///
    /// 注：文件名沿用 EntityType.cs 以保持 .meta / GUID 稳定，类名改为 EntityTypes。
    /// </summary>
    public static class EntityTypes
    {
        /// <summary>无实体（保留值，不用于创建实体）。</summary>
        public const int None = 0;

        /// <summary>树 / 可砍伐植被（Terrain 导入已跑通）。</summary>
        public const int Tree = 1;

        /// <summary>矿点 / 岩石（可采集）。</summary>
        public const int Rock = 2;

        /// <summary>多格建筑（占用 SizeX × SizeZ，所有格存同一 OccupantId）。</summary>
        public const int Building = 3;

        /// <summary>工人（移动实体，只存在于 WorldData，不写 WorldGrid）。</summary>
        public const int Worker = 4;

        /// <summary>落地物品（P3 起；初期只进库存、不占格）。</summary>
        public const int Item = 5;

        /// <summary>建造工地（P2 Job 系统的中间态，建成后替换为 Building）。</summary>
        public const int ConstructionSite = 6;

        /// <summary>道路（单格、可行走、不设 Occupied；P4 连通性阶段启用）。</summary>
        public const int Road = 7;

        // 后续新增类型在这里追加即可，无需改动 Cell / WorldData / WorldGrid。
    }
}
