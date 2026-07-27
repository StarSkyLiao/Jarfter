using Jarfter.Core.Collections.ObjectModel;
using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Topology;
using System.Runtime.InteropServices;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.Build;

/// <summary>
/// 将已校验的二维可行走边界转换为不可变三角导航网格.
/// 当前实现使用可见桥接和耳切法处理简单外边界及非重叠障碍物, 适合离线或低频构建.
/// </summary>
public static class NavMeshBuilder
{
    /// <summary>
    /// 从构建输入生成二维导航网格.
    /// </summary>
    /// <param name="input">可行走外边界与障碍物输入.</param>
    /// <param name="options">几何容差与移动对象参数.</param>
    /// <returns>由边界三角化得到的不可变导航网格.</returns>
    /// <exception cref="NotSupportedException">要求对凹环执行移动对象半径偏移.</exception>
    public static Mesh Build(NavMeshBuildInput input, NavMeshBuildOptions? options = null)
    {
        NavMeshBuildInput normalized = NavMeshBuildValidator.Normalize(input, options);
        NavMeshBuildOptions effectiveOptions = options ?? new NavMeshBuildOptions();
        if (effectiveOptions.AgentRadius != 0)
        {
            NavMeshBuildInput offset = NavMeshBuildOffsetter.Offset(normalized, effectiveOptions.AgentRadius,
                effectiveOptions.Tolerance);
            normalized = NavMeshBuildValidator.Normalize(offset, new NavMeshBuildOptions
            {
                Tolerance = effectiveOptions.Tolerance
            });
        }

        List<NavMeshPoint> vertices = Factory.RentList<NavMeshPoint>();
        List<NavMeshPolygon> obstacles = Factory.RentList<NavMeshPolygon>();
        try
        {
            vertices.AddRange(normalized.Boundary.AsSpan());
            obstacles.AddRange(normalized.Obstacles);
            // 从最右侧障碍物开始桥接, 可避免后续桥接穿越已合并的障碍物环.
            obstacles.Sort(static (left, right) =>
                GetRightmostX(right.AsSpan()).CompareTo(GetRightmostX(left.AsSpan())));
            foreach (NavMeshPolygon polygon in obstacles)
                MergeHole(vertices, polygon.AsSpan(), effectiveOptions.Tolerance);
            return Triangulate(CollectionsMarshal.AsSpan(vertices),
                effectiveOptions.Tolerance * effectiveOptions.Tolerance, effectiveOptions.AreaId,
                effectiveOptions.Flags);
        }
        finally
        {
            Factory.Release(obstacles);
            Factory.Release(vertices);
        }
    }

    private static Mesh Triangulate(ReadOnlySpan<NavMeshPoint> vertices, double areaTolerance, int areaId, uint flags)
    {
        List<int> remaining = Factory.RentList<int>();
        List<NavMeshTriangle> triangles = Factory.RentList<NavMeshTriangle>();
        try
        {
            for (int index = 0; index < vertices.Length; index++) remaining.Add(index);
            while (remaining.Count > 3)
            {
                bool clippedEar = false;
                for (int currentIndex = 0; currentIndex < remaining.Count; currentIndex++)
                {
                    int previousIndex = currentIndex == 0 ? remaining.Count - 1 : currentIndex - 1;
                    int nextIndex = currentIndex == remaining.Count - 1 ? 0 : currentIndex + 1;
                    int previous = remaining[previousIndex];
                    int current = remaining[currentIndex];
                    int next = remaining[nextIndex];
                    if (!IsEar(vertices, remaining, previous, current, next, areaTolerance)) continue;

                    triangles.Add(new NavMeshTriangle(previous, current, next, areaId, flags));
                    remaining.RemoveAt(currentIndex);
                    clippedEar = true;
                    break;
                }

                // 不能找到耳朵意味着输入并非简单多边形, 或其数值条件已超出容差模型.
                if (!clippedEar) throw new ArgumentException("外边界无法三角化, 请检查自相交或几何容差.");
            }

            triangles.Add(new NavMeshTriangle(remaining[0], remaining[1], remaining[2], areaId, flags));
            return Mesh.Create(vertices, CollectionsMarshal.AsSpan(triangles));
        }
        finally
        {
            Factory.Release(triangles);
            Factory.Release(remaining);
        }
    }

    private static bool IsEar(ReadOnlySpan<NavMeshPoint> vertices, List<int> remaining, int previous, int current,
        int next, double areaTolerance)
    {
        NavMeshPoint first = vertices[previous];
        NavMeshPoint second = vertices[current];
        NavMeshPoint third = vertices[next];
        if (NavMeshPoint.Cross(first, second, third) <= areaTolerance) return false;

        foreach (int candidate in remaining)
        {
            if (candidate == previous || candidate == current || candidate == next) continue;
            NavMeshPoint candidatePoint = vertices[candidate];
            if (candidatePoint == first || candidatePoint == second || candidatePoint == third) continue;
            if (IsPointInOrOnTriangle(candidatePoint, first, second, third, areaTolerance)) return false;
        }

        return true;
    }

