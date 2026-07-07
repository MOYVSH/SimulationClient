using System;

namespace Simulation
{
    /// <summary>
    /// 世界格子的整数索引坐标。
    ///
    /// 坐标系约定：
    ///   - X 轴向右（East）
    ///   - Z 轴向上（North，即 Unity 世界坐标的 +Z 方向）
    ///   - Y 轴由 Terrain 高度决定，不存储在 GridPos 中
    ///
    /// 使用 readonly struct 的原因：
    ///   GridPos 本质上是一对整数，不需要引用语义。
    ///   readonly struct 在栈上分配，不产生 GC 压力，
    ///   适合在寻路、格子遍历等每帧大量创建的热路径中使用。
    ///
    /// 实现 IEquatable&lt;GridPos&gt; 的原因：
    ///   Dictionary/HashSet 等泛型容器在比较时，若类型实现了 IEquatable&lt;T&gt;，
    ///   会直接调用强类型的 Equals(GridPos)，避免装箱到 object 产生 GC。
    /// </summary>
    public readonly struct GridPos : IEquatable<GridPos>
    {
        /// <summary>格子 X 索引（对应 Unity 世界坐标的 X 轴方向）。</summary>
        public readonly int X;

        /// <summary>格子 Z 索引（对应 Unity 世界坐标的 Z 轴方向）。</summary>
        public readonly int Z;

        public GridPos(int x, int z)
        {
            X = x;
            Z = z;
        }

        // ---- 算术运算符 ----
        // 目的：方便表达"邻格偏移"，例如 pos + new GridPos(1, 0) 得到右邻格。
        // 后续 A* 寻路在展开邻居时会频繁使用这两个运算符。

        public static GridPos operator +(GridPos a, GridPos b) => new GridPos(a.X + b.X, a.Z + b.Z);
        public static GridPos operator -(GridPos a, GridPos b) => new GridPos(a.X - b.X, a.Z - b.Z);

        // ---- 相等性 ----
        // 注意：必须同时重写 Equals(object) 和 GetHashCode，
        // 否则编译器会警告，且行为在某些情况下不一致。

        public bool Equals(GridPos other) => X == other.X && Z == other.Z;

        public override bool Equals(object obj) => obj is GridPos other && Equals(other);

        /// <summary>
        /// 哈希算法说明：
        ///   将 X 左移 16 位后与 Z 做异或（XOR）。
        ///   这是一种简单且碰撞率低的方式，适合格子坐标通常在 [-32768, 32767] 范围内的场景。
        ///   如果地图极大（超过 65536 格），可以考虑用 HashCode.Combine(X, Z)。
        /// </summary>
        public override int GetHashCode() => (X << 16) ^ Z;

        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);
        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);

        public override string ToString() => $"GridPos({X}, {Z})";

        // ---- 常用方向常量 ----
        // 四方向邻格偏移，供寻路/扩散等算法直接引用，避免每次 new。
        public static readonly GridPos Zero  = new GridPos(0,  0);
        public static readonly GridPos Right = new GridPos(1,  0);
        public static readonly GridPos Left  = new GridPos(-1, 0);
        public static readonly GridPos Up    = new GridPos(0,  1);
        public static readonly GridPos Down  = new GridPos(0, -1);

        /// <summary>四方向邻格偏移数组（不含斜向），适合寻路时展开邻居。</summary>
        public static readonly GridPos[] CardinalDirections =
        {
            Right, Left, Up, Down
        };
    }
}
