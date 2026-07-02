using System.IO.Compression;
using System.Text;

namespace Jarfter.Core.IO;

/// <summary>
/// 提供字符串与字节数组编码转换相关的扩展方法.
/// </summary>
public static class EncodingExtension
{
    /// <summary>
    /// 为字符串提供编码转换扩展方法.
    /// </summary>
    /// <param name="self">当前字符串实例.</param>
    extension(string self)
    {
        /// <summary>
        /// 将当前字符串按 UTF-8 编码转换为字节数组.
        /// </summary>
        /// <returns>当前字符串的 UTF-8 字节数组.</returns>
        public byte[] Utf8Bytes() => Encoding.UTF8.GetBytes(self);

        /// <summary>
        /// 将当前 Base64 字符串转换为原始字节数组.
        /// </summary>
        /// <returns>Base64 解码后的字节数组.</returns>
        public byte[] Base64Bytes() => Convert.FromBase64String(self);

        /// <summary>
        /// 使用 GZip 将当前字符串压缩为字节数组.
        /// </summary>
        /// <returns>压缩后的字节数组; 如果当前字符串为 null 或空字符串, 则返回空数组.</returns>
        public byte[] GZipCompress()
        {
            if (string.IsNullOrEmpty(self)) return [];

            using MemoryStream ms = new MemoryStream(self.Length);
            using (GZipStream gzip = new GZipStream(ms, CompressionLevel.Fastest, true))
            using (StreamWriter writer = new StreamWriter(gzip, Encoding.UTF8)) writer.Write(self);
            return ms.ToArray();
        }
    }

    /// <summary>
    /// 为字节数组提供编码转换扩展方法.
    /// </summary>
    /// <param name="self">当前字节数组实例.</param>
    extension(byte[] self)
    {
        /// <summary>
        /// 将当前字节数组按 UTF-8 编码转换为字符串.
        /// </summary>
        /// <returns>UTF-8 解码后的字符串.</returns>
        public string Utf8String() => Encoding.UTF8.GetString(self);

        /// <summary>
        /// 将当前字节数组转换为 Base64 字符串.
        /// </summary>
        /// <returns>当前字节数组的 Base64 字符串.</returns>
        public string Base64String() => Convert.ToBase64String(self);

        /// <summary>
        /// 将当前 GZip 字节数组解压为原始字符串.
        /// </summary>
        /// <returns>解压后的字符串; 如果当前字节数组为空数组, 则返回空字符串.</returns>
        public string GZipDecompress()
        {
            if (self.Length == 0) return string.Empty;

            using MemoryStream input = new MemoryStream(self);
            using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
            using StreamReader reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
