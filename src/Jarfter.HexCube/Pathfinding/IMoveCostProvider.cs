using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 为路径规划提供进入指定六边形坐标的移动代价.
/// 实现可以直接查询动态地图, 或在地图版本未变化时缓存代价; 返回负值表示该坐标不可通行.
/// 单次 <see cref="IPathfinder.FindPath"/> 调用期间, 返回的代价必须来自同一份不可变的地图快照.
/// 地图更新应在下一次路径搜索前生效.
/// 实现不得返回 <see cref="double.NaN"/> 或正负无穷大.
/// </summary>
public interface IMoveCostProvider
{
    /// <summary>
    /// 获取进入指定坐标的移动代价.
    /// </summary>
    /// <param name="destination">即将进入的六边形坐标.</param>
    /// <returns>有限的移动代价; 非负值表示可通行, 负值表示该坐标不可通行.</returns>
    double GetMoveCost(HexCubeGridPoint destination);
}
