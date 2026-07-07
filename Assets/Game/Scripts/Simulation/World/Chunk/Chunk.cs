using System.Collections.Generic;

namespace Simulation
{
    /// <summary>
    /// Chunk（空间分区块）数据结构。
    ///
    /// 设计说明：
    ///   Chunk 本身只是一个"区域描述符"，描述一块 32x32 格子的区域。
    ///   它不存储 Cell 数据（Cell 在 WorldGrid 中按 GridPos 索引）。
    ///   它不管理激活状态（状态在 ChunkSystem 中统一管理）。
    ///
    ///   这样做的好处：
    ///   1. Chunk 是 readonly struct，零 GC 开销
    ///   2. 可以按需创建，不需要预先分配所有 Chunk 对象
    ///   3. 遍历 Chunk 内格子时，通过 GetCells() 方法按需生成
    ///
    /// Chunk 与 ChunkPos 的区别：
    ///   - ChunkPos：只是 (int X, int Z) 索引，用于字典 Key 和事件参数
    ///   - Chunk：包含完整信息（位置 + 尺寸），用于需要操作 Chunk 区域的场景
    /// </summary>
    public readonly struct Chunk
    {
        /// <summary>Chunk 的索引坐标。</summary>
        public readonly ChunkPos ChunkPos;

        /// <summary>Chunk 左下角（最小索引）的格子坐标。</summary>
        public readonly GridPos Origin;

        /// <summary>Chunk 的格子宽度（固定 32）。</summary>
        public int Size => CoordinateUtility.ChunkSize;

        public Chunk(ChunkPos chunkPos)
        {
            ChunkPos = chunkPos;
            Origin = CoordinateUtility.ChunkToGridOrigin(chunkPos);
        }

        /// <summary>
        /// 遍历此 Chunk 内的所有格子坐标。
        /// 返回 32x32 = 1024 个 GridPos。
        /// </summary>
        public IEnumerable<GridPos> GetCells()
        {
            int size = CoordinateUtility.ChunkSize;
            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    yield return new GridPos(Origin.X + x, Origin.Z + z);
                }
            }
        }

        /// <summary>检查指定格子是否属于此 Chunk。</summary>
        public bool Contains(GridPos cellPos)
        {
            int size = CoordinateUtility.ChunkSize;
            return cellPos.X >= Origin.X && cellPos.X < Origin.X + size
                && cellPos.Z >= Origin.Z && cellPos.Z < Origin.Z + size;
        }

        public override string ToString() => $"Chunk({ChunkPos.X}, {ChunkPos.Z})";
    }
}
