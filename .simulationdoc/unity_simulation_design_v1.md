# Unity 模拟经营项目技术设计文档（V1）

## 项目目标

参考：
- Against the Storm
- Timberborn
- Factorio

目标特征：
- Terrain 大地图
- 10000+ 树木资源
- 100+ 工人
- 斜45°视角
- 建筑 / 道路 / 农田系统
- 高性能可扩展架构

---

## 一、总体架构

Terrain（渲染层）
↓
WorldData（树/建筑/资源数据）
↓
WorldGrid（空间数据库）
↓
ChunkManager（分区管理）
↓
AI（A* Pathfinding）
↓
View（对象池表现层）

---

## 二、核心设计原则

1. 数据与表现分离
2. Terrain只负责渲染
3. Grid负责世界逻辑
4. A*只负责寻路
5. GameObject仅用于表现

---

## 三、WorldGrid

Cell结构：
- TreeId
- BuildingId
- RoadId
- Flags

职责：
- 空间查询
- 占用管理
- 可行走判断

---

## 四、Chunk系统

推荐尺寸：32×32

职责：
- 空间分区
- 局部激活
- 性能优化

玩家激活范围：3×3 Chunk

---

## 五、树木系统

TreeData：
- Id
- Position
- Size
- HP
- State

TreeView：
- 仅负责动画/特效
- 不存逻辑

---

## 六、对象池系统

TreeView Pool：
- 最大300~500实例
- 避免Instantiate/Destroy

---

## 七、Terrain同步

初始化：
Terrain TreeInstance → TreeData

运行时：
靠近 → TreeView生成
远离 → 回归Terrain

---

## 八、建筑系统

BuildingData：
- Id
- Position
- Size
- Type

占用Grid实现空间逻辑

---

## 九、道路系统

Road影响移动速度
Grid记录道路类型

---

## 十、寻路系统（A*）

仅负责：
- Worker路径计算

更新策略：
- 局部Graph Update
- 避免全图刷新

---

## 十一、工人系统

Worker状态：
Idle / Move / Work / Carry

特点：
- 不全局搜索
- 只执行任务

---

## 十二、任务系统

Job类型：
- CutTree
- Build
- Farm
- Haul

JobManager负责调度

---

## 十三、存档系统

保存数据：
- TreeData
- BuildingData
- WorkerData

不保存：
- GameObject

---

## 十四、性能目标

- TreeView ≤ 500
- Worker ≤ 100
- CPU < 10ms
- GPU < 8ms

---

## 十五、开发阶段

Phase 1：World + Grid
Phase 2：Tree + View
Phase 3：Worker + A*
Phase 4：Building + Road
Phase 5：Simulation
Phase 6：Optimization

---

## 核心架构总结

Terrain → WorldGrid → Chunk → Data → A* → Job → Worker → ViewPool
