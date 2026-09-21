// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.DependencyInjection;

namespace Refit.Tests;

/// <summary>Tests for the generated-client registrations that take a source-generated JSON context.</summary>
public partial class HttpClientFactoryExtensionsTests
{
    /// <summary>The base address given to the clients under test.</summary>
    private const string JsonContextBaseAddress = "http://json-context/";

    /// <summary>The service key used by the keyed JSON context registrations.</summary>
    private const string JsonContextServiceKey = "json-context-keyed";

    /// <summary>Verifies a registration made with only a context runs the client on the context's own options.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientWithContextUsesTheContextOptions()
    {
        var client = ResolveGeneratedClient(static services =>
            services.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(InventoryJsonContext.Default));

        var serializer = await Assert.That(client.Settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(serializer!.SerializerOptions).IsSameReferenceAs(InventoryJsonContext.Default.Options);
        await Assert.That(client.Client.BaseAddress).IsEqualTo(new(JsonContextBaseAddress));
    }

    /// <summary>Verifies a registration can allow the reflection fallback.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientWithContextCanAllowTheReflectionFallback()
    {
        var client = ResolveGeneratedClient(static services =>
            services.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(InventoryJsonContext.Default, true));

        var serializer = await Assert.That(client.Settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(serializer!.SerializerOptions.TypeInfoResolverChain[^1]).IsTypeOf<DefaultJsonTypeInfoResolver>();
    }

    /// <summary>Verifies settings from the factory keep their options and gain the context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientWithContextAndSettingsKeepsTheSettings()
    {
        var supplied = RefitSettings.SnakeCase();
        var namingPolicy = ((SystemTextJsonContentSerializer)supplied.ContentSerializer).SerializerOptions.PropertyNamingPolicy;

        var client = ResolveGeneratedClient(services =>
            services.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(InventoryJsonContext.Default, _ => supplied));

        var serializer = await Assert.That(client.Settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(client.Settings).IsSameReferenceAs(supplied);
        await Assert.That(serializer!.SerializerOptions.PropertyNamingPolicy).IsSameReferenceAs(namingPolicy);
        await Assert.That(serializer.SerializerOptions.TypeInfoResolverChain[0]).IsSameReferenceAs(InventoryJsonContext.Default);
    }

    /// <summary>Verifies a factory that yields no settings falls back to the context's own options.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientWithContextAndNoSettingsUsesTheContextOptions()
    {
        var client = ResolveGeneratedClient(static services =>
            services.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(InventoryJsonContext.Default, static _ => null));

        var serializer = await Assert.That(client.Settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(serializer!.SerializerOptions).IsSameReferenceAs(InventoryJsonContext.Default.Options);
    }

    /// <summary>Verifies the named overload registers the underlying HTTP client under that name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientWithContextUsesTheHttpClientName()
    {
        var services = new ServiceCollection();

        var builder = services.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            InventoryJsonContext.Default,
            static _ => new RefitSettings(),
            "json-context-named");

        await Assert.That(builder.Name).IsEqualTo("json-context-named");
    }

    /// <summary>Verifies the fullest overload applies the name and the reflection fallback.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientWithContextAppliesTheNameAndTheReflectionFallback()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));
        var services = new ServiceCollection();

        var builder = services.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            InventoryJsonContext.Default,
            static _ => new RefitSettings(),
            "json-context-fallback",
            true);
        _ = builder.ConfigureHttpClient(static c => c.BaseAddress = new(JsonContextBaseAddress));
        var resolved = (GeneratedSettingsFactoryApiClient)services.BuildServiceProvider().GetRequiredService<IGeneratedSettingsFactoryApi>();

        var serializer = (SystemTextJsonContentSerializer)resolved.Settings.ContentSerializer;
        await Assert.That(builder.Name).IsEqualTo("json-context-fallback");
        await Assert.That(serializer.SerializerOptions.TypeInfoResolverChain[^1]).IsTypeOf<DefaultJsonTypeInfoResolver>();
    }

    /// <summary>Verifies the keyed overload with only a context runs the client on the context's own options.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientWithContextUsesTheContextOptions()
    {
        var client = ResolveKeyedGeneratedClient(static services =>
            services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(JsonContextServiceKey, InventoryJsonContext.Default));

        var serializer = await Assert.That(client.Settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(serializer!.SerializerOptions).IsSameReferenceAs(InventoryJsonContext.Default.Options);
    }

    /// <summary>Verifies the keyed overload with a settings factory keeps the settings and gains the context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientWithContextAndSettingsKeepsTheSettings()
    {
        var supplied = new RefitSettings();

        var client = ResolveKeyedGeneratedClient(services =>
            services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(JsonContextServiceKey, InventoryJsonContext.Default, _ => supplied));

        var serializer = await Assert.That(client.Settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(client.Settings).IsSameReferenceAs(supplied);
        await Assert.That(serializer!.SerializerOptions.TypeInfoResolverChain[0]).IsSameReferenceAs(InventoryJsonContext.Default);
    }

    /// <summary>Verifies the fullest keyed overload applies the name and the reflection fallback.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddKeyedRefitGeneratedClientWithContextAppliesTheNameAndTheReflectionFallback()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));
        var services = new ServiceCollection();

        var builder = services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            JsonContextServiceKey,
            InventoryJsonContext.Default,
            static _ => new RefitSettings(),
            "json-context-keyed-fallback",
            true);
        _ = builder.ConfigureHttpClient(static c => c.BaseAddress = new(JsonContextBaseAddress));
        var resolved = (GeneratedSettingsFactoryApiClient)services.BuildServiceProvider()
            .GetRequiredKeyedService<IGeneratedSettingsFactoryApi>(JsonContextServiceKey);

        var serializer = (SystemTextJsonContentSerializer)resolved.Settings.ContentSerializer;
        await Assert.That(builder.Name).IsEqualTo("json-context-keyed-fallback");
        await Assert.That(serializer.SerializerOptions.TypeInfoResolverChain[^1]).IsTypeOf<DefaultJsonTypeInfoResolver>();
    }

    /// <summary>Verifies the registrations reject a missing context, a missing service key and a missing collection.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task JsonContextRegistrationsRejectMissingArguments()
    {
        var services = new ServiceCollection();
        IServiceCollection missing = null!;

        await Assert.That(() => services.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(null!, true)).Throws<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(null, InventoryJsonContext.Default))
            .Throws<ArgumentNullException>();
        await Assert.That(() => missing.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(InventoryJsonContext.Default, true))
            .Throws<ArgumentNullException>();
        await Assert.That(() => missing.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(InventoryJsonContext.Default))
            .Throws<ArgumentNullException>();
        await Assert.That(() => missing.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(InventoryJsonContext.Default, static _ => null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => missing.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(InventoryJsonContext.Default, static _ => null, "name"))
            .Throws<ArgumentNullException>();
        await Assert.That(() => missing.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(InventoryJsonContext.Default, static _ => null, "name", true))
            .Throws<ArgumentNullException>();
        await Assert.That(() => missing.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>("key", InventoryJsonContext.Default))
            .Throws<ArgumentNullException>();
        await Assert.That(() => missing.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>("key", InventoryJsonContext.Default, static _ => null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => missing.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>("key", InventoryJsonContext.Default, static _ => null, "name", true))
            .Throws<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(null, InventoryJsonContext.Default, static _ => null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitGeneratedClient<IGeneratedSettingsFactoryApi>(null, InventoryJsonContext.Default, static _ => null, "name", true))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Registers a generated client through <paramref name="register"/> and resolves it.</summary>
    /// <param name="register">Adds the registration under test.</param>
    /// <returns>The resolved generated client double.</returns>
    private static GeneratedSettingsFactoryApiClient ResolveGeneratedClient(
        Func<IServiceCollection, IHttpClientBuilder> register)
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));
        var services = new ServiceCollection();
        _ = register(services).ConfigureHttpClient(static c => c.BaseAddress = new(JsonContextBaseAddress));

        return (GeneratedSettingsFactoryApiClient)services.BuildServiceProvider().GetRequiredService<IGeneratedSettingsFactoryApi>();
    }

    /// <summary>Registers a keyed generated client through <paramref name="register"/> and resolves it.</summary>
    /// <param name="register">Adds the registration under test.</param>
    /// <returns>The resolved generated client double.</returns>
    private static GeneratedSettingsFactoryApiClient ResolveKeyedGeneratedClient(
        Func<IServiceCollection, IHttpClientBuilder> register)
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));
        var services = new ServiceCollection();
        _ = register(services).ConfigureHttpClient(static c => c.BaseAddress = new(JsonContextBaseAddress));

        return (GeneratedSettingsFactoryApiClient)services.BuildServiceProvider()
            .GetRequiredKeyedService<IGeneratedSettingsFactoryApi>(JsonContextServiceKey);
    }
}
