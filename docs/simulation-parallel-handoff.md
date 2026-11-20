# Simulation 并行开发交接文档

> 版本 **v1.0** ｜ 日期 2026-11-20 ｜ 适用 Unity **6000.3.2f1**（读自 `ProjectSettings/ProjectVersion.txt`）
> 状态：**P0 基线已冻结并可运行；P1–P6 待派发，本轮不开发**
> 路径约定：下文 `Simulation/` = `Assets/Game/Scripts/Simulation/`，`MiniGame_Scripts/` = `Assets/Game/Scripts/MiniGame_Scripts/`，`docs/` = 工程根 `docs/`。文档内不写机器绝对路径。

---

## 0. 这份文档怎么用

| 对象 | 怎么用 |
|---|---|
| 主程 / 派发人 | 按第 4 章把 WP 派给不同人；派发时改 `docs/design-freeze.json`（对应条目 `confirmed` 置 `true`，`version` +1）并登记负责人 |
| 各模块开发者 | 只看第 2 章（现有契约）+ 第 4 章自己那张任务卡 + 第 5 章（约定签名）+ 第 7 章（验收） |
| 所有人 | 三条铁律：**① 只改自己 WP 名下的新文件；② 共享基线文件只读；③ 完成后在实现文件里写 `// @trace 编号`** |

配套文件：

| 文件 | 用途 |
|---|---|
| `docs/design-freeze.json` | 设计冻结清单 / 任务派发清单（21 条，`status=frozen`） |
| `docs/unity-project-spec.json` | 工程自检规格（必需目录已对齐本工程实际结构，含 1 条抑制规则） |
| `docs/unity-baseline.json` | 历史欠债基线（39 个指纹，只判新增） |
| `docs/unity-check-report.json` | 本轮自检 JSON 报告（证据） |

---

## 1. 结论与现状

### 1.1 一句话

这是一个 Unity 6 的**殖民生存建造**格子制模拟经营原型。工程外壳（YooAsset 热更 / Luban 配置 / QFramework / 场景流 / 相机 / 输入 / 对象池）已能跑通；`Simulation` 是纯 C# 数据内核。**P0 已完成基础设施泛化**，从「四类实体写死」改为「开放类型 + 单格单实体」；玩法主体（P1–P6）尚未开工，14 个目录为空。

### 1.2 P0 改前 vs 改后（已落盘，未改动任何外壳文件）

| 维度 | 改前 | 改后（当前基线） |
|---|---|---|
| 实体类型 | `enum EntityType { Tree, Building, Road, Worker }` 封闭 4 类 | `static class EntityTypes` 开放 int 常量，`0=None`，`1..7` 已占用 |
| 格子 | `Cell { TreeId, BuildingId, RoadId, Flags }`，一格可记三类 | `Cell { OccupantId, TypeId, Flags, Reserved }`，**一格一个主实体**，仍 16 字节 |
| 实体仓储 | 4 张 `Dictionary<int,T>` + 4 套 CRUD + 11 个分类事件 | 1 张主表 + 按 TypeId 分组索引 + 1 套通用 CRUD + 3 个通用事件 |
| 类型查询 | `GetEntityAt(pos, EntityType)` switch 分支 | `GetEntityAt(pos, int typeId)`，新增 `GetOccupantAt` / `GetTypeIdAt` |
| Terrain 导入 | `CreateTree` + `cell.TreeId` | `Create` + `cell.SetOccupant(id, typeId, flags)` |

改动范围：`Simulation/` 内 9 个文件。`MiniGame_Scripts/` 桥接层、`GameScene.unity`、项目设置**一行未动**。静态核查：旧 API 全仓零残留，非代码资产无 `EntityType` / 旧字段序列化引用。

### 1.3 已冻结的决策（不得擅自回退）

1. 保留基础层与项目外壳，**不整块删除、不换架构**（用户已确认）。
2. 世界形态仍为**格子制**（`CellSize=1m`、`ChunkSize=32`）。
3. 核心循环 = **殖民生存建造**：工人 + Job 任务队列 + 采集 / 建造（类 RimWorld / 放逐之城）。
4. 实体清单：`0=None`｜`1 树`｜`2 矿点/岩石`｜`3 多格建筑`｜`4 工人`｜`5 落地物品`｜`6 建造工地`｜`7 道路(保留)`。
5. **工人不写 WorldGrid**（移动实体，只在 `WorldData` 中，由寻路动态查路）。
6. **物品初期只进库存、不占格**；真需要「地上堆料」再加 overlay 层，不退回多槽位。
7. 多格建筑**自己缓存 `OccupiedCells`**，不做全图反查；放置时整片校验 `IsWalkable && !Occupied`。
8. 事件**只传 ID**，用 C# 原生 `event Action<int>`，不用 QFramework `SendEvent`。
9. `TypeId` 常量值**进入存档后不可改动**（P6 依赖）。

### 1.4 项目外壳（只读，禁止在这些文件里加玩法逻辑）

