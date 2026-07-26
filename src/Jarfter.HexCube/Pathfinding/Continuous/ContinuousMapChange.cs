using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 表示一次连续导航地图变更.
/// <see cref="ChangedBounds"/> 同时覆盖变更前和变更后的障碍物区域, 可用于快速筛选可能受影响的路径线段.
/// </summary>
/// <param name="Revision">应用该变更后的地图版本.</param>
/// <param name="ChangedBounds">覆盖该变更影响范围的包围盒.</param>
public readonly record struct ContinuousMapChange(long Revision, HexCubeBounds2D ChangedBounds);
