namespace Jarfter.NavMesh.Topology;

/// <summary>
/// 标识不可变 <see cref="NavMesh"/> 中的一个逻辑凸多边形.
/// 该引用只能由同一个网格实例返回和使用. 网格重建或切换快照后, 旧引用会自动失效.
/// </summary>
public readonly record struct NavMeshPolygonRef
{
    /// <summary>
    /// 获取 polygon 在所属网格中的零基索引.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 获取所属网格实例的内部标识. 仅供网格验证引用归属.
    /// </summary>
    internal long NavMeshId { get; }

    internal NavMeshPolygonRef(long navMeshId, int index)
    {
        NavMeshId = navMeshId;
        Index = index;
    }

    /// <summary>
    /// 获取表示无效多边形的引用.
    /// </summary>
    public static NavMeshPolygonRef Invalid => default;
}
