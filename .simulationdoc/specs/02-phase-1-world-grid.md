---
title: "Phase 1：World + Grid"
phase: "1"
owner: "海豹"
status: "planned"
dependencies: ["phase-0"]
keywords: ["WorldGrid", "WorldData", "ChunkManager", "坐标系", "空间数据库", "Terrain"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Phase 1：World + Grid

## 负责人

海豹

## 目标

建立整个模拟世界的空间数据库、权威数据层和空间分区系统，使所有后续系统都能基于统一的坐标、Grid 和 Chunk 进行查询与更新。

## 依赖

- Phase 0 完成（模拟经营目录已创建、相机/输入可用）

## 任务列表

| # | 任务 | 关键产出 | 依赖 | 验收标准 |
|---|------|----------|------|----------|
| 1.1 | 坐标系定义 | `WorldPos.cs`、`GridPos.cs`、`ChunkPos.cs`、`CoordinateUtility.cs` | 0.1 | GridPos/ChunkPos/WorldPos 双向转换正确且零分配 |
| 1.2 | WorldGrid 空间数据库 | `CellFlags.cs`、`Cell.cs`、`WorldGrid.cs` | 1.1 | 支持 GetCell/SetCell/IsWalkable/IsOccupied，单格单一拥有者 |
| 1.3 | WorldData 权威数据层 | `WorldData.cs`、`IWorldEntity.cs`、`IdGenerator.cs` | 1.2 | 实体增删改查正常，ID 唯一，事件回调可用 |
| 1.4 | Chunk 与 ChunkManager | `Chunk.cs`、`ChunkManager.cs` | 1.1, 1.2 | 32×32 Chunk，3×3 激活窗口，激活/停用事件正确触发 |
| 1.5 | Terrain 树木初始同步 | `TerrainDataReader.cs` | 1.3, 1.4 | 启动时从 `Terrain.terrainData.treeInstances` 生成 TreeData 并写入 Grid |

## 1.1 坐标系定义

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Coordinates/WorldPos.cs`
- `Assets/Game/Scripts/Simulation/World/Coordinates/GridPos.cs`
- `Assets/Game/Scripts/Simulation/World/Coordinates/ChunkPos.cs`
- `Assets/Game/Scripts/Simulation/World/Coordinates/CoordinateUtility.cs`

### 实现细节

1. `GridPos`：整数 X/Z 格索引
2. `ChunkPos`：chunk 索引，`GridPos / chunkSize`
3. `WorldPos`：浮点世界坐标，Y 由 Terrain 高度采样
4. 提供零分配的双向转换：
   - `WorldPos → GridPos`
   - `GridPos → ChunkPos`
   - `GridPos → WorldPos`（取格中心）
5. 实现 `Equals`、`GetHashCode`、`ToString` 和比较运算符

### 数据结构建议

```csharp
public readonly struct GridPos : IEquatable<GridPos>
{
    public readonly int X;
    public readonly int Z;
    // 构造函数、运算符、Equals、GetHashCode
}

public readonly struct ChunkPos : IEquatable<ChunkPos>
{
    public readonly int X;
    public readonly int Z;
}
```

### 验收标准

- 任意 GridPos 经 WorldPos 再转回 GridPos，结果与原值一致
- 转换方法不产生 GC 分配
- 边缘坐标（负坐标、大坐标）处理正确

## 1.2 WorldGrid 空间数据库

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Grid/CellFlags.cs`
- `Assets/Game/Scripts/Simulation/World/Grid/Cell.cs`
- `Assets/Game/Scripts/Simulation/World/Grid/WorldGrid.cs`

### 实现细节

1. `CellFlags` 使用 `[Flags]` 枚举：
   - `Walkable`
   - `Occupied`
   - `HasTree`
   - `HasBuilding`
   - `HasRoad`

2. `Cell` 结构：
   ```csharp
   public struct Cell
   {
       public int TreeId;
       public int BuildingId;
       public int RoadId;
       public CellFlags Flags;
   }
   ```

3. `WorldGrid`：
   - 使用一维数组或字典 backing
   - 提供 `GetCell`、`SetCell`、`IsWalkable`、`IsOccupied`
   - 提供 `GetEntityAt(GridPos, EntityType)`
   - 边界外查询返回默认 Cell（不可行走）

### 对外接口

```csharp
public class WorldGrid
{
    public Cell GetCell(GridPos pos);
    public void SetCell(GridPos pos, Cell cell);
    public bool IsWalkable(GridPos pos);
    public bool IsOccupied(GridPos pos);
    public int GetEntityAt(GridPos pos, EntityType type);
    public bool IsInBounds(GridPos pos);
}
```

### 验收标准

- 读写 10000 次 Cell 无异常
- 占用标记设置后 `IsOccupied` 返回 true
- 边界外查询不抛异常

## 1.3 WorldData 权威数据层

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Data/WorldData.cs`
- `Assets/Game/Scripts/Simulation/World/Data/IWorldEntity.cs`
- `Assets/Game/Scripts/Simulation/World/Data/IdGenerator.cs`

### 实现细节

1. `WorldData` 持有按 ID 索引的字典：
   - `Dictionary<int, TreeData> Trees`
   - `Dictionary<int, BuildingData> Buildings`
   - `Dictionary<int, RoadData> Roads`
   - `Dictionary<int, WorkerData> Workers`

2. `IdGenerator` 统一生成递增 `int` ID，从 1 开始

3. 提供事件：
   ```csharp
   public event Action<int> TreeAdded, TreeRemoved, TreeModified;
   public event Action<int> BuildingAdded, BuildingRemoved, BuildingModified;
   public event Action<int> RoadAdded, RoadRemoved;
   public event Action<int> WorkerAdded, WorkerRemoved, WorkerModified;
   ```

4. 可结合 QFramework 的 `SendEvent<T>()` 进行跨模块通知，但 Tick 热路径优先使用 C# 原生事件避免 GC

### 对外接口

```csharp
public class WorldData
{
    public static WorldData Instance { get; }

    public int CreateTree(TreeData data);
    public TreeData GetTree(int id);
    public void RemoveTree(int id);
    public void ModifyTree(int id, Action<TreeData> modifier);

    // 建筑、道路、工人类似
}
```

### 验收标准

- ID 唯一且递增
- 增删改事件正确触发
- 删除后查询返回 null/默认值

## 1.4 Chunk 与 ChunkManager

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Chunk/Chunk.cs`
- `Assets/Game/Scripts/Simulation/World/Chunk/ChunkManager.cs`

### 实现细节

1. `Chunk` 大小固定 32×32
2. `ChunkManager`：
   - 跟踪以焦点为中心的 3×3 激活窗口
   - 焦点来源：`IsometricCameraController.FocusPosition`（Phase 0）
   - 提供 `IsChunkActive(ChunkPos)`
   - 当焦点移动时触发 `ChunkActivated` / `ChunkDeactivated`
   - 可选：加入滞后/惰性切换，避免边界抖动

### 对外接口

```csharp
public class ChunkManager : MonoBehaviour
{
    public int ChunkSize => 32;
    public int ActiveRadius => 1; // 3×3 => radius 1

    public event Action<ChunkPos> ChunkActivated;
    public event Action<ChunkPos> ChunkDeactivated;

    public void SetFocus(GridPos focus);
    public bool IsChunkActive(ChunkPos pos);
    public ChunkPos GetChunkForCell(GridPos pos);
    public IEnumerable<GridPos> GetCellsInChunk(ChunkPos pos);
}
```

### 验收标准

- 移动焦点时正确激活/停用 3×3 Chunk
- 边界切换时事件只触发一次
- 能遍历指定 Chunk 内的所有 Cell

## 1.5 Terrain 树木初始同步

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Terrain/TerrainDataReader.cs`

### 实现细节

1. 在场景启动时读取 `Terrain.terrainData.treeInstances`
2. 将每个 `TreeInstance.position` 转换为 `GridPos`
3. 创建 `TreeData` 并通过 `WorldData` 注册
4. 在 `WorldGrid` 对应 Cell 写入 `TreeId` 和 `Occupied/HasTree` 标记
5. 此步骤只读 Terrain，不修改 Terrain

### 对外接口

```csharp
public class TerrainDataReader : MonoBehaviour
{
    public int ReadTreesFromTerrain(Terrain terrain);
}
```

### 验收标准

- Terrain 上有 N 棵树，则生成 N 个 TreeData
- TreeData 的 GridPos 与世界位置对应正确
- WorldGrid 对应 Cell 标记为 HasTree + Occupied

## 提供给下游 Phase 的契约

| 系统 | 提供内容 |
|------|----------|
| `WorldGrid` | 空间查询、占用管理、可行走判断 |
| `WorldData` | 所有实体权威数据、增删改事件 |
| `ChunkManager` | 3×3 Chunk 激活/停用事件 |
| `CoordinateUtility` | 坐标转换 |
| `TerrainDataReader` | 初始世界生成 |

## 阻塞 downstream 的风险

- `WorldGrid` 性能差会影响寻路和建筑放置
- `WorldData` 事件设计不合理会导致系统耦合或 GC
- 坐标转换有 bug 会导致所有空间逻辑错误