using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Topology;

/// <summary>
/// 表示按行进方向定向的二维 polygon 门户.
/// </summary>
internal readonly record struct NavMeshPortal(NavMeshPoint Left, NavMeshPoint Right);
