using MOYV.RunTime.Game.Core;
using MOYV.RunTime.Game.Tool;
using QFramework;
using Simulation;
using UnityEngine;

/// <summary>
/// Terrain 系统：QFramework System，负责初始化和操作 TerrainDataReader。
///
/// 生命周期（参考 ChunkSystem / ActorSystem）：
///   OnInit()                    — System 注册时，初始化路由
///   AfterSceneInit()            — 进入地图后，执行树木同步
///   ClearDataAfterChangeLevel() — 离开地图时，清理状态
///
/// 职责：
///   1. 持有 TerrainDataReader 实例
///   2. 在场景初始化时驱动树木同步（Terrain → WorldData + WorldGrid）
///   3. 后续可扩展：地形高度查询、地形纹理读取等
///
/// 访问方式：
///   var terrainSystem = this.GetSystem<TerrainSystem>();
/// </summary>
public class TerrainSystem : AbstractSystem
{
    // ---- 路由与事件订阅 ----

    protected RouteService _routeService;
    protected RouteService routeService
    {
        get
        {
            if (_routeService == null)
                _routeService = CPool.Pop<RouteService>();
            return _routeService;
        }
    }

    protected EventRouterQF eventRoute;

    // ---- 内部组件 ----

    private TerrainDataReader _terrainDataReader;
    private Terrain _terrain;

    // ---- QFramework 生命周期 ----

    protected override void OnInit()
    {
        RegisterRoutes();
        _terrainDataReader = new TerrainDataReader();
    }

    /// <summary>进入地图场景后调用：执行树木初始同步。</summary>
    public void AfterSceneInit()
    {
        _terrain = Object.FindFirstObjectByType<Terrain>();
        if (_terrain == null)
        {
            Debug.LogError("[TerrainSystem] 未找到 Terrain，树木同步跳过");
            return;
        }

        var model = this.GetModel<GameWorldModel>();
        if (model.WorldData == null || model.WorldGrid == null)
        {
            Debug.LogError("[TerrainSystem] WorldData/WorldGrid 未初始化，请先调用 InitWorldData");
            return;
        }

        int count = _terrainDataReader.ReadTreesFromTerrain(_terrain, model.WorldData, model.WorldGrid);
        Debug.Log($"[TerrainSystem] 树木初始同步完成，共 {count} 棵");
    }

    /// <summary>离开地图时调用：清理状态。</summary>
    public void ClearDataAfterChangeLevel()
    {
        routeService.OnReset();
        _terrain = null;
    }

    // ---- 路由注册 ----

    private void RegisterRoutes()
    {
        eventRoute = routeService.Add<EventRouterQF>();
    }

    // ---- 查询接口 ----

    /// <summary>获取当前场景的 Terrain 引用（可能为 null）。</summary>
    public Terrain CurrentTerrain => _terrain;

    /// <summary>获取 TerrainDataReader 实例。</summary>
    public TerrainDataReader Reader => _terrainDataReader;
}
