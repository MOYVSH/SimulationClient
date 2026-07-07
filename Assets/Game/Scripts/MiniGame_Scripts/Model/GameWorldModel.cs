using QFramework;
using Simulation;
using UnityEngine;

/// <summary>
/// 游戏世界模型：QFramework Model，持有 WorldData 和 WorldGrid。
///
/// 生命周期：
///   - 进入地图场景时：调用 InitWorldData(terrain) 创建实例
///   - 离开地图场景时：调用 ClearWorldData() 销毁实例释放内存
///
/// 访问方式：
///   下游系统通过 QFramework 架构访问：
///   var model = this.GetModel&lt;GameWorldModel&gt;();
///   var worldData = model.WorldData;
///   var worldGrid = model.WorldGrid;
///
/// 注意：Model 的注册由用户自行处理，此处只定义 Model 本身。
/// </summary>
public class GameWorldModel : AbstractModel
{
    /// <summary>世界权威数据层：存储所有实体的完整数据。</summary>
    public WorldData WorldData { get; private set; }

    /// <summary>世界空间数据库：存储每个格子的状态信息。</summary>
    public WorldGrid WorldGrid { get; private set; }

    protected override void OnInit()
    {
        // QFramework Model 初始化回调，目前无需额外操作
        // WorldData/WorldGrid 在进入地图时通过 InitWorldData 创建
    }

    /// <summary>
    /// 进入地图时调用：创建 WorldData 和 WorldGrid 实例。
    /// </summary>
    /// <param name="terrain">Unity Terrain 组件，用于计算世界边界。</param>
    public void InitWorldData(Terrain terrain)
    {
        // 如果已有实例，先清理（防止重复初始化）
        ClearWorldData();

        WorldData = new WorldData();
        WorldGrid = new WorldGrid(terrain);
    }

    /// <summary>
    /// 离开地图时调用：销毁实例，释放内存。
    /// </summary>
    public void ClearWorldData()
    {
        WorldData?.Clear();
        WorldData = null;
        WorldGrid = null;
    }
}
