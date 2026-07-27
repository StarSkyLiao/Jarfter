using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.Tiles;

/// <summary>
/// 描述一次 tile 更新. <see cref="NavMesh"/> 为 <see langword="null"/> 时移除对应 tile;
/// 否则添加或替换对应 tile.
/// </summary>
public readonly record struct NavMeshTileUpdate(NavMeshTileId TileId, Mesh? NavMesh);
