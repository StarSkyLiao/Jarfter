using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 为路径规划提供进入指定六边形坐标的移动代价.
/// 实现可以直接查询动态地图, 或在地图版本未变化时缓存代价; 返回负值表示该坐标不可通行.
/// </summary>
public interface IMoveCostProvider
{
    /// <summary>
    /// 获取进入指定坐标的移动代价.
    /// </summary>
    /// <param name="destination">即将进入的六边形坐标.</param>
    /// <returns>非负的移动代价; 负值表示该坐标不可通行.</returns>
    double GetMoveCost(HexCubePoint destination);
}
