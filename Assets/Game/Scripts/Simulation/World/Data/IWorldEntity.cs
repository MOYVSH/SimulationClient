namespace Simulation
{
    /// <summary>
    /// 世界实体接口：定义所有实体（树、建筑、道路、工人）的公共属性。
    ///
    /// 为什么需要这个接口？
    ///   WorldData 需要对所有实体类型提供统一的 CRUD 操作。
    ///   通过接口，可以用泛型方法减少重复代码：
    ///     public int Create&lt;T&gt;(T entity) where T : IWorldEntity
    ///
    /// 为什么用 class 而不是 struct？
    ///   1. 实体数据较大（TreeData 含位置、状态、HP、类型等，约 40-80 字节）
    ///   2. 实体需要引用语义：修改字典中的实体应该直接反映，无需 Get → 修改 → Set
    ///   3. 实体可能持有对其他实体的引用（如工人引用目标建筑）
    ///
    /// ID 的约定：
    ///   - 由 IdGenerator 分配，创建后不可修改
    ///   - ID = 0 表示"无实体"（Cell 中的默认值）
    ///   - ID 在当前世界内唯一，场景切换后重新从 1 开始
    /// </summary>
    public interface IWorldEntity
    {
        /// <summary>实体唯一标识，由 IdGenerator 分配。</summary>
        int Id { get; set; }

        /// <summary>实体在世界中的格子坐标。</summary>
        GridPos Position { get; set; }

        /// <summary>实体类型（Tree / Building / Road / Worker）。</summary>
        EntityType EntityType { get; }
    }
}