| 文件 | 作用 |
|---|---|
| `MiniGame_Scripts/Model/GameWorldModel.cs` | 持有 `WorldData` / `WorldGrid`，`InitWorldData(Terrain)` / `ClearWorldData()` |
| `MiniGame_Scripts/Command/AfterSceneInitLogicCmd.cs` | 场景加载后 `InitWorldData` → `TerrainSystem.AfterSceneInit()` |
| `MiniGame_Scripts/System/TerrainSystem.cs` | 驱动 Terrain 树木导入 |
| `MiniGame_Scripts/System/ChunkSystem.cs` | 每帧算 3×3 Chunk 激活窗口，发 `ChunkActivated/Deactivated` |
| `MiniGame_Scripts/Controller/SceneFlowController.cs` | 启动必经，发 `AfterSceneInitLogicCmd` |
| `MiniGame_Scripts/MiniGame.cs` | 注册 Model / System |
| `MiniGame_Scripts/Controller/Camera/IsometricCameraController.cs` | 依赖 `GameInput`，相机平移/缩放/旋转 |
| `Simulation/Input/GameInput.cs` | New Input System 内联定义 InputActionMap |
| `Simulation/World/Terrain/TerrainDataReader.cs` | Terrain → 实体 + 格子（P0 已适配） |

---

## 2. 当前代码契约（并行开发的接口基准）

> 以下**全部是已存在的真实 API**，签名以源码为准。开发者直接调用，不要自行包装第二套。

### 2.1 `EntityTypes`（`Simulation/World/Grid/EntityType.cs`）

| 常量 | 值 | 说明 |
|---|---|---|
| `None` | 0 | 无实体，保留值；与 `Cell.OccupantId=0`、`IdGenerator` 从 1 起一致 |
| `Tree` | 1 | 树 / 可砍伐植被（Terrain 导入已跑通） |
| `Rock` | 2 | 矿点 / 岩石（可采集） |
| `Building` | 3 | 多格建筑 |
| `Worker` | 4 | 工人，**不写网格** |
| `Item` | 5 | 落地物品（P3 起，初期不占格） |
| `ConstructionSite` | 6 | 建造工地（P2 中间态） |
| `Road` | 7 | 保留类型，P4 连通性阶段启用 |

`TypeId` 只回答「是什么」；「占几格 / 能否通行」由实体自身数据 + `CellFlags` 决定。

### 2.2 `Cell`（`Simulation/World/Grid/Cell.cs`，struct，16 字节）

| 成员 | 类型 | 说明 |
|---|---|---|
| `OccupantId` | `int` | 主实体 ID，`0` = 无实体 |
| `TypeId` | `int` | 开放类型 ID（权威分类） |
| `Flags` | `CellFlags` | 位掩码状态 |
| `Reserved` | `int` | 预留 4 字节，供未来 overlay 层 |
| `HasOccupant` | `bool` | `OccupantId != 0`（比 `IsOccupied` 更可靠，不依赖 Flags 是否被正确维护） |
| `IsWalkable` / `IsOccupied` | `bool` | 读 Flags |
| `SetFlag` / `ClearFlag` / `HasFlag` | 方法 | 位运算封装 |
| `GetEntityId(int typeId)` | `int` | 类型匹配才返回 `OccupantId`，否则 0 |
| `SetOccupant(int id, int typeId, CellFlags extraFlags)` | `void` | 一次性写 ID + 类型 + 派生位（**只做 OR，不清旧位**） |
| `ClearOccupant()` | `void` | 清 ID 与 TypeId；Flags 需调用方自行清 |

**⚠️ 高频坑**：`Flags` 里的 `HasTree` / `HasBuilding` / `HasRoad` 是**派生缓存位**，`TypeId` 才是权威。把格子从一种实体替换成另一种（砍树建楼、工地建成）时，必须 `ClearFlag` 掉旧的 `HasX`，否则派生位与新 `TypeId` 不一致。

### 2.3 `IWorldEntity`（`Simulation/World/Data/IWorldEntity.cs`）

```csharp
public interface IWorldEntity
{
    int Id { get; set; }
    GridPos Position { get; set; }
    int TypeId { get; }
}
```

实现类必须是 **`class`**（`WorldData.Get<T>` / `Modify<T>` 有 `where T : class, IWorldEntity` 约束）。现有 4 个最小骨架：`TreeData` / `BuildingData` / `RoadData` / `WorkerData`。注意 `TreeTypeId`、`BuildingTypeId`、`RoadTypeId` 是**配置 ID**（决定外观/产量），与 `TypeId`（实体分类）不是一回事。

### 2.4 `WorldData`（`Simulation/World/Data/WorldData.cs`）完整公开 API

| 签名 | 返回 | 行为 |
|---|---|---|
| `int Create(IWorldEntity entity)` | 新 ID | 分配 ID、写主表与类型索引、触发 `EntityAdded(id)`；传 null 返回 0 且不触发事件 |
| `IWorldEntity Get(int id)` | 接口 | 不存在返回 null |
| `T Get<T>(int id)` | `T` | `where T : class, IWorldEntity`；类型不符返回 null |
| `int GetTypeId(int id)` | int | 不存在返回 `EntityTypes.None` |
| `bool Remove(int id)` | bool | 同步清主表与类型索引（分组空则移除分组），**移除后**触发 `EntityRemoved(id)` |
| `void Modify(int id, Action<IWorldEntity> modifier)` | void | 存在才执行并触发 `EntityModified(id)` |
| `void Modify<T>(int id, Action<T> modifier)` | void | 类型安全版 |
| `IEnumerable<IWorldEntity> AllEntities` | 属性 | 值集合视图（只读） |
| `IEnumerable<IWorldEntity> GetAllOfType(int typeId)` | — | 无该类型时返回空集合 |
| `IEnumerable<T> GetAll<T>()` | — | 按 CLR 类型过滤 |
| `int Count` / `int CountOfType(int typeId)` | int | 统计 |
| `void Clear()` | void | 清两张表、重置 ID 生成器、**清空全部事件订阅** |
| `event Action<int> EntityAdded / EntityRemoved / EntityModified` | 事件 | 只传 ID |

