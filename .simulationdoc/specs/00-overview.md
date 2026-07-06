---
title: "Unity 模拟经营项目 — 总览与协作约定"
phase: "overview"
owner: "海豹"
status: "planned"
dependencies: []
keywords: ["Unity", "模拟经营", "架构", "接口契约", "性能目标", "WorldGrid"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Unity 模拟经营项目 — 总览与协作约定

## 项目背景

基于 `unity_simulation_design_v1.md` 技术设计文档，实现一款参考 *Against the Storm*、*Timberborn*、*Factorio* 的斜 45° 视角模拟经营游戏。

项目已有基础框架和场景（`ApplicationScene.unity`），需要在此基础上新建模拟经营系统代码。

## 目标规模

- 大地图 Terrain
- 10000+ 树木资源
- 100+ 工人
- 建筑 / 道路 / 农田系统
- 高性能可扩展架构

## 核心架构

```
Terrain（渲染） → WorldData → WorldGrid → ChunkManager → A* → Job → Worker → ViewPool
```

## 核心设计原则

1. 数据与表现分离
2. Terrain 只负责渲染
3. Grid 负责世界逻辑
4. A* 只负责寻路
5. GameObject 仅用于表现

## 技术选型

- **Unity 版本**：Unity 6 (6000.3.2f1)
- **渲染管线**：URP（Universal Render Pipeline 17.3.0）
- **输入系统**：New Input System
- **UI**：uGUI（已有 MOYVUnityUGUI 扩展框架）
- **架构模式**：QFramework MVC（Command / Controller / Model / System / Event）
- **暂不引入**：DOTS/ECS

## 已有框架与插件（必须复用）

| 框架/插件 | 路径 | 用途 |
|-----------|------|------|
| QFramework | `Assets/Game/Framework/Qframework/` | MVC 架构（Command/Controller/Model/System/Event） |
| MonsterLove FSM | `Assets/Game/Framework/FSM/` | Worker 状态机 |
| MPool | `Assets/Game/Framework/MPool/` | 对象池基础 |
| MOYVBase | `Assets/Game/Framework/Base/` | 基础工具库、扩展方法 |
| MOYVCollections | `Assets/Game/Framework/Collections/` | 自定义集合库 |
| MOYVUnityUGUI | `Assets/Game/Framework/Ugui/` | UGUI 扩展组件 |
| MOYVDoTween | `Assets/Game/Framework/DoTween/` | DOTween 动画封装 |
| MDebug | `Assets/Game/Framework/MDebug/` | 调试日志工具 |
| MEvent | `Assets/Game/Framework/MEvent/` | 事件系统 |
| A* Pathfinding Project | `Assets/Plugins/Astar/` | 寻路系统（已集成） |
| UniTask | `Assets/Plugins/UniTask/` | 异步操作 |
| UniFramework.Pooling | `Assets/Game/Scripts/MiniGame_Scripts/Utility/UniPooling/` | 对象池运行时 |

## 性能预算

| 指标 | 目标 |
|------|------|
| TreeView 实例数 | ≤ 500 |
| Worker 数量 | ≤ 100 |
| CPU 帧耗时 | < 10 ms |
| GPU 帧耗时 | < 8 ms |

## 全局关键决策

1. **坐标约定**
   - `GridPos`：整数 X/Z 格索引
   - `ChunkPos`：`GridPos / chunkSize`
   - `WorldPos`：浮点世界坐标，Y 由 Terrain 高度采样或 Cell 存储
   - 原点与世界原点对齐

2. **Chunk 尺寸**
   - 默认 32×32
   - 玩家激活范围 3×3 Chunk

3. **Entity ID 规范**
   - 统一使用 `int`
   - `0` 表示"无"/空
   - 由 `IdGenerator` 统一分配

4. **系统通信方式**
   - 优先使用 C# 事件或 `System.Action`
   - QFramework 的 `SendEvent<T>()` 用于跨模块通信
   - 避免在 Tick 热路径中频繁创建委托/闭包

5. **序列化格式**
   - 开发期使用 JSON（便于调试）
   - 发布前可切换为 Binary（需保留版本号）

6. **Terrain 所有权**
   - Terrain 仅在启动时是树木位置权威
   - 运行时权威为 `WorldData` + `WorldGrid`

7. **模拟 Tick 频率**
   - 固定频率，例如 10 ticks/秒
   - 支持 Pause / 1× / 2× / 4× 速度

## 全局目录结构

> 模拟经营代码统一放置在 `Assets/Game/` 下，与现有框架和脚本共存。

```
Assets/Game/
├── Framework/                      ← 已有框架层（不修改）
│   ├── Base/                       ← MOYVBase 基础工具
│   ├── Collections/                ← 自定义集合
│   ├── FSM/                        ← MonsterLove 状态机
│   ├── MPool/                      ← 对象池
│   ├── Qframework/                 ← QFramework MVC
│   ├── Ugui/                       ← UGUI 扩展
│   └── ...
├── Scripts/
│   ├── Simulation/                 ← ★ 模拟经营系统代码（新建）
│   │   ├── World/                  ← 世界基础
│   │   │   ├── Coordinates/        ← GridPos, ChunkPos, WorldPos
│   │   │   ├── Grid/               ← WorldGrid, Cell, CellFlags
│   │   │   ├── Data/               ← WorldData, IWorldEntity, IdGenerator
│   │   │   ├── Chunk/              ← Chunk, ChunkManager
│   │   │   └── Terrain/            ← TerrainDataReader
│   │   ├── Trees/                  ← TreeData, TreeView, TreeRegistry
│   │   ├── Workers/                ← WorkerData, WorkerView, Movement
│   │   ├── Pathfinding/            ← PathGrid, PathGraphUpdater
│   │   ├── Buildings/              ← BuildingData, Placement, View
│   │   ├── Roads/                  ← RoadData, Placement
│   │   ├── Jobs/                   ← JobData, JobManager, Steps/
│   │   ├── Items/                  ← ItemData
│   │   ├── Storage/                ← StorageData
│   │   ├── Farming/                ← CropData, FarmJob
│   │   ├── Simulation/             ← SimulationTickManager
│   │   ├── SaveLoad/               ← SaveData, SaveLoadManager
│   │   ├── View/                   ← 通用表现层
│   │   │   └── Pool/               ← ObjectPool, IPoolable, PoolManager
│   │   ├── Camera/                 ← IsometricCameraController
│   │   ├── Input/                  ← GameInput (New Input System)
│   │   ├── UI/                     ← PlacementUI, WorkerInfoPanel
│   │   └── Profiling/              ← PerformanceMonitor
│   ├── Common/                     ← 已有公共工具（不修改）
│   ├── ConfigCode/                 ← 已有配置表代码（不修改）
│   ├── Game/                       ← 已有游戏逻辑（不修改）
│   └── MiniGame_Scripts/           ← 已有 QFramework MVC 代码 + ★ 模拟经营系统/工具
│       ├── Command/
│       ├── Controller/
│       │   └── Camera/             ← 已有相机控制器
│       ├── Event/
│       ├── Model/
│       ├── System/                 ← ★ 新增: IGameSystem, TreeLifecycleSystem, BuildingLifecycleSystem, RoadLifecycleSystem, TreeViewSystem, WorkerStateSystem, TerrainTreeSyncSystem
│       └── Utility/                ← ★ 新增: RoadSpeedUtility
├── Scenes/
│   └── ApplicationScene.unity      ← 已有主场景
├── Resources/
└── MiniGame_Res/
    ├── Prefabs/                    ← ★ 模拟经营 Prefabs（新建）
    │   ├── Trees/
    │   ├── Workers/
    │   ├── Buildings/
    │   └── UI/
    └── ScriptableObjects/          ← ★ SO 配置（新建）
        ├── TreeTypes/
        ├── BuildingTypes/
        └── RoadTypes/
```

### 程序集（Assembly Definition）策略

模拟经营代码位于 `Assets/Game/Scripts/Simulation/`，属于默认的 `Assembly-CSharp` 程序集，可直接引用所有已有框架（QFramework、MonsterLove FSM、MPool、MOYVBase 等）和 A* Pathfinding 插件。

若后续需要独立编译加速，可在 `Simulation/` 下新建 `Simulation.asmdef`，需显式引用：
- `QFramework`
- `MonsterLove.StateMachine.Runtime`
- `MOYVBase`
- `MOYVCollections`
- `MOYVUnityUGUI`
- `MOYVDoTween`
- `AstarPathfindingProject`
- `UniTask`

## 跨 Phase 接口契约

### WorldGrid 提供（由 Phase 1 实现，所有人依赖）

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

### WorldData 提供（由 Phase 1 实现，所有人依赖）

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

### ChunkManager 提供（由 Phase 1 实现）

```csharp
public class ChunkManager
{
    public event Action<ChunkPos> ChunkActivated;
    public event Action<ChunkPos> ChunkDeactivated;
    public bool IsChunkActive(ChunkPos pos);
    public ChunkPos GetChunkForCell(GridPos pos);
}
```

### A* Pathfinder 提供（由 Phase 3 实现，基于已有 A* Pathfinding Project）

```csharp
public class AStarPathfinder
{
    public List<GridPos> FindPath(GridPos start, GridPos goal);
    public void RequestGraphUpdate(GridPos center, int radius);
}
```

### JobManager 提供（由 Phase 5 实现）

```csharp
public class JobManager
{
    public int CreateJob(JobType type, GridPos target, int targetEntityId);
    public bool AssignJobToWorker(int workerId, out int jobId);
    public void CompleteJob(int jobId);
}
```

## 最小可玩里程碑

完成以下 Phase 中的标红任务后，即可得到一个可运行原型：

- Phase 0：0.1、0.2
- Phase 1：1.1、1.2、1.3、1.4、1.5
- Phase 2：2.1、2.2、2.3、2.4、2.5、2.6
- Phase 3：3.1、3.2、3.3、3.4、3.5、3.6
- Phase 5：5.1、5.2、5.3、5.9

**可验证行为**：
- 斜 45° 相机漫游大地图
- 近处树木以 GameObject View 显示，远处回归 Terrain
- 可生成工人
- 可标记树木砍伐
- 工人自动寻路到树、砍伐、树木从世界中移除

## 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| 10000+ Terrain TreeInstance 运行时编辑开销 | 高 | 仅将 Terrain 用作远处 LOD，活跃树用 GameObject |
| 100 工人同时 A* | CPU 峰值 | 按帧分摊、缓存路径、局部 Graph Update |
| Chunk 边界对象池抖动 | 帧率尖刺 | 增加滞后/惰性停用、预生成池 |
| 热点路径 GC 分配 | CPU 超标 | 池化集合、使用 struct、避免 LINQ/闭包 |
| 500+ View + 建筑 + 工人 Draw Call | GPU 超标 | GPU Instancing、LOD、材质合并 |
| 多格建筑碰撞 | 逻辑错误 | 为 footprint 重叠和 Graph Update 写单元测试 |
| 存档版本不兼容 | 迭代破坏旧档 | 保存格式从第一天起带版本号 |

## 文件清单

| 文件 | 说明 |
|------|------|
| `01-phase-0-setup.md` | 项目初始化、相机、输入 |
| `02-phase-1-world-grid.md` | 坐标系、WorldGrid、WorldData、Chunk、Terrain 同步 |
| `03-phase-2-tree-view.md` | 树木数据、生命周期、View、对象池、Terrain 同步 |
| `04-phase-3-worker-pathfinding.md` | Worker 数据/表现、A*、移动、状态机 |
| `05-phase-4-building-road.md` | 建筑、道路、放置、生命周期 |
| `06-phase-5-simulation.md` | Job 系统、四种任务、Tick、存档、UI |
| `07-phase-6-optimization.md` | 性能监控、LOD、寻路优化、GC、GPU、最终验证 |