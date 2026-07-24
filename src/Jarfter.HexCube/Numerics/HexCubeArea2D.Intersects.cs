namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubeArea2D
{
    /// <summary>
    /// 判断线段是否与当前六边形区域相交.
    /// </summary>
    public bool IntersectsHex(HexCubeLine2D line)
    {
        double tMin = 0;
        double tMax = 1;

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
        const double Epsilon = 1e-12;
        double delta = end - start;

        // 线段在该坐标轴方向没有变化.
        if (Math.Abs(delta) < Epsilon)
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
