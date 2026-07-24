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
}
