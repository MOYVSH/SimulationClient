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
| 1.4 | Chunk 与 ChunkManager | `Chunk.cs`、`ChunkManager.cs` | 1.1, 1.2 | 32x32 Chunk，3x3 激活窗口，激活/停用事件正确触发 |
| 1.5 | Terrain 树木初始同步 | `TerrainDataReader.cs` | 1.3, 1.4 | 启动时从 `Terrain.terrainData.treeInstances` 生成 TreeData 并写入 Grid |

---

## 1.1 坐标系定义

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Coordinates/WorldPos.cs`
- `Assets/Game/Scripts/Simulation/World/Coordinates/GridPos.cs`
- `Assets/Game/Scripts/Simulation/World/Coordinates/ChunkPos.cs`
- `Assets/Game/Scripts/Simulation/World/Coordinates/CoordinateUtility.cs`

### 三套坐标系的层级关系

三套坐标系形成**由细到粗**的层级，各自承担不同职责：

| 类型 | 字段 | 语义 | 数量级（256x256 地图） | 用途 |
|------|------|------|----------------------|------|
| `WorldPos` | `float X, Y, Z` | 世界坐标（连续） | 无穷 | 与 Unity Transform / 射线检测等 3D API 对接 |
| `GridPos` | `int X, Z` | 格子索引（离散） | 65536 | 寻路、格子遍历、空间查询 |
| `ChunkPos` | `int X, Z` | 区块索引（离散） | 64 | 区块加载/卸载管理 |

转换链路：

> `WorldPos` --WorldToGrid--> `GridPos` --GridToChunk--> `ChunkPos`
> `ChunkPos` --ChunkToGridOrigin--> `GridPos` --GridToWorld--> `WorldPos`

### 数据结构

`GridPos` 和 `ChunkPos` 均为 `readonly struct`（栈分配、零 GC），实现 `IEquatable<T>`（Dictionary 查找时不产生装箱 GC）。哈希函数为 `(X << 16) ^ Z`，在 [-32768, 32767] 范围内碰撞率极低。

```csharp
public readonly struct GridPos : IEquatable<GridPos>
{
    public readonly int X;
    public readonly int Z;
    // 构造函数、加减运算符、Equals、GetHashCode、== / !=
    // 四方向常量：Right, Left, Up, Down
    // CardinalDirections 数组供寻路展开邻居
}

public readonly struct ChunkPos : IEquatable<ChunkPos>
{
    public readonly int X;
    public readonly int Z;
    // GetNeighborsInRadius(int radius) 供 ChunkManager 遍历激活窗口
}

