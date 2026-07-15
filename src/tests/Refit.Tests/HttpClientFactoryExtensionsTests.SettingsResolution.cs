// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;

namespace Refit.Tests;

/// <summary>
/// Tests that the registration overloads run their settings factories when the settings holder is resolved,
/// covering both supplied and absent factory paths and the keyed primary-handler configuration.
/// </summary>
public partial class HttpClientFactoryExtensionsTests
{
    /// <summary>The service key used by the keyed settings-resolution registrations.</summary>
    private const string TypeKeyedFactoryKey = "type-keyed-null-factory";

    /// <summary>The service key used by the generic keyed settings-resolution registration.</summary>
    private const string GenericKeyedFactoryKey = "generic-keyed-null-factory";

    /// <summary>The service key used by the keyed primary-handler registration.</summary>
    private const string KeyedHandlerKey = "keyed-handler-factory";

    /// <summary>The service key used by the keyed default-handler registration.</summary>
    private const string KeyedDefaultHandlerKey = "keyed-default-handler";

    /// <summary>Verifies the builder type overload resolves a null settings holder from its null factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientBuilderTypeOverloadResolvesNullSettings()
    {
        var services = new ServiceCollection();
        _ = services.AddHttpClient("builder-type-null").AddRefitClient(typeof(IFooWithOtherAttribute));

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(
                serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings)
            .IsNull();
    }

    /// <summary>Verifies the type settings-and-name overload resolves the supplied settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task ServiceCollectionTypeSettingsAndNameOverloadResolvesSuppliedSettings()
    {
        var settings = new RefitSettings();
        var services = new ServiceCollection();
        _ = services.AddRefitClient(typeof(IFooWithOtherAttribute), settings, "type-settings-and-name");

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(
                serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings)
            .IsSameReferenceAs(settings);
    }

    /// <summary>Verifies the generic settings-and-name overload resolves the supplied settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ServiceCollectionGenericSettingsAndNameOverloadResolvesSuppliedSettings()
    {
        var settings = new RefitSettings();
        var services = new ServiceCollection();
        _ = services.AddRefitClient<IFooWithOtherAttribute>(settings, "generic-settings-and-name");

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(
                serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings)
            .IsSameReferenceAs(settings);
    }

    /// <summary>Verifies the type settings-action overload resolves a null settings holder from a null factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task ServiceCollectionTypeSettingsActionOverloadResolvesNullFactory()
    {
        var services = new ServiceCollection();
        _ = services.AddRefitClient(typeof(IFooWithOtherAttribute), (Func<IServiceProvider, RefitSettings?>?)null);

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(
                serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings)
            .IsNull();
    }

    /// <summary>Verifies the keyed type settings-action overload resolves a null settings holder from a null factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task ServiceCollectionKeyedTypeSettingsActionOverloadResolvesNullFactory()
    {
        var services = new ServiceCollection();
        _ = services.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            TypeKeyedFactoryKey,
            (Func<IServiceProvider, RefitSettings?>?)null);

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(
                serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(TypeKeyedFactoryKey).Settings)
            .IsNull();
    }

    /// <summary>Verifies the keyed generic settings-action overload resolves a null settings holder from a null factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ServiceCollectionKeyedGenericSettingsActionOverloadResolvesNullFactory()
    {
        var services = new ServiceCollection();
        _ = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            GenericKeyedFactoryKey,
            (Func<IServiceProvider, RefitSettings?>?)null);

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(
                serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(GenericKeyedFactoryKey).Settings)
            .IsNull();
    }

    /// <summary>Verifies the generated-client settings-action overload resolves a null settings holder from a null factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ServiceCollectionGeneratedSettingsActionOverloadResolvesNullFactory()
    {
        var services = new ServiceCollection();
        _ = services.AddRefitGeneratedClient<IFooWithOtherAttribute>((Func<IServiceProvider, RefitSettings?>?)null);

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(
                serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings)
            .IsNull();
    }

    /// <summary>Verifies a keyed client builds its primary handler from the configured message-handler factory.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task KeyedClientPrimaryHandlerUsesConfiguredMessageHandlerFactory()
    {
        using var configuredHandler = new HttpClientHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => configuredHandler };
        var services = new ServiceCollection();
        _ = services.AddKeyedRefitClient<IFooWithOtherAttribute>(KeyedHandlerKey, settings);

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(serviceProvider.GetRequiredKeyedService<IFooWithOtherAttribute>(KeyedHandlerKey)).IsNotNull();
    }

    /// <summary>Verifies a keyed client falls back to a default primary handler when no message-handler factory is set.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task KeyedClientPrimaryHandlerFallsBackToDefaultWhenNoMessageHandlerFactory()
    {
        var settings = new RefitSettings();
        var services = new ServiceCollection();
        _ = services.AddKeyedRefitClient<IFooWithOtherAttribute>(KeyedDefaultHandlerKey, settings);

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(serviceProvider.GetRequiredKeyedService<IFooWithOtherAttribute>(KeyedDefaultHandlerKey)).IsNotNull();
    }
}
