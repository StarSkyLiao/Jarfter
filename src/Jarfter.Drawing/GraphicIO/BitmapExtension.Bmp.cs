namespace Jarfter.Drawing.GraphicIO;

/// <summary>
/// 提供 <see cref="Bitmap"/> 的图像格式导出功能.
/// </summary>
public static partial class BitmapExtension
{
    /// <summary>
    /// 将位图保存为 24 位未压缩 BMP 文件.
    /// </summary>
    /// <param name="bitmap">要保存的位图.</param>
    /// <param name="filePath">目标 BMP 文件的路径.</param>
    public static void SaveAsBmp(Bitmap bitmap, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);
        WriteData(bitmap, writer);
    }

    #region Private Methods

    private static void WriteData(Bitmap bitmap, BinaryWriter writer)
    {
        // BMP 每行必须按 4 字节对齐, 因此行缓冲区包含末尾填充字节.
        int rowSize = (bitmap.Width * 3 + 3) / 4 * 4;
        int dataSize = rowSize * bitmap.Height;
        int fileSize = 54 + dataSize;

        // 先写入固定长度的文件头, 使读取方能够定位像素数据.
        writer.Write((ushort)0x4D42); // BMP 标识.
        writer.Write(fileSize); // 文件大小.
        writer.Write((ushort)0); // 保留字段.
        writer.Write((ushort)0); // 保留字段.
        writer.Write(54); // 像素数据相对于文件开头的偏移量.

        writer.Write(40); // 信息头大小.
        writer.Write(bitmap.Width); // 宽度.
        writer.Write(bitmap.Height); // 高度.
        writer.Write((ushort)1); // 颜色平面数.
        writer.Write((ushort)24); // 每像素 24 位.
        writer.Write(0); // 不使用压缩.
        writer.Write(dataSize); // 图像数据大小.
        writer.Write(0); // 水平分辨率.
        writer.Write(0); // 垂直分辨率.
        writer.Write(0); // 调色板颜色数.
        writer.Write(0); // 重要颜色数.

        // 使用单行缓冲区, 一次写入像素和末尾填充.
        byte[] rowBuffer = new byte[rowSize];
        for (int y = bitmap.Height - 1; y >= 0; y--) // BMP 像素行按自下而上的顺序存储.
        {
            int rowIndex = 0;
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color32 color = bitmap[x, y];

                // 24 位 BMP 使用 BGR 字节顺序, 且不保存 Alpha 分量.
                rowBuffer[rowIndex++] = color.B;
                rowBuffer[rowIndex++] = color.G;
                rowBuffer[rowIndex++] = color.R;
            }

            // 清零填充区域, 防止上一行的缓冲数据写入当前行.
            int padding = rowSize - (bitmap.Width * 3);
            if (padding > 0)
            {
                Array.Clear(rowBuffer, rowIndex, padding);
            }

            // 一次写入完整行, 以包含 BMP 所需的行尾填充.
            writer.Write(rowBuffer, 0, rowSize);
        }
    }

    #endregion

}
