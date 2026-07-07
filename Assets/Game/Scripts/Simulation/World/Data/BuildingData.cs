namespace Simulation
{
    /// <summary>
    /// 建筑实体数据。
    ///
    /// 当前为最小骨架，Phase 4（建筑系统）会补充：
    ///   - State（BuildingState 枚举：Planned / UnderConstruction / Complete / Demolished）
    ///   - ConstructionProgress（建造进度 0-100）
    ///   - OwnerWorkerId（负责建造的工人 ID）
    ///   - 各种建筑特有的功能数据
    ///
    /// 建筑的特殊性——多格占用：
    ///   与树只占 1 格不同，建筑占用 SizeX × SizeZ 的矩形区域。
    ///   Position 是建筑的"原点"（通常是左下角），WorldGrid 中所有被占用的格子
    ///   都存储同一个 BuildingId。
    ///
    /// 示例：3×2 建筑，Origin=(5,3)，ID=7
    ///   WorldGrid 中 (5,3)(6,3)(7,3)(5,4)(6,4)(7,4) 这 6 个格子的
    ///   BuildingId 都是 7，Flags 都有 HasBuilding | Occupied。
    /// </summary>
    public class BuildingData : IWorldEntity
    {
        public int Id { get; set; }
        public GridPos Position { get; set; }
        public EntityType EntityType => EntityType.Building;

        /// <summary>建筑类型 ID，引用 BuildingTypeData ScriptableObject。</summary>
        public int BuildingTypeId;

        /// <summary>建筑 X 方向尺寸（格数）。</summary>
        public int SizeX = 1;

        /// <summary>建筑 Z 方向尺寸（格数）。</summary>
        public int SizeZ = 1;

        public BuildingData() { }

        public BuildingData(GridPos position, int buildingTypeId, int sizeX = 1, int sizeZ = 1)
        {
            Position = position;
            BuildingTypeId = buildingTypeId;
            SizeX = sizeX;
            SizeZ = sizeZ;
        }
    }
}
