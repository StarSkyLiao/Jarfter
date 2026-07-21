using System.Collections.Concurrent;

namespace Jarfter.Net.Server.Collections;

/// <summary>
/// 键为 string 类型的同步字典的简写.
/// </summary>
/// <typeparam name="TValue">同步字典的值类型.</typeparam>
public class LookupTable<TValue>() : ConcurrentDictionary<string, TValue>(StringComparer.Ordinal);
