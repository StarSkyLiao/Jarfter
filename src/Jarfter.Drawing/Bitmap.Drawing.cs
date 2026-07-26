namespace Jarfter.Drawing;

public sealed partial class Bitmap
{
    /// <summary>
    /// 绘制连接两个像素坐标的线段.
    /// 超出位图边界的部分会被自动裁剪.
    /// </summary>
    /// <param name="start">线段起点.</param>
    /// <param name="end">线段终点.</param>
    /// <param name="color">线段颜色.</param>
    /// <param name="thickness">线段粗细, 单位为像素.</param>
    public void DrawLine((int x, int y) start, (int x, int y) end, Color32 color, int thickness = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(thickness);

        int x = start.x;
        int y = start.y;
        int deltaX = Math.Abs(end.x - x);
        int deltaY = Math.Abs(end.y - y);
        int stepX = x < end.x ? 1 : -1;
        int stepY = y < end.y ? 1 : -1;
        int error = deltaX - deltaY;

        while (true)
        {
            DrawBrush(x, y, color, thickness);
            if (x == end.x && y == end.y) return;

            int doubledError = error * 2;
            if (doubledError > -deltaY)
            {
                error -= deltaY;
                x += stepX;
            }

            if (doubledError < deltaX)
            {
                error += deltaX;
                y += stepY;
            }
        }
    }

    /// <summary>
    /// 绘制正六边形.
    /// <paramref name="radius"/> 表示中心到顶点的距离, 且六边形的顶点朝上和朝下.
    /// </summary>
    /// <param name="center">六边形的中心坐标.</param>
    /// <param name="radius">中心到顶点的像素距离.</param>
    /// <param name="borderColor">边框颜色.</param>
    /// <param name="fillColor">填充颜色; 为 <see langword="null"/> 时不填充.</param>
    /// <param name="borderThickness">边框粗细, 单位为像素.</param>
    public void DrawRegularHexagon(
        (int x, int y) center,
        int radius,
        Color32 borderColor,
        Color32? fillColor = null,
        int borderThickness = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(borderThickness);

        (int x, int y)[] vertices = CreatePointyTopHexagonVertices(center, radius);
        if (fillColor is { } color) FillPolygon(vertices, color);

        for (int index = 0; index < vertices.Length; index++)
        {
            DrawLine(vertices[index], vertices[(index + 1) % vertices.Length], borderColor, borderThickness);
        }
    }

    private static (int x, int y)[] CreatePointyTopHexagonVertices((int x, int y) center, int radius)
    {
        (int x, int y)[] vertices = new (int x, int y)[6];
        for (int index = 0; index < vertices.Length; index++)
        {
            double angle = -Math.PI / 2 + Math.PI / 3 * index;
            vertices[index] = (
                center.x + (int)Math.Round(radius * Math.Cos(angle)),
                center.y + (int)Math.Round(radius * Math.Sin(angle)));
        }

        return vertices;
    }

    private void DrawBrush(int centerX, int centerY, Color32 color, int thickness)
    {
        int offset = (thickness - 1) / 2;
        Fill(
            (centerX - offset, centerY - offset),
            (centerX + thickness - offset - 1, centerY + thickness - offset - 1),
            color);
    }

    private void FillPolygon((int x, int y)[] vertices, Color32 color)
    {
        int minimumY = vertices.Min(static vertex => vertex.y);
        int maximumY = vertices.Max(static vertex => vertex.y);
        double[] intersections = new double[vertices.Length];

        for (int y = minimumY; y <= maximumY; y++)
        {
            int intersectionCount = 0;
            for (int index = 0; index < vertices.Length; index++)
            {
                (int x, int y) start = vertices[index];
                (int x, int y) end = vertices[(index + 1) % vertices.Length];
                if ((start.y > y) == (end.y > y)) continue;

                intersections[intersectionCount++] =
                    start.x + (double)(y - start.y) * (end.x - start.x) / (end.y - start.y);
            }

            Array.Sort(intersections, 0, intersectionCount);
            for (int index = 0; index + 1 < intersectionCount; index += 2)
            {
                Fill(((int)Math.Ceiling(intersections[index]), y), ((int)Math.Floor(intersections[index + 1]), y), color);
            }
        }
    }
}
