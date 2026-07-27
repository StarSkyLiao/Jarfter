using Jarfter.NavMesh.Topology;

namespace Jarfter.NavMesh.Query;

/// <summary>
/// 表示局部导航查询中按累计移动成本发现的一个 polygon.
/// <see cref="ParentPolygonRef"/> 可用于在调用方缓冲区中还原从查询起点到该 polygon 的局部树.
/// </summary>
public readonly record struct NavMeshLocalPolygon(
    NavMeshPolygonRef PolygonRef,
    NavMeshPolygonRef ParentPolygonRef,
    double Cost
);
