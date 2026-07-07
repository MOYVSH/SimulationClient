using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// 三套坐标系（WorldPos / GridPos / ChunkPos）之间的双向转换工具类。
    ///
    /// 坐标系规则：
    ///   - 每个格子（Cell）在世界中是边长为 cellSize 的正方形
    ///   - 格子 (X, Z) 的世界中心点为 ((X + 0.5) * cellSize, Y, (Z + 0.5) * cellSize)
    ///   - Chunk 大小固定为 chunkSize 个格子，例如 32 × 32
    ///
    /// 负数处理说明（重要）：
    ///   C# 的整数除法是"向零取整"（Truncation Division）：
    ///     -1 / 32 = 0（向零取整结果）
    ///   但我们需要"向负无穷取整"（Floor Division）：
    ///     -1 应属于 Chunk -1，而不是 Chunk 0
    ///   因此所有涉及负数的整除都要用 FloorDiv 辅助方法。
    ///
    ///   对比示例（chunkSize = 32）：
    ///     GridPos(-1,  0) → 期望 ChunkPos(-1, 0)
    ///     C# 直接 -1/32 = 0  ← 错误！
    ///     FloorDiv(-1, 32) = -1 ← 正确
    ///
    /// 零 GC 保证：
    ///   所有方法只操作 readonly struct（栈分配），不产生堆分配。
    /// </summary>
    public static class CoordinateUtility
    {
        // ---- 全局参数 ----
        // 这里用常量而非配置文件，原因：
        //   格子大小和 Chunk 大小是整个系统的基础约定，不应在运行时变化。
        //   改成常量让编译器能做内联优化，也让所有使用方更易理解。

        /// <summary>每个格子的世界边长（单位：Unity 米）。</summary>
        public const float CellSize = 1f;

        /// <summary>每个 Chunk 包含的格子数（一维）。Chunk 为 ChunkSize × ChunkSize 格。</summary>
        public const int ChunkSize = 32;

        // ---- WorldPos → GridPos ----
        // 将浮点世界坐标转换为整数格子索引。
        // 使用 FloorToInt 而不是 RoundToInt 或 TruncToInt：
        //   - RoundToInt：会把 (0.9, 0, 0) 归到格子 1，但它实际还在格子 0 内
        //   - FloorToInt：正确地把 [0, 1) 范围内所有点归到格子 0
        //   - 负数同样正确：(-0.1, 0, 0) → FloorToInt(-0.1) = -1，属于格子 -1

        /// <summary>
        /// 将世界坐标转换为格子索引。
        /// Y 轴被丢弃（格子坐标是水平面 2D 的）。
        /// </summary>
        public static GridPos WorldToGrid(WorldPos world)
        {
            int x = Mathf.FloorToInt(world.X / CellSize);
            int z = Mathf.FloorToInt(world.Z / CellSize);
            return new GridPos(x, z);
        }

        // ---- GridPos → WorldPos ----
        // 返回格子中心点的世界坐标（不含高度采样）。
        // 为什么取中心而不是角点？
        //   角点坐标是 (X * cellSize, ...)，中心是 ((X + 0.5) * cellSize, ...)。
        //   取中心更符合直觉，也方便放置对象时自动居中到格子内。

        /// <summary>
        /// 将格子索引转换为该格子中心点的世界坐标（Y = 0，需调用方自行采样高度）。
        /// </summary>
        public static WorldPos GridToWorld(GridPos grid)
        {
            float x = (grid.X + 0.5f) * CellSize;
            float z = (grid.Z + 0.5f) * CellSize;
            return new WorldPos(x, 0f, z);
        }

        /// <summary>
        /// 将格子索引转换为该格子中心点的世界坐标，并通过 terrain 采样 Y 高度。
        /// </summary>
        public static WorldPos GridToWorld(GridPos grid, Terrain terrain)
        {
            float x = (grid.X + 0.5f) * CellSize;
            float z = (grid.Z + 0.5f) * CellSize;
            float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0f, z)) : 0f;
            return new WorldPos(x, y, z);
        }

        // ---- GridPos → ChunkPos ----
        // 核心公式：ChunkX = Floor(GridX / ChunkSize)
        // 必须用 FloorDiv，否则负坐标会归到错误的 Chunk。

        /// <summary>根据格子索引计算所属 Chunk 索引。</summary>
        public static ChunkPos GridToChunk(GridPos grid)
        {
            int cx = FloorDiv(grid.X, ChunkSize);
            int cz = FloorDiv(grid.Z, ChunkSize);
            return new ChunkPos(cx, cz);
        }

        // ---- ChunkPos → GridPos（Chunk 起始角）----
        // 返回 Chunk 左下角（最小 X/Z）的格子索引。
        // 用途：遍历 Chunk 内所有格子时作为起始点。

        /// <summary>返回指定 Chunk 内左下角（最小索引）的格子坐标。</summary>
        public static GridPos ChunkToGridOrigin(ChunkPos chunk)
        {
            return new GridPos(chunk.X * ChunkSize, chunk.Z * ChunkSize);
        }

        // ---- WorldPos → ChunkPos（快捷方法）----

        /// <summary>将世界坐标直接转换为所属 Chunk 索引。</summary>
        public static ChunkPos WorldToChunk(WorldPos world)
        {
            return GridToChunk(WorldToGrid(world));
        }

        // ---- 辅助：地板除法（Floor Division）----
        // C# 内建的 / 运算符是"向零取整"，对负数结果不符合我们的需求。
        //
        // 推导：
        //   Floor Division 定义：a / b = floor(a / b)（数学意义上的地板）
        //   实现方式：先做 C# 整除，若有余数且被除数与除数异号，则结果再减 1。
        //
        //   验证：
        //     FloorDiv(-1, 32) → C# -1/32 = 0，余数 = -1%32 = -1，异号 → 0-1 = -1 ✓
        //     FloorDiv(32, 32) → C# 32/32 = 1，余数 = 0 → 1 ✓
        //     FloorDiv(31, 32) → C# 31/32 = 0，余数 = 31，同号 → 0 ✓
        //     FloorDiv(-33, 32) → C# -33/32 = -1，余数 = -1，异号 → -1-1 = -2 ✓

        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            // 有余数 且 被除数与除数异号（即数学上结果是负小数）
            if (a % b != 0 && ((a ^ b) < 0))
                q--;
            return q;
        }
    }
}