public readonly struct WorldPos
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;
    // 与 Vector3 的隐式（implicit）互转
}
```

`GridPos` 与 `ChunkPos` **刻意保持为独立类型**，不能隐式互转。两者内存结构完全相同（8 字节），但语义不同——编译器可以在类型检查阶段拦截"把格子坐标误当块坐标传入"的逻辑错误。

### 转换公式

全局常量：`CellSize = 1f`（每格世界边长），`ChunkSize = 32`（每 Chunk 含 32x32 格）。

| 转换方向 | 公式 | 说明 |
|----------|------|------|
| `WorldPos` -> `GridPos` | `X = FloorToInt(world.X / CellSize)` | 使用 Floor 而非 Round 或 Trunc，确保 [0,1) 范围归到格子 0 |
| `GridPos` -> `WorldPos` | `X = (grid.X + 0.5) * CellSize` | 返回格子**中心点**，Y 由 Terrain.SampleHeight 填入 |
| `GridPos` -> `ChunkPos` | `ChunkX = FloorDiv(GridX, ChunkSize)` | 必须用地板除法，见下方详解 |
| `ChunkPos` -> `GridPos` | `GridX = ChunkX * ChunkSize` | 返回 Chunk 左下角（最小索引）格子 |

### 负数处理与 FloorDiv（重要）

**问题根源**：C# 的整数除法 `/` 对正数和负数都是**向零取整**（Truncation Division），直接截掉小数部分。但坐标系统需要**向负无穷取整**（Floor Division），即数学意义上的 `floor(a/b)`。

假设 `ChunkSize = 32`，Chunk 的管辖范围：

| Chunk | 包含的格子范围 |
|-------|---------------|
| Chunk -2 | [-64, -33] |
| Chunk -1 | [-32, -1] |
| Chunk 0 | [0, 31] |
| Chunk 1 | [32, 63] |

格子 -1 在世界坐标 -0.5（格子中心），它在 0 的**左边**，应属于 Chunk -1。但 C# 的 `-1 / 32 = 0`，把它算到了 Chunk 0。后果：

- 对象不会被 Chunk -1 管理（丢失）
- 或者 Chunk 0 多管了一个不在自己范围内的对象

**C# 向零取整 vs Floor Division 对比**：

| 除法 | 精确值 | C# 向零取整 | Floor Division | 是否一致 |
|------|--------|------------|----------------|----------|
| 33 / 32 | 1.03125 | 1 | 1 | 是 |
| 31 / 32 | 0.96875 | 0 | 0 | 是 |
| -1 / 32 | -0.03125 | 0 | **-1** | **否** |
| -32 / 32 | -1 | -1 | -1 | 是（整除无余数） |
| -33 / 32 | -1.03125 | -1 | **-2** | **否** |

规律：当 `a` 为负数且除法**有余数**时，C# 的结果比 floor 大了 1。

> **正数区域两种取整结果相同，所以测试阶段通常不会发现这个 bug**——只有当相机移动到负坐标区域时，Chunk 切换才会出异常。

**FloorDiv 实现**：

```csharp
private static int FloorDiv(int a, int b)
{
    int q = a / b;                // C# 的向零取整
    if (a % b != 0                // 条件 1：除法有余数
        && ((a ^ b) < 0))         // 条件 2：a 和 b 异号（结果为负小数）
        q--;                      // 向零取整偏高了 1，减 1 得到 floor
    return q;
}
```

两个修正条件缺一不可：

- **条件 1** `a % b != 0`：如果除尽（如 -32/32 = -1 余 0），向零取整和 floor 结果相同，无需修正
- **条件 2** `(a ^ b) < 0`：XOR 的最高位（符号位）为 1 表示两数异号。因为 `b`（ChunkSize）永远为正，这里等价于 `a < 0`。用 XOR 写法更通用——即使 `b` 为负数也能正确工作

验证示例：`FloorDiv(-1, 32)` -> q = 0, 余数 -1 != 0, 异号 -> q-- -> **-1**。`FloorDiv(-32, 32)` -> q = -1, 余数 0 -> 不修正 -> **-1**。`FloorDiv(-33, 32)` -> q = -1, 余数 -1 != 0, 异号 -> q-- -> **-2**。`FloorDiv(33, 32)` -> q = 1, 两正数 -> 不修正 -> **1**。

**结论**：所有 `GridPos` -> `ChunkPos` 的转换必须通过 `CoordinateUtility.FloorDiv`，不能用 C# 原生 `/` 运算符。

### 验收标准

- 任意 GridPos 经 WorldPos 再转回 GridPos，结果与原值一致
- 转换方法不产生 GC 分配
- 边缘坐标（负坐标、大坐标）处理正确

---

## 1.2 WorldGrid 空间数据库

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Grid/CellFlags.cs`
- `Assets/Game/Scripts/Simulation/World/Grid/Cell.cs`
- `Assets/Game/Scripts/Simulation/World/Grid/WorldGrid.cs`

### 实现细节

1. `CellFlags` 使用 `[Flags]` 枚举：`Walkable`、`Occupied`、`HasTree`、`HasBuilding`、`HasRoad`

2. `Cell` 结构体：

   ```csharp
   public struct Cell
   {
       public int TreeId;
       public int BuildingId;
       public int RoadId;
       public CellFlags Flags;
   }
   ```

3. `WorldGrid` 使用一维数组或字典 backing，提供空间查询

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

边界外查询返回默认 Cell（不可行走），不抛异常。

### 验收标准

- 读写 10000 次 Cell 无异常
- 占用标记设置后 `IsOccupied` 返回 true
- 边界外查询不抛异常

---

