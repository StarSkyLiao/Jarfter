using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Query;

/// <summary>
/// 表示一条路径实际使用的一次跳跃连接.
/// </summary>
public readonly record struct NavMeshJumpTraversal(
    int ConnectionIndex,
    NavMeshPoint Start,
    NavMeshPoint End,
    double FixedCost);
