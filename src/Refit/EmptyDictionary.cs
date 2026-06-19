// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Provides a shared empty dictionary instance.</summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
internal static class EmptyDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The shared empty dictionary instance.</summary>
    private static readonly Dictionary<TKey, TValue> _value = [];

    /// <summary>Gets the shared empty dictionary.</summary>
    /// <returns>The shared empty dictionary instance.</returns>
    internal static Dictionary<TKey, TValue> Get() => _value;
}
