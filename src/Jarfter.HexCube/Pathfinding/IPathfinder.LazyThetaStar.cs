using Jarfter.Core.Collections.ObjectModel;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

public partial interface IPathfinder
{
    /// <summary>
    /// 提供延迟视线验证的 Lazy Theta* 寻路算法.
    /// 算法仅在节点出队时验证其父节点连线; 当验证失败时, 从已关闭的相邻节点中重设父节点.
    /// 该策略显著减少视线检测次数, 但会以乐观代价顺序扩展节点, 因此不保证获得与 <see cref="IPathfinder.ThetaStar"/> 相同的最低总代价路径.
    /// </summary>
    /// <param name="heuristic">用于估算到目标剩余代价的启发函数. 返回值不得高估 <paramref name="navigationProvider"/> 的实际线段移动代价.</param>
    /// <param name="navigationProvider">提供网格移动代价、线段视线和线段移动代价的对象.</param>
    public sealed class LazyThetaStar(IHeuristic heuristic, IThetaStarNavigationProvider navigationProvider) : IPathfinder
    {
        /// <summary>
        /// 在六边形网格中搜索经过延迟视线验证的路径.
        /// </summary>
        /// <param name="start">路径的起点.</param>
        /// <param name="goal">路径的目标点.</param>
        /// <returns>包含从起点到目标点的路径及其总代价的搜索结果.</returns>
        public PathfindingResult FindPath(HexCubeGridPoint start, HexCubeGridPoint goal)
        {
            PriorityQueue<(HexCubeGridPoint point, double pathCost), double> open =
                Factory.RentPriorityQueue<(HexCubeGridPoint point, double pathCost), double>();
            Dictionary<HexCubeGridPoint, HexCubeGridPoint> cameFrom =
                Factory.RentDictionary<Dictionary<HexCubeGridPoint, HexCubeGridPoint>>();
            Dictionary<HexCubeGridPoint, double> gScore =
                Factory.RentDictionary<Dictionary<HexCubeGridPoint, double>>();
            Dictionary<HexCubeGridPoint, HexCubeGridPoint> localParents =
                Factory.RentDictionary<Dictionary<HexCubeGridPoint, HexCubeGridPoint>>();
            Dictionary<HexCubeGridPoint, byte> closed =
                Factory.RentDictionary<Dictionary<HexCubeGridPoint, byte>>();

            try
            {
                gScore[start] = 0;
                open.Enqueue((start, 0), heuristic.Calculate(start, goal));

                while (open.TryDequeue(out (HexCubeGridPoint point, double pathCost) entry, out _))
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (!gScore.TryGetValue(entry.point, out double currentG) || currentG != entry.pathCost) continue;
                    if (closed.ContainsKey(entry.point)) continue;
                    if (!TryValidateVertex(entry.point, cameFrom, localParents, gScore, closed, out currentG)) continue;
                    if (entry.point == goal) return new PathfindingResult(ReconstructPath(cameFrom, entry.point), currentG);

                    closed.Add(entry.point, 0);

                    foreach (HexCubeGridPoint neighbor in entry.point.Neighbors)
                    {
                        if (closed.ContainsKey(neighbor)) continue;

                        double moveCost = navigationProvider.GetMoveCost(neighbor);
                        if (moveCost < 0) continue;

                        double tentativeG = currentG + moveCost;
                        if (gScore.TryGetValue(neighbor, out double oldG) && !(tentativeG < oldG)) continue;

                        // 延迟到邻居出队时再验证父节点连线, 避免对每个扩展边立即执行 LOS.
                        cameFrom[neighbor] = cameFrom.TryGetValue(entry.point, out HexCubeGridPoint parent) ? parent : entry.point;
                        localParents[neighbor] = entry.point;
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
                Factory.ReleaseDictionary(localParents);
                Factory.ReleaseDictionary(closed);
            }
        }

        /// <summary>
        /// 验证当前节点与其乐观父节点之间的视线, 并比较直达、普通前驱和已关闭邻居的可行代价.
        /// </summary>
        /// <param name="current">待验证节点.</param>
        /// <param name="cameFrom">当前的父节点表.</param>
        /// <param name="localParents">当前节点的普通网格前驱表.</param>
        /// <param name="gScore">累计代价表.</param>
        /// <param name="closed">已完成视线验证的节点集合.</param>
        /// <param name="currentG">验证后到达 <paramref name="current"/> 的代价.</param>
        /// <returns>当前节点可以通过已验证路径到达时返回 true, 否则返回 false.</returns>
        private bool TryValidateVertex(
            HexCubeGridPoint current,
            Dictionary<HexCubeGridPoint, HexCubeGridPoint> cameFrom,
            Dictionary<HexCubeGridPoint, HexCubeGridPoint> localParents,
            Dictionary<HexCubeGridPoint, double> gScore,
            Dictionary<HexCubeGridPoint, byte> closed,
            out double currentG)
        {
            if (!localParents.TryGetValue(current, out HexCubeGridPoint localParent))
            {
                currentG = gScore[current];
                return true;
            }

            double moveCost = navigationProvider.GetMoveCost(current);
            if (moveCost < 0)
            {
                currentG = 0;
                return false;
            }

            bool hasCandidate = false;
            HexCubeGridPoint bestParent = default;
            double bestG = 0;

            if (cameFrom.TryGetValue(current, out HexCubeGridPoint parent))
            {
                HexCubeLine2D parentLine = new HexCubeLine2D(parent, current);

                if (navigationProvider.TryGetLineCost(parentLine, out double parentLineCost))
                {
                    bestParent = parent;
                    bestG = gScore[parent] + parentLineCost;
                    hasCandidate = true;
                }
            }

            if (localParent != parent)
            {
                HexCubeLine2D localLine = new HexCubeLine2D(localParent, current);

                if (navigationProvider.HasLineOfSight(localLine))
                {
                    double localG = gScore[localParent] + moveCost;

                    if (!hasCandidate || localG < bestG)
                    {
                        bestParent = localParent;
                        bestG = localG;
                        hasCandidate = true;
                    }
                }
            }

            // 当乐观父节点和实际普通前驱都不可用时, 才枚举其余已关闭邻居.
            if (!hasCandidate)
            {
                foreach (HexCubeGridPoint neighbor in current.Neighbors)
                {
                    if (neighbor == localParent || !closed.ContainsKey(neighbor)) continue;

                    HexCubeLine2D neighborLine = new HexCubeLine2D(neighbor, current);
                    if (!navigationProvider.HasLineOfSight(neighborLine)) continue;

                    double candidateG = gScore[neighbor] + moveCost;

                    if (!hasCandidate || candidateG < bestG)
                    {
                        bestParent = neighbor;
                        bestG = candidateG;
                        hasCandidate = true;
                    }
                }
            }

            if (!hasCandidate)
            {
                currentG = 0;
                return false;
            }

            cameFrom[current] = bestParent;
            gScore[current] = bestG;
            currentG = bestG;
            return true;
        }

        /// <summary>
        /// 回溯生成路径.
        /// </summary>
        private static List<HexCubeGridPoint> ReconstructPath(Dictionary<HexCubeGridPoint, HexCubeGridPoint> cameFrom, HexCubeGridPoint current)
        {
            List<HexCubeGridPoint> path = [current];

            while (cameFrom.TryGetValue(current, out HexCubeGridPoint parent))
            {
                current = parent;
                path.Add(current);
            }

            path.Reverse();

            return path;
        }
    }
}
