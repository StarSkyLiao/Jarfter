using Jarfter.Core.Numerics;

namespace Jarfter.Hexagonal.Coordinates;

/// <summary>
/// 使用二维数组存储的六边形地图.
/// </summary>
internal static class HexagonalMapInternal
{
    extension<TElement>(HexagonalMap<TElement> map)
    {
        public int ToIndex(HexagonalCubePoint cubePoint)
        {
            int q = cubePoint.Q;
            int r = cubePoint.R;
            int s = -q - r;

            int ring = Math.Max(Math.Max(q.Abs(), r.Abs()), s.Abs());
            if (ring == 0) return 0;

            // 当前 ring 起始位置
            int index = 1 + 3 * ring * (ring - 1);

            int offset;

            // 六条边判断

            if (r == -ring)
            {
                // 边 2: (n,-n) -> (-n,0)
                offset = 2 * ring + (ring - q);
            }
            else if (s == -ring)
            {
                // 边 3
                offset = 3 * ring + ring + r;
            }
            else if (q == -ring)
            {
                // 边 4
                offset = 4 * ring + ring + s;
            }
            else if (r == ring)
            {
                // 边 5
                offset = 5 * ring + ring + q;
            }
            else if (s == ring)
            {
                // 边 0
                offset = ring - r;
            }
            else
            {
                // q == ring 边 1
                offset = ring + q;
            }

            return index + offset;
        }

        public HexagonalCubePoint FromIndex(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, map.Count);

            if (index == 0) return new HexagonalCubePoint(0, 0);

            // 求 ring
            int ring = (int)((Math.Sqrt(12 * index - 3) + 3) / 6);

            // 修正误差
            while (1 + 3 * ring * (ring + 1) <= index) ring++;
            while (1 + 3 * (ring - 1) * ring > index) ring--;

            int ringStart = 1 + 3 * ring * (ring - 1);

            int offset = index - ringStart;

            int q;
            int r;

            // 每条边长度 ring
            if (offset < ring)
            {
                // 边 0 起点 (-n,0) (+1,0)
                q = -ring + offset;
                r = 0;
            }
            else if (offset < ring * 2)
            {
                // 边 1 (n,-1)
                int t = offset - ring;
                q = 0;
                r = -t;
                q += ring;
            }
            else if (offset < ring * 3)
            {
                // 边2
                int t = offset - ring * 2;
                q = ring - t;
                r = -ring;
            }
            else if (offset < ring * 4)
            {
                // 边3
                int t = offset - ring * 3;
                q = -t;
                r = -ring + t;
            }
            else if (offset < ring * 5)
            {
                // 边4
                int t = offset - ring * 4;
                q = -ring;
                r = t;
            }
            else
            {
                // 边5
                int t = offset - ring * 5;
                q = -ring + t;
                r = ring - t;
            }

            return new HexagonalCubePoint(q, r);
        }
    }
}
