using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.Map;

/// <summary>
/// 提供六边形地图的便捷查询扩展方法.
/// </summary>
public static class HexagonalMapExtensions
{
    extension<T>(IHexagonalMap<T> self)
    {
        /// <summary>
        /// 获取指定坐标上的值; 当坐标不存在时返回默认值.
        /// </summary>
        /// <param name="position">地图坐标.</param>
        /// <returns>指定坐标上的值, 或 <typeparamref name="T"/> 的默认值.</returns>
        /// <exception cref="ArgumentNullException">当 self 为 null 时抛出.</exception>
        public T? GetValueOrDefault(HexagonalGrid<int> position)
        {
            ArgumentNullException.ThrowIfNull(self);
            return self.TryGetValue(position, out T value) ? value : default;
        }

        /// <summary>
        /// 获取指定坐标上的值; 当坐标不存在时返回指定默认值.
        /// </summary>
        /// <param name="position">地图坐标.</param>
        /// <param name="defaultValue">坐标不存在时返回的默认值.</param>
        /// <returns>指定坐标上的值, 或 defaultValue.</returns>
        /// <exception cref="ArgumentNullException">当 self 为 null 时抛出.</exception>
        public T GetValueOrDefault(HexagonalGrid<int> position, T defaultValue)
        {
            ArgumentNullException.ThrowIfNull(self);
            return self.TryGetValue(position, out T value) ? value : defaultValue;
        }
    }
}
