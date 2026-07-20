// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>Provides parameter attributes for generated code.</summary>
/// <param name="attributes">The attribute information.</param>
public sealed class GeneratedParameterAttributeProvider(Dictionary<Type, object[]> attributes) : ICustomAttributeProvider
{
    /// <summary>A shared provider for parameters that declare no attributes, avoiding a per-parameter empty dictionary.</summary>
    public static readonly GeneratedParameterAttributeProvider Empty = new([]);

    /// <summary>The lazily flattened array of every attribute, memoized on first access.</summary>
    private object[]? _allAttributes;

    /// <inheritdoc/>
    public object[] GetCustomAttributes(bool inherit)
    {
        if (Volatile.Read(ref _allAttributes) is { } cached)
        {
            return cached;
        }

        var flattened = FlattenAttributes(attributes);
        return Interlocked.CompareExchange(ref _allAttributes, flattened, null) ?? flattened;
    }

    /// <inheritdoc/>
    public object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        ArgumentExceptionHelper.ThrowIfNull(attributeType);

        return attributes.TryGetValue(attributeType, out var matches) ? matches : [];
    }

    /// <inheritdoc/>
    public bool IsDefined(Type attributeType, bool inherit) => attributes.ContainsKey(attributeType);

    /// <summary>Flattens the per-type attribute arrays into a single array without nested iteration.</summary>
    /// <param name="attributes">The attribute arrays keyed by attribute type.</param>
    /// <returns>Every attribute in a single array.</returns>
    internal static object[] FlattenAttributes(Dictionary<Type, object[]> attributes)
    {
        var totalCount = 0;
        foreach (var attributeArray in attributes.Values)
        {
            totalCount += attributeArray.Length;
        }

        var allAttributes = new object[totalCount];
        var index = 0;
        foreach (var attributeArray in attributes.Values)
        {
            Array.Copy(attributeArray, 0, allAttributes, index, attributeArray.Length);
            index += attributeArray.Length;
        }

        return allAttributes;
    }
}
