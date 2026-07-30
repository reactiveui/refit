// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>Provides parameter attribute for generated code.</summary>
/// <param name="type">Type of the custom attribute.</param>
/// <param name="attributes">The attribute information.</param>
internal sealed class GeneratedParameterAttributeProviderSingle(Type type, object[] attributes) : ICustomAttributeProvider
{
    /// <inheritdoc/>
    public object[] GetCustomAttributes(bool inherit) => attributes;

    /// <inheritdoc/>
    public object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        ArgumentExceptionHelper.ThrowIfNull(attributeType);

        return attributeType == type ? attributes : [];
    }

    /// <inheritdoc/>
    public bool IsDefined(Type attributeType, bool inherit) => attributeType == type;
}
