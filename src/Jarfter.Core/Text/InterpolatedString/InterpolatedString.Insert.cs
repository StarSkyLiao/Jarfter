using System.Buffers;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Text;

/// <summary>
/// 提供 <see cref="InterpolatedString"/> 的插入扩展方法.
/// </summary>
public static partial class InterpolatedStringExtension
{
    extension(ref InterpolatedString self)
    {
        /// <summary>
        /// 在指定索引处插入一个字符.
        /// </summary>
        /// <param name="index">插入位置.</param>
        /// <param name="value">要插入的字符.</param>
        public ref InterpolatedString Insert(int index, char value)
        {
            if (index < 0 || index > self.Length) throw new ArgumentOutOfRangeException(nameof(index));

            int newLength = self.Length + 1;
            if (newLength > self.InternalCharSpan.Length) self.GrowCore(newLength);

            Span<char> span = self.InternalCharSpan;
            // 先整体右移尾部区间, 再写入新字符, 避免覆盖原内容.
            span[index..self.Length].CopyTo(span[(index + 1)..]);
            span[index] = value;
            self.Length = newLength;
            return ref self;
        }

        /// <summary>
        /// 在指定索引处插入一个字符串.
        /// </summary>
        /// <param name="index">插入位置.</param>
        /// <param name="value">要插入的字符串.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref InterpolatedString Insert(int index, string value) => ref self.Insert(index, value.AsSpan());

        /// <summary>
        /// 在指定索引处插入一个字符区域.
        /// </summary>
        /// <param name="index">插入位置.</param>
        /// <param name="value">要插入的字符区域.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref InterpolatedString Insert(int index, scoped ReadOnlySpan<char> value)
        {
            if (index < 0 || index > self.Length) throw new ArgumentOutOfRangeException(nameof(index));

            int valueLength = value.Length;
            if (valueLength == 0) return ref self;

            int newLength = self.Length + valueLength;
            if (newLength > self.InternalCharSpan.Length) self.GrowCore(newLength);

            Span<char> span = self.InternalCharSpan;
            // 先右移尾部区间, 为待插入内容腾出连续空间.
            span[index..self.Length].CopyTo(span[(index + valueLength)..]);

            if (valueLength <= 2)
            {
                span[index] = value[0];
                if (valueLength == 2) span[index + 1] = value[1];
            }
            else
            {
                value.CopyTo(span[index..]);
            }

            self.Length = newLength;
            return ref self;
        }

        /// <summary>
        /// 在指定索引处插入一个对象, 使用 <see cref="object.ToString"/> 进行转换.
        /// </summary>
        /// <param name="index">插入位置.</param>
        /// <param name="value">要插入的对象.</param>
        public ref InterpolatedString Insert<T>(int index, T value) where T : notnull => ref self.Insert(index, value.ToString()!);

        /// <summary>
        /// 在指定索引处插入一个支持 <see cref="ISpanFormattable"/> 的对象.
        /// </summary>
        /// <param name="index">插入位置.</param>
        /// <param name="value">要插入的对象.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref InterpolatedString InsertFormattable<T>(int index, T value) where T : ISpanFormattable
        {
            if (index < 0 || index > self.Length) throw new ArgumentOutOfRangeException(nameof(index));

            // 暂存尾部数据, 让格式化写入可以直接复用原缓冲区的目标区间.
            int tailLength = self.Length - index;
            const int stackallocThreshold = 256;
            char[]? rentedBuffer = null;
            Span<char> buffer = tailLength <= stackallocThreshold
                ? stackalloc char[tailLength]
                : (rentedBuffer = ArrayPool<char>.Shared.Rent(tailLength)).AsSpan(0, tailLength);

            try
            {
                self.InternalCharSpan[index..self.Length].CopyTo(buffer);

                Span<char> destination = self.InternalCharSpan[index..];
                if (value.TryFormat(destination, out int written, default, null))
                {
                    self.Length = index + written;
                    self.Append(buffer);
                }
                else
                {
                    self.Insert(index, value);
                }
            }
            finally
            {
                if (rentedBuffer is not null) ArrayPool<char>.Shared.Return(rentedBuffer);
            }

            return ref self;
        }
    }
}
