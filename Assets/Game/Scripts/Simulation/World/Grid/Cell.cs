namespace Simulation
{
    /// <summary>
    /// 世界格子的状态数据。
    ///
    /// 内存布局（16 字节）：
    ///   int TreeId      (4 bytes) — 树上实体 ID，0 表示无树
    ///   int BuildingId  (4 bytes) — 建筑实体 ID，0 表示无建筑
    ///   int RoadId      (4 bytes) — 道路实体 ID，0 表示无道路
    ///   CellFlags Flags (4 bytes) — 位掩码状态标记
    ///
    /// 为什么 ID 用 int 而不是 bool？
    ///   因为一个格子可能先后被不同的树/建筑占据。
    ///   存储 ID 可以：
    ///   1. 通过 WorldData.GetTree(id) 反查实体的完整数据
    ///   2. 支持"替换"操作（先删除旧树，再种新树）
    ///   3. 避免在热路径中做额外的 ID 查找
    ///
    /// 默认值（default(Cell)）的语义：
    ///   所有 ID = 0（无实体），Flags = None（不可行走、无占据）
    ///   这是 WorldGrid 对"未设置过的格子"或"边界外格子"的返回约定
    /// </summary>
    public struct Cell
    {
        /// <summary>该格子上的树实体 ID。0 表示无树。</summary>
        public int TreeId;

        /// <summary>该格子上的建筑实体 ID。0 表示无建筑。</summary>
        public int BuildingId;

        /// <summary>该格子上的道路实体 ID。0 表示无道路。</summary>
        public int RoadId;

        /// <summary>格子状态位掩码。</summary>
        public CellFlags Flags;

        // ---- 标记便捷方法 ----
        // 这些方法封装了位运算，让调用方代码更清晰：
        //   原始写法：cell.Flags |= CellFlags.HasTree;
        //   封装后：  cell.SetFlag(CellFlags.HasTree);
        //   原始写法：cell.Flags &= ~CellFlags.HasTree;
        //   封装后：  cell.ClearFlag(CellFlags.HasTree);

        /// <summary>添加指定的标记位。</summary>
        public void SetFlag(CellFlags flag)
        {
            Flags |= flag;
        }

        /// <summary>移除指定的标记位。</summary>
        public void ClearFlag(CellFlags flag)
        {
            Flags &= ~flag;
        }

        /// <summary>检查是否包含指定的标记位。</summary>
        public bool HasFlag(CellFlags flag)
        {
            return (Flags & flag) == flag;
        }

        // ---- 查询便捷方法 ----

        /// <summary>该格子是否可行走。</summary>
        public bool IsWalkable => HasFlag(CellFlags.Walkable);

        /// <summary>该格子是否被任意实体占据。</summary>
        public bool IsOccupied => HasFlag(CellFlags.Occupied);

        /// <summary>根据实体类型获取该格子上的实体 ID。返回 0 表示该类型无实体。</summary>
        public int GetEntityId(EntityType type)
        {
            switch (type)
            {
                case EntityType.Tree:     return TreeId;
                case EntityType.Building: return BuildingId;
                case EntityType.Road:     return RoadId;
                default:                  return 0;
            }
        }
    }
}
