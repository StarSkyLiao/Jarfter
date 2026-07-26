namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubeArea2D
{
    /// <summary>
    /// 判断线段是否与当前六边形区域相交.
    /// </summary>
    /// <param name="line">待判断的线段.</param>
    /// <returns>当线段与当前区域存在交集时返回 true, 否则返回 false.</returns>
    public bool IntersectsHex(HexCubeLine2D line) => TryGetIntersectionRange(line, out _, out _);

    /// <summary>
    /// 判断线段是否穿过当前六边形区域的内部.
    /// 仅在单点接触边界或顶点时返回 false, 使连续寻路能够以扩大后的障碍物顶点作为绕行折点.
    /// </summary>
    /// <param name="line">待判断的线段.</param>
    /// <param name="epsilon">用于忽略单点边界接触的参数容差. 必须为有限非负数.</param>
    /// <returns>线段与区域内部存在非零长度交集时返回 true, 否则返回 false.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="epsilon"/> 不是有限非负数时抛出.</exception>
    public bool IntersectsInterior(HexCubeLine2D line, double epsilon = 1e-12)
    {
        if (!(epsilon >= 0) || !double.IsFinite(epsilon))
        {
            throw new ArgumentOutOfRangeException(nameof(epsilon), epsilon, "Epsilon must be a finite non-negative number.");
        }

        return TryGetIntersectionRange(line, out double tMin, out double tMax) && tMax - tMin > epsilon;
    }

    /// <summary>
    /// 尝试获取线段与当前六边形区域相交的参数区间.
    /// </summary>
    /// <param name="line">待判断的线段.</param>
    /// <param name="tMin">相交区间的起始参数, 位于 [0, 1] 范围内.</param>
    /// <param name="tMax">相交区间的结束参数, 位于 [0, 1] 范围内.</param>
    /// <returns>当线段与当前区域存在交集时返回 true, 否则返回 false.</returns>
    public bool TryGetIntersectionRange(HexCubeLine2D line, out double tMin, out double tMax)
    {
        tMin = 0;
        tMax = 1;

        if (!ClipAxis(line.Start.Q, line.End.Q, Position.Q - RadiusScale, Position.Q + RadiusScale, ref tMin, ref tMax))
        {
            return false;
        }

        if (!ClipAxis(line.Start.R, line.End.R, Position.R - RadiusScale, Position.R + RadiusScale, ref tMin, ref tMax))
        {
            return false;
        }

        if (!ClipAxis(line.Start.S, line.End.S, Position.S - RadiusScale, Position.S + RadiusScale, ref tMin, ref tMax))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 使用线段裁剪算法限制单个坐标轴上的有效范围.
    /// </summary>
    private static bool ClipAxis(double start, double end, double min, double max, ref double tMin, ref double tMax)
    {
        const double epsilon = 1e-12;
        double delta = end - start;

        // 线段在该坐标轴方向没有变化.
        if (Math.Abs(delta) < epsilon)
        {
            return start >= min && start <= max;
        }

        double t1 = (min - start) / delta;
        double t2 = (max - start) / delta;

        if (t1 > t2) (t1, t2) = (t2, t1);

        tMin = Math.Max(tMin, t1);
        tMax = Math.Min(tMax, t2);

        return tMin <= tMax;
    }

}
