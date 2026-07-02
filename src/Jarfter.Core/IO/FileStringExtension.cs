namespace Jarfter.Core.IO;

/// <summary>
/// 提供文件操作的扩展方法, 用于简化文件的读写功能.
/// 该类包含将字符串写入文件和从文件中读取字符串的扩展方法.
/// </summary>
public static class FileStringExtension
{
    /// <summary>
    /// 为字符串提供文件读写相关的扩展操作.
    /// </summary>
    /// <param name="self">作为写入内容或读取路径的字符串.</param>
    extension(string self)
    {
        /// <summary>
        /// 将字符串内容写入指定路径的文件.
        /// 如果目标文件的目录不存在, 将自动创建目录.
        /// </summary>
        /// <param name="path">目标文件的路径.</param>
        public void ToFile(string path)
        {
            string? directoryPath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new DirectoryNotFoundException();
            }
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(path, self);
        }

        /// <summary>
        /// 从指定路径的文件读取字符串内容.
        /// 如果文件不存在, 返回 null.
        /// </summary>
        /// <returns>文件内容的字符串, 如果文件不存在则返回 null.</returns>
        public string? ReadFile() => File.Exists(self) ? File.ReadAllText(self) : null;
    }
}
