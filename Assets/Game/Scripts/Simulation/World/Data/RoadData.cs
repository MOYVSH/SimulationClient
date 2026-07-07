namespace Simulation
{
    /// <summary>
    /// 道路实体数据。
    ///
    /// 当前为最小骨架，Phase 4（道路系统）会补充：
    ///   - RoadState（Planned / Built）
    ///   - ConnectedRoadIds（相邻道路 ID，用于寻路图更新）
    ///
    /// 道路与建筑的区别：
    ///   道路只占 1 格（与树相同），但道路格子仍然"可行走"。
    ///   即：HasRoad | Walkable，但不设 Occupied（工人/单位可以走在路上）。
    /// </summary>
    public class RoadData : IWorldEntity
    {
        public int Id { get; set; }
        public GridPos Position { get; set; }
        public EntityType EntityType => EntityType.Road;

        /// <summary>道路类型 ID，引用 RoadTypeData ScriptableObject。</summary>
        public int RoadTypeId;

        public RoadData() { }

        public RoadData(GridPos position, int roadTypeId)
        {
            Position = position;
            RoadTypeId = roadTypeId;
        }
    }
}