### 2.5 `WorldGrid`（`Simulation/World/Grid/WorldGrid.cs`）完整公开 API

| 签名 | 说明 |
|---|---|
| `WorldGrid(GridPos min, GridPos max)` / `WorldGrid(Terrain)` | 构造；后者按 Terrain 尺寸算边界 |
| `GridPos MinPos` / `MaxPos` | 边界 |
| `bool IsInBounds(GridPos)` | 边界检查 |
| `Cell GetCell(GridPos)` | 边界内未设置返回 `Flags=Walkable, OccupantId=0`；边界外返回 `default(Cell)`（不可走） |
| `void SetCell(GridPos, Cell)` | 边界外静默忽略 |
| `bool IsWalkable(GridPos)` / `IsOccupied(GridPos)` | 便捷查询 |
| `int GetOccupantAt(GridPos)` | 无实体返回 0 |
| `int GetTypeIdAt(GridPos)` | 无实体返回 `EntityTypes.None` |
| `int GetEntityAt(GridPos, int typeId)` | 类型匹配才返回 ID |
| `void SetFlag(GridPos, CellFlags, bool)` | 封装 GetCell → 位运算 → SetCell |
| `int CellCount` | 调试用 |

`Cell` 是 **struct**：修改必须 `var c = GetCell(pos); c.SetOccupant(...); SetCell(pos, c);`，不要就地改返回值。

### 2.6 `CellFlags`（`Simulation/World/Grid/CellFlags.cs`）

| 位 | 值 | 含义 |
|---|---|---|
| `None` | 0 | — |
| `Walkable` | `1<<0` | 地形可行走 |
| `Occupied` | `1<<1` | 有实体占据（通用快速判断） |
| `HasTree` | `1<<2` | 派生位 |
| `HasBuilding` | `1<<3` | 派生位 |
| `HasRoad` | `1<<4` | 派生位；道路 = `HasRoad \| Walkable`，**不设 Occupied** |
| bit 5–31 | — | 预留 |

### 2.7 事件契约

| 事件 | 载荷 | 触发时机 |
|---|---|---|
| `WorldData.EntityAdded(int id)` | 仅 ID | 实体入表后 |
| `WorldData.EntityRemoved(int id)` | 仅 ID | 实体**移出表后**（订阅方 `Get(id)` 返回 null 属预期） |
| `WorldData.EntityModified(int id)` | 仅 ID | `Modify` 执行后 |
| `ChunkSystem.ChunkActivated(ChunkPos)` | ChunkPos | 3×3 窗口新增 Chunk |
| `ChunkSystem.ChunkDeactivated(ChunkPos)` | ChunkPos | 窗口移除 Chunk |

跨系统通知**一律走事件**，不得反向直接引用；`WorldData` 只在 Unity 主线程访问。

---

## 3. 需求编号与冻结清单

清单在 `docs/design-freeze.json`：`version=1`、`status=frozen`、`frozenAt=2026-11-20`、21 条（已确认 5 / 待派发 16）。规格已打开 `requireFrozenDesign`，未冻结或缺清单会被自检脚本拦下。

### 3.1 编号表

| 编号 | 类型 | 状态 | 内容 |
|---|---|---|---|
| `MECH-001`–`005` | mechanic | **已确认** | P0 基线：单占用模型 / 开放类型 ID / 统一仓储 / Terrain 导入适配 / WorldGrid 类型查询 |
| `MECH-010`–`019` | mechanic | 待派发 | P1–P6 与治理，共 10 条（见第 4 章 WP 映射） |
| `UI-001` / `UI-002` | ui | 待派发 | 建造预览交互 / 主 HUD |
| `ART-001` | art | 待派发 | 状态色板与图标集 |
| `NUM-001` | numeric | **待确认** | 建造/采集耗时、Worker 速度等可调数值 |
| `PLAT-001` | platform | **待确认** | 目标平台与输入方案统一 |
| `ART-002` | art | **待确认** | 美术风格锚点与禁忌 |

### 3.2 `@trace` 规则 —— 覆盖率就是进度看板

- 实现文件里写一行 `// @trace MECH-011`（可逗号分隔多个编号），**只写在真正实现该条目的文件里**。
- 自检脚本统计：覆盖率 = 被工程引用的**已确认**条目 ÷ 已确认条目总数。
- 派发流程：主程把某条 `confirmed` 改为 `true` → `version` +1 → 开发者完成后补 `// @trace` → 覆盖率上升。
- 编号一经使用不得复用；废弃条目保留并标记，不删号。需求变更必须同步改清单并递增版本，**禁止只改代码**。

**当前状态（如实）**：覆盖率 `0/5 = 0%`。本轮遵守「不开发」未改动任何代码，P0 的 5 条属于**已实现但漏标**，不是没做。首批开发者顺手补以下 5 处即可到 100%：

| 文件（相对 `Simulation/`） | 补的标记 |
|---|---|
| `World/Grid/Cell.cs` | `// @trace MECH-001` |
| `World/Grid/EntityType.cs` | `// @trace MECH-002` |
| `World/Data/WorldData.cs` | `// @trace MECH-003` |
| `World/Terrain/TerrainDataReader.cs` | `// @trace MECH-004` |
| `World/Grid/WorldGrid.cs` | `// @trace MECH-005` |