## 1.3 WorldData 权威数据层

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Data/WorldData.cs`
- `Assets/Game/Scripts/Simulation/World/Data/IWorldEntity.cs`
- `Assets/Game/Scripts/Simulation/World/Data/IdGenerator.cs`
- `Assets/Game/Scripts/MiniGame_Scripts/Model/GameWorldModel.cs`

### 生命周期管理

WorldData **不使用单例模式**，而是由 `GameWorldModel`（QFramework Model）持有：

- `GameWorldModel` 继承 `AbstractModel`，通过 QFramework 架构访问
- 进入地图场景时，调用 `InitWorldData()` 创建 WorldData 实例
- 离开地图场景时，调用 `ClearWorldData()` 销毁 WorldData，释放内存
- 下游系统通过 `this.GetModel<GameWorldModel>().WorldData` 访问

这样做的好处：
- WorldData 的生命周期与地图场景绑定，避免场景切换后残留旧数据
- 符合 QFramework 的 Model 规范，数据统一管理
- 易于测试——可以直接 new WorldData() 而不依赖单例初始化

### 实现细节

1. `WorldData` 持有按 ID 索引的字典：`Dictionary<int, TreeData> Trees`、`Buildings`、`Roads`、`Workers`

2. `IdGenerator` 统一生成递增 `int` ID，从 1 开始

3. 提供事件回调：

   | 实体类型 | 事件 |
   |---------|------|
   | Tree | `TreeAdded`、`TreeRemoved`、`TreeModified` |
   | Building | `BuildingAdded`、`BuildingRemoved`、`BuildingModified` |
   | Road | `RoadAdded`、`RoadRemoved` |
   | Worker | `WorkerAdded`、`WorkerRemoved`、`WorkerModified` |

4. 可结合 QFramework 的 `SendEvent<T>()` 进行跨模块通知，但 Tick 热路径优先使用 C# 原生事件避免 GC

### 对外接口

```csharp
// WorldData 本身是纯数据类，不含单例
public class WorldData
{
    public int CreateTree(TreeData data);
    public TreeData GetTree(int id);
    public void RemoveTree(int id);
    public void ModifyTree(int id, Action<TreeData> modifier);
    // 建筑、道路、工人类似
}

// GameWorldModel 持有 WorldData，通过 QFramework 访问
public class GameWorldModel : AbstractModel
{
    public WorldData WorldData { get; private set; }
    public WorldGrid WorldGrid { get; private set; }

    protected override void OnInit() { }

    /// <summary>进入地图时调用，创建 WorldData 和 WorldGrid 实例。</summary>
    public void InitWorldData(Terrain terrain);

    /// <summary>离开地图时调用，销毁实例释放内存。</summary>
    public void ClearWorldData();
}

// 下游系统访问方式
public class SomeSystem : AbstractSystem
{
    protected override void OnInit()
    {
        var model = this.GetModel<GameWorldModel>();
        var worldData = model.WorldData;
        var tree = worldData.GetTree(42);
    }
}
```

### 验收标准

- ID 唯一且递增
- 增删改事件正确触发
- 删除后查询返回 null/默认值
- InitWorldData/ClearWorldData 正确管理生命周期

---

## 1.3a WorldData 与 WorldGrid 协作机制

本节描述 WorldData（权威数据层）与 WorldGrid（空间数据库）之间的协作关系：如何通过 `int ID` 建立双向索引，以及增删实体时两边如何同步更新。

### 整体架构定位

两个系统是**互补的分层设计**，各管一件事：

```
┌─────────────────────────────────────────────────────────────┐
│                     上游业务系统                              │
│  (TreeLifecycleSystem, BuildingPlacementSystem, 寻路等)      │
└──────────┬──────────────────────────────┬───────────────────┘
           │                              │
     按ID查实体详情                    按坐标查空间状态
           │                              │
           ▼                              ▼
  ┌─────────────────┐          ┌──────────────────────┐
  │   WorldData      │          │     WorldGrid         │
  │  (权威数据层)     │          │   (空间数据库)         │
  │                  │          │                      │
  │ Dictionary<int,  │◄──ID反查──│ Dictionary<GridPos,   │
  │   TreeData>      │          │   Cell>               │
  │ Dictionary<int,  │          │                      │
  │   BuildingData>  │          │ Cell.TreeId = 42     │
  │ Dictionary<int,  │          │ Cell.BuildingId = 7  │
  │   RoadData>      │          │ Cell.Flags = HasTree │
  │                  │          │        | Occupied    │
  │ IdGenerator      │          │                      │
  │ (递增ID, 从1开始)  │          │ 边界管理 + 可行走判断   │
  └─────────────────┘          └──────────────────────┘
     "实体是什么"                   "格子上有什么"
