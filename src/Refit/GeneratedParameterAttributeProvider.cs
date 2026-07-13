// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit;

/// <summary>Provides parameter attributes for generated code.</summary>
/// <param name="attributes">The attribute information.</param>
public sealed class GeneratedParameterAttributeProvider(Dictionary<Type, object[]> attributes) : ICustomAttributeProvider
{
    /// <summary>A shared provider for parameters that declare no attributes, avoiding a per-parameter empty dictionary.</summary>
    public static readonly GeneratedParameterAttributeProvider Empty = new([]);

    /// <summary>Gets a lazily initialised array of all attributes.</summary>
    private object[] AllAttributesCache
    {
        [SuppressMessage("StyleCop.Analyzers", "SST1443:ReduceNestedFlowComplexity", Justification = "The nested logic is necessary here to avoid LINQ usage.")]
        get
        {
            if (field is not null)
            {
                return field;
            }

            var totalCount = 0;
            foreach (var attributeArray in attributes.Values)
            {
                totalCount += attributeArray.Length;
            }

            var allAttributes = new object[totalCount];
            var index = 0;
            foreach (var attributeArray in attributes.Values)
            {
                foreach (var item in attributeArray)
                {
                    allAttributes[index++] = item;
                }
            }

            _ = Interlocked.CompareExchange(ref field, allAttributes, null);

            return field;
        }
    }

    /// <inheritdoc/>
    public object[] GetCustomAttributes(bool inherit) => AllAttributesCache;

    /// <inheritdoc/>
    public object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        ArgumentExceptionHelper.ThrowIfNull(attributeType);

        return attributes.TryGetValue(attributeType, out var matches) ? matches : [];
    }

    /// <inheritdoc/>
    public bool IsDefined(Type attributeType, bool inherit) => attributes.ContainsKey(attributeType);
}
