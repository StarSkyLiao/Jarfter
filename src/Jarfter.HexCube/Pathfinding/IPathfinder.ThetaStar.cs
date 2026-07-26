using Jarfter.Core.Collections.ObjectModel;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

public partial interface IPathfinder
{
    /// <summary>
    /// 提供基于视线优化的 Theta* 寻路算法.
    /// 路径节点始终是六边形网格中心, 但相邻路径节点之间可以沿任意 <see cref="HexCubeLine2D"/> 直接移动.
    /// </summary>
    /// <param name="heuristic">用于估算到目标剩余代价的启发函数. 返回值不得高估 <paramref name="navigationProvider"/> 的实际线段移动代价.</param>
    /// <param name="navigationProvider">提供网格移动代价、线段视线和线段移动代价的对象.</param>
    public sealed class ThetaStar(IHeuristic heuristic, IThetaStarNavigationProvider navigationProvider) : IPathfinder
    {
        /// <summary>
        /// 在六边形网格中搜索经过视线优化的路径.
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

                    foreach (HexCubePoint neighbor in entry.point.Neighbors)
                    {
                        if (!TryGetTentativePath(entry.point, neighbor, currentG, cameFrom, gScore, out HexCubePoint predecessor, out double tentativeG)) continue;
                        if (gScore.TryGetValue(neighbor, out double oldG) && !(tentativeG < oldG)) continue;

                        cameFrom[neighbor] = predecessor;
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
        /// 尝试获取从当前节点扩展至相邻节点的最优候选路径.
        /// </summary>
        /// <param name="current">当前扩展节点.</param>
        /// <param name="neighbor">待扩展的相邻节点.</param>
        /// <param name="currentG">到达当前节点的已知最小代价.</param>
        /// <param name="cameFrom">已确定的前驱节点表.</param>
        /// <param name="gScore">已确定的累计代价表.</param>
        /// <param name="predecessor">候选路径中 <paramref name="neighbor"/> 的前驱节点.</param>
        /// <param name="tentativeG">候选路径的累计代价.</param>
        /// <returns>存在可通行候选路径时返回 true, 否则返回 false.</returns>
        private bool TryGetTentativePath(
            HexCubePoint current,
            HexCubePoint neighbor,
            double currentG,
            Dictionary<HexCubePoint, HexCubePoint> cameFrom,
            Dictionary<HexCubePoint, double> gScore,
            out HexCubePoint predecessor,
            out double tentativeG)
        {
            bool hasCandidate = false;
            predecessor = default;
            tentativeG = default;

            if (cameFrom.TryGetValue(current, out HexCubePoint parent))
            {
                HexCubeLine2D line = new HexCubeLine2D(parent, neighbor);

                // Theta* 优先尝试从父节点直达邻居, 从而移除不必要的网格拐点.
                if (navigationProvider.TryGetLineCost(line, out double lineCost))
                {
                    predecessor = parent;
                    tentativeG = gScore[parent] + lineCost;
                    hasCandidate = true;

                    // 均匀代价满足三角不等式, 父节点直达不会劣于经当前节点的普通移动.
                    if (navigationProvider.UsesUniformTraversalCost) return true;
                }
            }

            HexCubeLine2D neighborLine = new HexCubeLine2D(current, neighbor);
            double moveCost = navigationProvider.GetMoveCost(neighbor);
            if (moveCost < 0) return hasCandidate;
            if (!navigationProvider.HasLineOfSight(neighborLine)) return hasCandidate;

            double neighborG = currentG + moveCost;

            // 高成本地形下, 可见的直达线段不一定比经当前节点的移动更便宜.
            if (!hasCandidate || neighborG < tentativeG)
            {
                predecessor = current;
                tentativeG = neighborG;
                hasCandidate = true;
            }

            return hasCandidate;
        }

        /// <summary>
        /// 回溯生成路径.
        /// </summary>
        private static List<HexCubePoint> ReconstructPath(Dictionary<HexCubePoint, HexCubePoint> cameFrom, HexCubePoint current)
        {
            List<HexCubePoint> path = [current];

            while (cameFrom.TryGetValue(current, out HexCubePoint parent))
            {
                current = parent;
                path.Add(current);
            }

            path.Reverse();

            return path;
        }
    }
}
