using System.Runtime.InteropServices;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

public partial interface IPathfinder
{
    /// <summary>
    /// 提供默认的 A* 寻路算法.
    /// </summary>
    public class AStar(IHeuristic heuristic, Func<HexCubePoint, double> moveCost) : IPathfinder
    {
        /// <summary>
        /// 在六边形网格中搜索路径.
        /// </summary>
        public IReadOnlyList<HexCubePoint> FindPath(HexCubePoint start, HexCubePoint goal)
        {
            PriorityQueue<HexCubePoint, double> open = new PriorityQueue<HexCubePoint, double>();
            Dictionary<HexCubePoint, HexCubePoint> cameFrom = [];
            Dictionary<HexCubePoint, double> gScore = [];

            gScore[start] = 0;

            open.Enqueue(start, heuristic.Calculate(start, goal));

            while (open.Count > 0)
            {
                HexCubePoint current = open.Dequeue();

                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }
                foreach (HexCubePoint neighbor in current.Neighbors)
                {
                    double cost = moveCost(neighbor);
                    if (cost < 0) continue;
                    double tentativeG = gScore[current] + cost;
                    if (gScore.TryGetValue(neighbor, out double oldG) && !(tentativeG < oldG)) continue;
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    double f = tentativeG + heuristic.Calculate(neighbor, goal);
                    open.Enqueue(neighbor, f);
                }
            }

            return [];
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
