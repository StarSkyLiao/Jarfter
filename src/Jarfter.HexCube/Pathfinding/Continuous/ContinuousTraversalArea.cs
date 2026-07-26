using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 表示连续导航地图中的高代价六边形区域.
/// 移动线段穿过该区域的部分会按 <see cref="TraversalMultiplier"/> 计算额外代价, 但不会阻断通行.
/// </summary>
/// <param name="Shape">高代价区域的连续六边形形状.</param>
/// <param name="TraversalMultiplier">穿过区域时的移动代价倍率. 必须大于 1.</param>
public readonly record struct ContinuousTraversalArea(HexCubeArea2D Shape, double TraversalMultiplier);
