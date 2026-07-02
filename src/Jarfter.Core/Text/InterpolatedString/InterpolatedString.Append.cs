namespace Jarfter.Core.Text;

/// <summary>
/// 提供 <see cref="InterpolatedString"/> 的追加扩展方法.
/// </summary>
public static partial class InterpolatedStringExtension
{
    extension(ref InterpolatedString self)
    {
        /// <summary>
        /// 追加一个布尔值.
        /// </summary>
        public ref InterpolatedString Append(bool value) => ref self.Append(value ? "True" : "False");

        /// <summary>
        /// 追加一个字节值.
        /// </summary>
        public ref InterpolatedString Append(byte value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个有符号字节值.
        /// </summary>
        public ref InterpolatedString Append(sbyte value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个字符.
        /// </summary>
        public ref InterpolatedString Append(char value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个短整型值.
        /// </summary>
        public ref InterpolatedString Append(short value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个无符号短整型值.
        /// </summary>
        public ref InterpolatedString Append(ushort value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个整型值.
        /// </summary>
        public ref InterpolatedString Append(int value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个无符号整型值.
        /// </summary>
        public ref InterpolatedString Append(uint value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个长整型值.
        /// </summary>
        public ref InterpolatedString Append(long value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个无符号长整型值.
        /// </summary>
        public ref InterpolatedString Append(ulong value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个单精度浮点值.
        /// </summary>
        public ref InterpolatedString Append(float value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个双精度浮点值.
        /// </summary>
        public ref InterpolatedString Append(double value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个十进制值.
        /// </summary>
        public ref InterpolatedString Append(decimal value) => ref self.AppendFormatted(value);

        /// <summary>
        /// 追加一个字符串.
        /// </summary>
        public ref InterpolatedString Append(string value)
        {
            if (value.TryCopyTo(self.InternalCharSpan[self.Length..])) self.Length += value.Length;
            else self.GrowThenCopyString(value);
            return ref self;
        }

        /// <summary>
        /// 追加一个字符区域.
        /// </summary>
        public ref InterpolatedString Append(scoped ReadOnlySpan<char> value)
        {
            if (value.TryCopyTo(self.InternalCharSpan[self.Length..])) self.Length += value.Length;
            else self.GrowThenCopySpan(value);
            return ref self;
        }

        /// <summary>
        /// 追加一个只读内存块.
        /// </summary>
        public ref InterpolatedString Append(ReadOnlyMemory<char> value)
        {
            ReadOnlySpan<char> span = value.Span;
            if (span.TryCopyTo(self.InternalCharSpan[self.Length..])) self.Length += span.Length;
            else self.GrowThenCopySpan(span);
            return ref self;
        }

        /// <summary>
        /// 追加一个任意非空对象.
        /// </summary>
        public ref InterpolatedString Append<T>(T value) where T : notnull
        {
            if (value is ISpanFormattable formattable) self.AppendFormatted(formattable);
            else self.Append(value.ToString()!);
            return ref self;
        }

        /// <summary>
        /// 追加一个支持 <see cref="ISpanFormattable"/> 的值.
        /// </summary>
        public ref InterpolatedString AppendFormatted<T>(T value) where T : ISpanFormattable
        {
            int charsWritten;
            // 持续扩容直到格式化写入成功, 避免中间字符串分配.
            while (!value.TryFormat(self.InternalCharSpan[self.Length..], out charsWritten, default, null))
                self.GrowCore(self.InternalCharSpan.Length << 1);
            self.Length += charsWritten;
            return ref self;
        }

        /// <summary>
        /// 追加一个换行符.
        /// </summary>
        public ref InterpolatedString AppendLine() => ref self.Append('\n');

        /// <summary>
        /// 追加一个字符串并在末尾追加换行符.
        /// </summary>
        public ref InterpolatedString AppendLine(string value)
        {
            if (value.TryCopyTo(self.InternalCharSpan[self.Length..])) self.Length += value.Length;
            else self.GrowThenCopyString(value);
            self.Append('\n');
            return ref self;
        }
    }
}
