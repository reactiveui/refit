// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>Provides parameter attributes for generated code.</summary>
/// <param name="attributes">The attribute information.</param>
public sealed class GeneratedParameterAttributeProvider(Dictionary<Type, object[]> attributes) : ICustomAttributeProvider
{
    /// <summary>List of all attributes.</summary>
    private readonly Lazy<object[]> _allAttributesCache = new(() =>
    {
        var allAttributes = new List<object>();
        foreach (var value in attributes.Values)
        {
            allAttributes.AddRange(value);
        }

        return allAttributes.ToArray();
    });

    /// <inheritdoc/>
    public object[] GetCustomAttributes(bool inherit) => _allAttributesCache.Value;

    /// <inheritdoc/>
    public object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        ArgumentExceptionHelper.ThrowIfNull(attributeType);

        return attributes.TryGetValue(attributeType, out var matches) ? matches : [];
    }

    /// <inheritdoc/>
    public bool IsDefined(Type attributeType, bool inherit) => attributes.ContainsKey(attributeType);
}
