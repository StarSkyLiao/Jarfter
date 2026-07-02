using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Numerics;

public static partial class NumberExtension
{
    extension(double value)
    {
        /// <summary>
        /// 返回数字的绝对值.
        /// </summary>
        /// <returns>数字的绝对值.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Abs() => Math.Abs(value);

        /// <summary>
        /// 返回数字的符号.
        /// </summary>
        /// <returns>小于 0 返回 -1, 大于 0 返回 1, 等于 0 返回 0.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Sign() => value < 0d ? -1 : value > 0d ? 1 : 0;

        /// <summary>
        /// 返回数字的指定整数幂.
        /// </summary>
        /// <param name="power">幂指数.</param>
        /// <returns>幂运算结果.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public double Pow(int power)
        {
            double result = 1d;
            double baseValue = value;
            // 通过二进制拆分幂指数, 将乘法次数从 O(power) 降到 O(log power).
            while (power > 0)
            {
                if ((power & 1) == 1) result *= baseValue;
                baseValue *= baseValue;
                power >>= 1;
            }
            return result;
        }

        /// <summary>
        /// 将给定的数字限制在指定下限之上.
        /// </summary>
        /// <param name="min">允许的最小值.</param>
        /// <returns>不小于 <paramref name="min"/> 的结果.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Min(double min) => value < min ? min : value;

        /// <summary>
        /// 将给定的数字限制在指定上限之下.
        /// </summary>
        /// <param name="max">允许的最大值.</param>
        /// <returns>不大于 <paramref name="max"/> 的结果.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Max(double max) => value > max ? max : value;

        /// <summary>
        /// 将指定数字限制在 0 到 1 的范围内.
        /// </summary>
        /// <returns>位于 0 到 1 范围内的结果.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Clamp01() => value < 0d ? 0d : value > 1d ? 1d : value;

        /// <summary>
        /// 将给定的数字限制在指定范围内.
        /// </summary>
        /// <param name="min">范围下限.</param>
        /// <param name="max">范围上限.</param>
        /// <returns>位于 [min, max] 范围内的结果.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Clamp(double min, double max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            return value;
        }

        /// <summary>
        /// 将给定数字限制在指定范围内, 并支持循环回绕.
        /// </summary>
        /// <param name="leftInclude">包含的左边界.</param>
        /// <param name="rightExclude">不包含的右边界.</param>
        /// <returns>回绕后位于 [leftInclude, rightExclude) 范围内的结果.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CircleClamp(double leftInclude, double rightExclude)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rightExclude, leftInclude);

            double range = rightExclude - leftInclude;
            double offset = value - leftInclude;
            if (range > 0d && offset >= 0d && offset < range) return value;
            offset %= range;
            if (offset < 0d) offset += range;
            return offset + leftInclude;
        }
    }
}
