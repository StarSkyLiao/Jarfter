using System.Collections.Concurrent;

namespace Jarfter.Net.Server.Collections;

/// <summary>
/// 值为 byte 类型的同步字典的简写.
/// </summary>
/// <typeparam name="TKey">同步字典的键类型.</typeparam>
public class ConcurrentSet<TKey>(IEqualityComparer<TKey>? comparer = null) : ConcurrentDictionary<TKey, byte>(comparer) where TKey : notnull;
