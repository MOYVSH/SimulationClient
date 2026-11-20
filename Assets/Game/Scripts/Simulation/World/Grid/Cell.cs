namespace Simulation
{
    /// <summary>
    /// 世界格子的状态数据（P0 泛化：单占用模型）。
    ///
    /// 内存布局（16 字节，与泛化前一致）：
    ///   int OccupantId (4 bytes) — 格子上的主实体 ID，0 表示无实体
    ///   int TypeId     (4 bytes) — 主实体的开放类型 ID，见 EntityTypes
    ///   CellFlags Flags(4 bytes) — 位掩码状态标记
    ///   int Reserved   (4 bytes) — 预留：未来「叠加层」（如地上堆料）用
    ///
    /// 为什么从「TreeId / BuildingId / RoadId 三槽」改成「OccupantId + TypeId」？
    ///   三槽把实体种类写死在数据结构里：新增矿点、工地、落地物品都要改 Cell，
    ///   且 WorldData 必须同步加一张 Dictionary。泛化后格子只回答两个问题：
    ///     「谁占着」(OccupantId) + 「它是什么」(TypeId)
    ///   新增实体类型不需要动数据内核。
    ///
    /// 语义变更（重要）：
    ///   泛化前允许一格同时记录树 / 建筑 / 道路三个 ID；
    ///   P0 起改为「一格一个主实体」。这符合殖民建造玩法的实际约束：
    ///   树砍掉才能建、道路与建筑不共格、物品初期只进库存不占格。
    ///   若未来确实需要叠加（道路上放装饰 / 地上堆料），
    ///   应在 WorldGrid 之外另加一层 overlay 索引，而不是退回多槽位。
    ///
    /// 多格建筑：
    ///   与旧 BuildingId 的做法一致——建筑自己缓存 OccupiedCells，
    ///   占地范围内每个格子都存同一个 OccupantId 和 TypeId = EntityTypes.Building，
    ///   由调用方逐格设置 HasBuilding | Occupied。
    ///
    /// 默认值（default(Cell)）的语义：
    ///   OccupantId = 0、TypeId = None(0)、Flags = None（不可行走、无占据）
    ///   这是 WorldGrid 对「未设置过的格子」或「边界外格子」的返回约定。
    ///
    /// 关于 Flags 里的 HasTree / HasBuilding / HasRoad：
    ///   TypeId 是分类的权威来源，这三个位是「派生缓存」，用于一次位运算快速筛选，
    ///   也直接对应设计侧的状态色板。写入 OccupantId 时必须同步维护它们
    ///   （推荐用 SetOccupant，避免漏设 TypeId）。
    /// </summary>
    public struct Cell
    {
        /// <summary>该格子上的主实体 ID。0 表示无实体。</summary>
        public int OccupantId;

        /// <summary>主实体的开放类型 ID，见 EntityTypes。</summary>
        public int TypeId;

        /// <summary>格子状态位掩码。</summary>
        public CellFlags Flags;

        /// <summary>预留 4 字节：保持 16 字节布局，供未来叠加层索引使用。</summary>
        public int Reserved;

        /// <summary>该格子是否有主实体（比 IsOccupied 更直接：不依赖 Flags 是否被正确维护）。</summary>
        public bool HasOccupant => OccupantId != 0;

        // ---- 标记便捷方法 ----
        // 这些方法封装了位运算，让调用方代码更清晰：
        //   原始写法：cell.Flags |= CellFlags.HasTree;
        //   封装后：  cell.SetFlag(CellFlags.HasTree);

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

        /// <summary>
        /// 获取指定类型在该格子上的实体 ID；类型不匹配或无实体返回 0。
        /// P0 起不再按具体实体种类分支，只比较 TypeId。
        /// </summary>
        public int GetEntityId(int typeId)
        {
            return OccupantId != 0 && TypeId == typeId ? OccupantId : 0;
        }

        // ---- 占用写入 ----

        /// <summary>
        /// 一次性写入主实体并同步派生标记位，避免「只设 ID 漏设 TypeId」。
        /// extraFlags 由调用方按语义传入，例如：
        ///   树/建筑：CellFlags.HasTree | CellFlags.Occupied
        ///   道路：  CellFlags.HasRoad | CellFlags.Walkable（道路不设 Occupied）
        /// </summary>
        public void SetOccupant(int id, int typeId, CellFlags extraFlags)
        {
            OccupantId = id;
            TypeId = typeId;
            SetFlag(extraFlags);
        }

        /// <summary>
        /// 清空主实体与分类信息；Flags 由调用方按需要自行清理。
        /// 注意：若要把格子从一种实体「替换」成另一种（如砍树后建楼、工地建成），
        /// 必须同时 ClearFlag 掉旧的 HasTree / HasBuilding / HasRoad 位，
        /// 否则派生标记会与新的 TypeId 不一致（SetOccupant 只做 OR，不会清旧位）。
        /// </summary>
        public void ClearOccupant()
        {
            OccupantId = 0;
            TypeId = EntityTypes.None;
        }
    }
}
