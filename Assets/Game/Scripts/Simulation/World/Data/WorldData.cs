using System;
using System.Collections.Generic;

namespace Simulation
{
    /// <summary>
    /// 世界权威数据层：存储所有实体的完整数据，提供 CRUD 和事件通知。
    ///
    /// 与 WorldGrid 的关系：
    ///   WorldData = "实体是什么"（按 ID 查完整数据）
    ///   WorldGrid = "格子上有什么"（按坐标查空间状态）
    ///   两者通过 int ID 建立双向索引（详见文档 1.3a）
    ///
    /// 事件设计原则：
    ///   1. 事件参数只传 ID，不传实体对象
    ///      - 订阅方按需去字典取数据，避免传递大对象
    ///      - 如果实体已被删除，订阅方查询返回 null，自然处理
    ///   2. 用 C# 原生 event Action&lt;int&gt;，不用 QFramework SendEvent
    ///      - Tick 热路径（如每秒触发多次）避免 GC 分配
    ///      - QFramework 事件适合跨模块低频通知，不适合高频场景
    ///
    /// 线程安全：
    ///   WorldData 只应在 Unity 主线程访问，不做线程安全处理。
    ///   如果需要异步加载（如从存档读取），应在协程/Task 中统一在主线程操作。
    /// </summary>
    public class WorldData
    {
        // ---- 数据存储 ----
        // 使用 Dictionary<int, T> 而非数组，原因：
        //   1. ID 稀疏（删除后 ID 不复用，数组会有空洞）
        //   2. 删除操作 O(1)
        //   3. 不需要按 ID 排序遍历

        private readonly Dictionary<int, TreeData> _trees = new Dictionary<int, TreeData>();
        private readonly Dictionary<int, BuildingData> _buildings = new Dictionary<int, BuildingData>();
        private readonly Dictionary<int, RoadData> _roads = new Dictionary<int, RoadData>();
        private readonly Dictionary<int, WorkerData> _workers = new Dictionary<int, WorkerData>();

        private readonly IdGenerator _idGenerator = new IdGenerator();

        // ---- 事件定义 ----
        // 每种实体提供 Added/Removed/Modified 三种事件
        // Modified 事件在某些实体（如 Road）可能不常用，保留以备扩展

        /// <summary>树实体事件：添加、删除、修改。</summary>
        public event Action<int> TreeAdded, TreeRemoved, TreeModified;

        /// <summary>建筑实体事件：添加、删除、修改。</summary>
        public event Action<int> BuildingAdded, BuildingRemoved, BuildingModified;

        /// <summary>道路实体事件：添加、删除。</summary>
        public event Action<int> RoadAdded, RoadRemoved;

        /// <summary>工人实体事件：添加、删除、修改。</summary>
        public event Action<int> WorkerAdded, WorkerRemoved, WorkerModified;

        // ================================================================
        // Tree CRUD
        // ================================================================

        /// <summary>创建树实体，分配 ID 并触发 TreeAdded 事件。</summary>
        public int CreateTree(TreeData data)
        {
            int id = _idGenerator.Next();
            data.Id = id;
            _trees[id] = data;
            TreeAdded?.Invoke(id);
            return id;
        }

        /// <summary>根据 ID 获取树实体。不存在返回 null。</summary>
        public TreeData GetTree(int id)
        {
            return _trees.TryGetValue(id, out var data) ? data : null;
        }

        /// <summary>删除树实体，触发 TreeRemoved 事件。</summary>
        public bool RemoveTree(int id)
        {
            if (!_trees.Remove(id))
                return false;
            TreeRemoved?.Invoke(id);
            return true;
        }

        /// <summary>修改树实体，触发 TreeModified 事件。</summary>
        public void ModifyTree(int id, Action<TreeData> modifier)
        {
            if (_trees.TryGetValue(id, out var data))
            {
                modifier?.Invoke(data);
                TreeModified?.Invoke(id);
            }
        }

        /// <summary>遍历所有树（只读）。</summary>
        public IEnumerable<KeyValuePair<int, TreeData>> AllTrees => _trees;

        /// <summary>树的总数。</summary>
        public int TreeCount => _trees.Count;

        // ================================================================
        // Building CRUD
        // ================================================================

        /// <summary>创建建筑实体，分配 ID 并触发 BuildingAdded 事件。</summary>
        public int CreateBuilding(BuildingData data)
        {
            int id = _idGenerator.Next();
            data.Id = id;
            _buildings[id] = data;
            BuildingAdded?.Invoke(id);
            return id;
        }

