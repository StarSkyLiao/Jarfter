namespace Jarfter.Drawing;

/// <summary>
/// 表示由 <see cref="Color32"/> 像素组成的二维位图.
/// 像素通过索引器以 <c>[x, y]</c> 坐标访问, 其中 x 轴向右延伸, y 轴向下延伸.
/// </summary>
public sealed class Bitmap
{
    private readonly Color32[,] m_Pixels;

    /// <summary>
    /// 初始化指定尺寸的位图.
    /// </summary>
    /// <param name="width">位图的宽度, 单位为像素.</param>
    /// <param name="height">位图的高度, 单位为像素.</param>
    public Bitmap(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);

        Width = width;
        Height = height;
        m_Pixels = new Color32[width, height];
    }

    /// <summary>
    /// 获取位图的宽度, 单位为像素.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// 获取位图的高度, 单位为像素.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// 获取或设置指定坐标处的像素.
    /// </summary>
    /// <param name="x">像素的 x 坐标.</param>
    /// <param name="y">像素的 y 坐标.</param>
    /// <value>指定坐标处的颜色.</value>
    public Color32 this[int x, int y]
    {
        get => m_Pixels[x, y];
        internal set => m_Pixels[x, y] = value;
    }

    /// <summary>
    /// 将由两个端点确定的矩形区域填充为指定颜色.
    /// </summary>
    /// <param name="start">矩形的一个端点.</param>
    /// <param name="end">矩形的另一个端点.</param>
    /// <param name="color">用于填充的颜色.</param>
    public void Fill((int x, int y) start, (int x, int y) end, Color32 color)
    {
        // 允许两个端点以任意顺序传入, 因此先标准化矩形边界.
        int xStart = Math.Min(start.x, end.x);
        int xEnd = Math.Max(start.x, end.x);
        int yStart = Math.Min(start.y, end.y);
        int yEnd = Math.Max(start.y, end.y);

        // 将矩形限制在位图边界内, 避免访问数组越界.
        xStart = Math.Max(xStart, 0);
        xEnd = Math.Min(xEnd, Width - 1);
        yStart = Math.Max(yStart, 0);
        yEnd = Math.Min(yEnd, Height - 1);

        // 逐行填充, 以匹配底层二维数组的坐标语义.
        for (int y = yStart; y <= yEnd; y++)
        for (int x = xStart; x <= xEnd; x++)
            m_Pixels[x, y] = color;
    }

    /// <summary>
    /// 将所有像素设置为指定颜色.
    /// </summary>
    /// <param name="color">用于填充的颜色.</param>
    public void FillAll(Color32 color)
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            m_Pixels[x, y] = color;
    }

    /// <summary>
    /// 使用颜色生成函数设置所有像素.
    /// </summary>
    /// <param name="colorFunction">根据像素的 x 和 y 坐标生成颜色的函数.</param>
    public void FillAll(Func<int, int, Color32> colorFunction)
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            m_Pixels[x, y] = colorFunction(x, y);
    }

}
