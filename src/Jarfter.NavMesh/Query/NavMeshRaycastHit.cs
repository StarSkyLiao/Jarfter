using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Query;

/// <summary>
/// 表示二维射线与导航网格边界的首次命中.
/// </summary>
public readonly record struct NavMeshRaycastHit(double T, NavMeshPoint Position, NavMeshPoint Normal);
