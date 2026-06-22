// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Refit.Tests;

/// <summary>Tests for the Refit HttpClientFactory dependency-injection extension methods.</summary>
public class HttpClientFactoryExtensionsTests
{
    /// <summary>Verifies that generic Refit clients registered for different interfaces receive unique client names.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GenericHttpClientsAreAssignedUniqueNames()
    {
        var services = new ServiceCollection();

        var userClientName = services.AddRefitClient<IBoringCrudApi<User, string>>().Name;
        var roleClientName = services.AddRefitClient<IBoringCrudApi<Role, string>>().Name;

        await Assert.That(roleClientName).IsNotEqualTo(userClientName);
    }

    /// <summary>Verifies that the keyed and non-keyed Refit registrations resolve to distinct services and settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientServicesAreDifferentThanKeyedServices()
    {
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
        _ = serviceCollection.AddKeyedRefitClient<IFooWithOtherAttribute>("keyed");

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var nonKeyedService = serviceProvider.GetService<IFooWithOtherAttribute>();
        var keyedService = serviceProvider.GetKeyedService<IFooWithOtherAttribute>("keyed");

        await Assert.That(nonKeyedService).IsNotNull();
        await Assert.That(keyedService).IsNotNull();
        await Assert.That(keyedService).IsNotSameReferenceAs(nonKeyedService);

        var nonKeyedSettings = serviceProvider.GetService<SettingsFor<IFooWithOtherAttribute>>();
        var keyedSettings = serviceProvider.GetKeyedService<SettingsFor<IFooWithOtherAttribute>>("keyed");
        await Assert.That(keyedSettings).IsNotSameReferenceAs(nonKeyedSettings);

        var nonKeyedRequestBuilder = serviceProvider.GetService<IRequestBuilder<IFooWithOtherAttribute>>();
        var keyedRequestBuilder = serviceProvider.GetKeyedService<IRequestBuilder<IFooWithOtherAttribute>>("keyed");
        await Assert.That(keyedRequestBuilder).IsNotSameReferenceAs(nonKeyedRequestBuilder);
    }

    /// <summary>Verifies the generic overload registers the settings and request-builder services.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientServicesAreAddedCorrectlyGivenGenericArgument()
    {
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
        await Assert.That(serviceCollection).Contains(
            z => z.ServiceType == typeof(SettingsFor<IFooWithOtherAttribute>));
        await Assert.That(serviceCollection).Contains(
            z => z.ServiceType == typeof(IRequestBuilder<IFooWithOtherAttribute>));
    }

    /// <summary>Verifies the <see cref="Type"/> overload registers the settings and request-builder services.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientServicesAreAddedCorrectlyGivenTypeArgument()
    {
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddRefitClient(typeof(IFooWithOtherAttribute));
        await Assert.That(serviceCollection).Contains(
            z => z.ServiceType == typeof(SettingsFor<IFooWithOtherAttribute>));
        await Assert.That(serviceCollection).Contains(
            z => z.ServiceType == typeof(IRequestBuilder<IFooWithOtherAttribute>));
    }

    /// <summary>Verifies the generic overload resolves a usable client instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientReturnsClientGivenGenericArgument()
    {
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        await Assert.That(serviceProvider.GetService<IFooWithOtherAttribute>()).IsNotNull();
    }

    /// <summary>Verifies the <see cref="Type"/> overload resolves a usable client instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientReturnsClientGivenTypeArgument()
    {
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddRefitClient(typeof(IFooWithOtherAttribute));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        await Assert.That(serviceProvider.GetService<IFooWithOtherAttribute>()).IsNotNull();
    }

    /// <summary>Verifies the generic overload injects settings resolved from the service provider.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientSettingsAreInjectableGivenGenericArgument()
    {
        var serviceCollection = new ServiceCollection().Configure<ClientOptions>(
            o => o.Serializer = new(new()));
        _ = serviceCollection.AddRefitClient<IFooWithOtherAttribute>(
            _ =>
                new()
                {
                    ContentSerializer = _.GetRequiredService<
                        IOptions<ClientOptions>>().Value.Serializer!
                });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        await Assert.That(
            serviceProvider
                .GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!.ContentSerializer).IsSameReferenceAs(
            serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer);
    }

