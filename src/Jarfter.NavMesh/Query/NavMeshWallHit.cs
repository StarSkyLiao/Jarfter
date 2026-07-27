using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Query;

/// <summary>
/// 表示从网格内一点到最近边界墙的查询结果.
/// </summary>
public readonly record struct NavMeshWallHit(double Distance, NavMeshPoint Position, NavMeshPoint Normal);
