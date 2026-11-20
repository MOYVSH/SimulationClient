namespace Simulation
{
    /// <summary>
    /// 道路实体数据。
    ///
    /// 当前为最小骨架，P4 会补充：
    ///   - RoadState（Planned / Built）
    ///   - ConnectedRoadIds（相邻道路 ID，用于寻路图更新）
    ///
    /// 道路与建筑的区别：
    ///   道路只占 1 格（与树相同），但道路格子仍然「可行走」。
    ///   即：HasRoad | Walkable，但不设 Occupied（工人/单位可以走在路上）。
    ///   写入格子时用 cell.SetOccupant(id, EntityTypes.Road, CellFlags.HasRoad | CellFlags.Walkable)。
    ///
    /// 注意：RoadTypeId 是道路类型配置 ID，与 IWorldEntity.TypeId（实体分类）不是一回事。
    /// </summary>
    public class RoadData : IWorldEntity
    {
        public int Id { get; set; }
        public GridPos Position { get; set; }
        public int TypeId => EntityTypes.Road;

        /// <summary>道路类型配置 ID，引用 RoadTypeData ScriptableObject。</summary>
        public int RoadTypeId;

        public RoadData() { }

        public RoadData(GridPos position, int roadTypeId)
        {
            Position = position;
            RoadTypeId = roadTypeId;
        }
    }
}
