namespace Jarfter.NavMesh.Geometry;

/// <summary>
/// 表示一个不包含重复闭合点的二维多边形环.
/// </summary>
/// <param name="vertices">不含重复首尾点的多边形顶点.</param>
public sealed class NavMeshPolygon(ReadOnlySpan<NavMeshPoint> vertices)
{
    private readonly NavMeshPoint[] m_Vertices = [.. vertices];

    /// <summary>
    /// 获取按边界顺序排列的只读顶点.
    /// </summary>
    public IReadOnlyList<NavMeshPoint> Vertices => m_Vertices;

    internal ReadOnlySpan<NavMeshPoint> AsSpan() => m_Vertices;
}
