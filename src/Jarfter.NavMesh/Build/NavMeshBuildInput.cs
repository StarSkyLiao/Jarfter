using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Build;

/// <summary>
/// 描述一次二维导航网格构建的业务几何输入.
/// </summary>
public sealed class NavMeshBuildInput
{
    private readonly NavMeshPolygon[] m_Obstacles;

    /// <summary>
    /// 创建构建输入并复制障碍物集合结构.
    /// </summary>
    /// <param name="boundary">可行走区域的唯一外边界.</param>
    /// <param name="obstacles">可选的障碍物环.</param>
    public NavMeshBuildInput(NavMeshPolygon boundary, IReadOnlyList<NavMeshPolygon>? obstacles = null)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        Boundary = boundary;
        m_Obstacles = obstacles is null ? [] : [.. obstacles];
    }

    /// <summary>
    /// 获取可行走区域的外边界.
    /// </summary>
    public NavMeshPolygon Boundary { get; }

    /// <summary>
    /// 获取需从可行走区域中扣除的障碍物环.
    /// </summary>
    public IReadOnlyList<NavMeshPolygon> Obstacles => m_Obstacles;
}
