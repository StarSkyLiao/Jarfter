namespace Jarfter.NavMesh.Geometry;

/// <summary>
/// 表示二维导航网格查询使用的轴对齐包围盒.
/// Min 坐标必须不大于对应的 Max 坐标, 且所有坐标必须为有限 double.
/// </summary>
public readonly record struct NavMeshBounds(double MinX, double MinY, double MaxX, double MaxY);
