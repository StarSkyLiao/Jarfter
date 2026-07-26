using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 定义一种六边形距离启发函数.
/// </summary>
public interface IHeuristic
{
    /// <summary>
    /// 计算当前点到目标点的启发价值.
    /// </summary>
    double Calculate(HexCubePoint current, HexCubePoint target);

    /// <summary>
    /// 提供默认的启发函数实现.
    /// </summary>
    public class Default : IHeuristic
    {
        /// <summary>
        /// 提供默认启发函数实现的单例.
        /// </summary>
        public static readonly Default Instance = new Default();

        /// <inheritdoc />
        public double Calculate(HexCubePoint current, HexCubePoint target)
        {
            double dq = current.Q - target.Q;
            double dr = current.R - target.R;
            double ds = current.S - target.S;

            return Math.Max(Math.Abs(dq), Math.Max(Math.Abs(dr), Math.Abs(ds)));
        }

        private Default(){}
    }

    /// <summary>
    /// 提供基于六边形坐标平面直线距离的启发函数实现.
    /// 适用于线段代价不小于对应 <see cref="HexCubePoint.DistanceTo"/> 结果的 Theta* 配置.
    /// </summary>
    public sealed class Euclidean : IHeuristic
    {
        /// <summary>
        /// 提供欧几里得启发函数实现的单例.
        /// </summary>
        public static readonly Euclidean Instance = new Euclidean();

        /// <inheritdoc />
        public double Calculate(HexCubePoint current, HexCubePoint target)
        {
            return current.DistanceTo(target);
        }

        private Euclidean(){}
    }
}