    private static bool IsPointInOrOnTriangle(NavMeshPoint point, NavMeshPoint first, NavMeshPoint second,
        NavMeshPoint third, double areaTolerance)
    {
        return NavMeshPoint.Cross(first, second, point) >= -areaTolerance &&
               NavMeshPoint.Cross(second, third, point) >= -areaTolerance &&
               NavMeshPoint.Cross(third, first, point) >= -areaTolerance;
    }

    private static void MergeHole(List<NavMeshPoint> boundary, ReadOnlySpan<NavMeshPoint> hole, double tolerance)
    {
        int holeIndex = FindRightmostVertex(hole);
        int boundaryIndex = FindVisibleBoundaryVertex(boundary, hole, holeIndex, tolerance);
        if (boundaryIndex < 0) throw new ArgumentException("障碍物无法连接到外边界, 请检查几何形状和容差.");

        List<NavMeshPoint> merged = Factory.RentList<NavMeshPoint>();
        try
        {
            merged.AddRange(CollectionsMarshal.AsSpan(boundary)[..(boundaryIndex + 1)]);
            for (int index = 0; index <= hole.Length; index++) merged.Add(hole[(holeIndex + index) % hole.Length]);
            merged.Add(boundary[boundaryIndex]);
            merged.AddRange(CollectionsMarshal.AsSpan(boundary)[(boundaryIndex + 1)..]);
            boundary.Clear();
            boundary.AddRange(CollectionsMarshal.AsSpan(merged));
        }
        finally
        {
            Factory.Release(merged);
        }
    }

    private static int FindRightmostVertex(ReadOnlySpan<NavMeshPoint> vertices)
    {
        int result = 0;
        for (int index = 1; index < vertices.Length; index++)
        {
            if (vertices[index].X > vertices[result].X ||
                vertices[index].X == vertices[result].X && vertices[index].Y < vertices[result].Y)
                result = index;
        }

        return result;
    }

    private static double GetRightmostX(ReadOnlySpan<NavMeshPoint> vertices)
    {
        double result = vertices[0].X;
        for (int index = 1; index < vertices.Length; index++) result = Math.Max(result, vertices[index].X);
        return result;
    }

    private static int FindVisibleBoundaryVertex(List<NavMeshPoint> boundary, ReadOnlySpan<NavMeshPoint> hole,
        int holeIndex, double tolerance)
    {
        int result = -1;
        double bestDistanceSquared = double.PositiveInfinity;
        NavMeshPoint holePoint = hole[holeIndex];
        for (int index = 0; index < boundary.Count; index++)
        {
            NavMeshPoint candidate = boundary[index];
            if (candidate.X + tolerance < holePoint.X ||
                !IsBridgeVisible(holePoint, candidate, boundary, hole, holeIndex, index, tolerance))
                continue;
            double x = candidate.X - holePoint.X;
            double y = candidate.Y - holePoint.Y;
            double distanceSquared = x * x + y * y;
            if (distanceSquared >= bestDistanceSquared) continue;
            result = index;
            bestDistanceSquared = distanceSquared;
        }

        return result;
    }

    private static bool IsBridgeVisible(NavMeshPoint holePoint, NavMeshPoint boundaryPoint, List<NavMeshPoint> boundary,
        ReadOnlySpan<NavMeshPoint> hole, int holeIndex, int boundaryIndex, double tolerance)
    {
        for (int index = 0; index < boundary.Count; index++)
        {
            if (index == boundaryIndex || (index + 1) % boundary.Count == boundaryIndex) continue;
            if (SegmentsIntersectStrict(holePoint, boundaryPoint, boundary[index],
                    boundary[(index + 1) % boundary.Count], tolerance))
                return false;
        }

        for (int index = 0; index < hole.Length; index++)
        {
            if (index == holeIndex || (index + 1) % hole.Length == holeIndex) continue;
            if (SegmentsIntersectStrict(holePoint, boundaryPoint, hole[index], hole[(index + 1) % hole.Length],
                    tolerance))
                return false;
        }

        return true;
    }

    private static bool SegmentsIntersectStrict(NavMeshPoint firstStart, NavMeshPoint firstEnd,
        NavMeshPoint secondStart, NavMeshPoint secondEnd, double tolerance)
    {
        double first = NavMeshPoint.Cross(firstStart, firstEnd, secondStart);
        double second = NavMeshPoint.Cross(firstStart, firstEnd, secondEnd);
        double third = NavMeshPoint.Cross(secondStart, secondEnd, firstStart);
        double fourth = NavMeshPoint.Cross(secondStart, secondEnd, firstEnd);
        if (Math.Abs(first) <= tolerance || Math.Abs(second) <= tolerance || Math.Abs(third) <= tolerance ||
            Math.Abs(fourth) <= tolerance)
            return false;
        return (first > 0) != (second > 0) && (third > 0) != (fourth > 0);
    }
}
