---
title: "Unity 模拟经营项目 — 知识库索引"
phase: "meta"
owner: "海豹"
status: "planned"
dependencies: []
keywords: ["索引", "知识库", "RAG", "MCP", "协作"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Unity 模拟经营项目 — 知识库索引

本文件是 `specs/` 目录下所有文档的机器可读索引，便于 MCP、RAG 系统或团队成员快速定位信息。

## 快速入口

| 你需要了解 | 阅读文件 |
|------------|----------|
| 项目整体目标、架构、全局约定 | [00-overview.md](00-overview.md) |
| 我现在该做什么、开发顺序 | [README.md](README.md) |
| Phase 0：模拟经营目录初始化、相机、输入 | [01-phase-0-setup.md](01-phase-0-setup.md) |
| Phase 1：坐标系、WorldGrid、WorldData、Chunk | [02-phase-1-world-grid.md](02-phase-1-world-grid.md) |
| Phase 2：树木、对象池、View、Terrain 同步 | [03-phase-2-tree-view.md](03-phase-2-tree-view.md) |
| Phase 3：Worker、A* 适配层、移动、FSM 状态机 | [04-phase-3-worker-pathfinding.md](04-phase-3-worker-pathfinding.md) |
| Phase 4：建筑、道路、放置、生命周期 | [05-phase-4-building-road.md](05-phase-4-building-road.md) |
| Phase 5：Job 系统、任务、Tick、存档、UI | [06-phase-5-simulation.md](06-phase-5-simulation.md) |
| Phase 6：性能监控、LOD、优化、验证 | [07-phase-6-optimization.md](07-phase-6-optimization.md) |

## 按主题索引

### 已有框架复用

- [00-overview.md § 已有框架与插件](00-overview.md#已有框架与插件必须复用)
- [00-overview.md § 程序集策略](00-overview.md#程序集assembly-definition策略)

### 架构与设计原则

- [00-overview.md § 核心架构](00-overview.md#核心架构)
- [00-overview.md § 核心设计原则](00-overview.md#核心设计原则)
- [00-overview.md § 全局关键决策](00-overview.md#全局关键决策)
- [00-overview.md § 跨 Phase 接口契约](00-overview.md#跨-phase-接口契约)

### 坐标与空间

- [02-phase-1-world-grid.md § 1.1 坐标系定义](02-phase-1-world-grid.md#11-坐标系定义)
- [02-phase-1-world-grid.md § 1.2 WorldGrid 空间数据库](02-phase-1-world-grid.md#12-worldgrid-空间数据库)
- [02-phase-1-world-grid.md § 1.4 Chunk 与 ChunkManager](02-phase-1-world-grid.md#14-chunk-与-chunkmanager)

### 数据层

- [02-phase-1-world-grid.md § 1.3 WorldData 权威数据层](02-phase-1-world-grid.md#13-worlddata-权威数据层)
- [02-phase-1-world-grid.md § 1.5 Terrain 树木初始同步](02-phase-1-world-grid.md#15-terrain-树木初始同步)

### 表现层与对象池

- [03-phase-2-tree-view.md § 2.3 TreeView 表现](03-phase-2-tree-view.md#23-treeview-表现)
- [03-phase-2-tree-view.md § 2.4 通用对象池](03-phase-2-tree-view.md#24-通用对象池)
- [03-phase-2-tree-view.md § 2.5 Chunk 驱动的 TreeView 激活](03-phase-2-tree-view.md#25-chunk-驱动的-treeview-激活)
- [03-phase-2-tree-view.md § 2.6 Terrain TreeInstance ↔ TreeData 运行时同步](03-phase-2-tree-view.md#26-terrain-treeinstance--treedata-运行时同步)

### AI 与寻路

- [04-phase-3-worker-pathfinding.md § 3.3 A* 寻路适配层](04-phase-3-worker-pathfinding.md#33-a-寻路适配层)
- [04-phase-3-worker-pathfinding.md § 3.4 局部 Graph Update](04-phase-3-worker-pathfinding.md#34-局部-graph-update)
- [04-phase-3-worker-pathfinding.md § 3.5 Worker 移动系统](04-phase-3-worker-pathfinding.md#35-worker-移动系统)
- [04-phase-3-worker-pathfinding.md § 3.6 Worker 状态机](04-phase-3-worker-pathfinding.md#36-worker-状态机)

### 建筑与道路

- [05-phase-4-building-road.md § 4.2 建筑放置与占用](05-phase-4-building-road.md#42-建筑放置与占用)
- [05-phase-4-building-road.md § 4.5 道路铺设与速度修正](05-phase-4-building-road.md#45-道路铺设与速度修正)

### 任务与模拟

- [06-phase-5-simulation.md § 5.2 JobManager 调度](06-phase-5-simulation.md#52-jobmanager-调度)
- [06-phase-5-simulation.md § 5.7 模拟 Tick 管理](06-phase-5-simulation.md#57-模拟-tick-管理)
- [06-phase-5-simulation.md § 5.8 存档系统](06-phase-5-simulation.md#58-存档系统)

### 性能优化

- [00-overview.md § 性能预算](00-overview.md#性能预算)
- [07-phase-6-optimization.md § 6.1 性能监控](07-phase-6-optimization.md#61-性能监控)
- [07-phase-6-optimization.md § 6.5 内存与 GC 优化](07-phase-6-optimization.md#65-内存与-gc-优化)

## 关键接口速查

### WorldGrid

```csharp
public class WorldGrid
{
    public Cell GetCell(GridPos pos);
    public void SetCell(GridPos pos, Cell cell);
    public bool IsWalkable(GridPos pos);
    public bool IsOccupied(GridPos pos);
    public int GetEntityAt(GridPos pos, EntityType type);
}
```

### WorldData

```csharp
public class WorldData
{
    public event Action<int> TreeAdded, TreeRemoved, TreeModified;
    public event Action<int> BuildingAdded, BuildingRemoved, BuildingModified;
    public event Action<int> RoadAdded, RoadRemoved;
    public event Action<int> WorkerAdded, WorkerRemoved;

    public TreeData GetTree(int id);
    public BuildingData GetBuilding(int id);
    public RoadData GetRoad(int id);
    public WorkerData GetWorker(int id);
}
```

### AStarPathfinder

```csharp
public class AStarPathfinder
{
    public List<GridPos> FindPath(GridPos start, GridPos goal);
    public void RequestGraphUpdate(GridPos center, int radius);
}
```

### JobManager

```csharp
public class JobManager
{
    public int CreateJob(JobType type, GridPos target, int targetEntityId);
    public bool TryAssignJob(int workerId, out int jobId);
    public void CompleteJob(int jobId);
}
```

## 元数据文件

- 结构化元数据：`specs-metadata.json`
- 所有 `.md` 文件顶部均包含 YAML frontmatter，可被静态站点生成器、Obsidian、Notion、向量数据库等工具解析。

## 使用建议（针对 MCP / RAG）

1. 检索全局约定时优先读取 `00-overview.md`
2. 检索具体任务时按 `phase` 字段过滤
3. 检索接口依赖时使用 `dependencies` 字段
4. 检索关键词时使用 `keywords` 字段
5. 追踪进度时读取并更新每个文件的 `status` 和 `owner` 字段
