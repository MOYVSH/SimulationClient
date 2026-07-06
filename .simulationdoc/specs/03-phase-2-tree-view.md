---
title: "Phase 2：Tree + View"
phase: "2"
owner: "海豹"
status: "planned"
dependencies: ["phase-0", "phase-1"]
keywords: ["Tree", "TreeView", "对象池", "Terrain同步", "生命周期", "LOD"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Phase 2：Tree + View

## 负责人

海豹

## 目标

实现树木的数据模型、生命周期、对象池和基于 Chunk 的视图激活系统，确保 10000+ 棵树在性能预算内正确显示和同步。

## 依赖

- Phase 0（项目基础）
- Phase 1（WorldGrid、WorldData、ChunkManager、Terrain 同步）

## 已有框架复用

| 需求 | 使用框架 | 说明 |
|------|----------|------|
| 对象池 | `MPool`（`Assets/Game/Framework/MPool/`）或 `UniFramework.Pooling` | 优先复用已有池实现，不足时再自建 `ObjectPool<T>` |
| 动画 | `MOYVDoTween`（`Assets/Game/Framework/DoTween/`） | 树木缩放、风吹摇摆动画 |

## 任务列表

| # | 任务 | 关键产出 | 依赖 | 验收标准 |
|---|------|----------|------|----------|
| 2.1 | TreeData 与类型注册 | `TreeData.cs`、`TreeState.cs`、`TreeRegistry.cs`、`TreeTypeSO.cs` | 1.3 | 可创建/修改/删除树木数据，状态枚举可用 |
| 2.2 | 树木生命周期 | `TreeLifecycleSystem.cs` | 2.1 | 树木生长、标记砍伐、移除时更新 Grid |
| 2.3 | TreeView 表现 | `TreeView.prefab`、`TreeView.cs` | 2.1 | View 只处理缩放/材质/动画，不存逻辑 |
| 2.4 | 通用对象池 | `ObjectPool.cs`、`IPoolable.cs`、`PoolManager.cs` | 2.3 | 预生成 300~500 实例，Rent/Return 无分配 |
| 2.5 | Chunk 驱动的 TreeView 激活 | `TreeViewSystem.cs` | 1.4, 2.2, 2.4 | 相机移动时生成/回收 TreeView，激活数 ≤ 500 |
| 2.6 | Terrain TreeInstance ↔ TreeData 运行时同步 | `TerrainTreeSyncSystem.cs` | 1.5, 2.5 | 近处显示 View、远处恢复 Terrain，砍伐后两者都消失 |

## 2.1 TreeData 与类型注册

### 产出文件

- `Assets/Game/Scripts/Simulation/Trees/TreeData.cs`
- `Assets/Game/Scripts/Simulation/Trees/TreeState.cs`
- `Assets/Game/Scripts/Simulation/Trees/TreeRegistry.cs`
- `Assets/Game/MiniGame_Res/ScriptableObjects/TreeTypes/TreeTypeSO.cs`

### 实现细节

1. `TreeState` 枚举：
   - `Seedling`
   - `Growing`
   - `Mature`
   - `Marked`（被标记砍伐）
   - `Cut`
   - `Stump`

2. `TreeData`：
   ```csharp
   public class TreeData : IWorldEntity
   {
       public int Id;
       public GridPos GridPos;
       public Vector3 Position;
       public float Size;
       public float HP;
       public TreeState State;
       public int TreeTypeId;
   }
   ```

3. `TreeTypeSO`（ScriptableObject，放在 `Assets/Game/MiniGame_Res/ScriptableObjects/TreeTypes/`）：
   - `treeName`
   - `prefab`（引用 TreeView 预制体）
   - `growthTime`
   - `maxHP`
   - `visualScaleCurve`

4. `TreeRegistry`：
   - `Dictionary<int, TreeData>` 按 ID 索引
   - 提供 `Get(id)`、`Register`、`Unregister`

### 验收标准

- 可创建、修改、删除 TreeData
- TreeRegistry 与 WorldData 同步一致
- 状态切换不抛异常

## 2.2 树木生命周期

### 产出文件

- `Assets/Game/Scripts/MiniGame_Scripts/System/TreeLifecycleSystem.cs`

### 实现细节

1. 订阅 `WorldData.TreeAdded` 事件注册到 `TreeRegistry`
2. 每 Tick 推进树木生长：
   - `Seedling → Growing → Mature`
   - 根据 `TreeTypeSO.growthTime` 和 `Size` 曲线更新
3. 标记砍伐：
   - 将 TreeData.State 设为 `Marked`
   - 更新 `WorldGrid` 中 Cell 的标记（可选）
4. 砍伐完成：
   - HP 降到 0 时，从 `WorldData` 移除
   - 清除 `WorldGrid` 中对应 Cell 的 `TreeId` 和 `HasTree/Occupied` 标记
   - 产出木材 Item（Phase 5 接管，本阶段可预留事件）

### 对外接口

```csharp
public class TreeLifecycleSystem : MonoBehaviour, IGameSystem
{
    public void Tick(float deltaTime);
    public void MarkTreeForCutting(int treeId);
    public void DamageTree(int treeId, float damage);
}
```

### 验收标准

- 树木按时间从 Seedling 生长到 Mature
- 标记砍伐后状态变为 Marked
- HP 归零后 TreeData 和 Grid 都正确清理

## 2.3 TreeView 表现

### 产出文件

- `Assets/Game/MiniGame_Res/Prefabs/Trees/TreeView.prefab`
- `Assets/Game/Scripts/Simulation/Trees/TreeView.cs`

### 实现细节

1. `TreeView` 只处理视觉：
   - 根据 `TreeData.Size` 设置缩放（可使用 DOTween）
   - 根据 `TreeState` 切换材质/颜色
   - 播放风吹摇摆动画（可选 Shader 或 Animator）
2. 不存储逻辑状态
3. 提供 `Initialize(TreeData data)` 和 `Refresh(TreeData data)`

### 对外接口

```csharp
public class TreeView : MonoBehaviour, IPoolable
{
    public void Initialize(TreeData data, TreeTypeSO type);
    public void Refresh(TreeData data);
    public void OnRent();
    public void OnReturn();
}
```

### 验收标准

- Prefab 实例化后缩放与 TreeData.Size 一致
- View 代码不直接修改 TreeData

## 2.4 通用对象池

### 产出文件

- `Assets/Game/Scripts/Simulation/View/Pool/ObjectPool.cs`
- `Assets/Game/Scripts/Simulation/View/Pool/IPoolable.cs`
- `Assets/Game/Scripts/Simulation/View/Pool/PoolManager.cs`

> **注意**：项目已有 `MPool`（`Assets/Game/Framework/MPool/`）和 `UniFramework.Pooling`。优先评估是否满足需求（预生成、Rent/Return、容量控制），若满足则直接复用并在 `PoolManager` 中封装；若不满足则自建 `ObjectPool<T>`。

### 实现细节

1. `IPoolable`：
   ```csharp
   public interface IPoolable
   {
       void OnRent();
       void OnReturn();
   }
   ```

2. `ObjectPool<T>`：
   - 预生成指定数量实例
   - `Rent()` 返回可用实例
   - `Return(T)` 回收实例
   - 达到最大容量时拒绝或复用最旧实例

3. `PoolManager`：
   - 管理多个 `ObjectPool`
   - 预生成 TreeView 池 300~500 个

### 对外接口

```csharp
public class ObjectPool<T> where T : Component, IPoolable
{
    public ObjectPool(T prefab, int initialSize, int maxSize, Transform parent);
    public T Rent();
    public void Return(T item);
    public int ActiveCount { get; }
    public int PoolCount { get; }
}
```

### 验收标准

- Rent/Return 不产生 GC 分配
- ActiveCount 在借出/归还时正确变化
- 达到 maxSize 后行为确定（可配置）

## 2.5 Chunk 驱动的 TreeView 激活

### 产出文件

- `Assets/Game/Scripts/MiniGame_Scripts/System/TreeViewSystem.cs`

### 实现细节

1. 订阅 `ChunkManager.ChunkActivated` / `ChunkDeactivated`
2. Chunk 激活时：
   - 遍历 Chunk 内所有 Cell
   - 对每棵成熟树（Mature 等）从对象池 Rent TreeView
   - 设置位置、初始化数据
   - 移除 Terrain 上对应的 TreeInstance
3. Chunk 停用时：
   - 回收 TreeView
   - 恢复 Terrain TreeInstance
4. 维护活跃 TreeView 数量 ≤ 500

### 对外接口

```csharp
public class TreeViewSystem : MonoBehaviour
{
    public int ActiveTreeViewCount { get; }
    public void OnChunkActivated(ChunkPos pos);
    public void OnChunkDeactivated(ChunkPos pos);
}
```

### 验收标准

- 相机移动时 TreeView 正确生成/回收
- 活跃 TreeView 数不超过 500
- 无可见闪烁或重复渲染

## 2.6 Terrain TreeInstance ↔ TreeData 运行时同步

### 产出文件

- `Assets/Game/Scripts/MiniGame_Scripts/System/TerrainTreeSyncSystem.cs`

### 实现细节

1. 当树进入活跃 Chunk：
   - 从 Terrain 移除对应 TreeInstance
   - 显示 TreeView
2. 当树离开活跃 Chunk：
   - 回收 TreeView
   - 将 TreeInstance 加回 Terrain
3. 当树被砍伐：
   - 确保 TreeInstance 和 TreeView 都不存在
4. 使用 `TerrainData.SetTreeInstances` 或类似 API 批量修改 Terrain

### 对外接口

```csharp
public class TerrainTreeSyncSystem : MonoBehaviour
{
    public void ShowAsView(int treeId);
    public void ShowAsTerrain(int treeId);
    public void RemoveFromTerrain(int treeId);
}
```

### 验收标准

- 活跃树以 View 显示，非活跃树以 Terrain 显示
- 同一棵树不会同时以两种形式渲染
- 砍伐后的树从 Terrain 永久消失

## 提供给下游 Phase 的契约

| 系统 | 提供内容 |
|------|----------|
| `TreeData` / `TreeRegistry` | 树木权威数据 |
| `TreeLifecycleSystem` | 生长、标记、砍伐逻辑 |
| `TreeViewSystem` | 活跃树木的视觉表现 |
| `TerrainTreeSyncSystem` | Terrain 与 View 的切换 |

## 阻塞 downstream 的风险

- TreeView 池容量不足或 Rent/Return 有分配，会导致性能不达标
- Terrain 同步逻辑有 bug 会导致树木丢失或重复
- 砍伐后未正确清理 Grid，会导致寻路和建筑放置错误