        /// <summary>根据 ID 获取建筑实体。不存在返回 null。</summary>
        public BuildingData GetBuilding(int id)
        {
            return _buildings.TryGetValue(id, out var data) ? data : null;
        }

        /// <summary>删除建筑实体，触发 BuildingRemoved 事件。</summary>
        public bool RemoveBuilding(int id)
        {
            if (!_buildings.Remove(id))
                return false;
            BuildingRemoved?.Invoke(id);
            return true;
        }

        /// <summary>修改建筑实体，触发 BuildingModified 事件。</summary>
        public void ModifyBuilding(int id, Action<BuildingData> modifier)
        {
            if (_buildings.TryGetValue(id, out var data))
            {
                modifier?.Invoke(data);
                BuildingModified?.Invoke(id);
            }
        }

        /// <summary>遍历所有建筑（只读）。</summary>
        public IEnumerable<KeyValuePair<int, BuildingData>> AllBuildings => _buildings;

        /// <summary>建筑总数。</summary>
        public int BuildingCount => _buildings.Count;

        // ================================================================
        // Road CRUD
        // ================================================================

        /// <summary>创建道路实体，分配 ID 并触发 RoadAdded 事件。</summary>
        public int CreateRoad(RoadData data)
        {
            int id = _idGenerator.Next();
            data.Id = id;
            _roads[id] = data;
            RoadAdded?.Invoke(id);
            return id;
        }

        /// <summary>根据 ID 获取道路实体。不存在返回 null。</summary>
        public RoadData GetRoad(int id)
        {
            return _roads.TryGetValue(id, out var data) ? data : null;
        }

        /// <summary>删除道路实体，触发 RoadRemoved 事件。</summary>
        public bool RemoveRoad(int id)
        {
            if (!_roads.Remove(id))
                return false;
            RoadRemoved?.Invoke(id);
            return true;
        }

        /// <summary>遍历所有道路（只读）。</summary>
        public IEnumerable<KeyValuePair<int, RoadData>> AllRoads => _roads;

        /// <summary>道路总数。</summary>
        public int RoadCount => _roads.Count;

        // ================================================================
        // Worker CRUD
        // ================================================================

        /// <summary>创建工人实体，分配 ID 并触发 WorkerAdded 事件。</summary>
        public int CreateWorker(WorkerData data)
        {
            int id = _idGenerator.Next();
            data.Id = id;
            _workers[id] = data;
            WorkerAdded?.Invoke(id);
            return id;
        }

        /// <summary>根据 ID 获取工人实体。不存在返回 null。</summary>
        public WorkerData GetWorker(int id)
        {
            return _workers.TryGetValue(id, out var data) ? data : null;
        }

        /// <summary>删除工人实体，触发 WorkerRemoved 事件。</summary>
        public bool RemoveWorker(int id)
        {
            if (!_workers.Remove(id))
                return false;
            WorkerRemoved?.Invoke(id);
            return true;
        }

        /// <summary>修改工人实体，触发 WorkerModified 事件。</summary>
        public void ModifyWorker(int id, Action<WorkerData> modifier)
        {
            if (_workers.TryGetValue(id, out var data))
            {
                modifier?.Invoke(data);
                WorkerModified?.Invoke(id);
            }
        }

        /// <summary>遍历所有工人（只读）。</summary>
        public IEnumerable<KeyValuePair<int, WorkerData>> AllWorkers => _workers;

        /// <summary>工人总数。</summary>
        public int WorkerCount => _workers.Count;

        // ================================================================
        // 通用查询
        // ================================================================

        /// <summary>根据实体类型和 ID 获取实体（返回 IWorldEntity 接口）。</summary>
        public IWorldEntity GetEntity(EntityType type, int id)
        {
            switch (type)
            {
                case EntityType.Tree:     return GetTree(id);
                case EntityType.Building: return GetBuilding(id);
                case EntityType.Road:     return GetRoad(id);
                case EntityType.Worker:   return GetWorker(id);
                default:                  return null;
            }
        }

        // ================================================================
        // 生命周期
        // ================================================================

        /// <summary>清空所有数据和事件订阅，重置 ID 计数器。</summary>
        public void Clear()
        {
            _trees.Clear();
            _buildings.Clear();
            _roads.Clear();
            _workers.Clear();
            _idGenerator.Reset();

            // 清空事件订阅，避免内存泄漏
            TreeAdded = TreeRemoved = TreeModified = null;
            BuildingAdded = BuildingRemoved = BuildingModified = null;
            RoadAdded = RoadRemoved = null;
            WorkerAdded = WorkerRemoved = WorkerModified = null;
        }
    }
}
