---
title: "Phase 4：Building + Road"
phase: "4"
owner: "海豹"
status: "planned"
dependencies: ["phase-0", "phase-1", "phase-3"]
keywords: ["Building", "Road", "放置", "生命周期", "碰撞检测", "速度修正"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Phase 4：Building + Road

## 负责人

海豹

## 目标

实现建筑与道路的数据模型、放置系统、视觉表现和生命周期，使玩家可以在世界中放置建筑、铺设道路，并影响工人移动速度。

## 依赖

- Phase 0（相机/输入）
- Phase 1（WorldGrid、WorldData）
- Phase 3（A*、局部 Graph Update）

## 任务列表

| # | 任务 | 关键产出 | 依赖 | 验收标准 |
|---|------|----------|------|----------|
| 4.1 | BuildingData 与类型 | `BuildingData.cs`、`BuildingState.cs`、`BuildingTypeSO.cs`、`BuildingRegistry.cs` | 1.3 | 支持多尺寸建筑注册 |
| 4.2 | 建筑放置与占用 | `BuildingPlacementSystem.cs` | 1.2, 4.1 | 碰撞检测正确，幽灵预览可显示红/绿，确认后写入 Grid |
| 4.3 | BuildingView 与建造状态 | `BuildingView.cs`、`BuildingViewManager.cs`、建筑 Prefabs | 4.1, 4.2 | 建造进度填满后从脚手架切换到完成模型 |
| 4.4 | RoadData 与类型 | `RoadData.cs`、`RoadType.cs`、`RoadTypeSO.cs`、`RoadRegistry.cs` | 1.3 | 按格注册道路 |
| 4.5 | 道路铺设与速度修正 | `RoadPlacementSystem.cs`、`RoadSpeedUtility.cs` | 3.4, 4.4 | 铺设道路更新 Grid 和寻路图，工人在道路上移动更快 |
| 4.6 | 建筑与道路生命周期 | `BuildingLifecycleSystem.cs`、`RoadLifecycleSystem.cs` | 4.2, 4.5 | 拆除后释放 Grid 单元并更新寻路 |

## 4.1 BuildingData 与类型

### 产出文件

- `Assets/Game/Scripts/Simulation/Buildings/BuildingData.cs`
- `Assets/Game/Scripts/Simulation/Buildings/BuildingState.cs`
- `Assets/Game/MiniGame_Res/ScriptableObjects/BuildingTypes/BuildingTypeSO.cs`
- `Assets/Game/Scripts/Simulation/Buildings/BuildingRegistry.cs`

### 实现细节

1. `BuildingState` 枚举：
   - `Planned`
   - `UnderConstruction`
   - `Operational`
   - `Disabled`

2. `BuildingData`：
   ```csharp
   public class BuildingData : IWorldEntity
   {
       public int Id;
       public GridPos Origin;
       public int SizeX;
       public int SizeZ;
       public int BuildingTypeId;
       public BuildingState State;
       public float ConstructionProgress;
   }
   ```

3. `BuildingTypeSO`（ScriptableObject，放在 `Assets/Game/MiniGame_Res/ScriptableObjects/BuildingTypes/`）：
   - `buildingName`
   - `sizeX`、`sizeZ`
   - `constructionTime`
   - `requiredMaterials`
   - `prefab`（建造完成模型）
   - `constructionPrefab`（脚手架模型）

4. `BuildingRegistry`：按 ID 索引建筑

### 验收标准

- 支持多尺寸建筑注册
- 建筑数据与 WorldData 同步

## 4.2 建筑放置与占用

### 产出文件

- `Assets/Game/Scripts/Simulation/Buildings/BuildingPlacementSystem.cs`

### 实现细节

1. 根据建筑 footprint（SizeX × SizeZ）检测碰撞：
   - 与树木冲突
   - 与其他建筑冲突
   - 与不可行走地形冲突（可选）
2. 提供幽灵预览：
   - 有效时显示绿色
   - 无效时显示红色
3. 确认放置后：
   - 创建 `BuildingData`，State = `Planned` 或 `UnderConstruction`
   - 占用 footprint 内所有 Cell：`BuildingId` + `Occupied/HasBuilding`
   - 请求寻路图局部更新（`PathGraphUpdater`）

### 对外接口

```csharp
public class BuildingPlacementSystem : MonoBehaviour
{
    public void StartPlacement(int buildingTypeId);
    public void UpdateGhost(GridPos targetPos);
    public bool CanPlaceAt(GridPos origin, int buildingTypeId);
    public int ConfirmPlacement();
    public void CancelPlacement();
}
```

### 验收标准

- 重叠建筑无法放置
- 幽灵预览颜色正确
- 确认后 Grid 占用正确写入

## 4.3 BuildingView 与建造状态

### 产出文件

- `Assets/Game/MiniGame_Res/Prefabs/Buildings/` 目录
- `Assets/Game/Scripts/Simulation/Buildings/BuildingView.cs`
- `Assets/Game/Scripts/Simulation/Buildings/BuildingViewManager.cs`

### 实现细节

1. `BuildingView`：
   - 根据 `BuildingState` 显示脚手架或完成模型
   - 根据 `ConstructionProgress` 调整视觉进度
2. `BuildingViewManager`：
   - 为激活 Chunk 内的建筑生成 View
   - 类似 TreeViewSystem 的 Chunk 驱动逻辑
   - 可复用 `PoolManager`（Phase 2）管理建筑 View 池

### 对外接口

```csharp
public class BuildingView : MonoBehaviour
{
    public void Initialize(BuildingData data, BuildingTypeSO type);
    public void Refresh(BuildingData data);
}
```

### 验收标准

- 建筑放置后显示脚手架
- 进度填满后切换为完成模型
- 建筑拆除后 View 回收

## 4.4 RoadData 与类型

### 产出文件

- `Assets/Game/Scripts/Simulation/Roads/RoadData.cs`
- `Assets/Game/Scripts/Simulation/Roads/RoadType.cs`
- `Assets/Game/MiniGame_Res/ScriptableObjects/RoadTypes/RoadTypeSO.cs`
- `Assets/Game/Scripts/Simulation/Roads/RoadRegistry.cs`

### 实现细节

1. `RoadData`：
   ```csharp
   public class RoadData : IWorldEntity
   {
       public int Id;
       public GridPos GridPos;
       public int RoadTypeId;
   }
   ```

2. `RoadTypeSO`（ScriptableObject，放在 `Assets/Game/MiniGame_Res/ScriptableObjects/RoadTypes/`）：
   - `roadName`
   - `speedMultiplier`
   - `prefab`

### 验收标准

- 可按格注册道路
- 道路数据与 WorldData 同步

## 4.5 道路铺设与速度修正

### 产出文件

- `Assets/Game/Scripts/Simulation/Roads/RoadPlacementSystem.cs`
- `Assets/Game/Scripts/MiniGame_Scripts/Utility/RoadSpeedUtility.cs`

### 实现细节

1. `RoadPlacementSystem`：
   - 玩家在 Grid 上拖动铺设道路
   - 道路写入 `WorldGrid.RoadId`
   - 触发寻路图局部更新（`PathGraphUpdater`）

2. `RoadSpeedUtility`：
   - 查询某格道路类型
   - 返回速度倍率（默认 1.0，道路 > 1.0）
   - 供 `WorkerMovementSystem` 使用

### 对外接口

```csharp
public class RoadPlacementSystem : MonoBehaviour
{
    public void StartRoadPlacement(int roadTypeId);
    public void AddRoadSegment(GridPos pos);
    public void FinishRoadPlacement();
}

public class RoadSpeedUtility
{
    public float GetSpeedMultiplier(GridPos pos);
}
```

### 验收标准

- 道路可连续铺设
- 工人在道路上移动速度更快
- 道路铺设后寻路图正确更新

## 4.6 建筑与道路生命周期

### 产出文件

- `Assets/Game/Scripts/MiniGame_Scripts/System/BuildingLifecycleSystem.cs`
- `Assets/Game/Scripts/MiniGame_Scripts/System/RoadLifecycleSystem.cs`

### 实现细节

1. 建筑拆除：
   - 移除 BuildingData
   - 清除 footprint 内 Cell 的 BuildingId 和 Occupied/HasBuilding
   - 回收 BuildingView
   - 请求寻路图更新
2. 道路拆除：
   - 移除 RoadData
   - 清除 Cell 的 RoadId
   - 请求寻路图更新
3. 处理建筑与道路重叠的边界情况

### 验收标准

- 拆除后 Grid 单元释放
- 寻路图恢复可行走
- 无残留 View 或数据

## 提供给下游 Phase 的契约

| 系统 | 提供内容 |
|------|----------|
| `BuildingData` / `BuildingRegistry` | 建筑权威数据 |
| `BuildingPlacementSystem` | 放置验证与确认 |
| `BuildingViewManager` | 建筑视觉表现 |
| `RoadData` / `RoadRegistry` | 道路权威数据 |
| `RoadPlacementSystem` | 道路铺设 |
| `RoadSpeedUtility` | 移动速度修正 |

## 阻塞 downstream 的风险

- 多格建筑碰撞检测有 bug 会导致建筑重叠
- 道路速度未正确应用会导致移动系统行为异常
- 拆除后未更新寻路图会导致工人路径错误
