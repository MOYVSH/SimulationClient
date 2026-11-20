using System;
using System.Collections.Generic;

namespace Simulation
{
    /// <summary>
    /// 世界权威数据层（P0 泛化：统一实体仓储）。
    ///
    /// 与泛化前的区别：
    ///   旧：4 张 Dictionary&lt;int, T&gt;、4 套 CRUD、11 个按类型分开的事件。
    ///       每加一种实体都要加一张表 + 一套 CRUD + 三个事件 + 一个 switch 分支。
    ///   新：1 张主表 + 1 张按 TypeId 的分组索引 + 1 套通用 CRUD + 3 个通用事件。
    ///       新增实体类型只需实现 IWorldEntity 并给一个 TypeId 常量。
    ///
    /// 与 WorldGrid 的关系（不变）：
    ///   WorldData = 「实体是什么」（按 ID 查完整数据）
    ///   WorldGrid = 「格子上有什么」（按坐标查 OccupantId + TypeId）
    ///   两者通过 int ID 建立双向索引。
    ///
    /// 事件设计原则（沿用旧版，保持热路径零额外分配）：
    ///   1. 事件只传 ID，不传实体对象；订阅方按需 Get(id) 反查
    ///   2. 用 C# 原生 event Action&lt;int&gt;，不用 QFramework SendEvent
    ///   3. 通用事件不区分类型：想知道类型就用 Get(id).TypeId，
    ///      或改用 CountOfType / GetAllOfType 按类型过滤
    ///   4. Removed 在实体移出主表之后触发，订阅方 Get(id) 返回 null 属于预期行为
    ///
    /// 线程安全：
    ///   WorldData 只应在 Unity 主线程访问，不做线程安全处理。
    /// </summary>
    public class WorldData
    {
        // ---- 数据存储 ----
        // _entities：按 ID 查完整数据的主表（权威来源）
        // _byType  ：按 TypeId 的分组索引，值指向同一批对象引用（不复制数据）
        // 两张表在 Create / Remove 中同步维护；类型为空时分组会被移除，避免空字典堆积。

        private readonly Dictionary<int, IWorldEntity> _entities = new Dictionary<int, IWorldEntity>();
        private readonly Dictionary<int, Dictionary<int, IWorldEntity>> _byType =
            new Dictionary<int, Dictionary<int, IWorldEntity>>();

        private readonly IdGenerator _idGenerator = new IdGenerator();

        // ---- 事件定义 ----
        // 通用三件套，取代旧版 Tree/Building/Road/Worker 各自的 Added/Removed/Modified。

        /// <summary>实体创建后触发，参数为实体 ID。</summary>
        public event Action<int> EntityAdded;

        /// <summary>实体删除后触发，参数为实体 ID。</summary>
        public event Action<int> EntityRemoved;

        /// <summary>实体被修改后触发，参数为实体 ID。</summary>
        public event Action<int> EntityModified;

        // ================================================================
        // 通用 CRUD
        // ================================================================

        /// <summary>
        /// 创建实体：分配 ID、写入主表与类型索引、触发 EntityAdded。
        /// 返回新 ID；传入 null 时返回 0（0 = 无实体，不触发事件）。
        /// </summary>
        public int Create(IWorldEntity entity)
        {
            if (entity == null)
                return 0;

            int id = _idGenerator.Next();
            entity.Id = id;
            _entities[id] = entity;

            int typeId = entity.TypeId;
            if (!_byType.TryGetValue(typeId, out var group))
            {
                group = new Dictionary<int, IWorldEntity>();
                _byType[typeId] = group;
            }
            group[id] = entity;

            EntityAdded?.Invoke(id);
            return id;
        }

        /// <summary>按 ID 获取实体（接口形式）。不存在返回 null。</summary>
        public IWorldEntity Get(int id)
        {
            return _entities.TryGetValue(id, out var entity) ? entity : null;
        }

        /// <summary>按 ID 获取指定 CLR 类型的实体。类型不匹配或不存在返回 null。</summary>
        public T Get<T>(int id) where T : class, IWorldEntity
        {
            return Get(id) as T;
        }

        /// <summary>按 ID 查询类型。不存在返回 EntityTypes.None(0)。</summary>
        public int GetTypeId(int id)
        {
            return _entities.TryGetValue(id, out var entity) ? entity.TypeId : EntityTypes.None;
        }

        /// <summary>删除实体：同步清理主表与类型索引，触发 EntityRemoved。</summary>
        public bool Remove(int id)
        {
            if (!_entities.TryGetValue(id, out var entity))
                return false;

            _entities.Remove(id);

            if (_byType.TryGetValue(entity.TypeId, out var group))
            {
                group.Remove(id);
                if (group.Count == 0)
                    _byType.Remove(entity.TypeId);
            }

            EntityRemoved?.Invoke(id);
            return true;
        }

        /// <summary>修改实体并触发 EntityModified（仅当 ID 存在时）。</summary>
        public void Modify(int id, Action<IWorldEntity> modifier)
        {
            if (_entities.TryGetValue(id, out var entity))
            {
                modifier?.Invoke(entity);
                EntityModified?.Invoke(id);
            }
        }

        /// <summary>按具体类型修改实体，避免调用方自己做 as 转换。</summary>
        public void Modify<T>(int id, Action<T> modifier) where T : class, IWorldEntity
        {
            if (Get(id) is T typed)
            {
                modifier?.Invoke(typed);
                EntityModified?.Invoke(id);
            }
        }

        // ================================================================
        // 遍历与统计
        // ================================================================

        /// <summary>遍历全部实体（只读，值集合视图）。</summary>
        public IEnumerable<IWorldEntity> AllEntities => _entities.Values;

        /// <summary>遍历指定 TypeId 的全部实体；该类型无实体时返回空集合。</summary>
        public IEnumerable<IWorldEntity> GetAllOfType(int typeId)
        {
            if (_byType.TryGetValue(typeId, out var group))
                return group.Values;

            return Array.Empty<IWorldEntity>();
        }

        /// <summary>遍历指定 CLR 类型的全部实体（类型安全便利方法）。</summary>
        public IEnumerable<T> GetAll<T>() where T : class, IWorldEntity
        {
            foreach (var entity in _entities.Values)
            {
                if (entity is T typed)
                    yield return typed;
            }
        }

        /// <summary>全部实体总数。</summary>
        public int Count => _entities.Count;

        /// <summary>指定 TypeId 的实体数量。</summary>
        public int CountOfType(int typeId)
        {
            return _byType.TryGetValue(typeId, out var group) ? group.Count : 0;
        }

        // ================================================================
        // 生命周期
        // ================================================================

        /// <summary>清空所有数据和事件订阅，重置 ID 计数器。</summary>
        public void Clear()
        {
            _entities.Clear();
            _byType.Clear();
            _idGenerator.Reset();

            // 清空事件订阅，避免内存泄漏
            EntityAdded = null;
            EntityRemoved = null;
            EntityModified = null;
        }
    }
}
