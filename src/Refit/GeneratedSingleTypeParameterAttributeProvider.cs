// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Provides the attributes declared on a parameter when every one of them has the same type, which needs
/// neither a dictionary nor a flattening pass. Use <see cref="GeneratedParameterAttributeProvider"/> when the parameter
/// carries attributes of more than one type.</summary>
/// <param name="type">The attribute type every entry of <paramref name="attributes"/> has.</param>
/// <param name="attributes">The attribute information.</param>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public sealed class GeneratedSingleTypeParameterAttributeProvider(Type type, object[] attributes) : ICustomAttributeProvider
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object[] GetCustomAttributes(bool inherit) => attributes;

    /// <inheritdoc/>
    public object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        ArgumentExceptionHelper.ThrowIfNull(attributeType);

        return attributeType == type ? attributes : [];
    }

    /// <inheritdoc/>
    public bool IsDefined(Type attributeType, bool inherit)
    {
        ArgumentExceptionHelper.ThrowIfNull(attributeType);

        return attributeType == type;
    }
}
