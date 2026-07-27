namespace Jarfter.NavMesh.Topology;

/// <summary>
/// 引用三个顶点的逆时针三角形.
/// AreaId 可供查询过滤器和移动代价策略区分不同通行区域, Flags 可提供位掩码通行过滤.
/// </summary>
public readonly record struct NavMeshTriangle(
    int First,
    int Second,
    int Third,
    int AreaId = 0,
    uint Flags = uint.MaxValue);
