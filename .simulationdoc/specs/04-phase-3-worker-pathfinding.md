---
title: "Phase 3：Worker + A*"
phase: "3"
owner: "海豹"
status: "planned"
dependencies: ["phase-0", "phase-1"]
keywords: ["Worker", "A*", "寻路", "状态机", "移动系统", "Pathfinding"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Phase 3：Worker + A*

## 负责人

海豹

## 目标

实现工人数据模型、表现层、A* 寻路系统、移动系统和状态机，使工人能够基于 Grid 在世界中移动并执行后续 Job 系统分派的任务。

## 依赖

- Phase 0（相机/输入）
- Phase 1（WorldGrid、WorldData、ChunkManager）

## 已有框架复用

| 需求 | 使用框架 | 说明 |
|------|----------|------|
| 寻路 | `A* Pathfinding Project`（`Assets/Plugins/Astar/`） | 已集成的 A* 插件，提供图管理和寻路 API |
| 状态机 | `MonsterLove FSM`（`Assets/Game/Framework/FSM/`） | Worker 的 Idle/Move/Work/Carry 状态切换 |
| 动画 | `MOYVDoTween`（`Assets/Game/Framework/DoTween/`） | Worker 平滑移动插值 |

## 任务列表

| # | 任务 | 关键产出 | 依赖 | 验收标准 |
|---|------|----------|------|----------|
| 3.1 | WorkerData 与注册 | `WorkerData.cs`、`WorkerState.cs`、`WorkerRegistry.cs` | 1.3 | 可生成并跟踪最多 100 个工人数据 |
| 3.2 | WorkerView 表现 | `WorkerView.prefab`、`WorkerView.cs` | 3.1 | View 平滑插值跟随数据，播放对应动画 |
| 3.3 | A* 寻路适配层 | `PathGrid.cs`、`AStarPathfinder.cs` | 1.2 | 可绕障碍物寻路，不可达时返回失败 |
| 3.4 | 局部 Graph Update | `PathGraphUpdater.cs` | 3.3 | 树木/建筑/道路变化只更新受影响节点 |
| 3.5 | Worker 移动系统 | `WorkerMovementSystem.cs` | 3.1, 3.3 | 工人沿路径在格间移动，不穿过障碍 |
| 3.6 | Worker 状态机 | `WorkerStateSystem.cs`、`WorkerBrain.cs` | 3.1, 3.5 | Idle→Move→Work/Carry→Idle 切换正确，空闲时向 JobManager 要任务 |

## 3.1 WorkerData 与注册

### 产出文件

- `Assets/Game/Scripts/Simulation/Workers/WorkerData.cs`
- `Assets/Game/Scripts/Simulation/Workers/WorkerState.cs`
- `Assets/Game/Scripts/Simulation/Workers/WorkerRegistry.cs`

### 实现细节

1. `WorkerState` 枚举：
   - `Idle`
   - `Move`
   - `Work`
   - `Carry`

2. `WorkerData`：
   ```csharp
   public class WorkerData : IWorldEntity
   {
       public int Id;
       public GridPos GridPos;
       public Vector3 Position;
       public WorkerState State;
       public int CurrentJobId;
       public int CarriedItemId;
       public float MoveProgress; // 0~1，表示在相邻格之间的进度
   }
   ```

3. `WorkerRegistry`：
   - 按 ID 索引工人
   - 最大容量 100

### 验收标准

- 可生成最多 100 个工人
- ID 唯一，与 WorldData 同步
- 状态枚举可用

## 3.2 WorkerView 表现

### 产出文件

- `Assets/Game/MiniGame_Res/Prefabs/Workers/WorkerView.prefab`
- `Assets/Game/Scripts/Simulation/Workers/WorkerView.cs`

### 实现细节

1. `WorkerView` 只处理视觉：
   - 平滑插值跟随 `WorkerData.Position`（可使用 DOTween）
   - 根据 `WorkerState` 播放 Idle/Walk/Work/Carry 动画
   - 朝向移动方向
2. 不存储逻辑状态

### 对外接口

```csharp
public class WorkerView : MonoBehaviour
{
    public void Initialize(WorkerData data);
    public void Refresh(WorkerData data);
    public void SetState(WorkerState state);
}
```

### 验收标准

- WorkerView 位置与 WorkerData.Position 一致
- 状态切换时动画正确变化

## 3.3 A* 寻路适配层

> **重要**：项目已集成 `A* Pathfinding Project`（`Assets/Plugins/Astar/`），提供 `AstarPath`、`GraphGrid`、`ABPath` 等核心 API。本任务不重写 A* 算法，而是编写**适配层**将 WorldGrid 与 A* 插件对接。

### 产出文件

- `Assets/Game/Scripts/Simulation/Pathfinding/PathGrid.cs`
- `Assets/Game/Scripts/Simulation/Pathfinding/AStarPathfinder.cs`

### 实现细节

1. **与 A* Pathfinding Project 集成**：
   - 在场景中添加 `AstarPath` 组件
   - 配置 Grid Graph 与 WorldGrid 对齐（相同的格大小和原点）
   - 使用 A* 插件的 `ABPath.Construct(start, end, callback)` 发起寻路

2. `PathGrid`：
   - 初始化时根据 WorldGrid 设置 A* Graph 节点可行走性
   - 提供 `SyncFromWorldGrid(WorldGrid grid)` 方法

3. `AStarPathfinder`：
   - 封装 A* 插件的寻路调用
   - 将 A* 返回的 `Vector3` 路径转换为 `List<GridPos>`
   - 支持异步寻路（UniTask）
   - 返回 `List<GridPos>` 路径或空列表表示不可达

### 对外接口

```csharp
public class AStarPathfinder
{
    public List<GridPos> FindPath(GridPos start, GridPos goal);
    public void SetNodeWalkable(GridPos pos, bool walkable);
}
```

### 验收标准

- 可找到绕过障碍物的路径
- 不可达目标返回空路径
- 100 次寻路无异常

## 3.4 局部 Graph Update

### 产出文件

- `Assets/Game/Scripts/Simulation/Pathfinding/PathGraphUpdater.cs`

### 实现细节

1. 订阅 `WorldData` 的实体增删改事件
2. 当树木/建筑/道路变化时：
   - 使用 A* 插件的 `GraphUpdateObject` 只更新受影响区域
   - 不重建整张图
3. 提供批量更新接口：
   ```csharp
   public void RequestGraphUpdate(GridPos center, int radius);
   ```

### 对外接口

```csharp
public class PathGraphUpdater : MonoBehaviour
{
    public void RequestGraphUpdate(GridPos center, int radius);
}
```

### 验收标准

- 放置建筑后该位置不可行走
- 移除树木后该位置恢复可行走
- 更新不触发全图重建

## 3.5 Worker 移动系统

### 产出文件

- `Assets/Game/Scripts/Simulation/Workers/WorkerMovementSystem.cs`

### 实现细节

1. 消费 A* 返回的路径（`Queue<GridPos>`）
2. 每 Tick 推进 `MoveProgress`
3. 速度受道路类型影响（Phase 4 提供 `RoadSpeedUtility`）
4. 到达下一格时：
   - 更新 `WorkerData.GridPos`
   - 更新 `WorldGrid` 中工人占用（可选，取决于设计）
   - 重置 `MoveProgress`
5. 路径走完后切换到 Idle 或通知状态机

### 对外接口

```csharp
public class WorkerMovementSystem : MonoBehaviour, IGameSystem
{
    public void SetPath(int workerId, List<GridPos> path);
    public void Tick(float deltaTime);
    public bool HasArrived(int workerId);
}
```

### 验收标准

- 工人沿路径移动，不穿过障碍
- 到达目标格后停止
- 移动速度可被外部修改

## 3.6 Worker 状态机

> **框架复用**：使用已有的 `MonsterLove FSM`（`Assets/Game/Framework/FSM/`）实现状态机。

### 产出文件

- `Assets/Game/Scripts/MiniGame_Scripts/System/WorkerStateSystem.cs`
- `Assets/Game/Scripts/Simulation/Workers/WorkerBrain.cs`

### 实现细节

1. `WorkerStateSystem`：
   - 基于 MonsterLove FSM 定义状态：`Idle / Move / Work / Carry`
   - 管理状态转换和回调（OnEnter / OnUpdate / OnExit）
   - 提供 `ChangeState(int workerId, WorkerState newState)`

2. `WorkerBrain`：
   - 当工人进入 Idle 时，向 `JobManager` 请求任务
   - 不主动全局搜索资源
   - 只执行已分配任务

### 对外接口

```csharp
public class WorkerStateSystem : MonoBehaviour, IGameSystem
{
    public void ChangeState(int workerId, WorkerState newState);
    public WorkerState GetState(int workerId);
}

public class WorkerBrain : MonoBehaviour, IGameSystem
{
    public void Tick(float deltaTime);
}
```

### 验收标准

- 状态切换触发 View 动画变化
- Idle 工人自动请求任务
- 状态转换顺序正确，不出现非法跳转

## 提供给下游 Phase 的契约

| 系统 | 提供内容 |
|------|----------|
| `WorkerData` / `WorkerRegistry` | 工人权威数据 |
| `AStarPathfinder` | 路径计算（基于 A* Pathfinding Project） |
| `PathGraphUpdater` | 局部寻路图更新 |
| `WorkerMovementSystem` | 工人移动执行 |
| `WorkerStateSystem` / `WorkerBrain` | 状态管理与任务请求 |

## 阻塞 downstream 的风险

- A* 插件配置不正确或适配层有 bug 会导致寻路失败
- 局部 Graph Update 不正确会导致工人穿墙或卡住
- WorkerBrain 若主动全局搜索会破坏设计原则