（`World/Data/IWorldEntity.cs` 可补 `MECH-002, MECH-003`，非必需。）

---

## 4. 模块拆分与并行边界

### 4.1 工作包总表

> **波次 = 依赖顺序，不是时间排期。** 同一波次内可同时开工；跨波次按契约编码也可提前写，但联调需等上游。

| WP | 需求编号 | 模块 | 波次 | 依赖 | 产出目录（全部**新建文件**） |
|---|---|---|---|---|---|
| WP-01 | `MECH-010` | 实体数据补全与状态枚举 | A | P0 契约 | `World/Data/`、`Trees/`、`Buildings/`、`Workers/`、`Items/` |
| WP-02 | `MECH-011` | 多格放置校验与占格管理 | A | P0 契约 | `World/Grid/GridOccupancy.cs`、`Buildings/PlacementValidator.cs` |
| WP-03 | `MECH-012` | Job 队列、优先级与 Steps | A | P0 契约 | `Jobs/`、`Jobs/Steps/` |
| WP-08 | `MECH-017` | Chunk 激活接对象池与实体视图 | A | `IWorldEntity` 接口 + `ChunkSystem` 事件 | `View/`、`View/Pool/` |
| WP-10 | `UI-002` | 主 HUD 信息架构与面板骨架 | A | 数据用接口注入 | `UI/`（HUD 部分） |
| WP-04 | `MECH-013` | Worker 状态机与任务执行 | B | WP-03 契约 | `Workers/`、`Jobs/Steps/`（Step 实现） |
| WP-06 | `MECH-015` | 物品 / 库存 / Haul 搬运 | B | WP-03 契约 | `Items/`、`Storage/` |
| WP-07 | `MECH-016` | 格点寻路与 AstarPath 去留 | B | `WorldGrid.IsWalkable` | `Pathfinding/`、`Profiling/` |
| WP-09 | `UI-001` | 格线 / 悬停 / 建造预览 | B | WP-02 契约 + WP-08 | `UI/`（交互部分）、`Camera/` |
| WP-05 | `MECH-014` | 采集与建造闭环串联 | C | WP-01/02/03/04 | `Simulation/`（编排层，见 4.4） |
| WP-11 | `MECH-018` | 存档：序列化与恢复 | C | **WP-01 实体字段冻结后** | `SaveLoad/`、`Farming/`（不占用则留空） |
| WP-12 | `MECH-019` | 工程治理（需主程协调） | C | 需改共享文件 | `Input/`、`ProjectSettings/`、包清单 |
| — | `ART-001` | 状态色板 / 图标集 / 资源清单 | 并行 | 无 | 美术资源目录（由主程指定） |

### 4.2 依赖图

```
P0 基线（已冻结）
 ├─ 波次 A：WP-01  WP-02  WP-03  WP-08  WP-10   （彼此无依赖，只依赖 P0 契约）
 │            │      │      │      │
 │            │      │      ├─ WP-04（Worker FSM，按 IJob 契约）
 │            │      │      ├─ WP-06（库存/Haul，按 IJob 契约）
 │            │      ├─ WP-07（寻路，按 WorldGrid 契约）
 │            │      └─ WP-09（建造预览，按 GridOccupancy 契约）
 │            └─ WP-05（闭环串联，需 A+B 全部就绪）
 ├─ WP-11 存档：必须等 WP-01 把实体字段定死，否则序列化格式会返工
 └─ WP-12 治理：改共享文件，需先申请 + 递增冻结清单 version
```

### 4.3 文件归属表（防 Git 冲突的核心）

| 归属 | 文件 |
|---|---|
| **共享基线（只读，任何人不得直接改）** | `World/Grid/Cell.cs`、`CellFlags.cs`、`EntityType.cs`、`WorldGrid.cs`；`World/Data/IWorldEntity.cs`、`WorldData.cs`；`World/Chunk/Chunk.cs`；`World/Coordinates/*`；`Input/GameInput.cs`；`MiniGame_Scripts/` 全部桥接文件 |
| **共享文件变更流程** | 开发者提申请 → 主程评估 → 改 `docs/design-freeze.json`（`version` +1，注明变更）→ 主程改基线文件 → 全仓检索残留引用 → 全员同步 |
| WP-01 | `World/Data/*`（新增）、`Trees/`、`Buildings/`、`Workers/`、`Items/` |
| WP-02 | `World/Grid/GridOccupancy.cs`（新增，**不改** `WorldGrid.cs`）、`Buildings/PlacementValidator.cs` |
| WP-03 | `Jobs/`、`Jobs/Steps/`（接口与队列） |
| WP-04 | `Workers/`、`Jobs/Steps/`（Step 实现，与 WP-03 分文件） |
| WP-06 | `Items/`、`Storage/` |
| WP-07 | `Pathfinding/`、`Profiling/` |
| WP-08 | `View/`、`View/Pool/` |
| WP-09 / WP-10 | `UI/`（**按文件分工**：WP-09 占 `*Preview*`、`*Highlight*`、`*GridLine*`；WP-10 占 `*HUD*`、`*Panel*`）、`Camera/` |
| WP-11 | `SaveLoad/` |
| WP-12 | 需改共享文件时走上面的变更流程 |

同一目录多人共用时，**按文件名前缀划分**，不得两人改同一个文件。

