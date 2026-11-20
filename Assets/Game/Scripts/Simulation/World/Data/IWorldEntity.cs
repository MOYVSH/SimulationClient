namespace Simulation
{
    /// <summary>
    /// 世界实体接口（P0 泛化：统一为开放 TypeId）。
    ///
    /// 为什么需要这个接口？
    ///   WorldData 用一张主表管理所有实体，只需依赖这个最小契约：
    ///   Id / Position / TypeId。新增实体类型只要实现本接口，
    ///   无需修改 WorldData、WorldGrid 或 Cell。
    ///
    /// 为什么用 class 而不是 struct？
    ///   1. 实体数据较大（位置、状态、HP、类型等）
    ///   2. 实体需要引用语义：修改仓储中的实体应直接反映，无需 Get → 修改 → Set
    ///   3. 实体可能持有对其他实体的引用（如工人引用目标建筑）
    ///   这也是 WorldData.Get&lt;T&gt; / Modify&lt;T&gt; 要求 T : class 的原因。
    ///
    /// 为什么 TypeId 用 int 而不是枚举？
    ///   见 EntityTypes 的说明：开放类型集合，避免每加一种实体就改数据内核。
    ///
    /// ID 的约定：
    ///   - 由 IdGenerator 分配，创建后不可修改
    ///   - ID = 0 表示「无实体」（Cell.OccupantId 的默认值）
    ///   - ID 在当前世界内唯一，场景切换后重新从 1 开始
    /// </summary>
    public interface IWorldEntity
    {
        /// <summary>实体唯一标识，由 IdGenerator 分配。</summary>
        int Id { get; set; }

        /// <summary>实体在世界中的格子坐标。移动实体（Worker）会随时间变化。</summary>
        GridPos Position { get; set; }

        /// <summary>开放类型 ID，见 EntityTypes。创建后不应变更。</summary>
        int TypeId { get; }
    }
}
