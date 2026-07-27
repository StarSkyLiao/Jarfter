namespace Jarfter.NavMesh.Geometry;

/// <summary>
/// 表示导航网格中的二维边界线段.
/// </summary>
public readonly record struct NavMeshSegment(NavMeshPoint Start, NavMeshPoint End);
