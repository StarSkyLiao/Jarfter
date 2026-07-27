using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Topology;

/// <summary>
/// 描述两个可行走位置之间的跳跃连接.
/// 跳跃连接不代表地面 portal, 因此会在平滑路径中保留其两个端点, 并固定增加 <see cref="FixedCost"/>.
/// </summary>
public readonly record struct NavMeshJumpConnection(
    NavMeshPoint Start,
    NavMeshPoint End,
    double FixedCost,
    bool IsBidirectional = false
);