```

### 职责划分

| | WorldData | WorldGrid |
|---|---|---|
| **索引方式** | 按 `int ID` 索引 | 按 `GridPos`（坐标）索引 |
| **存什么** | 实体的**完整数据**（位置、状态、HP、类型等） | 格子的**空间快照**（标记位 + 实体ID） |
| **回答的问题** | "ID=42的树是什么状态？HP多少？" | "坐标(5,3)的格子能走吗？被谁占了？" |
| **数据结构** | `Dictionary<int, TreeData/BuildingData/RoadData>` | `Dictionary<GridPos, Cell>` |
| **角色** | 权威数据源（Source of Truth） | 空间索引/快速查询缓存 |

### Cell 中的 ID 如何反查 WorldData

这是两者协作的核心纽带。以树为例，查询分两步：

**第一步：空间查询（WorldGrid）**

```csharp
// 查坐标 (5, 3) 的格子上有什么
Cell cell = worldGrid.GetCell(new GridPos(5, 3));

// 快速判断：这格有树吗？
bool hasTree = cell.HasFlag(CellFlags.HasTree);  // 一次位运算，O(1)

// 拿到树的 ID
int treeId = cell.TreeId;  // 比如 = 42
```

**第二步：ID 反查完整数据（WorldData）**

```csharp
// 通过 GameWorldModel 获取 WorldData
var model = this.GetModel<GameWorldModel>();
TreeData tree = model.WorldData.GetTree(treeId);
// 现在拥有完整数据：
//   tree.Position    — 世界坐标
//   tree.State       — Mature / Marked / Stump...
//   tree.HP          — 剩余血量
//   tree.TreeTypeId  — 树种类型
//   tree.Size        — 当前大小
```

**封装快捷方式**——WorldGrid 已提供组合查询：

```csharp
// 一步到位：查某格子上指定类型的实体ID
int entityId = worldGrid.GetEntityAt(pos, EntityType.Tree);
// 然后再去 WorldData 查详情
```

Cell 中存 ID 而非 bool 的三个原因：

1. 通过 `WorldData.GetTree(id)` 反查实体的完整数据
2. 支持"替换"操作（先删除旧树，再种新树）
3. 避免在热路径中做额外的 ID 查找

### 增删实体时的双向同步

WorldData 和 WorldGrid 必须保持一致，否则会出现"数据说有树但格子上没标记"或反之的 bug。

#### 添加实体（以种树为例）

```
调用方                      WorldData                    WorldGrid
  │                            │                            │
  │──CreateTree(treeData)─────►│                            │
  │                            │ 1. IdGenerator 分配 ID=42   │
  │                            │ 2. _trees[42] = treeData    │
  │                            │ 3. 触发 TreeAdded(42) 事件  │
  │                            │        │                    │
  │                            │        └──► 业务系统收到通知  │
  │──写空间索引────────────────────────────────────────────►│
  │   grid.SetFlag(pos, HasTree | Occupied, true)           │
  │   var cell = grid.GetCell(pos);                         │
  │   cell.TreeId = 42;                                     │
  │   grid.SetCell(pos, cell);                              │
  │                            │                            │
  │◄──返回 treeId=42───────────│                            │
```

#### 删除实体（以砍树为例）

```
调用方                      WorldData                    WorldGrid
  │                            │                            │
  │──RemoveTree(42)───────────►│                            │
  │                            │ 1. _trees.Remove(42)        │
  │                            │ 2. 触发 TreeRemoved(42)     │
  │                            │        │                    │
  │                            │        └──► PathGraphUpdater │
  │                            │             收到通知，更新寻路 │
  │──清空间索引────────────────────────────────────────────►│
  │   var cell = grid.GetCell(pos);                         │
  │   cell.TreeId = 0;         // 清除 ID                    │
  │   cell.ClearFlag(HasTree | Occupied);  // 清除标记        │
  │   grid.SetCell(pos, cell);                              │
```

#### 建筑的特殊性：多格占用

建筑和树不同——树只占 1 格，建筑占 `SizeX × SizeZ` 的矩形区域：

```
放置一个 3×2 建筑，Origin=(5,3)，ID=7

