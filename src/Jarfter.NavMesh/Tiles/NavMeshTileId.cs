namespace Jarfter.NavMesh.Tiles;

/// <summary>
/// 标识 tiled 导航网格中的一个全局坐标 tile.
/// Layer 可用于在同一二维坐标上存放彼此独立的逻辑层.
/// </summary>
public readonly record struct NavMeshTileId(int X, int Y, int Layer = 0);
