---
title: "Unity 模拟经营项目 — 开发计划文档集"
phase: "meta"
owner: "海豹"
status: "planned"
dependencies: []
keywords: ["README", "协作", "开发顺序", "里程碑", "分工"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Unity 模拟经营项目 — 开发计划文档集

本目录包含 `unity_simulation_design_v1.md` 拆分后的独立开发计划，基于已有项目结构（`Assets/Game/`）重构，适配现有框架（QFramework MVC、A* Pathfinding Project、MonsterLove FSM 等）。

## 文件结构

| 文件 | 内容 | 通常负责领域 |
|------|------|--------------|
| `00-overview.md` | 项目总览、全局约定、已有框架复用、接口契约、风险 | 项目经理 / 主程 |
| `01-phase-0-setup.md` | 模拟经营目录初始化、相机、输入 | 客户端基础 |
| `02-phase-1-world-grid.md` | 坐标系、WorldGrid、WorldData、Chunk、Terrain 同步 | 世界系统 / 架构 |
| `03-phase-2-tree-view.md` | 树木数据、生命周期、View、对象池、Terrain 同步 | 资源与表现 |
| `04-phase-3-worker-pathfinding.md` | Worker、A* 适配层、移动、MonsterLove FSM 状态机 | AI / 寻路 |
| `05-phase-4-building-road.md` | 建筑、道路、放置、生命周期 | 建造系统 |
| `06-phase-5-simulation.md` | Job 系统、任务、Tick、存档、UI | 模拟核心 / UI |
| `07-phase-6-optimization.md` | 性能监控、LOD、寻路优化、GC、GPU、最终验证 | 性能优化 |

## 如何使用这些文档

### 对个人

1. 先阅读 `00-overview.md` 了解全局约定和已有框架
2. 找到自己负责的 Phase 文档
3. 按照任务列表逐项实现
4. 每个任务完成后对照"验收标准"自测

### 对团队

1. 每个 Phase 指定一位负责人（在文档顶部标注）
2. 开发前确认自己负责的 Phase 所依赖的 Phase 已完成或已定义接口
3. 若需要修改全局约定（坐标系、ID 类型、事件机制），必须同步更新 `00-overview.md`
4. 遇到跨 Phase 接口变更时，在相关文档中同步更新"对外接口"和"验收标准"

## 代码放置规则

- 所有模拟经营系统代码放在 `Assets/Game/Scripts/Simulation/` 下
- Prefabs 放在 `Assets/Game/MiniGame_Res/Prefabs/` 下
- ScriptableObjects 放在 `Assets/Game/MiniGame_Res/ScriptableObjects/` 下
- **不修改** 已有的 `Framework/`、`Common/`、`ConfigCode/`、`Game/`、`MiniGame_Scripts/` 中的代码
- 优先复用已有框架（详见 `00-overview.md` 的"已有框架与插件"表）

## 开发顺序

推荐按 Phase 编号顺序推进：

```
0 → 1 → 2 → 3 → 4 → 5 → 6
```

其中 Phase 2 和 Phase 3 都依赖 Phase 1，可在 Phase 1 稳定后并行开发。

## 最小可玩里程碑

完成以下任务即可得到一个可运行、可交互的原型：

- Phase 0：0.1, 0.2
- Phase 1：1.1~1.5
- Phase 2：2.1~2.6
- Phase 3：3.1~3.6
- Phase 5：5.1, 5.2, 5.3, 5.9

可验证行为：
- 斜 45° 相机漫游大地图
- 近处树木以 GameObject View 显示，远处回归 Terrain
- 可生成工人
- 可标记树木砍伐
- 工人自动寻路到树、砍伐、树木从世界中移除

## 修改记录

| 日期 | 修改人 | 内容 |
|------|--------|------|
| 2026-07-06 | Qoder | 从 `unity_simulation_design_v1.md` 拆分为独立 Phase 文档 |
| 2026-07-06 | Qoder | 重构适配已有项目结构（`Assets/Game/`），更新技术选型为 URP，整合已有框架（QFramework、A* Pathfinding、MonsterLove FSM 等） |
