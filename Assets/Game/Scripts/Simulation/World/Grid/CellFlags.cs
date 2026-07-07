using System;

namespace Simulation
{
    /// <summary>
    /// 格子状态标记，使用位掩码（bitmask）表示。
    ///
    /// 为什么用 [Flags] + 位运算而不是 bool 字段？
    ///
    /// 1. 内存紧凑：
    ///    5 个 bool 字段 = 5 字节（加上对齐填充可能更多）
    ///    1 个 Flags 枚举 = 4 字节（一个 int）
    ///    当世界有 65536 个格子时，节省约 64 KB
    ///
    /// 2. 组合查询高效：
    ///    检查"是否有树或建筑"只需一次位运算：
    ///      (flags & (CellFlags.HasTree | CellFlags.HasBuilding)) != 0
    ///    而 bool 字段需要两次 if 判断
    ///
    /// 3. 批量修改方便：
    ///    添加标记：  flags |= CellFlags.HasTree;
    ///    移除标记：  flags &amp;= ~CellFlags.HasTree;
    ///    检查标记：  (flags &amp; CellFlags.HasTree) != 0
    ///
    /// 位分配：
    ///    bit 0: Walkable    — 地形是否可行走
    ///    bit 1: Occupied    — 是否有实体占据（通用，用于快速判断"是否有东西"）
    ///    bit 2: HasTree     — 是否有树
    ///    bit 3: HasBuilding — 是否有建筑
    ///    bit 4: HasRoad     — 是否有道路
    ///    bit 5-31: 预留，供未来扩展（最多支持 32 种标记）
    /// </summary>
    [Flags]
    public enum CellFlags
    {
        None       = 0,
        Walkable   = 1 << 0,  // 地形可行走（默认状态）
        Occupied   = 1 << 1,  // 格子被实体占据
        HasTree    = 1 << 2,  // 格子上有树
        HasBuilding = 1 << 3, // 格子上有建筑
        HasRoad    = 1 << 4,  // 格子上有道路
    }
}