### 4.4 任务卡（每张卡 = 可独立领取的最小交付）

#### WP-01 实体数据补全 `MECH-010` · 波次 A
- **目标**：把 4 个最小骨架补成可用实体；定义状态枚举与计时字段。
- **产出**：`GrowthState`（Sapling/Mature/Marked/Stump）、`BuildingState`（Planned/UnderConstruction/Complete/Demolished）、`WorkerState`（Idle/Walking/Working/Returning）、`RoadState`；各 Data 类补 HP / Size / GrowthTimer / ConstructionProgress / OwnerWorkerId / Inventory / Speed / `OccupiedCells` 缓存。
- **接口**：全部实现 `IWorldEntity`，`TypeId` 返回 `EntityTypes` 常量，不得改 `TypeId` 值。
- **不碰**：`WorldData.cs`、`Cell.cs`、`EntityType.cs`。
- **验收**：每个新实体能 `Create` → `Get<T>` → `Modify<T>` → `Remove` 跑通；`GetAllOfType` 计数正确；`OccupiedCells` 与 `SizeX×SizeZ` 一致。
- **日志标签**：`[EntityData]`。

#### WP-02 多格放置校验与占格管理 `MECH-011` · 波次 A
- **目标**：提供整片合法性校验 + 占格写入/释放，封装 P0 的派生位坑。
- **接口**：见 5.1 `GridOccupancy`。
- **硬约束**：`TryPlace` 整片校验 `IsWalkable && !Occupied` 后才写入；`Release` 必须 `ClearOccupant()` **并** `ClearFlag` 掉旧的 `HasX`；建筑替换场景先 Release 再 Place。
- **验收**：边界用例表 —— `1×1` 空地 / `3×2` 合法 / 有一格被占 → 整片拒绝且 `blockedCount` 正确 / 边界外 → 拒绝 / Release 后 `GetOccupantAt=0` 且 `HasX` 位已清 / 重复 Release 不抛异常。
- **日志标签**：`[GridOccupancy]`。

#### WP-03 Job 队列与 Steps `MECH-012` · 波次 A
- **目标**：`Job`（类型 / 目标实体或格子 / 状态 / 优先级）→ `JobQueue` 按优先级出队 → 拆成 `Steps: WalkTo → Work(timer) → Deliver`。
- **接口**：见 5.2。
- **不碰**：`WorldData.cs`（Job 不是 `IWorldEntity`，单独存 `JobQueue`，不要塞进世界仓储）。
- **验收**：优先级出队顺序正确；同优先级按入队顺序；`Cancel` 后不再出队；`Count` 一致；空队列 `TryDequeue` 返回 false。
- **日志标签**：`[JobQueue]`。

#### WP-04 Worker 状态机与任务执行 `MECH-013` · 波次 B
- **目标**：空闲 Worker 领 Job → 按 Steps 推进 → 完成回 Idle。
- **依赖**：只依赖 5.2 的 `IJob` / `JobQueue` / `IJobStep` **契约**，不依赖 WP-03 的具体实现类，因此可与 WP-03 并行写。
- **接口**：`WorkerState` 枚举；执行器 `void Tick(int workerId, float dt)`；Worker 数据从 `WorldData.Get<WorkerData>(id)` 取。
- **硬约束**：Worker **不写 WorldGrid**；移动位置按 WP-07 的 `IPathfinder` 契约查询（契约未就绪时用直线占位，但不得直接依赖 A* 插件类型）。
- **验收**：状态迁移表覆盖 Idle→Walking→Working→Returning→Idle；Job 取消/目标实体被删时不抛异常。
- **日志标签**：`[WorkerFSM]`。

#### WP-05 采集与建造闭环 `MECH-014` · 波次 C
- **目标**：串起「标记树/矿 → 生成采集 Job → Worker 执行 → 产出入库 → 生成建造 Job → 工地建成 → 替换为 Building」。
- **依赖**：WP-01/02/03/04 全部就绪；工地→建筑替换必须走 WP-02 的 Release + Place。
- **验收**：一条端到端链路日志完整（每个环节有标签）；中途删目标实体不崩；`WorldData.CountOfType` 前后符合预期。
- **日志标签**：`[GameplayLoop]`。

#### WP-06 物品 / 库存 / Haul `MECH-015` · 波次 B
- **目标**：产出先进库存，Haul 任务把物品从 A 转到 B。
- **硬约束**：**物品初期不占格**（`TypeId=Item` 只存在于 `WorldData`）；不得为了堆料改 `Cell` 结构。确需占格 → 提申请加 overlay 层。
- **验收**：库存增减守恒；Haul 取消后物品不丢失；`Items/` 与 `Storage/` 无跨 WP 直接引用。
- **日志标签**：`[Inventory]` / `[Haul]`。

#### WP-07 格点寻路 `MECH-016` · 波次 B
- **目标**：`IPathfinder` 按 `WorldGrid.IsWalkable` 求路；决定是否保留 `GameScene/Land` 上的 `AstarPath` 组件。
- **接口**：见 5.3。
- **待确认**：自研格点 A* vs 现成 A* 插件（见第 8 章）。**未拍板前不得删除 `AstarPath` 组件**，否则场景会留 Missing Script。
- **验收**：起点/终点不可走、无路径、同格、绕障碍 4 类用例；结果不修改 `result` 以外的状态；`Profiling/` 记录一次寻路耗时量级（不设性能红线，本轮只取数）。
- **日志标签**：`[Pathfinding]`。