    /// <summary>Verifies the <see cref="Type"/> overload injects settings resolved from the service provider.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientSettingsAreInjectableGivenTypeArgument()
    {
        var serviceCollection = new ServiceCollection().Configure<ClientOptions>(
            o => o.Serializer = new(new()));
        _ = serviceCollection.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            _ =>
                new()
                {
                    ContentSerializer = _.GetRequiredService<
                        IOptions<ClientOptions>>().Value.Serializer!
                });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        await Assert.That(
            serviceProvider
                .GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!.ContentSerializer).IsSameReferenceAs(
            serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer);
    }

    /// <summary>Verifies the generic overload uses settings supplied statically at registration time.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientSettingsCanBeProvidedStaticallyGivenGenericArgument()
    {
        var contentSerializer = new SystemTextJsonContentSerializer(new());
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddRefitClient<IFooWithOtherAttribute>(
            new RefitSettings { ContentSerializer = contentSerializer });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        await Assert.That(
            serviceProvider
                .GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!.ContentSerializer).IsSameReferenceAs(contentSerializer);
    }

    /// <summary>Verifies the <see cref="Type"/> overload uses settings supplied statically at registration time.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientSettingsCanBeProvidedStaticallyGivenTypeArgument()
    {
        var contentSerializer = new SystemTextJsonContentSerializer(new());
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            new RefitSettings { ContentSerializer = contentSerializer });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        await Assert.That(
            serviceProvider
                .GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!.ContentSerializer).IsSameReferenceAs(contentSerializer);
    }

    /// <summary>Verifies keyed generic service-collection registrations can use static settings and a named HTTP client.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ServiceCollectionKeyedGenericSettingsOverloadUsesStaticSettingsAndClientName()
    {
        var contentSerializer = new SystemTextJsonContentSerializer(new());
        var settings = new RefitSettings { ContentSerializer = contentSerializer };
        var services = new ServiceCollection();

        var builder = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            "service-key",
            settings,
            "service-client");

        await Assert.That(builder.Name).IsEqualTo("service-client");

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("service-key")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(contentSerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<IRequestBuilder<IFooWithOtherAttribute>>("service-key"))
            .IsNotNull();
    }

    /// <summary>Verifies keyed <see cref="Type"/> service-collection registrations can resolve settings from services.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task ServiceCollectionKeyedTypeSettingsFactoryOverloadUsesServiceProvider()
    {
        var services = new ServiceCollection().Configure<ClientOptions>(
            o => o.Serializer = new(new()));

        var builder = services.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            "service-key",
            serviceProvider => new()
            {
                ContentSerializer = serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer!
            },
            "service-client");

        await Assert.That(builder.Name).IsEqualTo("service-client");

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("service-key")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(
            serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer);
    }

    /// <summary>Verifies keyed <see cref="Type"/> service-collection registrations can use static settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task ServiceCollectionKeyedTypeSettingsOverloadUsesStaticSettings()
    {
        var contentSerializer = new SystemTextJsonContentSerializer(new());
        var settings = new RefitSettings { ContentSerializer = contentSerializer };
        var services = new ServiceCollection();

        _ = services.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), "service-key", settings);

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("service-key")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(contentSerializer);
    }

    /// <summary>Verifies service-collection overloads reject missing required arguments.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task ServiceCollectionOverloadsValidateRequiredArguments()
    {
        IServiceCollection services = new ServiceCollection();

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
        await Assert.That(() => services.AddKeyedRefitClient(null!, "service-key"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient(null!, new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient(null!, new RefitSettings(), "service-client"))
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
                    "service-client"))
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
                    "service-client"))
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
                    "service-client"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    null!,
                    "service-key",
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    null!,
                    "service-key",
                    new RefitSettings(),
                    "service-client"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    null!,
                    "service-key",
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    null!,
                    "service-key",
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    "service-client"))
            .ThrowsExactly<ArgumentNullException>();
        var keyedTypeBuilder = services.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            "service-key",
            (Func<IServiceProvider, RefitSettings?>?)null);
        await Assert.That(keyedTypeBuilder).IsNotNull();

        services = null!;
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
                    "service-client"))
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
                    "service-client"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient<IFooWithOtherAttribute>(new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddRefitClient<IFooWithOtherAttribute>(new RefitSettings(), "service-client"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddRefitClient<IFooWithOtherAttribute>(
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddRefitClient<IFooWithOtherAttribute>(
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    "service-client"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), "service-key"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    "service-key",
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    "service-key",
                    new RefitSettings(),
                    "service-client"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    "service-key",
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    "service-key",
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    "service-client"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitClient<IFooWithOtherAttribute>("service-key"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => services.AddKeyedRefitClient<IFooWithOtherAttribute>("service-key", new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    "service-key",
                    new RefitSettings(),
                    "service-client"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    "service-key",
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => services.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    "service-key",
                    (Func<IServiceProvider, RefitSettings?>?)null,
                    "service-client"))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies service-collection overloads that accept client names pass those names through.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task ServiceCollectionNamedOverloadsUseProvidedClientNames()
    {
        var services = new ServiceCollection();
        var settings = new RefitSettings();

        var genericSettings = services.AddRefitClient<IFooWithOtherAttribute>(settings, "generic-settings");
        var genericFactory = services.AddRefitClient<IFooWithOtherAttribute>(
            static _ => new(),
            "generic-factory");
        var typeSettings = services.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            settings,
            "type-settings");
        var typeFactory = services.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            static _ => new(),
            "type-factory");

        await Assert.That(genericSettings.Name).IsEqualTo("generic-settings");
        await Assert.That(genericFactory.Name).IsEqualTo("generic-factory");
        await Assert.That(typeSettings.Name).IsEqualTo("type-settings");
        await Assert.That(typeFactory.Name).IsEqualTo("type-factory");
    }

    /// <summary>Verifies remaining keyed service-collection overloads register keyed services and settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task ServiceCollectionKeyedOverloadMatrixRegistersServices()
    {
        var genericSettingsSerializer = new SystemTextJsonContentSerializer(new());
        var genericFactorySerializer = new SystemTextJsonContentSerializer(new());
        var typeNamedSerializer = new SystemTextJsonContentSerializer(new());
        var typeFactorySerializer = new SystemTextJsonContentSerializer(new());
        var services = new ServiceCollection();

        var genericNoSettings = services.AddKeyedRefitClient<IFooWithOtherAttribute>("generic-none");
        _ = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            "generic-settings",
            new RefitSettings { ContentSerializer = genericSettingsSerializer });
        var genericNamedSettings = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            "generic-settings-named",
            new RefitSettings(),
            "generic-settings-client");
        _ = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            "generic-factory",
            static _ => new());
        var genericNamedFactory = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            "generic-factory-named",
            _ => new() { ContentSerializer = genericFactorySerializer },
            "generic-factory-client");
        _ = services.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), "type-none");
        var typeNamedSettings = services.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            "type-settings-named",
            new RefitSettings { ContentSerializer = typeNamedSerializer },
            "type-settings-client");
        _ = services.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            "type-factory",
            _ => new() { ContentSerializer = typeFactorySerializer });

        await Assert.That(genericNoSettings.Name).IsNotNull();
        await Assert.That(genericNamedSettings.Name).IsEqualTo("generic-settings-client");
        await Assert.That(genericNamedFactory.Name).IsEqualTo("generic-factory-client");
        await Assert.That(typeNamedSettings.Name).IsEqualTo("type-settings-client");

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("generic-settings")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(genericSettingsSerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("generic-factory-named")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(genericFactorySerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("type-settings-named")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(typeNamedSerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("type-factory")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(typeFactorySerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<IRequestBuilder<IFooWithOtherAttribute>>("type-none"))
            .IsNotNull();
        await Assert.That(serviceProvider.GetRequiredKeyedService<IFooWithOtherAttribute>("type-none")).IsNotNull();
    }

    /// <summary>Verifies the <see cref="IHttpClientBuilder"/> generic overload keeps the existing named client and registers Refit services.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientBuilderGenericOverloadUsesExistingBuilderName()
    {
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("builder-client", client => client.BaseAddress = new("https://builder.example"));

        var returnedBuilder = builder.AddRefitClient<IFooWithOtherAttribute>();

        await Assert.That(returnedBuilder.Name).IsEqualTo("builder-client");

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(serviceProvider.GetService<IFooWithOtherAttribute>()).IsNotNull();
        await Assert.That(
            serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings).IsNull();
    }

    /// <summary>Verifies the <see cref="IHttpClientBuilder"/> generic settings overload stores the supplied settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientBuilderGenericSettingsOverloadUsesStaticSettings()
    {
        var contentSerializer = new SystemTextJsonContentSerializer(new());
        var settings = new RefitSettings { ContentSerializer = contentSerializer };
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("builder-settings");

        _ = builder.AddRefitClient<IFooWithOtherAttribute>(settings);

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!
                .ContentSerializer).IsSameReferenceAs(contentSerializer);
    }

    /// <summary>Verifies the <see cref="IHttpClientBuilder"/> generic settings factory overload resolves settings from services.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientBuilderGenericSettingsFactoryOverloadUsesServiceProvider()
    {
        var services = new ServiceCollection().Configure<ClientOptions>(
            o => o.Serializer = new(new()));
        var builder = services.AddHttpClient("builder-settings-factory");

        _ = builder.AddRefitClient<IFooWithOtherAttribute>(
            serviceProvider => new()
            {
                ContentSerializer = serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer!
            });

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!
                .ContentSerializer).IsSameReferenceAs(
            serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer);
    }

    /// <summary>Verifies the <see cref="IHttpClientBuilder"/> <see cref="Type"/> settings overload stores the supplied settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientBuilderTypeSettingsOverloadUsesStaticSettings()
    {
        var contentSerializer = new SystemTextJsonContentSerializer(new());
        var settings = new RefitSettings { ContentSerializer = contentSerializer };
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("builder-type-settings");

        _ = builder.AddRefitClient(typeof(IFooWithOtherAttribute), settings);

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!
                .ContentSerializer).IsSameReferenceAs(contentSerializer);
    }

    /// <summary>Verifies the <see cref="IHttpClientBuilder"/> <see cref="Type"/> settings factory overload resolves settings from services.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientBuilderTypeSettingsFactoryOverloadUsesServiceProvider()
    {
        var services = new ServiceCollection().Configure<ClientOptions>(
            o => o.Serializer = new(new()));
        var builder = services.AddHttpClient("builder-type-settings-factory");

        _ = builder.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            serviceProvider => new()
            {
                ContentSerializer = serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer!
            });

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!
                .ContentSerializer).IsSameReferenceAs(
            serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer);
    }

    /// <summary>Verifies the <see cref="IHttpClientBuilder"/> keyed generic overload registers keyed services.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientBuilderKeyedGenericOverloadRegistersKeyedServices()
    {
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("builder-keyed");

        var returnedBuilder = builder.AddKeyedRefitClient<IFooWithOtherAttribute>("builder-key");

        await Assert.That(returnedBuilder.Name).IsEqualTo("builder-keyed");

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetKeyedService<IFooWithOtherAttribute>("builder-key")).IsNotNull();
        await Assert.That(
            serviceProvider.GetKeyedService<SettingsFor<IFooWithOtherAttribute>>("builder-key")).IsNotNull();
    }

    /// <summary>Verifies the <see cref="IHttpClientBuilder"/> keyed generic settings overload stores the supplied settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientBuilderKeyedGenericSettingsOverloadUsesStaticSettings()
    {
        var contentSerializer = new SystemTextJsonContentSerializer(new());
        var settings = new RefitSettings { ContentSerializer = contentSerializer };
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("builder-keyed-settings");

        _ = builder.AddKeyedRefitClient<IFooWithOtherAttribute>("builder-key", settings);

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("builder-key")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(contentSerializer);
    }

    /// <summary>Verifies the <see cref="IHttpClientBuilder"/> keyed <see cref="Type"/> settings factory overload resolves settings from services.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientBuilderKeyedTypeSettingsFactoryOverloadUsesServiceProvider()
    {
        var services = new ServiceCollection().Configure<ClientOptions>(
            o => o.Serializer = new(new()));
        var builder = services.AddHttpClient("builder-keyed-type-settings-factory");

        _ = builder.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            "builder-key",
            serviceProvider => new()
            {
                ContentSerializer = serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer!
            });

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("builder-key")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(
            serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer);
    }

    /// <summary>Verifies the <see cref="IHttpClientBuilder"/> overloads reject missing required arguments.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientBuilderOverloadsValidateRequiredArguments()
    {
        IHttpClientBuilder builder = new ServiceCollection().AddHttpClient("builder-validation");

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
        await Assert.That(() => builder.AddKeyedRefitClient(null!, "builder-key"))
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
                    "builder-key",
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient(
                    null!,
                    "builder-key",
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();

        builder = null!;
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
        await Assert.That(() => builder.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), "builder-key"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    "builder-key",
                    new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient(
                    typeof(IFooWithOtherAttribute),
                    "builder-key",
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddKeyedRefitClient<IFooWithOtherAttribute>("builder-key"))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => builder.AddKeyedRefitClient<IFooWithOtherAttribute>("builder-key", new RefitSettings()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(
                () => builder.AddKeyedRefitClient<IFooWithOtherAttribute>(
                    "builder-key",
                    (Func<IServiceProvider, RefitSettings?>?)null))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies the remaining <see cref="IHttpClientBuilder"/> overloads register Refit services on the existing builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task HttpClientBuilderOverloadMatrixRegistersServices()
    {
        var typeSettingsSerializer = new SystemTextJsonContentSerializer(new());
        var keyedTypeSettingsSerializer = new SystemTextJsonContentSerializer(new());
        var keyedGenericFactorySerializer = new SystemTextJsonContentSerializer(new());
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("builder-matrix");

        var typeBuilder = builder.AddRefitClient(typeof(IFooWithOtherAttribute));
        _ = builder.AddRefitClient<IFooWithOtherAttribute>(static _ => new());
        _ = builder.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            new RefitSettings { ContentSerializer = typeSettingsSerializer });
        var keyedTypeBuilder = builder.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), "type-none");
        _ = builder.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            "type-settings",
            new RefitSettings { ContentSerializer = keyedTypeSettingsSerializer });
        _ = builder.AddKeyedRefitClient<IFooWithOtherAttribute>(
            "generic-factory",
            _ => new() { ContentSerializer = keyedGenericFactorySerializer });

        await Assert.That(typeBuilder.Name).IsEqualTo("builder-matrix");
        await Assert.That(keyedTypeBuilder.Name).IsEqualTo("builder-matrix");

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!
                .ContentSerializer).IsSameReferenceAs(typeSettingsSerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("type-settings")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(keyedTypeSettingsSerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>("generic-factory")
                .Settings!
                .ContentSerializer).IsSameReferenceAs(keyedGenericFactorySerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<IRequestBuilder<IFooWithOtherAttribute>>("type-none"))
            .IsNotNull();
    }

    /// <summary>Verifies the generic <c>(IServiceCollection, RefitSettings)</c> overload still exists for binary compatibility.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitClient_ServiceCollectionGenericSettingsOverload_RemainsBinaryCompatible()
    {
        var method = typeof(HttpClientFactoryExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(method =>
                method.Name == nameof(HttpClientFactoryExtensions.AddRefitClient)
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 1
                && method.GetParameters() is
                [
                    { ParameterType: var servicesType },
                    { ParameterType: var settingsType }
                ]
                && servicesType == typeof(IServiceCollection)
                && settingsType == typeof(RefitSettings));

        await Assert.That(method).IsNotNull();
    }

    /// <summary>Verifies the <c>(IServiceCollection, Type, RefitSettings)</c> overload still exists for binary compatibility.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitClient_ServiceCollectionTypeSettingsOverload_RemainsBinaryCompatible()
    {
        var method = typeof(HttpClientFactoryExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(method =>
                method.Name == nameof(HttpClientFactoryExtensions.AddRefitClient)
                && !method.IsGenericMethodDefinition
                && method.GetParameters() is
                [
                    { ParameterType: var servicesType },
                    { ParameterType: var refitInterfaceType },
                    { ParameterType: var settingsType }
                ]
                && servicesType == typeof(IServiceCollection)
                && refitInterfaceType == typeof(Type)
                && settingsType == typeof(RefitSettings));

        await Assert.That(method).IsNotNull();
    }

    /// <summary>Verifies a pre-registered named <see cref="HttpClient"/> is honored by the generated Refit client.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ProvidedHttpClientIsUsedAsNamedClient()
    {
        var baseUri = new Uri("https://0:1337");
        var services = new ServiceCollection();

        _ = services.AddHttpClient("MyHttpClient", client =>
        {
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Add("X-Powered-By", Environment.OSVersion.VersionString);
        });
        var refitBuilder = services.AddRefitClient<IGitHubApi>(settingsAction: null, "MyHttpClient");

        var sp = services.BuildServiceProvider();
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("MyHttpClient");

        var gitHubApi = sp.GetRequiredService<IGitHubApi>();

        await Assert.That(refitBuilder.Name).IsEqualTo("MyHttpClient");
        await Assert.That(gitHubApi).IsNotNull();
        await Assert.That(httpClient.BaseAddress).IsEqualTo(baseUri);
        await Assert.That(httpClient.DefaultRequestHeaders).Contains(
            h => h.Key == "X-Powered-By"
                && h.Value.Contains(Environment.OSVersion.VersionString));
    }

    /// <summary>Verifies the shared core registration methods validate direct null inputs.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises non-generic core overloads.")]
    public async Task CoreRegistrationsRejectNullInputs()
    {
        var services = new ServiceCollection();

        await Assert.That(() => InvokeAddRefitClientCore(null!, typeof(IFooWithOtherAttribute), null, null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => InvokeAddRefitClientCore(services, null!, null, null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => InvokeAddRefitClientCoreGeneric<IFooWithOtherAttribute>(null!, null, null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => InvokeAddKeyedRefitClientCore(null!, typeof(IFooWithOtherAttribute), "key", null, null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => InvokeAddKeyedRefitClientCore(services, null!, "key", null, null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => InvokeAddKeyedRefitClientCore(services, typeof(IFooWithOtherAttribute), null, null, null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => InvokeAddKeyedRefitClientCoreGeneric<IFooWithOtherAttribute>(null!, "key", null, null))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => InvokeAddKeyedRefitClientCoreGeneric<IFooWithOtherAttribute>(services, null, null, null))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies configured handler factories and authorization getters are composed for named clients.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NonKeyedRegistrationComposesConfiguredHandlerAndAuthorizationGetter()
    {
        var recordingHandler = new RecordingHandler();
        var services = new ServiceCollection();
        var builder = services.AddRefitClient<IFooWithOtherAttribute>(
            new RefitSettings
            {
                HttpMessageHandlerFactory = () => recordingHandler,
                AuthorizationHeaderValueGetter = static (_, _) => Task.FromResult("token")
            });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(builder.Name);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "placeholder");

        using var response = await client.GetAsync(new Uri("https://example.test"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(recordingHandler.AuthorizationParameter).IsEqualTo("token");
    }

    /// <summary>Verifies keyed registrations compose primary and additional handlers from keyed settings.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task KeyedRegistrationComposesConfiguredHandlerAndAuthorizationGetter()
    {
        var recordingHandler = new RecordingHandler();
        var services = new ServiceCollection();
        var builder = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            "keyed-handler",
            new RefitSettings
            {
                HttpMessageHandlerFactory = () => recordingHandler,
                AuthorizationHeaderValueGetter = static (_, _) => Task.FromResult("keyed-token")
            });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(builder.Name);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "placeholder");

        using var response = await client.GetAsync(new Uri("https://example.test"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(recordingHandler.AuthorizationParameter).IsEqualTo("keyed-token");
    }

    /// <summary>Invokes the non-generic shared AddRefitClientCore method.</summary>
    /// <param name="services">The services argument.</param>
    /// <param name="refitInterfaceType">The interface type argument.</param>
    /// <param name="settings">The settings factory argument.</param>
    /// <param name="httpClientName">The client name argument.</param>
    private static void InvokeAddRefitClientCore(
        IServiceCollection services,
        Type refitInterfaceType,
        Func<IServiceProvider, RefitSettings?>? settings,
        string? httpClientName) =>
        services.AddRefitClient(refitInterfaceType, settings, httpClientName);

    /// <summary>Invokes the generic shared AddRefitClientCore method.</summary>
    /// <typeparam name="T">The interface type.</typeparam>
    /// <param name="services">The services argument.</param>
    /// <param name="settings">The settings factory argument.</param>
    /// <param name="httpClientName">The client name argument.</param>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter is intentionally specified explicitly by the test caller.")]
    private static void InvokeAddRefitClientCoreGeneric<T>(
        IServiceCollection services,
        Func<IServiceProvider, RefitSettings?>? settings,
        string? httpClientName)
        where T : class =>
        services.AddRefitClient<T>(settings, httpClientName);

    /// <summary>Invokes the non-generic shared AddKeyedRefitClientCore method.</summary>
    /// <param name="services">The services argument.</param>
    /// <param name="refitInterfaceType">The interface type argument.</param>
    /// <param name="serviceKey">The service key argument.</param>
    /// <param name="settings">The settings factory argument.</param>
    /// <param name="httpClientName">The client name argument.</param>
    private static void InvokeAddKeyedRefitClientCore(
        IServiceCollection services,
        Type refitInterfaceType,
        object? serviceKey,
        Func<IServiceProvider, RefitSettings?>? settings,
        string? httpClientName) =>
        services.AddKeyedRefitClient(refitInterfaceType, serviceKey, settings, httpClientName);

    /// <summary>Invokes the generic shared AddKeyedRefitClientCore method.</summary>
    /// <typeparam name="T">The interface type.</typeparam>
    /// <param name="services">The services argument.</param>
    /// <param name="serviceKey">The service key argument.</param>
    /// <param name="settings">The settings factory argument.</param>
    /// <param name="httpClientName">The client name argument.</param>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter is intentionally specified explicitly by the test caller.")]
    private static void InvokeAddKeyedRefitClientCoreGeneric<T>(
        IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, RefitSettings?>? settings,
        string? httpClientName)
        where T : class =>
        services.AddKeyedRefitClient<T>(serviceKey, settings, httpClientName);

    /// <summary>Marker type used as a generic argument to verify unique client naming.</summary>
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1436:Add members to a type or remove it",
        Justification = "Intentional empty marker fixture used as a generic argument for client naming tests.")]
    private sealed class User;

    /// <summary>Marker type used as a generic argument to verify unique client naming.</summary>
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1436:Add members to a type or remove it",
        Justification = "Intentional empty marker fixture used as a generic argument for client naming tests.")]
    private sealed class Role;

    /// <summary>Options carrying a content serializer used to verify settings injection.</summary>
    private sealed class ClientOptions
    {
        /// <summary>Gets or sets the content serializer injected into the Refit settings.</summary>
        public SystemTextJsonContentSerializer? Serializer { get; set; }
    }

    /// <summary>HTTP handler that records the final authorization header it receives.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        /// <summary>Gets the authorization parameter observed by the handler.</summary>
        public string? AuthorizationParameter { get; private set; }

        /// <summary>Gets the request URI observed by the handler.</summary>
        public Uri? RequestUri { get; private set; }

        /// <summary>Gets the powered-by header observed by the handler.</summary>
        public string? PoweredByHeader { get; private set; }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            RequestUri = request.RequestUri;
            PoweredByHeader = request.Headers.TryGetValues("X-Powered-By", out var values)
                ? values.FirstOrDefault()
                : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
