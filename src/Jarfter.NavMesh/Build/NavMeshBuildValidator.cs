using Jarfter.Core.Collections.ObjectModel;
using Jarfter.NavMesh.Geometry;
using System.Runtime.InteropServices;

namespace Jarfter.NavMesh.Build;

/// <summary>
/// 校验构建输入并规范化环绕序.
/// 外边界被规范为逆时针, 障碍物被规范为顺时针.
/// </summary>
public static class NavMeshBuildValidator
{
    /// <summary>
    /// 校验构建输入并返回绕序统一的独立副本.
    /// </summary>
    /// <param name="input">待校验的外边界和障碍物环.</param>
    /// <param name="options">几何容差与移动对象参数.</param>
    /// <returns>外边界为逆时针、障碍物为顺时针的输入副本.</returns>
    public static NavMeshBuildInput Normalize(NavMeshBuildInput input, NavMeshBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        options ??= new NavMeshBuildOptions();
        if (!double.IsFinite(options.Tolerance) || options.Tolerance <= 0 || !double.IsFinite(options.AgentRadius) ||
            options.AgentRadius < 0) throw new ArgumentOutOfRangeException(nameof(options));
        NavMeshPolygon boundary = NormalizeRing(input.Boundary, true, options.Tolerance, "外边界");
        List<NavMeshPolygon> obstacles = Factory.RentList<NavMeshPolygon>();
        try
        {
            for (int index = 0; index < input.Obstacles.Count; index++)
            {
                NavMeshPolygon navMeshPolygon = NormalizeRing(
                    input.Obstacles[index], false, options.Tolerance, $"障碍物 {index}"
                );
                obstacles.Add(navMeshPolygon);
            }
            ValidateRingRelationships(boundary, obstacles, options.Tolerance);
            return new NavMeshBuildInput(boundary, obstacles);
        }
        finally
        {
            Factory.Release(obstacles);
        }
    }

    private static void ValidateRingRelationships(NavMeshPolygon boundary, List<NavMeshPolygon> obstacles,
        double tolerance)
    {
        ReadOnlySpan<NavMeshPoint> boundaryVertices = boundary.AsSpan();
        for (int index = 0; index < obstacles.Count; index++)
        {
            ReadOnlySpan<NavMeshPoint> obstacle = obstacles[index].AsSpan();
            if (RingsIntersect(boundaryVertices, obstacle, tolerance) || !Contains(boundaryVertices, obstacle[0]))
                throw new ArgumentException($"障碍物 {index}未严格位于外边界内.");
            for (int otherIndex = 0; otherIndex < index; otherIndex++)
            {
                ReadOnlySpan<NavMeshPoint> other = obstacles[otherIndex].AsSpan();
                if (RingsIntersect(obstacle, other, tolerance) || Contains(obstacle, other[0]) ||
                    Contains(other, obstacle[0]))
                    throw new ArgumentException($"障碍物 {index}与障碍物 {otherIndex}相交或包含.");
            }
        }
    }

    private static bool RingsIntersect(ReadOnlySpan<NavMeshPoint> first, ReadOnlySpan<NavMeshPoint> second,
        double tolerance)
    {
        for (int firstIndex = 0; firstIndex < first.Length; firstIndex++)
        {
            NavMeshPoint firstStart = first[firstIndex];
            NavMeshPoint firstEnd = first[(firstIndex + 1) % first.Length];
            for (int secondIndex = 0; secondIndex < second.Length; secondIndex++)
            {
                NavMeshPoint secondStart = second[secondIndex];
                NavMeshPoint secondEnd = second[(secondIndex + 1) % second.Length];
                if (SegmentsIntersect(firstStart, firstEnd, secondStart, secondEnd, tolerance)) return true;
            }
        }

        return false;
    }

