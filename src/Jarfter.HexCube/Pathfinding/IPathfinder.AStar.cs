using Jarfter.Core.Collections.ObjectModel;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

public partial interface IPathfinder
{
    /// <summary>
    /// 提供默认的 A* 寻路算法.
    /// </summary>
    /// <param name="heuristic">用于估算到目标剩余代价的启发函数.</param>
    /// <param name="moveCostProvider">提供进入相邻坐标的移动代价和通行状态的对象.</param>
    public class AStar(IHeuristic heuristic, IMoveCostProvider moveCostProvider) : IPathfinder
    {
        /// <summary>
        /// 在六边形网格中搜索路径.
        /// </summary>
        /// <param name="start">路径的起点.</param>
        /// <param name="goal">路径的目标点.</param>
        /// <returns>包含从起点到目标点的路径及其总代价的搜索结果.</returns>
        public PathfindingResult FindPath(HexCubePoint start, HexCubePoint goal)
        {
            PriorityQueue<(HexCubePoint point, double pathCost), double> open =
                Factory.RentPriorityQueue<(HexCubePoint point, double pathCost), double>();
            Dictionary<HexCubePoint, HexCubePoint> cameFrom =
                Factory.RentDictionary<Dictionary<HexCubePoint, HexCubePoint>>();
            Dictionary<HexCubePoint, double> gScore =
                Factory.RentDictionary<Dictionary<HexCubePoint, double>>();

            try
            {
                gScore[start] = 0;
                open.Enqueue((start, 0), heuristic.Calculate(start, goal));

                while (open.TryDequeue(out (HexCubePoint point, double pathCost) entry, out _))
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (!gScore.TryGetValue(entry.point, out double currentG) || currentG != entry.pathCost) continue;
                    if (entry.point == goal) return new PathfindingResult(ReconstructPath(cameFrom, entry.point), currentG);

                    // 队列不支持降低优先级, 因此保留旧项并在出队时跳过, 避免重复扩展节点.
                    foreach (HexCubePoint neighbor in entry.point.Neighbors)
                    {
                        double cost = moveCostProvider.GetMoveCost(neighbor);
                        if (cost < 0) continue;
                        double tentativeG = currentG + cost;
                        if (gScore.TryGetValue(neighbor, out double oldG) && !(tentativeG < oldG)) continue;
                        cameFrom[neighbor] = entry.point;
                        gScore[neighbor] = tentativeG;
                        double f = tentativeG + heuristic.Calculate(neighbor, goal);
                        open.Enqueue((neighbor, tentativeG), f);
                    }
                }

                return PathfindingResult.Empty;
            }
            finally
            {
                Factory.ReleasePriorityQueue(open);
                Factory.ReleaseDictionary(cameFrom);
                Factory.ReleaseDictionary(gScore);
            }
        }

        /// <summary>
        /// 回溯生成路径.
        /// </summary>
        private static HexCubePoint[] ReconstructPath(Dictionary<HexCubePoint, HexCubePoint> cameFrom, HexCubePoint current)
        {
            int pathLength = 1;
            HexCubePoint pathPoint = current;

            while (cameFrom.TryGetValue(pathPoint, out HexCubePoint parent))
            {
                pathPoint = parent;
                pathLength++;
            }

            HexCubePoint[] path = new HexCubePoint[pathLength];

            // 先确定精确长度, 再从终点反向填充, 避免 List 在长网格路径中多次扩容.
            for (int index = pathLength - 1; index >= 0; index--)
            {
                path[index] = current;
                if (index != 0) current = cameFrom[current];
            }

            return path;
        }
    }
}
