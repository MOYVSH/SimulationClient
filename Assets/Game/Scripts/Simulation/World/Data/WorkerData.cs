namespace Simulation
{
    /// <summary>
    /// 工人实体数据。
    ///
    /// 当前为最小骨架，P2（Job 系统）会补充：
    ///   - WorkerState（Idle / Walking / Working / Returning）
    ///   - CurrentJobId（当前任务 ID）
    ///   - TargetPosition（目标位置）
    ///   - Inventory（携带的资源）
    ///   - Speed（移动速度）
    ///
    /// 工人的特殊性：
    ///   工人是移动实体，Position 会随时间变化。
    ///   与静态实体（树/建筑/道路）不同，工人不写入 WorldGrid，
    ///   而是由寻路系统动态查询可走路径。
    ///   TypeId = EntityTypes.Worker 只存在于 WorldData 中。
    /// </summary>
    public class WorkerData : IWorldEntity
    {
        public int Id { get; set; }
        public GridPos Position { get; set; }
        public int TypeId => EntityTypes.Worker;

        public WorkerData() { }

        public WorkerData(GridPos position)
        {
            Position = position;
        }
    }
}
