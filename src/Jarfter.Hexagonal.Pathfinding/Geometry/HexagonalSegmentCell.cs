using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.Pathfinding.Geometry;

/// <summary>
/// 表示线段在一个主六边形格子内部经过的参数区间.
/// 参数范围以原线段为基准, 0 表示起点, 1 表示终点.
/// </summary>
/// <param name="Point">线段经过的轴向格子坐标.</param>
/// <param name="StartFraction">线段进入该格子时的参数值.</param>
/// <param name="EndFraction">线段离开该格子时的参数值.</param>
public readonly record struct HexagonalSegmentCell(HexagonalCubePoint Point, double StartFraction, double EndFraction);
