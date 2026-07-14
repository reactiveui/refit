// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Creates request builders for Refit interface types.</summary>
internal interface IRequestBuilderFactory
{
    /// <summary>Creates a strongly typed request builder for the specified interface type.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="settings">The settings to configure the builder.</param>
    /// <returns>A request builder for the interface type.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode("Building requests from reflected interface methods requires interface and request object metadata to be available at runtime.")]
    IRequestBuilder<T> Create<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(RefitSettings? settings);

    /// <summary>Creates a request builder for the specified interface type.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="settings">The settings to configure the builder.</param>
    /// <returns>A request builder for the interface type.</returns>
    [RequiresUnreferencedCode("Building requests from reflected interface methods requires interface and request object metadata to be available at runtime.")]
    IRequestBuilder Create(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        RefitSettings? settings);
}
