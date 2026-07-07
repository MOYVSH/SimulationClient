using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// Unity 世界坐标的薄封装。
    ///
    /// 为什么不直接用 Vector3？
    ///   在代码中直接传递 Vector3 时，含义是模糊的：
    ///   它可能是世界坐标、本地坐标、方向向量……
    ///   用 WorldPos 作为独立类型，让方法签名更具自文档性：
    ///     void FocusOn(WorldPos pos)   ← 一眼知道是世界坐标
    ///     void FocusOn(Vector3 pos)    ← 不清楚是哪种坐标空间
    ///
    /// Y 轴的处理：
    ///   GridPos 和 ChunkPos 都忽略 Y 轴（水平面 2D 格子）。
    ///   WorldPos 保留 Y，用于与 Unity Transform、射线检测等 3D API 对接。
    ///   Y 值通常由 Terrain.SampleHeight() 填入，不由调用方手动设置。
    /// </summary>
    public readonly struct WorldPos
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public WorldPos(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>忽略 Y 轴的构造函数（Y 置 0）。常用于纯水平坐标的场景。</summary>
        public WorldPos(float x, float z) : this(x, 0f, z) { }

        // ---- 与 Unity Vector3 互转 ----
        // implicit（隐式）转换：允许在需要 Vector3 的地方直接传入 WorldPos，无需显式 cast。
        // 这在调用 Transform.position = worldPos 时非常方便。

        public static implicit operator Vector3(WorldPos pos) => new Vector3(pos.X, pos.Y, pos.Z);
        public static implicit operator WorldPos(Vector3 v)   => new WorldPos(v.x, v.y, v.z);

        public override string ToString() => $"WorldPos({X:F2}, {Y:F2}, {Z:F2})";

        public static readonly WorldPos Zero = new WorldPos(0f, 0f, 0f);
    }
}
