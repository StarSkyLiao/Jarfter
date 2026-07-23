// using System.IO.Compression;
// using System.Text;
//
// namespace MiHoMiao.Numerics.Drawing.GraphicIO;
//
// public static class PngIO
// {
//     /// <summary>
//     ///     将指定的 Bitmap 对象按照 .png 格式, 保存到 filePath 路径的文件中.
//     /// </summary>
//     public static void SaveToFile(Bitmap bitmap, string filePath)
//     {
//         using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
//         // 写入 PNG 文件头（8个字节固定值）
//         stream.Write([137, 80, 78, 71, 13, 10, 26, 10], 0, 8);
//         // 写入 IHDR 块（图像头信息）
//         WriteIhdrChunk(bitmap, stream);
//         // 写入 IDAT 块（图像数据块）
//         WriteIdatChunk(bitmap, stream);
//         // 写入 IEND 块（文件结束块）
//         WriteIendChunk(stream);
//     }
//
//     #region PrivateOutMethods
//
//     /// <summary>
//     ///     写入 IHDR 块
//     /// </summary>
//     private static void WriteIhdrChunk(Bitmap bitmap, FileStream stream)
//     {
//         // 创建 IHDR 块
//         var ihdrData = new byte[13];
//         BitConverter.GetBytes(bitmap.Width).Reverse().ToArray().CopyTo(ihdrData, 0); // 宽度
//         BitConverter.GetBytes(bitmap.Height).Reverse().ToArray().CopyTo(ihdrData, 4); // 高度
//         ihdrData[8] = 8; // 位深度：8 bits per channel
//         ihdrData[9] = 6; // 颜色类型：Truecolor with alpha (RGBA)
//         ihdrData[10] = 0; // 压缩方法：Deflate
//         ihdrData[11] = 0; // 滤波方法：None
//         ihdrData[12] = 0; // 分帧方法：未指定
//
//         // 写入块的长度
//         WriteChunk(stream, "IHDR", ihdrData);
//     }
//
//     /// <summary>
//     ///     写入 IDAT 块（图像数据块）
//     /// </summary>
//     private static void WriteIdatChunk(Bitmap bitmap, FileStream stream)
//     {
//         // 将像素数据转换为适合 PNG 的格式
//         byte[] imageData;
//         using (var ms = new MemoryStream())
//         {
//             using (var deflateStream = new DeflateStream(ms, CompressionLevel.Optimal))
//             {
//                 // 转换每行图像数据，并加入适当的过滤器
//                 for (var y = 0; y < bitmap.Height; y++)
//                 {
//                     var rowData = new byte[bitmap.Width * 4]; // 每个像素需要4个字节：RGBA
//                     for (var x = 0; x < bitmap.Width; x++)
//                     {
//                         Color32 color = bitmap.Pixels[x, y];
//                         rowData[x * 4] = color.r;
//                         rowData[x * 4 + 1] = color.g;
//                         rowData[x * 4 + 2] = color.b;
//                         rowData[x * 4 + 3] = color.a; // 添加Alpha值
//                     }
//
//                     // 写入过滤方法（对于 PNG 图像来说我们使用 None，值为 0）
//                     deflateStream.Write(new byte[] { 0 }, 0, 1); // 使用无过滤
//
//                     // 写入图像数据行
//                     deflateStream.Write(rowData, 0, rowData.Length);
//                 }
//             }
//
//             imageData = ms.ToArray();
//         }
//
//         // 写入块
//         WriteChunk(stream, "IDAT", imageData);
//     }
//
//     /// <summary>
//     ///     写入 IEND 块（文件结束块）
//     /// </summary>
//     private static void WriteIendChunk(FileStream stream)
//     {
//         WriteChunk(stream, "IEND", []);
//     }
//
//     /// <summary>
//     ///     写入指定块的数据
//     /// </summary>
//     private static void WriteChunk(FileStream stream, string chunkType, byte[] chunkData)
//     {
//         // 计算块的CRC值
//         byte[] chunkTypeBytes = Encoding.ASCII.GetBytes(chunkType);
//         byte[] crcBytes = GetCrc32(chunkTypeBytes);
//
//         // 写入块长度（4个字节）
//         stream.Write(BitConverter.GetBytes(chunkData.Length).Reverse().ToArray(), 0, 4);
//
//         // 写入块类型（4个字节）
//         stream.Write(chunkTypeBytes, 0, 4);
//
//         // 写入块数据
//         stream.Write(chunkData, 0, chunkData.Length);
//
//         // 写入CRC值（4字节）
//         stream.Write(crcBytes, 0, 4);
//     }
//
//     /// <summary>
//     ///     计算CRC32值
//     /// </summary>
//     private static byte[] GetCrc32(byte[] data)
//     {
//         var crc = 0xffffffff;
//         foreach (byte b in data)
//         {
//             crc ^= b;
//             for (var i = 0; i < 8; i++)
//                 if ((crc & 1) != 0)
//                     crc = (crc >> 1) ^ 0xedb88320;
//                 else
//                     crc >>= 1;
//         }
//
//         crc ^= 0xffffffff;
//         return BitConverter.GetBytes(crc).Reverse().ToArray();
//     }
//
//     #endregion
//
// }