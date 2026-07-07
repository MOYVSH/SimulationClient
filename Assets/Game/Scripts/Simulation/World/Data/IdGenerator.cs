namespace Simulation
{
    /// <summary>
    /// 实体 ID 生成器。
    ///
    /// 设计原则：
    ///   - 从 1 开始递增（ID = 0 保留给"无实体"，见 Cell.cs 的约定）
    ///   - 每个 WorldData 实例拥有独立的 IdGenerator
    ///   - 场景切换时通过 GameWorldModel.ClearWorldData() 销毁，新场景重新创建
    ///
    /// 为什么不用 GUID 或 UUID？
    ///   1. int 比较和哈希更快（4 字节 vs 16+ 字节）
    ///   2. int 在字典中内存更紧凑
    ///   3. 单机游戏不需要全局唯一，只需当前世界内唯一
    ///   4. 递增 ID 方便调试（ID 越小说明创建越早）
    ///
    /// 溢出问题：
    ///   int.MaxValue = 2,147,483,647（约 21 亿）
    ///   即使每秒创建 100 个实体，也要连续运行 248 天才会溢出
    ///   对于模拟经营游戏完全不是问题
    /// </summary>
    public class IdGenerator
    {
        private int _nextId;

        /// <summary>创建 ID 生成器，起始 ID 为 1。</summary>
        public IdGenerator()
        {
            _nextId = 1;
        }

        /// <summary>生成下一个唯一 ID。</summary>
        public int Next()
        {
            return _nextId++;
        }

        /// <summary>重置 ID 计数器（用于场景重新初始化）。</summary>
        public void Reset()
        {
            _nextId = 1;
        }

        /// <summary>当前已分配的 ID 数量（用于调试）。</summary>
        public int AllocatedCount => _nextId - 1;
    }
}
