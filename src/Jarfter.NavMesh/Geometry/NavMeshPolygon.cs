namespace Jarfter.NavMesh.Geometry;

/// <summary>
/// 表示一个不包含重复闭合点的二维多边形环.
/// </summary>
public sealed class NavMeshPolygon
{
    private readonly NavMeshPoint[] m_Vertices;

    /// <summary>
    /// 使用调用方提供的顶点创建多边形环副本.
    /// </summary>
    /// <param name="vertices">不含重复首尾点的多边形顶点.</param>
    public NavMeshPolygon(ReadOnlySpan<NavMeshPoint> vertices)
    {
        m_Vertices = vertices.ToArray();
    }

    /// <summary>
    /// 获取按边界顺序排列的只读顶点.
    /// </summary>
    public IReadOnlyList<NavMeshPoint> Vertices => m_Vertices;

    internal ReadOnlySpan<NavMeshPoint> AsSpan() => m_Vertices;
}
