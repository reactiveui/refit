// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>
/// Lazily resolves the reflection request-builder factory from the optional Refit.Reflection package. Core Refit
/// never references that assembly statically, so applications that only use generated clients neither ship it nor
/// carry the reflection pipeline through trimming and Native AOT.
/// </summary>
internal static class ReflectionRequestBuilderResolver
{
    /// <summary>The assembly-qualified name of the reflection request-builder factory.</summary>
    private const string FactoryTypeName = "Refit.RequestBuilderFactory, Refit.Reflection";

    /// <summary>The lazily resolved factory instance.</summary>
    private static IRequestBuilderFactory? _factory;

    /// <summary>Gets the reflection request-builder factory, loading the Refit.Reflection assembly on first use.</summary>
    /// <returns>The factory instance.</returns>
    /// <exception cref="NotSupportedException">The Refit.Reflection package is not installed.</exception>
    [RequiresUnreferencedCode("The reflection request builder requires runtime type lookup and request metadata.")]
    internal static IRequestBuilderFactory GetFactory() => _factory ??= CreateFactory();

    /// <summary>Loads and instantiates the reflection request-builder factory.</summary>
    /// <returns>The factory instance.</returns>
    [RequiresUnreferencedCode("The reflection request builder requires runtime type lookup and request metadata.")]
    private static IRequestBuilderFactory CreateFactory() =>
        Type.GetType(FactoryTypeName, throwOnError: false) is { } factoryType
        && Activator.CreateInstance(factoryType) is IRequestBuilderFactory factory
            ? factory
            : throw new NotSupportedException(
                "This interface needs the reflection request builder, which is not installed. Add a reference to "
                + "the Refit.Reflection NuGet package to opt in to it, or change the interface so every method "
                + "generates inline (the RF006 diagnostic reports the methods that cannot) and use a generated "
                + "client via RestService.ForGenerated or AddRefitGeneratedClient.");
}
