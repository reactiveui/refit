// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

namespace Refit.Tests;

/// <summary>Argument-validation helpers for <see cref="HttpClientFactoryExtensionsTests"/>.</summary>
public partial class HttpClientFactoryExtensionsTests
{
    /// <summary>Asserts that a valid service collection rejects null interface types and settings arguments.</summary>
    /// <param name="services">The service collection under test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    private static async Task AssertValidServicesRejectNullInterfaceAndSettings(IServiceCollection services)
    {
        await Assert.That(() => services.AddRefitClient(null!)).ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), null!))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitClient<IFooWithOtherAttribute>(null!))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitClient<IFooWithOtherAttribute>(null!, new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    null!,
                    static _ => null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitClient(null!, ServiceKey))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient(null!, new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient(null!, new RefitSettings(), ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddRefitClient(
                    null!,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddRefitClient(
                    null!,
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    null!,
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    null!,
                    new RefitSettings(),
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    null!,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    null!,
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Asserts that a valid service collection rejects null interface types on keyed overloads and builds a keyed registration.</summary>
    /// <param name="services">The service collection under test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    private static async Task AssertValidServicesRejectNullKeyedInterface(IServiceCollection services)
    {
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    null!,
                    ServiceKey,
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    null!,
                    ServiceKey,
                    new RefitSettings(),
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    null!,
                    ServiceKey,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    null!,
                    ServiceKey,
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        var keyedTypeBuilder = services.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            ServiceKey,
            (Func<IServiceProvider, RefitSettings?>?)null);
        await Assert.That(keyedTypeBuilder).IsNotNull();
    }

    /// <summary>Asserts that a null service collection rejects the type and generic AddRefitClient overloads.</summary>
    /// <param name="services">The null service collection under test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    private static async Task AssertNullServicesRejectTypeAndGenericOverloads(IServiceCollection services)
    {
        await Assert.That(() => services.AddRefitClient(typeof(IFooWithOtherAttribute)))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient<IFooWithOtherAttribute>())
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient(typeof(IFooWithOtherAttribute), new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddRefitClient(
                    typeof(IFooWithOtherAttribute),
                    new RefitSettings(),
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddRefitClient(
                    typeof(IFooWithOtherAttribute),
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddRefitClient(
                    typeof(IFooWithOtherAttribute),
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient<IFooWithOtherAttribute>(new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient<IFooWithOtherAttribute>(new RefitSettings(), ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddRefitClient<IFooWithOtherAttribute>(
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddRefitClient<IFooWithOtherAttribute>(
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Asserts that a null service collection rejects the keyed AddKeyedRefitClient overloads.</summary>
    /// <param name="services">The null service collection under test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    private static async Task AssertNullServicesRejectKeyedOverloads(IServiceCollection services)
    {
        await Assert.That(() => services.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), ServiceKey))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    ServiceKey,
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    ServiceKey,
                    new RefitSettings(),
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    ServiceKey,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    ServiceKey,
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitClient<IFooWithOtherAttribute>(ServiceKey))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitClient<IFooWithOtherAttribute>(ServiceKey, new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    ServiceKey,
                    new RefitSettings(),
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    ServiceKey,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    ServiceKey,
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Asserts that a valid HTTP client builder rejects null interface types and settings arguments.</summary>
    /// <param name="builder">The HTTP client builder under test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    private static async Task AssertValidBuilderRejectsNullArguments(IHttpClientBuilder builder)
    {
        await Assert.That(() => builder.AddRefitClient(null!)).ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), null!))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddKeyedRefitClient<IFooWithOtherAttribute>(null!))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddKeyedRefitClient<IFooWithOtherAttribute>(null!, new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    null!,
                    static _ => null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddKeyedRefitClient(null!, BuilderKey))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddRefitClient(null!, new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddRefitClient(
                    null!,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    null!,
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    null!,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient(
                    null!,
                    BuilderKey,
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient(
                    null!,
                    BuilderKey,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Asserts that a null HTTP client builder rejects every AddRefitClient and AddKeyedRefitClient overload.</summary>
    /// <param name="builder">The null HTTP client builder under test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    private static async Task AssertNullBuilderRejectsAllOverloads(IHttpClientBuilder builder)
    {
        await Assert.That(() => builder.AddRefitClient(typeof(IFooWithOtherAttribute)))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddRefitClient<IFooWithOtherAttribute>())
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddRefitClient(typeof(IFooWithOtherAttribute), new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddRefitClient(
                    typeof(IFooWithOtherAttribute),
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddRefitClient<IFooWithOtherAttribute>(new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddRefitClient<IFooWithOtherAttribute>(
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), BuilderKey))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    BuilderKey,
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    BuilderKey,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddKeyedRefitClient<IFooWithOtherAttribute>(BuilderKey))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddKeyedRefitClient<IFooWithOtherAttribute>(BuilderKey, new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    BuilderKey,
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
    }
}
