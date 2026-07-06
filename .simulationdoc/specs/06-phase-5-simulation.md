---
title: "Phase 5：Simulation"
phase: "5"
owner: "海豹"
status: "planned"
dependencies: ["phase-1", "phase-2", "phase-3", "phase-4"]
keywords: ["Job", "任务系统", "存档", "UI", "Tick", "Simulation"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Phase 5：Simulation

## 负责人

海豹

## 目标

实现任务系统、四种具体任务（砍树、建造、农田、搬运）、模拟 Tick 管理、存档系统和基础 UI，使游戏成为一个可玩、可保存/读取的完整模拟经营原型。

## 依赖

- Phase 1（WorldData、WorldGrid）
- Phase 2（树木生命周期）
- Phase 3（Worker、A*、状态机）
- Phase 4（建筑、道路）

## 已有框架复用

| 需求 | 使用框架 | 说明 |
|------|----------|------|
| UI | `MOYVUnityUGUI`（`Assets/Game/Framework/Ugui/`） | UGUI 扩展组件 |
| 事件 | `MEvent`（`Assets/Game/Framework/MEvent/`）+ QFramework `SendEvent<T>()` | 跨模块事件通信 |
| 异步存档 | `UniTask`（`Assets/Plugins/UniTask/`） | 异步文件读写 |

## 任务列表

| # | 任务 | 关键产出 | 依赖 | 验收标准 |
|---|------|----------|------|----------|
| 5.1 | Job 抽象 | `JobData.cs`、`JobType.cs`、`JobStatus.cs`、`IJobStep.cs` | 1.3 | 支持 CutTree/Build/Farm/Haul 四种任务 |
| 5.2 | JobManager 调度 | `JobManager.cs` | 3.1, 5.1 | 空闲工人按距离和优先级分配任务，避免全局扫描 |
| 5.3 | 砍树任务 | `CutTreeJob.cs` | 2.2, 3.6, 5.2 | 工人移动到树、工作、减少 HP、移除树木并产出木材 |
| 5.4 | 建造任务 | `BuildJob.cs` | 4.3, 5.2 | 工人建造并推进 BuildingData.Progress |
| 5.5 | 农田任务 | `FarmJob.cs`、`CropData.cs` | 4.3, 5.2 | 播种/照料/收获循环可执行 |
| 5.6 | 搬运任务 | `HaulJob.cs`、`ItemData.cs`、`StorageData.cs` | 3.6, 5.2 | 工人搬运物品并在存储点卸货 |
| 5.7 | 模拟 Tick 管理 | `SimulationTickManager.cs`、`IGameSystem.cs` | 5.3~5.6 | 可暂停/恢复/调速，按确定顺序更新系统 |
| 5.8 | 存档系统 | `SaveData.cs`、`SaveLoadManager.cs` | 1.3, 3.1, 4.1, 4.4, 5.1 | 保存/读取 WorldData，GameObject 不保存，格式带版本号 |
| 5.9 | 基础 UI 与控制 | `PlacementUI.cs`、`WorkerInfoPanel.cs`、UI Prefabs | 4.2, 4.5, 5.2 | 玩家可放置建筑/道路、标记树木、查看工人信息 |

## 5.1 Job 抽象

### 产出文件

- `Assets/Game/Scripts/Simulation/Jobs/JobData.cs`
- `Assets/Game/Scripts/Simulation/Jobs/JobType.cs`
- `Assets/Game/Scripts/Simulation/Jobs/JobStatus.cs`
- `Assets/Game/Scripts/Simulation/Jobs/IJobStep.cs`

### 实现细节

1. `JobType` 枚举：
   - `CutTree`
   - `Build`
   - `Farm`
   - `Haul`

2. `JobStatus` 枚举：
   - `Pending`
   - `Assigned`
   - `InProgress`
   - `Completed`
   - `Cancelled`

3. `JobData`：
   ```csharp
   public class JobData : IWorldEntity
   {
       public int Id;
       public JobType Type;
       public JobStatus Status;
       public GridPos TargetPos;
       public int TargetEntityId;
       public int AssignedWorkerId;
       public int Priority;
   }
   ```

4. `IJobStep`：定义任务的各个步骤接口

### 验收标准

- 可创建四种任务
- 状态切换正确

## 5.2 JobManager 调度

### 产出文件

- `Assets/Game/Scripts/Simulation/Jobs/JobManager.cs`

### 实现细节

1. 维护任务队列：
   - `PendingJobs`
   - `ActiveJobs`
   - `CompletedJobs`

2. 分配策略：
   - 只给 Idle 状态的工人分配任务
   - 按距离和优先级排序
   - 避免扫描整个世界；优先从激活 Chunk 或工人附近半径内选择

3. 提供任务完成/取消接口

### 对外接口

```csharp
public class JobManager : MonoBehaviour, IGameSystem
{
    public int CreateJob(JobType type, GridPos target, int targetEntityId);
    public bool TryAssignJob(int workerId, out int jobId);
    public void CompleteJob(int jobId);
    public void CancelJob(int jobId);
    public JobData GetJob(int jobId);
}
```

### 验收标准

- 空闲工人能收到任务
- 一个工人不会同时被分配多个任务
- 任务完成后正确归档

## 5.3 砍树任务

### 产出文件

- `Assets/Game/Scripts/Simulation/Jobs/Steps/CutTreeJob.cs`

### 实现细节

1. 任务步骤：
   - 移动到目标树
   - 进入 Work 状态
   - 每 Tick 减少树 HP
   - HP 归零后移除树、产出木材 Item
2. 任务开始前将树标记为 `Marked`
3. 任务完成后产出 `ItemData`（木材）

### 验收标准

- 工人砍树直到树消失
- 树木移除后 Grid 和 Terrain 都更新
- 产出一个可搬运的木材 Item

## 5.4 建造任务

### 产出文件

- `Assets/Game/Scripts/Simulation/Jobs/Steps/BuildJob.cs`

### 实现细节

1. 任务步骤：
   - 移动到建筑工地
   - 进入 Work 状态
   - 每 Tick 增加 `BuildingData.ConstructionProgress`
   - 进度满后建筑变为 `Operational`

### 验收标准

- 工人建造直到进度满
- 建筑状态从 UnderConstruction 变为 Operational
- BuildingView 同步切换模型

## 5.5 农田任务

### 产出文件

- `Assets/Game/Scripts/Simulation/Jobs/Steps/FarmJob.cs`
- `Assets/Game/Scripts/Simulation/Farming/CropData.cs`

### 实现细节

1. `CropData`：
   - `Id`
   - `GridPos`
   - `CropTypeId`
   - `GrowthProgress`
   - `State`：Empty / Planted / Growing / HarvestReady

2. 农田任务分为三个子任务或步骤：
   - 播种（Sow）
   - 照料（Tend）
   - 收获（Harvest）

3. 农田地块可作为特殊建筑类型处理

### 验收标准

- 工人能执行播种/照料/收获
- 作物生长进度推进
- 收获产出农作物 Item

## 5.6 搬运任务

### 产出文件

- `Assets/Game/Scripts/Simulation/Jobs/Steps/HaulJob.cs`
- `Assets/Game/Scripts/Simulation/Items/ItemData.cs`
- `Assets/Game/Scripts/Simulation/Storage/StorageData.cs`

### 实现细节

1. `ItemData`：
   - `Id`
   - `ItemTypeId`
   - `GridPos`
   - `Amount`

2. `StorageData`：
   - `Id`
   - `BuildingId`（关联仓库建筑）
   - `StoredItems` 字典

3. 搬运任务步骤：
   - 移动到取货点
   - 进入 Carry 状态，拾取 Item
   - 移动到卸货点
   - 卸下 Item

### 验收标准

- 工人状态在 Work/Carry 间切换
- 搬运后 Item 从取货点移到卸货点
- WorkerData.CarriedItem 正确更新

## 5.7 模拟 Tick 管理

### 产出文件

- `Assets/Game/Scripts/Simulation/Simulation/SimulationTickManager.cs`
- `Assets/Game/Scripts/MiniGame_Scripts/System/IGameSystem.cs`

### 实现细节

1. `IGameSystem`：
   ```csharp
   public interface IGameSystem
   {
       void Tick(float deltaTime);
   }
   ```

2. `SimulationTickManager`：
   - 注册所有 IGameSystem
   - 按固定频率调用 Tick
   - 支持 Pause / 1× / 2× / 4×
   - 更新顺序：Lifecycle → PathfindingUpdate → JobManager → WorkerBrain → WorkerMovement → WorkerStateSystem

### 验收标准

- 可暂停/恢复/调速
- 各系统按顺序更新
- 加速时不出现逻辑错误

## 5.8 存档系统

### 产出文件

- `Assets/Game/Scripts/Simulation/SaveLoad/SaveData.cs`
- `Assets/Game/Scripts/Simulation/SaveLoad/SaveLoadManager.cs`

### 实现细节

1. `SaveData`：
   - 版本号
   - TreeData 列表
   - BuildingData 列表
   - RoadData 列表
   - WorkerData 列表
   - JobData 列表（可选，或只保存 persistent jobs）

2. `SaveLoadManager`：
   - `Save(string slotName)`
   - `Load(string slotName)`
   - 不保存 GameObject、View、相机状态
   - 使用 UniTask 进行异步文件读写

3. 使用 JSON + 版本号，便于调试

### 验收标准

- 保存后退出，再读取能恢复世界状态
- GameObject 不进入存档
- 版本号正确写入和校验

## 5.9 基础 UI 与控制

### 产出文件

- `Assets/Game/Scripts/Simulation/UI/PlacementUI.cs`
- `Assets/Game/Scripts/Simulation/UI/WorkerInfoPanel.cs`
- `Assets/Game/MiniGame_Res/Prefabs/UI/` 目录

### 实现细节

1. `PlacementUI`：
   - 建筑选择面板
   - 道路选择面板
   - 标记树木模式切换
   - 显示资源/材料信息
   - 复用 `MOYVUnityUGUI` 的扩展组件

2. `WorkerInfoPanel`：
   - 点击工人显示信息
   - 状态、当前任务、携带物品

3. 使用 uGUI（与项目已有 UI 方案一致）

### 验收标准

- 玩家可放置建筑、铺设道路、标记树木
- 选中工人可查看信息
- UI 不阻塞模拟运行

## 提供给下游 Phase 的契约

| 系统 | 提供内容 |
|------|----------|
| `JobData` / `JobManager` | 任务创建、分配、完成 |
| `CutTreeJob` / `BuildJob` / `FarmJob` / `HaulJob` | 具体任务执行逻辑 |
| `SimulationTickManager` | 统一时间推进 |
| `SaveLoadManager` | 存档/读取 |
| `PlacementUI` / `WorkerInfoPanel` | 玩家交互界面 |

## 阻塞 downstream 的风险

- JobManager 分配逻辑性能差会影响 100 工人场景
- 存档格式未版本化会导致迭代中旧档损坏
- Tick 顺序错误会导致一帧内状态不一致
