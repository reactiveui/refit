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
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode(
        "Refit uses reflection to analyze interface methods. Ensure referenced interfaces and DTOs are preserved when trimming.")]
    [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
    IRequestBuilder<T> Create<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(RefitSettings? settings);

    /// <summary>Creates a request builder for the specified interface type.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="settings">The settings to configure the builder.</param>
    /// <returns>A request builder for the interface type.</returns>
    [RequiresUnreferencedCode(
        "Refit uses reflection to analyze interface methods. Ensure referenced interfaces and DTOs are preserved when trimming.")]
    [RequiresDynamicCode("Refit may generate or invoke code dynamically for this path.")]
    IRequestBuilder Create(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        RefitSettings? settings);
}
