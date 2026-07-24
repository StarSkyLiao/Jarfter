namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubeArea2D
{
    /// <summary>
    /// 判断指定点是否位于当前六边形区域内.
    /// </summary>
    public bool Contains(HexCubePoint point)
    {
        double deltaQ = Math.Abs(point.Q - Position.Q);
        double deltaR = Math.Abs(point.R - Position.R);
        double deltaS = Math.Abs(point.S - Position.S);

        return deltaQ <= RadiusScale && deltaR <= RadiusScale && deltaS <= RadiusScale;
    }

    /// <summary>
    /// 判断指定六边形区域是否完全包含于当前六边形区域.
    /// </summary>
    public bool Contains(HexCubeArea2D other)
    {
        double distance = RadiusScale - other.RadiusScale;
        double deltaQ = Math.Abs(other.Position.Q - Position.Q);
        double deltaR = Math.Abs(other.Position.R - Position.R);
        double deltaS = Math.Abs(other.Position.S - Position.S);

        return deltaQ <= distance && deltaR <= distance && deltaS <= distance;
    }

}