    private static bool SegmentsIntersect(NavMeshPoint firstStart, NavMeshPoint firstEnd, NavMeshPoint secondStart,
        NavMeshPoint secondEnd, double tolerance)
    {
        double first = NavMeshPoint.Cross(firstStart, firstEnd, secondStart);
        double second = NavMeshPoint.Cross(firstStart, firstEnd, secondEnd);
        double third = NavMeshPoint.Cross(secondStart, secondEnd, firstStart);
        double fourth = NavMeshPoint.Cross(secondStart, secondEnd, firstEnd);
        if ((first > tolerance && second < -tolerance || first < -tolerance && second > tolerance) &&
            (third > tolerance && fourth < -tolerance || third < -tolerance && fourth > tolerance))
            return true;
        return Math.Abs(first) <= tolerance && IsOnSegment(secondStart, firstStart, firstEnd, tolerance) ||
               Math.Abs(second) <= tolerance && IsOnSegment(secondEnd, firstStart, firstEnd, tolerance) ||
               Math.Abs(third) <= tolerance && IsOnSegment(firstStart, secondStart, secondEnd, tolerance) ||
               Math.Abs(fourth) <= tolerance && IsOnSegment(firstEnd, secondStart, secondEnd, tolerance);
    }

    private static bool IsOnSegment(NavMeshPoint point, NavMeshPoint start, NavMeshPoint end, double tolerance)
    {
        return point.X >= Math.Min(start.X, end.X) - tolerance && point.X <= Math.Max(start.X, end.X) + tolerance &&
               point.Y >= Math.Min(start.Y, end.Y) - tolerance && point.Y <= Math.Max(start.Y, end.Y) + tolerance;
    }

    private static bool Contains(ReadOnlySpan<NavMeshPoint> polygon, NavMeshPoint point)
    {
        bool contains = false;
        for (int index = 0, previous = polygon.Length - 1; index < polygon.Length; previous = index++)
        {
            NavMeshPoint current = polygon[index];
            NavMeshPoint prior = polygon[previous];
            if ((current.Y > point.Y) != (prior.Y > point.Y) && point.X <
                (prior.X - current.X) * (point.Y - current.Y) / (prior.Y - current.Y) + current.X)
                contains = !contains;
        }

        return contains;
    }

    private static NavMeshPolygon NormalizeRing(NavMeshPolygon polygon, bool counterClockwise, double tolerance,
        string name)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        ReadOnlySpan<NavMeshPoint> vertices = polygon.AsSpan();
        if (vertices.Length < 3) throw new ArgumentException($"{name}至少需要三个顶点.");
        double area = 0;
        for (int index = 0; index < vertices.Length; index++)
        {
            NavMeshPoint current = vertices[index];
            NavMeshPoint next = vertices[(index + 1) % vertices.Length];
            if (!current.IsFinite) throw new ArgumentException($"{name}包含非有限坐标.");
            double x = next.X - current.X;
            double y = next.Y - current.Y;
            if (x * x + y * y <= tolerance * tolerance) throw new ArgumentException($"{name}包含长度不大于容差的边.");
            area += current.X * next.Y - current.Y * next.X;
        }

        if (Math.Abs(area) <= tolerance * tolerance) throw new ArgumentException($"{name}面积不大于容差.");
        ValidateSimpleRing(vertices, tolerance, name);
        List<NavMeshPoint> normalized = Factory.RentList<NavMeshPoint>();
        try
        {
            normalized.AddRange(vertices);
            if (counterClockwise ? area < 0 : area > 0) normalized.Reverse();
            return new NavMeshPolygon(CollectionsMarshal.AsSpan(normalized));
        }
        finally
        {
            Factory.Release(normalized);
        }
    }

    private static void ValidateSimpleRing(ReadOnlySpan<NavMeshPoint> vertices, double tolerance, string name)
    {
        for (int firstIndex = 0; firstIndex < vertices.Length; firstIndex++)
        {
            NavMeshPoint firstStart = vertices[firstIndex];
            NavMeshPoint firstEnd = vertices[(firstIndex + 1) % vertices.Length];
            for (int secondIndex = firstIndex + 1; secondIndex < vertices.Length; secondIndex++)
            {
                if (secondIndex == firstIndex + 1) continue;
                if (firstIndex == 0 && secondIndex == vertices.Length - 1) continue;
                NavMeshPoint secondStart = vertices[secondIndex];
                NavMeshPoint secondEnd = vertices[(secondIndex + 1) % vertices.Length];
                if (SegmentsIntersect(firstStart, firstEnd, secondStart, secondEnd, tolerance))
                    throw new ArgumentException($"{name}包含自相交边.");
            }
        }
    }
}
