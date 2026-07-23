using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 定义一次寻路操作读取的不可变导航地图快照.
/// 快照在创建后不得反映源地图的后续变化, 以便寻路任务能够无锁地并发读取.
/// </summary>
public interface IHexNavigationSnapshot
{
    /// <summary>
    /// 获取导航地图版本. 调用方可使用此版本判断路径结果是否基于过期地图计算.
    /// </summary>
    long Version { get; }

    /// <summary>
    /// 尝试获取指定坐标上的静态导航数据.
    /// </summary>
    /// <param name="point">要查询的轴向坐标.</param>
    /// <param name="cell">查询成功时获取到的导航格子数据.</param>
    /// <returns>当快照包含指定坐标时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    bool TryGetCell(HexagonalCubePoint point, out HexNavigationCell cell);
}