WorldData:                    WorldGrid (需要写 6 个格子):
_buildings[7] = BuildingData   (5,3) (6,3) (7,3)
  Origin=(5,3)                 (5,4) (6,4) (7,4)
  SizeX=3, SizeZ=2
  State=Planned               每个格子都要:
  BuildingTypeId=...            cell.BuildingId = 7
                                cell.SetFlag(HasBuilding | Occupied)
```

### 事件驱动的同步机制

WorldData 设计了完整的事件体系来通知变更，下游系统通过订阅事件实现解耦同步：

```csharp
public class WorldData
{
    public event Action<int> TreeAdded, TreeRemoved, TreeModified;
    public event Action<int> BuildingAdded, BuildingRemoved, BuildingModified;
    public event Action<int> RoadAdded, RoadRemoved;
    public event Action<int> WorkerAdded, WorkerRemoved, WorkerModified;
}
```

事件订阅者及其职责：

```
WorldData 事件
    │
    ├──► PathGraphUpdater
    │      收到 TreeRemoved → 更新寻路图，该格恢复可行走
    │      收到 BuildingAdded → 更新寻路图，footprint 区域不可行走
    │
    ├──► TreeViewSystem
    │      收到 TreeAdded → 生成 TreeView GameObject
    │      收到 TreeRemoved → 回收 TreeView 到对象池
    │
    ├──► BuildingViewManager
    │      收到 BuildingAdded → 生成建筑 View（脚手架）
    │      收到 BuildingModified → 更新建造进度视觉
    │
    └──► 其他下游系统...
```

> **性能注意**：Tick 热路径优先用 C# 原生事件（`Action<int>`）而非 QFramework 的 `SendEvent<T>()`，避免 GC 分配。

### 为什么不合并成一个系统？

分治设计的关键动机是**性能**——两种查询场景对数据结构的要求完全不同：

| 查询场景 | 用 WorldGrid | 用 WorldData |
|---------|-------------|--------------|
| "这个坐标能走吗？" | ✅ 一次字典查找 + 位运算 | ❌ 需遍历所有实体判断坐标 |
| "这棵树HP多少？" | ❌ Cell 只存 ID，没有 HP | ✅ 一次字典查找 |
| 寻路每帧检查几百个格子 | ✅ 16字节Cell，极速 | ❌ 不可行 |
| 实体属性修改 | ❌ Cell 不存业务数据 | ✅ 直接改 dict value |

WorldGrid 是为空间查询优化的"瘦索引"（16 字节/格），WorldData 是为业务逻辑服务的"胖数据"（TreeData 含 Position、HP、State 等数十字节）。两者通过 `int ID` 这个轻量桥梁连接。

---

## 1.4 Chunk 与 ChunkSystem

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Chunk/Chunk.cs`
- `Assets/Game/Scripts/MiniGame_Scripts/System/ChunkSystem.cs`

### QFramework 架构接入

ChunkSystem 继承 `AbstractSystem`，遵循项目中 ActorSystem 的设计模式：

| 生命周期方法 | 调用时机 | 职责 |
|-------------|---------|------|
| `OnInit()` | System 注册时 | 初始化内部状态 |
| `AfterSceneInit()` | 进入地图场景后 | 订阅 Update 事件，启动 Chunk 检测 |
| `OnUpdate(UpdateEvent)` | 每帧 | 检测相机焦点移动，触发 Chunk 激活/停用 |
| `ClearDataAfterChangeLevel()` | 离开地图时 | 清理状态，取消事件订阅 |

### 实现细节

1. `Chunk` 是纯数据结构（`readonly struct`），大小固定 32x32
2. `ChunkSystem`：
   - 在 `AfterSceneInit()` 中订阅 `UpdateEvent`，开始每帧检测
   - 每帧从 `IsometricCameraController.FocusPosition` 获取焦点
   - 将焦点转换为 `ChunkPos`，与上一帧的 Chunk 中心比较
   - 当中心 Chunk 变化时，计算新的 3x3 激活窗口，触发 `ChunkActivated` / `ChunkDeactivated` 事件
   - 可选：加入滞后/惰性切换，避免边界抖动

### 对外接口

