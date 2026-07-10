// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Refit.Internal;

/// <summary>
/// Polyfills for dictionary APIs that .NET Framework lacks. Only compiled into the
/// <c>net4*</c> targets, where the instance methods do not exist; every other target
/// binds to the built-in overloads instead.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CollectionExtensions
{
    /// <summary>Supplies the state-taking <c>GetOrAdd</c> overload added after .NET Framework.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dictionary">The dictionary being extended.</param>
    extension<TKey, TValue>(ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        /// <summary>Gets the value for a key, creating it from <paramref name="arg"/> when absent.</summary>
        /// <typeparam name="TArg">The type of the state passed to <paramref name="valueFactory"/>.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="valueFactory">Builds the value from the key and <paramref name="arg"/>.</param>
        /// <param name="arg">State handed to <paramref name="valueFactory"/> so it need not capture.</param>
        /// <returns>The existing value, or the newly created one.</returns>
        /// <remarks>
        /// As with the built-in overload, <paramref name="valueFactory"/> runs outside the lock and may
        /// run more than once under contention; the first value inserted wins.
        /// </remarks>
        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg arg) =>
            dictionary.TryGetValue(key, out var existing)
                ? existing
                : dictionary.GetOrAdd(key, valueFactory(key, arg));
    }

    /// <summary>Supplies the <c>TryAdd</c> method added after .NET Framework.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dictionary">The dictionary being extended.</param>
    extension<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        /// <summary>Adds the pair when the key is absent, reporting whether it acted.</summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns><see langword="true"/> if the pair was added; <see langword="false"/> if the key was already present.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                return false;
            }

            dictionary.Add(key, value);
            return true;
        }
    }
}
