// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;

namespace Refit.Tests;

/// <summary>Tests for the keyed, generated-only Refit dependency-injection extension methods.</summary>
public partial class HttpClientFactoryExtensionsTests
{
    /// <summary>The service key used for the keyed generated-client registrations under test.</summary>
    private const string GeneratedServiceKey = "generated-keyed";

    /// <summary>Verifies the keyed generated-only DI helper resolves a source-generated client under its key and injects the supplied settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientResolvesGeneratedImplementation()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var settings = new RefitSettings(new SystemTextJsonContentSerializer());
        var serviceCollection = new ServiceCollection();
        var builder = serviceCollection.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey,
            settings);
        _ = builder.ConfigureHttpClient(static c => c.BaseAddress = new("http://generated-keyed/"));

        await Assert.That(serviceCollection).Contains(
            static z => z.ServiceType == typeof(SettingsFor<IGeneratedSettingsFactoryApi>)
                && z.IsKeyedService);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredKeyedService<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey);

        var generated = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        await Assert.That(generated!.Settings).IsSameReferenceAs(settings);
        await Assert.That(generated.Client.BaseAddress).IsEqualTo(new("http://generated-keyed/"));
    }

    /// <summary>Verifies the keyed generated-only registration is not resolvable without the key.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientIsNotResolvableWithoutKey()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        await Assert.That(serviceProvider.GetService<IGeneratedSettingsFactoryApi>()).IsNull();
    }

    /// <summary>Verifies the settings-factory overload of the keyed generated-only DI helper resolves settings from the provider.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientUsesSettingsFactoryFromProvider()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var serializer = new SystemTextJsonContentSerializer();
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddSingleton(new ClientOptions { Serializer = serializer });
        _ = serviceCollection.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey,
            static provider => new RefitSettings(provider.GetRequiredService<ClientOptions>().Serializer!));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredKeyedService<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey);

        var generated = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        await Assert.That(generated!.Settings.ContentSerializer).IsSameReferenceAs(serializer);
    }

    /// <summary>Verifies the key-only overload resolves a client with default settings and the default primary handler.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientWithoutSettingsResolvesWithDefaults()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredKeyedService<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey);

        var generated = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();

        // No settings were supplied, so ForGenerated receives a fresh default RefitSettings instance.
        await Assert.That(generated!.Settings).IsNotNull();
    }

    /// <summary>Verifies the settings-and-name overload honors the custom client name and builds the handler from the supplied settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientWithSettingsAndNameUsesCustomNameAndHandler()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var recordingHandler = new RecordingHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => recordingHandler };
        var serviceCollection = new ServiceCollection();
        var builder = serviceCollection.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey,
            settings,
            "generated-keyed-named-client");

        await Assert.That(builder.Name).IsEqualTo("generated-keyed-named-client");

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredKeyedService<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey);

        var generated = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        await Assert.That(generated!.Settings).IsSameReferenceAs(settings);
    }

    /// <summary>Verifies the settings-factory-and-name overload honors the custom client name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientWithSettingsFactoryAndNameUsesCustomName()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var serviceCollection = new ServiceCollection();
        var builder = serviceCollection.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey,
            static _ => new RefitSettings(),
            "generated-keyed-factory-named-client");

        await Assert.That(builder.Name).IsEqualTo("generated-keyed-factory-named-client");

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredKeyedService<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey);

        _ = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();
    }

    /// <summary>Verifies two keys for the same interface produce independently configured HTTP clients.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientIsolatesRegistrationsPerKey()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var serviceCollection = new ServiceCollection();
        _ = serviceCollection
            .AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>("primary")
            .ConfigureHttpClient(static c => c.BaseAddress = new("http://primary/"));
        _ = serviceCollection
            .AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>("secondary")
            .ConfigureHttpClient(static c => c.BaseAddress = new("http://secondary/"));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var primary = (GeneratedSettingsFactoryApiClient)serviceProvider
            .GetRequiredKeyedService<IGeneratedSettingsFactoryApi>("primary");
        var secondary = (GeneratedSettingsFactoryApiClient)serviceProvider
            .GetRequiredKeyedService<IGeneratedSettingsFactoryApi>("secondary");

        await Assert.That(primary.Client.BaseAddress).IsEqualTo(new("http://primary/"));
        await Assert.That(secondary.Client.BaseAddress).IsEqualTo(new("http://secondary/"));
    }

    /// <summary>Verifies the keyed generated registration composes the configured handler and the authorization getter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientComposesConfiguredHandlerAndAuthorizationGetter()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var recordingHandler = new RecordingHandler();
        var services = new ServiceCollection();
        var builder = services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            GeneratedServiceKey,
            new RefitSettings { HttpMessageHandlerFactory = () => recordingHandler, AuthorizationHeaderValueGetter = static (_, _) => new ValueTask<string>("generated-keyed-token"), });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(builder.Name);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "placeholder");

        using var response = await client.GetAsync(new Uri("https://example.test"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(recordingHandler.AuthorizationParameter).IsEqualTo("generated-keyed-token");
    }

    /// <summary>Verifies every keyed generated overload rejects a null service collection and a null service key.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientRejectsNullArguments()
    {
        IServiceCollection nullServices = null!;

        await Assert.That(
                () => nullServices.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(GeneratedServiceKey))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => nullServices.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
                    GeneratedServiceKey,
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => nullServices.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
                    GeneratedServiceKey,
                    new RefitSettings(),
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => nullServices.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
                    GeneratedServiceKey,
                    static _ => null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => nullServices.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
                    GeneratedServiceKey,
                    static _ => null,
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();

        var services = new ServiceCollection();

        await Assert.That(
                () => services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(null!))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
                    null!,
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
                    null!,
                    new RefitSettings(),
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
                    null!,
                    static _ => null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
                    null!,
                    static _ => null,
                    ServiceClientName))
            .ThrowsExactly<ArgumentNullException>();
    }
}
