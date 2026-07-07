using System;
using System.Collections.Generic;

namespace Simulation
{
    /// <summary>
    /// Chunk（空间分区块）的整数索引坐标。
    ///
    /// 与 GridPos 的关系：
    ///   ChunkPos = GridPos / ChunkSize（整除，向负无穷取整）
    ///   例如：GridPos(33, 0) 在 ChunkSize=32 时 → ChunkPos(1, 0)
    ///         GridPos(-1, 0) → ChunkPos(-1, 0)（注意负数需要 Floor 除法）
    ///
    /// 为什么不复用 GridPos？
    ///   两者虽然都是 (int X, int Z)，但语义不同：
    ///   - GridPos 代表"第几格"，数值可达数万
    ///   - ChunkPos 代表"第几块"，数值通常在 ±几百以内
    ///   保持两个独立类型，让编译器在类型检查阶段就能发现
    ///   "把格子坐标误当块坐标传入"这类逻辑错误。
    /// </summary>
    public readonly struct ChunkPos : IEquatable<ChunkPos>
    {
        public readonly int X;
        public readonly int Z;

        public ChunkPos(int x, int z)
        {
            X = x;
            Z = z;
        }

        // ---- 相等性（与 GridPos 实现一致）----

        public bool Equals(ChunkPos other) => X == other.X && Z == other.Z;

        public override bool Equals(object obj) => obj is ChunkPos other && Equals(other);

        public override int GetHashCode() => (X << 16) ^ Z;

        public static bool operator ==(ChunkPos a, ChunkPos b) => a.Equals(b);
        public static bool operator !=(ChunkPos a, ChunkPos b) => !a.Equals(b);

        public override string ToString() => $"ChunkPos({X}, {Z})";

        // ---- 相邻 Chunk 遍历 ----
        // GetNeighborsInRadius 返回以当前 Chunk 为中心、半径 r 的正方形范围内的所有 ChunkPos。
        // radius=1 → 3×3=9 个（ChunkManager 的激活窗口默认使用此值）
        // radius=2 → 5×5=25 个
        //
        // 使用方式示例：
        //   foreach (var cp in focusChunk.GetNeighborsInRadius(1)) { ... }
        //
        // 注意：此方法会分配一个 List，不适合极高频调用（每帧调用一次是完全可以的）。
        // 如果需要零分配版本，可以改为接受 ICollection<ChunkPos> 参数由调用方传入。
        public IEnumerable<ChunkPos> GetNeighborsInRadius(int radius)
        {
            for (int dz = -radius; dz <= radius; dz++)
            for (int dx = -radius; dx <= radius; dx++)
                yield return new ChunkPos(X + dx, Z + dz);
        }

        public static readonly ChunkPos Zero = new ChunkPos(0, 0);
    }
}
