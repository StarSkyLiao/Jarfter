using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Topology;

/// <summary>
/// 表示已投影到导航网格上的位置及其所属 polygon 引用.
/// 可由移动对象缓存, 以避免后续查询重复定位起点.
/// </summary>
public readonly record struct NavMeshLocation
{
    /// <summary>
    /// 获取位置所属的 polygon 引用.
    /// </summary>
    public NavMeshPolygonRef PolygonRef { get; }

    /// <summary>
    /// 获取网格内的二维坐标.
    /// </summary>
    public NavMeshPoint Position { get; }

    internal NavMeshLocation(NavMeshPolygonRef polygonRef, NavMeshPoint position)
    {
        PolygonRef = polygonRef;
        Position = position;
    }
}
