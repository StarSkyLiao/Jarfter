using Jarfter.Core.Collections.ObjectModel;
using Jarfter.NavMesh.Geometry;
using System.Runtime.InteropServices;

namespace Jarfter.NavMesh.Build;

/// <summary>
/// 为移动对象半径偏移已规范化的凸环.
/// 外边界向可行走区域内侧偏移, 障碍物环向可行走区域内侧扩张.
/// </summary>
internal static class NavMeshBuildOffsetter
{
    /// <summary>
    /// 对外边界和障碍物应用移动对象半径.
    /// </summary>
    /// <param name="input">已规范化的构建输入.</param>
    /// <param name="radius">需要保留的移动对象半径.</param>
    /// <param name="tolerance">几何容差.</param>
    /// <returns>偏移后的独立构建输入.</returns>
    public static NavMeshBuildInput Offset(NavMeshBuildInput input, double radius, double tolerance)
    {
        NavMeshPolygon boundary = OffsetRing(input.Boundary, radius, tolerance, "外边界");
        List<NavMeshPolygon> obstacles = Factory.RentList<NavMeshPolygon>();
        try
        {
            for (int index = 0; index < input.Obstacles.Count; index++)
                obstacles.Add(OffsetRing(input.Obstacles[index], radius, tolerance, $"障碍物 {index}"));
            return new NavMeshBuildInput(boundary, obstacles);
        }
        finally
        {
            Factory.Release(obstacles);
        }
    }

    private static NavMeshPolygon OffsetRing(NavMeshPolygon polygon, double radius, double tolerance, string name)
    {
        ReadOnlySpan<NavMeshPoint> vertices = polygon.AsSpan();
        if (!IsStrictlyConvex(vertices, tolerance))
            throw new NotSupportedException($"{name}不是严格凸环, 当前 AgentRadius 偏移尚不支持凹环.");

        List<NavMeshPoint> offset = Factory.RentList<NavMeshPoint>();
        try
        {
            for (int index = 0; index < vertices.Length; index++)
            {
                NavMeshPoint previous = vertices[(index + vertices.Length - 1) % vertices.Length];
                NavMeshPoint current = vertices[index];
                NavMeshPoint next = vertices[(index + 1) % vertices.Length];
                OffsetLine previousLine = CreateLeftOffsetLine(previous, current, radius);
                OffsetLine currentLine = CreateLeftOffsetLine(current, next, radius);
                if (!TryIntersect(previousLine, currentLine, tolerance, out NavMeshPoint point))
                    throw new NotSupportedException($"{name}包含无法偏移的近平行边.");
                offset.Add(point);
            }

            return new NavMeshPolygon(CollectionsMarshal.AsSpan(offset));
        }
        finally
        {
            Factory.Release(offset);
        }
    }

    private static bool IsStrictlyConvex(ReadOnlySpan<NavMeshPoint> vertices, double tolerance)
    {
        double orientation = 0;
        for (int index = 0; index < vertices.Length; index++)
        {
            double cross = NavMeshPoint.Cross(vertices[index], vertices[(index + 1) % vertices.Length],
                vertices[(index + 2) % vertices.Length]);
            if (Math.Abs(cross) <= tolerance) return false;
            if (orientation == 0)
            {
                orientation = cross;
                continue;
            }

            if ((orientation > 0) != (cross > 0)) return false;
        }

        return true;
    }

    private static OffsetLine CreateLeftOffsetLine(NavMeshPoint start, NavMeshPoint end, double radius)
    {
        double x = end.X - start.X;
        double y = end.Y - start.Y;
        double inverseLength = 1 / Math.Sqrt(x * x + y * y);
        return new OffsetLine(new NavMeshPoint(start.X - y * inverseLength * radius,
            start.Y + x * inverseLength * radius), x, y);
    }

    private static bool TryIntersect(OffsetLine first, OffsetLine second, double tolerance, out NavMeshPoint point)
    {
        double cross = first.DirectionX * second.DirectionY - first.DirectionY * second.DirectionX;
        if (Math.Abs(cross) <= tolerance)
        {
            point = default;
            return false;
        }

        double offsetX = second.Origin.X - first.Origin.X;
        double offsetY = second.Origin.Y - first.Origin.Y;
        double t = (offsetX * second.DirectionY - offsetY * second.DirectionX) / cross;
        point = new NavMeshPoint(first.Origin.X + first.DirectionX * t, first.Origin.Y + first.DirectionY * t);
        return true;
    }

    private readonly record struct OffsetLine(NavMeshPoint Origin, double DirectionX, double DirectionY);
}