#### WP-08 视图层与对象池 `MECH-017` · 波次 A
- **目标**：订阅 `ChunkActivated/Deactivated` 与 `WorldData` 三个事件，按 Chunk 生成/回收实体视图。
- **依赖**：只依赖 `IWorldEntity` 接口 + `ChunkSystem` 事件 + `WorldData` API；**不要依赖 WP-01 的具体 Data 类**（用 `TypeId` 分派），因此可先写骨架。
- **接口**：见 5.4。
- **价值**：`ChunkActivated/Deactivated` 目前算完无订阅者、每帧空转；本 WP 接上后流式加载才真正生效。
- **验收**：Chunk 进/出窗口时视图数量对称（无泄漏）；`EntityRemoved` 后视图回池；切场景 `Clear()` 后无残留视图。
- **日志标签**：`[WorldView]` / `[ViewPool]`。

#### WP-09 建造预览交互 `UI-001` · 波次 B
- **目标**：格线、悬停高亮、多格建造预览，**整体半透明 + 逐格合法性着色**（比整块变红/变绿更能指出哪几格被挡）。
- **依赖**：WP-02 的 `CanPlace` 契约；WP-08 的视图层（可先用线框占位）。
- **色板**：见 6.2。
- **验收**：预览随鼠标覆盖格数正确；非法格单独着色；确认建造时调用 `GridOccupancy.TryPlace`，失败不改世界数据；取消预览不留残影。
- **日志标签**：`[BuildPreview]`。

#### WP-10 主 HUD `UI-002` · 波次 A
- **目标**：UI 信息架构（资源 / 任务队列 / 选中实体 / 建造菜单）与面板骨架。
- **硬约束**：数据通过接口注入，**不得直接读 `WorldData` 单例或 `FindObjectOfType`**；现有 `UIRoot.prefab` / `LoadingView.prefab` / `Game.spriteatlas` / `bar01-02.png` 是仅有的 UI 资产，新增资源走 ART-002 规范。
- **验收**：面板可开关、无空引用；切场景后不持有旧世界引用。
- **日志标签**：`[HUD]`。

#### WP-11 存档 `MECH-018` · 波次 C
- **目标**：`WorldData` + `WorldGrid` 序列化与恢复。
- **前置硬门禁**：必须等 WP-01 把实体字段**冻结**（否则存档格式反复返工）。
- **接口**：见 5.5。
- **硬约束**：`TypeId` 常量值写入存档后不可改；`EntityAdded/Removed/Modified` 订阅在 `Clear()` 时会清空，恢复后需重新订阅；ID 生成器从 1 重置，恢复时按存档最大 ID 续接。
- **验收**：空世界 / 含多格建筑 / 含 Chunk 外格子 三类往返一致；损坏数据不崩溃并返回 false。
- **日志标签**：`[SaveSystem]`。

#### WP-12 工程治理 `MECH-019` · 波次 C（主程专属）
- **内容**：统一两套输入（`Simulation/Input/GameInput.cs` 内联定义 vs 根目录 `InputSystem_Actions.inputactions`，当前 `activeInputHandler=2` 即 Both）；清理 `ChunkSystem` 空转；卸载未使用的 DOTS Entities；清理 `Simulation/Simulation` 这个多余的嵌套空目录；处理 `UICircleImage.cs` 非 UTF-8 编码。
- **前置**：改共享文件 / 项目设置前必须走 4.3 的变更流程并递增冻结清单 `version`。
- **验收**：改完在编辑器里实测相机 WASD / 滚轮 / QE / 左右键仍可用；自检脚本新增问题 0 个。

---

## 5. 待实现接口契约（约定签名）

> **以下全部是「约定」，不是既有代码。** 签名一经确认不得由单个开发者擅自更改；变更需主程同意 → 改 `docs/design-freeze.json` 并 `version` +1 → 通知所有依赖方。不确定处标注「以官方文档当前版本为准」。

### 5.1 WP-02 `GridOccupancy`

```csharp
namespace Simulation
{
    public sealed class GridOccupancy
    {
        public GridOccupancy(WorldGrid grid, WorldData data);
        public bool CanPlace(GridPos origin, int sizeX, int sizeZ, out int blockedCount);
        public bool TryPlace(int entityId, GridPos origin, int sizeX, int sizeZ);
        public void Release(int entityId);
        public IReadOnlyList<GridPos> GetOccupiedCells(int entityId);
    }
}
```
`CanPlace` 返回 false 时 `blockedCount` 为非法格数量（供建造预览逐格着色）；`GetOccupiedCells` 从缓存读，不做全图反查。

### 5.2 WP-03 `Job` / `JobQueue` / `IJobStep`

```csharp
namespace Simulation
{
    public enum JobState { Pending, Assigned, Running, Completed, Cancelled }

    public interface IJob
    {
        int JobId { get; }
        int TargetEntityId { get; }   // 0 = 无目标实体，改用 TargetPos
        GridPos TargetPos { get; }
        int Priority { get; }         // 数值越大越先出队
        JobState State { get; set; }
    }

    public sealed class JobQueue
    {
        public void Enqueue(IJob job);
        public bool TryDequeue(out IJob job);
        public bool Cancel(int jobId);
        public int Count { get; }
    }

    public interface IJobStep
    {
        bool Tick(float dt);          // true = 该步完成
    }
}
```
标准 Steps：`WalkTo → Work(timer) → Deliver`，放在 `Jobs/Steps/`。可调数值（timer、优先级系数）一律进 Config，不硬编码。

