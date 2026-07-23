using System.Globalization;
using System.Runtime.InteropServices;

namespace Jarfter.Drawing;

/// <summary>
/// 表示由 8 位 RGBA 分量组成的颜色.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly record struct Color32 : IFormattable
{
    /// <summary>
    /// 初始化指定 RGBA 分量的颜色.
    /// </summary>
    /// <param name="r">红色分量.</param>
    /// <param name="g">绿色分量.</param>
    /// <param name="b">蓝色分量.</param>
    /// <param name="a">透明度分量, 默认值为 <see cref="byte.MaxValue"/>.</param>
    public Color32(byte r, byte g, byte b, byte a = byte.MaxValue)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// 获取红色分量.
    /// </summary>
    [FieldOffset(0)]
    public readonly byte R;

    /// <summary>
    /// 获取绿色分量.
    /// </summary>
    [FieldOffset(1)]
    public readonly byte G;

    /// <summary>
    /// 获取蓝色分量.
    /// </summary>
    [FieldOffset(2)]
    public readonly byte B;

    /// <summary>
    /// 获取透明度分量.
    /// </summary>
    [FieldOffset(3)]
    public readonly byte A;

    /// <summary>
    /// 返回颜色的 RGBA 文本表示.
    /// </summary>
    /// <returns>颜色的 RGBA 文本表示.</returns>
    public override string ToString() => ToString(null, null);

    /// <summary>
    /// 使用指定格式和区域性信息返回颜色的 RGBA 文本表示.
    /// </summary>
    /// <param name="format">各个分量的数值格式.</param>
    /// <param name="formatProvider">格式提供程序; 为 <see langword="null"/> 时使用固定区域性格式.</param>
    /// <returns>颜色的 RGBA 文本表示.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        formatProvider ??= CultureInfo.InvariantCulture.NumberFormat;
        return $"RGBA({R.ToString(format, formatProvider)}, {G.ToString(format, formatProvider)}," +
               $" {B.ToString(format, formatProvider)}, {A.ToString(format, formatProvider)})";
    }
}
