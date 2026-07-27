namespace Jarfter.NavMesh.Topology;

/// <summary>
/// 引用一组逆时针顶点的凸导航多边形.
/// 该类型是二维 NavMesh 的逻辑构建单元. 顶点索引由创建网格时提供的顶点表解释.
/// </summary>
public sealed class NavMeshConvexPolygon
{
    private readonly int[] m_VertexIndices;

    /// <summary>
    /// 使用顶点索引创建凸多边形定义的副本.
    /// </summary>
    /// <param name="vertexIndices">按逆时针顺序排列, 且不含重复闭合点的顶点索引.</param>
    /// <param name="areaId">供查询过滤器和移动代价策略使用的区域标识.</param>
    /// <param name="flags">供查询过滤器使用的可通行位掩码.</param>
    public NavMeshConvexPolygon(ReadOnlySpan<int> vertexIndices, int areaId = 0, uint flags = uint.MaxValue)
    {
        m_VertexIndices = vertexIndices.ToArray();
        AreaId = areaId;
        Flags = flags;
    }

    private NavMeshConvexPolygon(int[] vertexIndices, int areaId, uint flags)
    {
        m_VertexIndices = vertexIndices;
        AreaId = areaId;
        Flags = flags;
    }

    /// <summary>
    /// 获取按边界顺序排列的顶点索引.
    /// </summary>
    public IReadOnlyList<int> VertexIndices => m_VertexIndices;

    /// <summary>
    /// 获取区域标识.
    /// </summary>
    public int AreaId { get; }

    /// <summary>
    /// 获取可通行位掩码.
    /// </summary>
    public uint Flags { get; }

    /// <summary>
    /// 使用调用方已独占的顶点索引数组创建多边形, 不再复制数组.
    /// 调用方在调用后不得修改或保留该数组用于可变用途.
    /// </summary>
    /// <param name="vertexIndices">仅由新多边形持有的顶点索引数组.</param>
    /// <param name="areaId">供查询过滤器和移动代价策略使用的区域标识.</param>
    /// <param name="flags">供查询过滤器使用的可通行位掩码.</param>
    /// <returns>取得数组所有权的凸多边形.</returns>
    internal static NavMeshConvexPolygon CreateOwned(int[] vertexIndices, int areaId, uint flags)
    {
        return new NavMeshConvexPolygon(vertexIndices, areaId, flags);
    }

    internal ReadOnlySpan<int> AsSpan() => m_VertexIndices;
}