### 5.3 WP-07 `IPathfinder`

```csharp
namespace Simulation
{
    public interface IPathfinder
    {
        bool TryFindPath(GridPos from, GridPos to, List<GridPos> result);
    }
}
```

### 5.4 WP-08 视图与对象池

```csharp
namespace Simulation
{
    public interface IWorldView
    {
        void OnEntityAdded(int id);
        void OnEntityRemoved(int id);
        void OnChunkActivated(ChunkPos pos);
        void OnChunkDeactivated(ChunkPos pos);
    }

    public interface IViewPool
    {
        GameObject Acquire(int typeId);       // UnityEngine.GameObject
        void Release(int typeId, GameObject view);
    }
}
```

### 5.5 WP-11 存档

```csharp
namespace Simulation
{
    public interface IWorldSaveSerializer
    {
        byte[] Serialize(WorldData data, WorldGrid grid);
        bool TryDeserialize(byte[] bytes, out WorldData data, out WorldGrid grid);
    }
}
```

---

## 6. 数据与表现约定

### 6.1 数据规则

| 项 | 规则 |
|---|---|
| TypeId 常量 | 值不可复用、不可改；新增只追加（`EntityTypes`），改共享文件走 4.3 流程 |
| 派生标记位 | 写入 `OccupantId` 必同步 `HasX`；替换必先 ClearFlag（见 2.2） |
| 可调数值 | **全部放 ScriptableObject / Config，不得硬编码在玩法脚本**；`OnValidate()` 做上下限与空值校验。`NUM-001` 未拍板前用集中常量占位 |
| 坐标 | `CellSize=1m`、`ChunkSize=32`；负坐标用 `CoordinateUtility.FloorDiv` 修正，不要自己写 `/` 归 Chunk |
| 依赖方向 | 数据内核不引用视图/UI；跨层通知一律事件；禁止 `FindObjectOfType` 取引用 |
| 日志 | 统一 `[模块名]` 方括号标签，全工程唯一；关键分支必打日志；异常带消息，不吞异常 |

### 6.2 状态色板（`ART-001`，UI 与建造预览共用）

| 语义 | 色值 |
|---|---|
| 可建 | `#97C459` |
| 不可建 | `#E24B4A` |
| 可采集 | `#EF9F27` |
| 建造中 | `#85B7EB` |
| 路径 | `#B4B2A9` |
| 悬停描边 | `#534AB7` |

多格预览：**整体半透明 + 逐格合法性着色**。`ART-002`（风格锚点与禁忌、资源尺寸与命名规范）确认前，不批量产出美术资源。

---

## 7. 验证与门禁

### 7.1 自检命令（在工程根目录执行）

```bash
# 首次：脚本自检（确认检查器本身没坏）
python <技能包>/scripts/check_unity_project.py --selftest

# 日常门禁：只判新增问题
python <技能包>/scripts/check_unity_project.py . \
  --spec docs/unity-project-spec.json \
  --freeze docs/design-freeze.json \
  --baseline docs/unity-baseline.json \
  --json docs/unity-check-report.json

# 定期复核抑制范围（建议每个修复批次后跑一次）
python <技能包>/scripts/check_unity_project.py . --spec docs/unity-project-spec.json --no-ignore
```

`<技能包>` 按本机 `unity-game-suite` 技能包实际安装位置替换；脚本仅依赖 Python 3，无需打开 Unity 编辑器。退出码 `0` 通过 / `1` 发现问题 / `2` 参数或环境问题 / `3` 脚本自身错误。

### 7.2 本轮自检证据

| 轮次 | 结论 | 明细 |
|---|---|---|
| 首次全量（未配规格） | **未通过**，错误 7 / 警告 3 | 5 条 layout「缺必需目录」是规格默认目录与本工程结构不符；1 条 scenes（Demo 场景未入构建列表）；1 条 csharp 非 UTF-8；danglingGuids 191 个 GUID；traceability 2 类 |
| 按工程结构 `--init-spec` 对齐 + 1 条精确抑制 + 基线固化后 | **通过（exit 0）** | 错误 1 / 警告 4 全部为**遗留**，**新增 0**；基线 39 个指纹；已按规格抑制 `scenes×1`（理由见规格文件） |

通过项（可作基线事实）：`projectVersion 6000.3.2f1`、`manifest 52 包合法`、`metaPairing 1209 文件 + 253 目录配对完整`、`guidUnique 1462 个 GUID 全唯一`、`buildSettings 2 条有效且启用`、`asmdef 26 个`、`csharp 814 个脚本`、`absolutePaths 无机器绝对路径`、`traceability 冻结 21 条（已确认 5），覆盖率 0/5`。

**已抑制 1 条**（`scenes`，`Assets/Game/Framework/Ugui/Runtime/ProceduralUIImage/Demo/demo_examples.unity`，理由：第三方示例场景按设计不入构建列表）。**未静默抑制任何项**；历史欠债全部走基线，未手工编辑基线文件。

### 7.3 每个 WP 的验收清单（缺一不可）

