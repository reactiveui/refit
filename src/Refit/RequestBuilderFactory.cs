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
    [RequiresUnreferencedCode("Building requests from reflected interface methods requires interface and request object metadata to be available at runtime.")]
    public IRequestBuilder<T> Create<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
    T>(RefitSettings? settings) =>
        new CachedRequestBuilderImplementation<T>(
            new RequestBuilderImplementation<T>(settings));

    /// <inheritdoc/>
    [RequiresUnreferencedCode("Building requests from reflected interface methods requires interface and request object metadata to be available at runtime.")]
    public IRequestBuilder Create(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        RefitSettings? settings) =>
        new CachedRequestBuilderImplementation(
            new RequestBuilderImplementation(refitInterfaceType, settings));
}