```csharp
public class ChunkSystem : AbstractSystem
{
    public int ChunkSize => CoordinateUtility.ChunkSize; // 32
    public int ActiveRadius => 1; // 3x3 => radius 1

    // 事件
    public event Action<ChunkPos> ChunkActivated;
    public event Action<ChunkPos> ChunkDeactivated;

    // QFramework 生命周期
    protected override void OnInit();
    public void AfterSceneInit();
    public void OnUpdate(UpdateEvent e);
    public void ClearDataAfterChangeLevel();

    // 查询接口
    public bool IsChunkActive(ChunkPos pos);
    public ChunkPos GetChunkForCell(GridPos pos);
    public IEnumerable<GridPos> GetCellsInChunk(ChunkPos pos);
}
```

### 访问方式

```csharp
// 下游系统通过 QFramework 访问
var chunkSystem = this.GetSystem<ChunkSystem>();
bool active = chunkSystem.IsChunkActive(someChunkPos);
```

### 验收标准

- 进入地图后 OnUpdate 开始运行
- 移动焦点时正确激活/停用 3x3 Chunk
- 边界切换时事件只触发一次
- 离开地图时正确清理状态
- 能遍历指定 Chunk 内的所有 Cell

---

## 1.5 Terrain 树木初始同步

### 产出文件

- `Assets/Game/Scripts/Simulation/World/Terrain/TerrainDataReader.cs`
- `Assets/Game/Scripts/MiniGame_Scripts/System/TerrainSystem.cs`

### 实现细节

1. `TerrainDataReader` 为纯数据类（不继承 MonoBehaviour），由 `TerrainSystem` 持有
2. `TerrainSystem` 继承 `AbstractSystem`，负责初始化和操作 TerrainDataReader
3. 在 `AfterSceneInit()` 时读取 `Terrain.terrainData.treeInstances`
4. 将每个 `TreeInstance.position` 转换为 `GridPos`
5. 创建 `TreeData` 并通过 `WorldData` 注册
6. 在 `WorldGrid` 对应 Cell 写入 `TreeId` 和 `Occupied/HasTree` 标记
7. 此步骤只读 Terrain，不修改 Terrain

### 对外接口

```csharp
// TerrainDataReader：纯数据类，由 TerrainSystem 持有
public class TerrainDataReader
{
    public int ReadTreesFromTerrain(Terrain terrain, WorldData worldData, WorldGrid worldGrid);
}

// TerrainSystem：QFramework System，驱动 TerrainDataReader
public class TerrainSystem : AbstractSystem
{
    protected override void OnInit();            // 创建 TerrainDataReader 实例
    public void AfterSceneInit();                // 执行树木同步
    public void ClearDataAfterChangeLevel();     // 清理状态
    public Terrain CurrentTerrain { get; }       // 当前场景 Terrain 引用
    public TerrainDataReader Reader { get; }     // TerrainDataReader 实例
}
```

### 验收标准

- Terrain 上有 N 棵树，则生成 N 个 TreeData
- TreeData 的 GridPos 与世界位置对应正确
- WorldGrid 对应 Cell 标记为 HasTree + Occupied
- TerrainSystem 在 MiniGame 中注册，通过 this.GetSystem<TerrainSystem>() 访问

---

## 提供给下游 Phase 的契约

| 系统 | 提供内容 | 访问方式 |
|------|----------|----------|
| `GameWorldModel` | WorldData 和 WorldGrid 的持有者 | `this.GetModel<GameWorldModel>()` |
| `WorldData` | 所有实体权威数据、增删改事件 | `model.WorldData` |
| `WorldGrid` | 空间查询、占用管理、可行走判断 | `model.WorldGrid` |
| `ChunkSystem` | 3x3 Chunk 激活/停用事件 | `this.GetSystem<ChunkSystem>()` |
| `TerrainSystem` | Terrain 数据读取、树木初始同步 | `this.GetSystem<TerrainSystem>()` |
| `CoordinateUtility` | 坐标转换 | 静态方法直接调用 |

## 阻塞 downstream 的风险

- `WorldGrid` 性能差会影响寻路和建筑放置
- `WorldData` 事件设计不合理会导致系统耦合或 GC
- 坐标转换有 bug 会导致所有空间逻辑错误
