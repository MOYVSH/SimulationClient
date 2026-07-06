---
title: "Phase 6：Optimization"
phase: "6"
owner: "海豹"
status: "planned"
dependencies: ["phase-0", "phase-1", "phase-2", "phase-3", "phase-4", "phase-5"]
keywords: ["优化", "性能", "LOD", "GC", "GPU", "Profiler", "Draw Call"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Phase 6：Optimization

## 负责人

海豹

## 目标

在功能基本完成后，对 CPU、GPU、内存和 GC 进行系统性优化，确保项目在目标规模下稳定达到性能预算。

## 依赖

- Phase 0~5 全部完成并基本可运行

## 已有框架复用

| 需求 | 使用框架 | 说明 |
|------|----------|------|
| 集合池 | `MOYVCollections`（`Assets/Game/Framework/Collections/`） | 自定义集合库，可能已有池化实现 |
| 调试 | `MDebug`（`Assets/Game/Framework/MDebug/`） | 性能数据输出 |
| 渲染优化 | URP GPU Instancing / SRP Batcher | URP 内置的批处理优化 |

## 任务列表

| # | 任务 | 关键产出 | 依赖 | 验收标准 |
|---|------|----------|------|----------|
| 6.1 | 性能监控 | `PerformanceMonitor.cs`、Profiler Markers | 前面全部 | 实时显示 CPU ms、TreeView 数、Worker 数、寻路请求数、GC 分配 |
| 6.2 | TreeView 裁剪与 LOD | `TreeLodManager.cs` | 2.5, 2.6 | TreeView ≤ 500，GPU < 8ms |
| 6.3 | 寻路优化 | `AStarOptimizer.cs` | 3.3, 3.4 | 路径缓存、按帧分摊请求，100 工人同时寻路 CPU 不超标 |
| 6.4 | 任务调度优化 | 更新 `JobManager.cs` | 5.2 | 按 Chunk 建立任务索引，任务分配成本与总量解耦 |
| 6.5 | 内存与 GC 优化 | `CollectionPool.cs` | 前面全部 | 热点路径零 GC.Alloc |
| 6.6 | GPU 与 Draw Call 优化 | `GpuInstancingHelper.cs` | 2.3, 3.2, 4.3 | GPU Instancing、材质合并、批处理生效 |
| 6.7 | 最终性能验证 | 性能报告 | 6.1~6.6 | 在目标硬件上 10000+ 树、100 工人、3×3 Chunk 下 CPU < 10ms、GPU < 8ms |

## 6.1 性能监控

### 产出文件

- `Assets/Game/Scripts/Simulation/Profiling/PerformanceMonitor.cs`

### 实现细节

1. 每帧记录：
   - CPU 帧耗时
   - GPU 帧耗时
   - 活跃 TreeView 数量
   - 活跃 Worker 数量
   - 寻路请求数量
   - GC 分配量

2. 使用 Unity Profiler Custom Markers：
   - `Simulation.Tick`
   - `Pathfinding.FindPath`
   - `TreeViewSystem.Update`
   - `JobManager.Assign`

3. 提供 HUD 或 Console 输出（可结合 `MDebug`），超预算时警告

### 验收标准

- 运行时能看到实时性能数据
- Profiler 中各系统有清晰的 Marker

## 6.2 TreeView 裁剪与 LOD

### 产出文件

- `Assets/Game/Scripts/Simulation/Trees/TreeLodManager.cs`

### 实现细节

1. 为 TreeView 添加 LOD Group
2. 远处树木使用更低面数模型或 Billboard
3. 进一步缩小激活 Chunk 半径或按距离分层激活
4. 利用 URP 的 GPU Instancing 和 SRP Batcher 优化渲染

### 验收标准

- 活跃 TreeView ≤ 500
- GPU 帧时间 < 8ms

## 6.3 寻路优化

### 产出文件

- `Assets/Game/Scripts/Simulation/Pathfinding/AStarOptimizer.cs`

### 实现细节

1. 路径缓存：
   - 缓存常见起点-终点路径
   - 世界变化时使缓存失效

2. 按帧分摊：
   - 每帧最多处理 N 个寻路请求
   - 未处理的请求排队到下一帧

3. 可选：使用 A* Pathfinding Project 的 Burst/Jobs 支持实现高性能寻路

### 验收标准

- 100 工人同时请求寻路不造成 CPU 尖峰
- 寻路结果正确性不受影响

## 6.4 任务调度优化

### 产出文件

- 更新 `Assets/Game/Scripts/Simulation/Jobs/JobManager.cs`

### 实现细节

1. 按 Chunk 建立任务空间索引
2. 为每个工人只搜索附近 Chunk 的任务
3. 限制 JobManager 重评估频率
4. 批量创建/销毁任务

### 验收标准

- 任务分配成本与总任务数解耦
- 100 工人 + 大量任务时 CPU 稳定

## 6.5 内存与 GC 优化

### 产出文件

- `Assets/Game/Scripts/Simulation/Utils/CollectionPool.cs`

### 实现细节

1. 池化常用集合：
   - `List<T>`
   - `Queue<T>`
   - `Dictionary<TKey, TValue>`
   - 路径结果数组

2. 优先复用 `MOYVCollections` 中已有的池化集合

3. 热路径使用 struct 而非 class
4. 避免 LINQ 和闭包 in Tick/update loops
5. 使用 Unity Profiler Memory 模块验证

### 验收标准

- Profiler 显示 0 B GC.Alloc 每帧（steady state）
- 无内存泄漏

## 6.6 GPU 与 Draw Call 优化

### 产出文件

- `Assets/Game/Scripts/Simulation/View/GpuInstancingHelper.cs`

### 实现细节

1. 在 Tree/Building 材质上启用 URP GPU Instancing
2. 利用 URP SRP Batcher 减少 Draw Call
3. 合并 Worker 材质，限制独特材质数量
4. 使用 Frame Debugger 分析 Draw Call

### 验收标准

- Draw Call 和 SetPass Call 随可见对象数量稳定
- GPU 帧时间 < 8ms

## 6.7 最终性能验证

### 实现细节

1. 在目标硬件上测试：
   - 10000+ 树
   - 100 工人
   - 3×3 激活 Chunk
   - 建筑、道路、农田、搬运全功能运行

2. 记录：
   - 平均 CPU ms
   - 平均 GPU ms
   - 峰值帧耗时
   - GC 分配
   - Draw Call 数量

3. 输出报告，列出剩余瓶颈和下一步计划

### 验收标准

- 平均 CPU < 10ms
- 平均 GPU < 8ms
- 报告包含实测数据和优化建议

## 提供给项目的最终交付

- 性能监控基础设施
- 优化后的 TreeView、Pathfinding、JobManager
- 零 GC 分配的热路径
- 可复用的 CollectionPool
- 性能验证报告

## 阻塞项目发布的风险

- 优化过度导致代码复杂度上升、维护困难
- 过早使用 Burst/Jobs 增加学习成本
- 只关注 FPS 而忽视存档正确性和逻辑一致性