1. **编译**：回 Unity 编辑器刷新，Console 无 `error CS`。⚠️ 本机安全策略未允许执行 C# 编译，**P0 及本轮所有代码均未跑过真实编译**，这一项必须由开发者在编辑器里补齐；编译不通过时，其余运行时结论一律无效。
2. **自检**：按 7.1 命令复跑，`新增错误 0 / 新增警告 0`（遗留项允许存在）。
3. **追溯**：实现文件写 `// @trace 编号`；主程确认 `docs/design-freeze.json` 对应条目 `confirmed=true` 且 `version` 已递增。
4. **用例**：按各任务卡「验收」行的边界用例逐条过一遍，留日志证据。
5. **不越界**：`git diff` 只含自己 WP 名下文件；共享基线文件无改动。

> 修复任一问题后必须**复跑验证**并确认没引入新问题；同一问题连续三次未解决就停下来同步主程，不要继续试。

---

## 8. 历史欠债与待确认清单

### 8.1 历史欠债（已进基线，不阻塞新增开发）

| 项 | 证据 | 建议处置 |
|---|---|---|
| `Assets/Game/Scripts/Common/UI/UICircleImage.cs` 非合法 UTF-8 | 自检 `csharp` 错误级（遗留） | 转 UTF-8 后复跑，更新基线 |
| 191 个 GUID / 319 处引用找不到归属资源 | 自检 `danglingGuids` 警告级（遗留） | 多为内置或包内资源；确认后用规格 `builtinGuidAllowlist` 收敛，或在编辑器里重指派真实丢失的引用 |
| `Assets/Scripts/Editor` 建议目录缺失 | 自检 `layout` 警告级（遗留） | 按规范补目录或接受，不阻塞 |
| 输入两套并存 | 代码事实 + `activeInputHandler=2` | `PLAT-001` / WP-12 |
| `ChunkActivated/Deactivated` 无订阅者，3×3 窗口空转 | 代码事实 | WP-08 接上 |
| `GameScene/Land` 挂 `AstarPath` 组件 | 场景事实 | `MECH-016` 拍板前不得删 |
| DOTS Entities 已安装但无使用痕迹 | `Packages/manifest.json` | WP-12 清理 |
| `Simulation/Simulation` 多余嵌套空目录 | 目录扫描 | WP-12 清理 |
| 产品名 / 公司名仍为默认值 | `ProjectSettings/ProjectSettings.asset` | 发布前由主程定 |

### 8.2 待主程拍板（未确认条目不得开发）

| 编号 | 问题 | 影响面 | 建议 |
|---|---|---|---|
| `PLAT-001` | 输入方案统一到哪一套 | 相机、建造交互、WP-12 | 倾向保留 `GameInput` 内联方案；删 `.inputactions` 引用前先在编辑器实测 |
| `NUM-001` | 建造/采集耗时、Worker 速度、Job 优先级系数 | 手感、Steps 计时 | 先集中占位常量，别散落硬编码；定稿后补可复算表格与极值验算 |
| `ART-002` | 美术风格锚点与禁忌、资源尺寸与命名 | 全部美术资源 | 定锚点后再批量出图；AI 生成图与占位资源需人工精修与合规确认 |
| `MECH-016` | 自研格点 A* vs 现成 A* 插件 | `Pathfinding/`、场景组件 | 与 WP-07 一并决策，不要在未定前删 `AstarPath` |
| — | 物品是否需要占格 / 叠加层是否提前 | `Cell` 结构 | 当前按「不占格」；若要改，须改冻结清单并 `version` +1，另加 overlay 层而非退回多槽位 |

---

## 9. 变更记录

| 版本 | 日期 | 变更 | 影响面 |
|---|---|---|---|
| v1.0 | 2026-11-20 | 初版：P0 基线冻结、21 条需求编号、12 个工作包、接口契约、自检门禁与历史欠债基线 | 全文档；`docs/` 下 4 个配套文件首次生成 |

> 需求变更流程：改 `docs/design-freeze.json`（`version` +1，注明变更）→ 全仓检索残留引用 → 同步本文档对应章节 → 通知所有 WP 负责人。**禁止只改代码不改清单。**

---

## 10. 本轮产出 / 未完成 / 建议下一步

**本轮产出**
- `docs/simulation-parallel-handoff.md`（本文档）：P0 基线契约、12 个可并行工作包、文件归属锁、待实现接口契约、验收门禁。
- `docs/design-freeze.json`：21 条需求，`status=frozen`，已确认 5 条。
- `docs/unity-project-spec.json` / `docs/unity-baseline.json` / `docs/unity-check-report.json`：自检规格、历史欠债基线（39 指纹）、可复核报告。
- 自检门禁已跑通：新增问题 0，抑制 1 条（有理由）。

**未完成**
- P1–P6 全部功能**未开发**（用户本轮明确暂停）。
- 真实 C# 编译**未跑**（本机安全策略限制），P0 代码仍需在 Unity 编辑器里确认一次编译。
- `@trace` 标记**未补**（遵守「不开发」未改动代码），覆盖率 0/5，补法见 3.2。
- `NUM-001` / `PLAT-001` / `ART-002` / `MECH-016` 四项**待主程确认**；确认前不得启动对应子项。
- 8.1 的历史欠债全部**遗留未修**（已进基线，不阻塞新增）。

**建议下一步**（按优先级）
1. 主程拍板 8.2 的 4 项待确认，把要派发的条目 `confirmed` 置 `true` 并 `version` +1。
2. 首批开发者在 Unity 编辑器里确认 P0 编译通过，顺手补 3.2 的 5 行 `@trace`（覆盖率到 100%）。
3. 按第 4 章派发波次 A（WP-01/02/03/08/10 可 5 人同时开工），每人按 7.3 验收。
