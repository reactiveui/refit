// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Creates the reflection request builders.</summary>
internal static class RequestBuilderFactory
{
    /// <summary>Creates a strongly typed request builder for the specified interface type.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="settings">The settings to configure the builder.</param>
    /// <returns>A request builder for the interface type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal static IRequestBuilder<T> Create<T>(RefitSettings? settings)
    {
        ReflectionSupport.EnsureRegistered();
        return new CachedRequestBuilderImplementation<T>(
            new RequestBuilderImplementation<T>(settings));
    }

    /// <summary>Creates a request builder for the specified interface type.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="settings">The settings to configure the builder.</param>
    /// <returns>A request builder for the interface type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IRequestBuilder Create(Type refitInterfaceType, RefitSettings? settings)
    {
        ReflectionSupport.EnsureRegistered();
        return new CachedRequestBuilderImplementation(
            new RequestBuilderImplementation(refitInterfaceType, settings));
    }
}
