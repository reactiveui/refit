// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Default implementation of <c>IRequestBuilderFactory</c>.</summary>
internal class RequestBuilderFactory : IRequestBuilderFactory
{
    /// <inheritdoc/>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode(
        "Refit uses reflection to analyze interface methods. Ensure referenced interfaces and DTOs are preserved when trimming.")]
    [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
    public IRequestBuilder<T> Create<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(RefitSettings? settings)
    {
        return new CachedRequestBuilderImplementation<T>(
            new RequestBuilderImplementation<T>(settings));
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode(
        "Refit uses reflection to analyze interface methods. Ensure referenced interfaces and DTOs are preserved when trimming.")]
    [RequiresDynamicCode("Refit may generate or invoke code dynamically for this path.")]
    public IRequestBuilder Create(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        RefitSettings? settings)
    {
        return new CachedRequestBuilderImplementation(
            new RequestBuilderImplementation(refitInterfaceType, settings));
    }
}
