using System.Diagnostics;

namespace Jarfter.HexCube.Numerics;

/// <summary>
/// 表示六边形网格中一个具有面积的正六边形区域.
/// </summary>
/// <param name="Position">区域的坐标.</param>
/// <param name="RadiusScale">六边形边长相对于单位六边形的倍数.</param>
[DebuggerDisplay("Position = {Position}, RadiusScale = {RadiusScale}")]
public readonly partial record struct HexCubeArea2D(HexCubePoint Position, double RadiusScale)
{
    /// <summary>
    /// 面积为 0 的正六边形区域.
    /// </summary>
    public static HexCubeArea2D Zero => new HexCubeArea2D(HexCubePoint.Zero, 0);

    /// <summary>
    /// 单位正六边形区域.
    /// </summary>
    public static HexCubeArea2D Identity => new HexCubeArea2D(HexCubePoint.Zero, 1);

}
