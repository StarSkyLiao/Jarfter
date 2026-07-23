using Jarfter.Core.Numerics;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;

namespace Jarfter.Hexagonal.Pathfinding.Geometry;

/// <summary>
/// 提供无分配的线段主穿格枚举.
/// 当线段恰好沿多个格子的公共边或顶点前进时, 枚举器选择运动方向上的一个主格子;
/// 需要保守碰撞判断的调用方应额外检查主格子的相邻格子.
/// </summary>
public struct HexagonalSegmentTraversal
{
    private readonly HexagonalLayout m_Layout;
    private readonly HexagonalWorldPoint m_Start;
    private readonly HexagonalWorldPoint m_End;
    private readonly HexagonalOrientation m_Orientation;
    private readonly double m_DeltaX;
    private readonly double m_DeltaY;
    private readonly double m_SegmentLength;
    private readonly double m_SampleFractionOffset;
    private bool m_HasCurrent;
    private bool m_Finished;
    private HexagonalSegmentCell m_Current;

    internal HexagonalSegmentTraversal(
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        HexagonalOrientation orientation)
    {
        m_Layout = layout;
        m_Start = start;
        m_End = end;
        m_Orientation = orientation;
        m_DeltaX = end.X - start.X;
        m_DeltaY = end.Y - start.Y;
        m_SegmentLength = Math.Sqrt(m_DeltaX * m_DeltaX + m_DeltaY * m_DeltaY);
        m_SampleFractionOffset = GetSampleFractionOffset(layout.UnitApothem, start, end, m_SegmentLength);
        m_HasCurrent = false;
        m_Finished = false;
        m_Current = default;
    }

    /// <summary>
    /// 获取当前线段穿格项.
    /// </summary>
    public readonly HexagonalSegmentCell Current => m_Current;

    /// <summary>
    /// 获取无装箱的结构体枚举器.
    /// </summary>
    /// <returns>新的线段穿格枚举器.</returns>
    public readonly HexagonalSegmentTraversal GetEnumerator() => this;

    /// <summary>
    /// 前进到线段经过的下一个主格子.
    /// </summary>
    /// <returns>当存在下一个主格子时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (m_Finished) return false;

        double startFraction;
        HexagonalCubePoint point;

        if (m_HasCurrent)
        {
            if (m_Current.EndFraction >= 1)
            {
                m_Finished = true;
                return false;
            }

            startFraction = m_Current.EndFraction;
            point = GetPointAfter(startFraction);
        }
        else
        {
            startFraction = 0;
            point = GetPointAfter(startFraction);
            m_HasCurrent = true;
        }

        double endFraction = GetExitFraction(point, startFraction);
        m_Current = new HexagonalSegmentCell(point, startFraction, endFraction);
        return true;
    }

    private HexagonalCubePoint GetPointAfter(double fraction)
    {
        if (m_SegmentLength == 0) return m_Layout.GetNearestPoint(m_Start);

        double sampleFraction = Math.Min(1, fraction + m_SampleFractionOffset);
        HexagonalWorldPoint sample = new HexagonalWorldPoint(
            m_Start.X + m_DeltaX * sampleFraction,
            m_Start.Y + m_DeltaY * sampleFraction
        );
        return m_Layout.GetNearestPoint(sample);
    }

    private double GetExitFraction(HexagonalCubePoint point, double startFraction)
    {
        if (m_SegmentLength == 0) return 1;

        HexagonalWorldPoint center = m_Layout.GetCenter(point);
        double startX = m_Start.X - center.X;
        double startY = m_Start.Y - center.Y;
        double exitFraction = 1;

        foreach (HexagonalWorldPoint normal in HexNavigationGeometry.GetSideNormals(m_Orientation))
        {
            double deltaProjection = m_DeltaX * normal.X + m_DeltaY * normal.Y;
            if (deltaProjection <= 0) continue;

            double startProjection = startX * normal.X + startY * normal.Y;
            double boundaryFraction = (m_Layout.UnitApothem - startProjection) / deltaProjection;

            if (boundaryFraction > startFraction && boundaryFraction < exitFraction)
            {
                exitFraction = boundaryFraction;
            }
        }

        return exitFraction;
    }

    private static double GetSampleFractionOffset(
        double unitApothem,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        double segmentLength)
    {
        if (segmentLength == 0) return 0;

        double maximumCoordinate = Math.Max(
            Math.Max(start.X.Abs(), start.Y.Abs()),
            Math.Max(end.X.Abs(), end.Y.Abs())
        );
        double coordinateUlp = Math.Abs(Math.BitIncrement(maximumCoordinate) - maximumCoordinate);
        double worldOffset = Math.Max(unitApothem * 1e-12, coordinateUlp * 32);
        return Math.Min(0.5, worldOffset / segmentLength);
    }
}
