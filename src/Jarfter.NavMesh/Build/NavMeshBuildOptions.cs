namespace Jarfter.NavMesh.Build;

/// <summary>
/// 控制二维导航网格构建时几何校验和后续偏移处理的参数.
/// </summary>
public sealed class NavMeshBuildOptions
{
    /// <summary>
    /// 获取或设置用来识别重合顶点和零面积环的正数几何容差.
    /// </summary>
    public double Tolerance { get; init; } = 1e-9;

    /// <summary>
    /// 获取或设置移动对象的半径.
    /// 对凸外边界和凸障碍物环会烘焙为安全边距.
    /// </summary>
    public double AgentRadius { get; init; }

    /// <summary>
    /// 获取或设置写入所有已生成三角形的区域标识.
    /// 可由移动代价策略区分区域.
    /// </summary>
    public int AreaId { get; init; }

    /// <summary>
    /// 获取或设置写入所有已生成三角形的通行 flags 位掩码.
    /// 默认允许全部 flags 位.
    /// </summary>
    public uint Flags { get; init; } = uint.MaxValue;
}
