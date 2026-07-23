using System.Diagnostics;
using Jarfter.Core.Numerics;
using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.MapProvider;

public partial class HexGridCentralProvider<TElement>
{
    internal static HexagonalCubePoint FromIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (index == 0)
        {
            return new HexagonalCubePoint(0, 0);
        }

        // 找到 index 所在的环。
        //
        // 第 n 环的索引范围：
        // start = 1 + 3n(n - 1)
        // end   = 3n(n + 1)
        int ring = (int)((Math.Sqrt(12.0 * index - 3.0) + 3.0) / 6.0);

        int ringStart = 1 + 3 * ring * (ring - 1);
        int offset = index - ringStart;

        int side = offset / ring;
        int step = offset % ring;

        int q;
        int r;

        switch (side)
        {
            case 0:
                // (-n, 0) -> (0, -n)
                q = -ring + step;
                r = -step;
                break;

            case 1:
                // (0, -n) -> (n, -n)
                q = step;
                r = -ring;
                break;

            case 2:
                // (n, -n) -> (n, 0)
                q = ring;
                r = -ring + step;
                break;

            case 3:
                // (n, 0) -> (0, n)
                q = ring - step;
                r = step;
                break;

            case 4:
                // (0, n) -> (-n, n)
                q = -step;
                r = ring;
                break;

            case 5:
                // (-n, n) -> (-n, 0)
                q = -ring;
                r = ring - step;
                break;

            default:
                throw new UnreachableException();
        }

        return new HexagonalCubePoint(q, r);
    }

    internal static int ToIndex(HexagonalCubePoint cubePoint)
    {
        int q = cubePoint.Q;
        int r = cubePoint.R;
        int s = -q - r;

        int ring = Math.Max(q.Abs(), Math.Max(r.Abs(), s.Abs()));

        if (ring == 0) return 0;

        int ringStart = 1 + 3 * ring * (ring - 1);
        int offset;

        if (s == ring && q < 0)
        {
            // 边 0：(-n, 0) -> (0, -n)
            offset = q + ring;
        }
        else if (r == -ring && q >= 0)
        {
            // 边 1：(0, -n) -> (n, -n)
            offset = ring + q;
        }
        else if (q == ring && r < 0)
        {
            // 边 2：(n, -n) -> (n, 0)
            offset = 2 * ring + r + ring;
        }
        else if (s == -ring && q > 0)
        {
            // 边 3：(n, 0) -> (0, n)
            offset = 3 * ring + ring - q;
        }
        else if (r == ring && q <= 0)
        {
            // 边 4：(0, n) -> (-n, n)
            offset = 4 * ring - q;
        }
        else
        {
            // 边 5：(-n, n) -> (-n, 0)
            offset = 5 * ring + ring - r;
        }

        return ringStart + offset;
    }
}
