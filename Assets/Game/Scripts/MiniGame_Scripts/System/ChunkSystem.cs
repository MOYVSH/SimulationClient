using System;
using System.Collections.Generic;
using MOYV.RunTime.Game.Core;
using MOYV.RunTime.Game.Tool;
using QFramework;
using Simulation;
using UnityEngine;

/// <summary>
/// Chunk 管理系统：QFramework System，管理 3x3 Chunk 激活窗口。
///
/// 生命周期（参考 ActorSystem）：
///   OnInit()                  — System 注册时，初始化路由
///   AfterSceneInit()          — 进入地图后，订阅 UpdateEvent，获取相机引用
///   OnUpdate(UpdateEvent)     — 每帧检测焦点变化，触发 Chunk 激活/停用
///   ClearDataAfterChangeLevel() — 离开地图时，清理状态
///
/// 激活窗口逻辑：
///   以相机焦点所在的 Chunk 为中心，激活周围 3x3 = 9 个 Chunk。
///   当焦点移动导致中心 Chunk 变化时：
///   1. 计算新的 3x3 窗口
///   2. 新窗口中不在旧窗口的 Chunk → 触发 ChunkActivated
///   3. 旧窗口中不在新窗口的 Chunk → 触发 ChunkDeactivated
///
/// 访问方式：
///   var chunkSystem = this.GetSystem&lt;ChunkSystem&gt;();
/// </summary>
public class ChunkSystem : AbstractSystem
{
    // ---- 配置 ----

    /// <summary>激活窗口半径（radius=1 表示 3x3 窗口）。</summary>
    public int ActiveRadius => 1;

    /// <summary>Chunk 边长（格数）。</summary>
    public int ChunkSize => CoordinateUtility.ChunkSize;

    // ---- 事件 ----

    /// <summary>Chunk 被激活时触发，参数为 ChunkPos。</summary>
    public event Action<ChunkPos> ChunkActivated;

    /// <summary>Chunk 被停用时触发，参数为 ChunkPos。</summary>
    public event Action<ChunkPos> ChunkDeactivated;

    // ---- 内部状态 ----

    /// <summary>当前激活的 Chunk 集合。</summary>
    private readonly HashSet<ChunkPos> _activeChunks = new HashSet<ChunkPos>();

    /// <summary>上一帧的中心 Chunk，用于检测变化。</summary>
    private ChunkPos _lastCenterChunk = new ChunkPos(int.MinValue, int.MinValue);

    /// <summary>临时集合，用于计算新旧窗口的差集。</summary>
    private readonly HashSet<ChunkPos> _newWindow = new HashSet<ChunkPos>();

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

    // ---- 相机引用 ----

    private IsometricCameraController _camera;

    // ---- QFramework 生命周期 ----

    protected override void OnInit()
    {
        RegisterRoutes();
    }

    /// <summary>进入地图场景后调用：订阅 UpdateEvent，获取相机引用。</summary>
    public void AfterSceneInit()
    {
        eventRoute = CPool.Pop<EventRouterQF>();
        eventRoute.Register<UpdateEvent>(UpdateEvent.eventID, OnUpdate);

        // 获取场景中的相机控制器
        _camera = UnityEngine.Object.FindFirstObjectByType<IsometricCameraController>();
        if (_camera == null)
        {
            Debug.LogError("[ChunkSystem] 未找到 IsometricCameraController，Chunk 系统无法工作");
        }
    }

    /// <summary>每帧调用：检测相机焦点变化，更新激活窗口。</summary>
    public void OnUpdate(UpdateEvent e)
    {
        if (_camera == null) return;

        // 获取相机焦点的世界坐标
        Vector3 focusWorld = _camera.FocusPosition;
        WorldPos worldPos = new WorldPos(focusWorld.x, focusWorld.y, focusWorld.z);

        // 转换为 Chunk 坐标
        ChunkPos centerChunk = CoordinateUtility.WorldToChunk(worldPos);

        // 检查中心 Chunk 是否变化
        if (centerChunk == _lastCenterChunk)
            return; // 无变化，跳过

        // 中心 Chunk 变化，重新计算激活窗口
        UpdateActiveWindow(centerChunk);
        _lastCenterChunk = centerChunk;
    }

    /// <summary>离开地图时调用：清理所有状态。</summary>
    public void ClearDataAfterChangeLevel()
    {
        // 取消事件订阅
        routeService.OnReset();

        // 触发所有 Chunk 的停用事件
        foreach (var cp in _activeChunks)
        {
            ChunkDeactivated?.Invoke(cp);
        }

        // 清理内部状态
        _activeChunks.Clear();
        _newWindow.Clear();
        _lastCenterChunk = new ChunkPos(int.MinValue, int.MinValue);
        _camera = null;
    }

    // ---- 核心逻辑：更新激活窗口 ----

    private void UpdateActiveWindow(ChunkPos center)
    {
        // 计算新的 3x3 窗口
        _newWindow.Clear();
        int r = ActiveRadius;
        for (int dz = -r; dz <= r; dz++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                _newWindow.Add(new ChunkPos(center.X + dx, center.Z + dz));
            }
        }

        // 找出新增的 Chunk（在新窗口中，不在旧集合中）
        foreach (var cp in _newWindow)
        {
            if (!_activeChunks.Contains(cp))
            {
                _activeChunks.Add(cp);
                ChunkActivated?.Invoke(cp);
            }
        }

        // 找出移除的 Chunk（在旧集合中，不在新窗口中）
        // 注意：遍历 HashSet 时不能修改，所以先收集再处理
        var toRemove = new List<ChunkPos>();
        foreach (var cp in _activeChunks)
        {
            if (!_newWindow.Contains(cp))
            {
                toRemove.Add(cp);
            }
        }

        foreach (var cp in toRemove)
        {
            _activeChunks.Remove(cp);
            ChunkDeactivated?.Invoke(cp);
        }
    }

    private void RegisterRoutes()
    {
        eventRoute = routeService.Add<EventRouterQF>();
    }

    // ---- 查询接口 ----

    /// <summary>检查指定 Chunk 是否处于激活状态。</summary>
    public bool IsChunkActive(ChunkPos pos)
    {
        return _activeChunks.Contains(pos);
    }

    /// <summary>根据格子坐标获取所属 Chunk 索引。</summary>
    public ChunkPos GetChunkForCell(GridPos pos)
    {
        return CoordinateUtility.GridToChunk(pos);
    }

    /// <summary>遍历指定 Chunk 内的所有格子坐标。</summary>
    public IEnumerable<GridPos> GetCellsInChunk(ChunkPos pos)
    {
        return new Chunk(pos).GetCells();
    }

    /// <summary>获取当前激活的 Chunk 数量（用于调试）。</summary>
    public int ActiveChunkCount => _activeChunks.Count;
}
