namespace Jarfter.NavMesh.Geometry;

/// <summary>
/// 表示二维导航网格中的双精度坐标点.
/// </summary>
public readonly record struct NavMeshPoint(double X, double Y)
{
    internal static double Cross(NavMeshPoint origin, NavMeshPoint first, NavMeshPoint second)
        => (first.X - origin.X) * (second.Y - origin.Y) - (first.Y - origin.Y) * (second.X - origin.X);

    internal static double Distance(NavMeshPoint first, NavMeshPoint second)
    {
        double x = second.X - first.X;
        double y = second.Y - first.Y;
        return Math.Sqrt(x * x + y * y);
    }

    internal bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);
}
