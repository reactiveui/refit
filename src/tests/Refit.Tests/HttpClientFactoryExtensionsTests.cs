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
public partial class HttpClientFactoryExtensionsTests
{
    /// <summary>The service key used for keyed Refit registrations under test.</summary>
    private const string ServiceKey = "service-key";

    /// <summary>The HTTP client name used for named Refit registrations under test.</summary>
    private const string ServiceClientName = "service-client";

    /// <summary>The service key used to distinguish keyed from non-keyed registrations.</summary>
    private const string KeyedKey = "keyed";

    /// <summary>The name used for the generic settings registration under test.</summary>
    private const string GenericSettingsName = "generic-settings";

    /// <summary>The name used for the generic factory registration under test.</summary>
    private const string GenericFactoryName = "generic-factory";

    /// <summary>The name used for the type settings registration under test.</summary>
    private const string TypeSettingsName = "type-settings";

    /// <summary>The name used for the type factory registration under test.</summary>
    private const string TypeFactoryName = "type-factory";

    /// <summary>The service key used for the keyed type registration without settings.</summary>
    private const string TypeNoneKey = "type-none";

    /// <summary>The service key used for keyed HTTP client builder registrations under test.</summary>
    private const string BuilderKey = "builder-key";

    /// <summary>The HTTP client name used when validating builder argument checks.</summary>
    private const string BuilderValidationClientName = "builder-validation";

    /// <summary>The name of a pre-registered HTTP client honored by the generated Refit client.</summary>
    private const string MyHttpClientName = "MyHttpClient";

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
        _ = serviceCollection.AddKeyedRefitClient<IFooWithOtherAttribute>(KeyedKey);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var nonKeyedService = serviceProvider.GetService<IFooWithOtherAttribute>();
        var keyedService = serviceProvider.GetKeyedService<IFooWithOtherAttribute>(KeyedKey);

        await Assert.That(nonKeyedService).IsNotNull();
        await Assert.That(keyedService).IsNotNull();
        await Assert.That(keyedService).IsNotSameReferenceAs(nonKeyedService);

        var nonKeyedSettings = serviceProvider.GetService<SettingsFor<IFooWithOtherAttribute>>();
        var keyedSettings = serviceProvider.GetKeyedService<SettingsFor<IFooWithOtherAttribute>>(KeyedKey);
        await Assert.That(keyedSettings).IsNotSameReferenceAs(nonKeyedSettings);

        var nonKeyedRequestBuilder = serviceProvider.GetService<IRequestBuilder<IFooWithOtherAttribute>>();
        var keyedRequestBuilder = serviceProvider.GetKeyedService<IRequestBuilder<IFooWithOtherAttribute>>(KeyedKey);
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
            ServiceKey,
            settings,
            ServiceClientName);

        await Assert.That(builder.Name).IsEqualTo(ServiceClientName);

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(ServiceKey)
                .Settings!
                .ContentSerializer).IsSameReferenceAs(contentSerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<IRequestBuilder<IFooWithOtherAttribute>>(ServiceKey))
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
            ServiceKey,
            serviceProvider => new()
            {
                ContentSerializer = serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer!
            },
            ServiceClientName);

        await Assert.That(builder.Name).IsEqualTo(ServiceClientName);

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(ServiceKey)
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

        _ = services.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), ServiceKey, settings);

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(ServiceKey)
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

        await AssertValidServicesRejectNullInterfaceAndSettings(services);
        await AssertValidServicesRejectNullKeyedInterface(services);

        services = null!;
        await AssertNullServicesRejectTypeAndGenericOverloads(services);
        await AssertNullServicesRejectKeyedOverloads(services);
    }

    /// <summary>Verifies service-collection overloads that accept client names pass those names through.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload.")]
    public async Task ServiceCollectionNamedOverloadsUseProvidedClientNames()
    {
        var services = new ServiceCollection();
        var settings = new RefitSettings();

        var genericSettings = services.AddRefitClient<IFooWithOtherAttribute>(settings, GenericSettingsName);
        var genericFactory = services.AddRefitClient<IFooWithOtherAttribute>(
            static _ => new(),
            GenericFactoryName);
        var typeSettings = services.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            settings,
            TypeSettingsName);
        var typeFactory = services.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            static _ => new(),
            TypeFactoryName);

        await Assert.That(genericSettings.Name).IsEqualTo(GenericSettingsName);
        await Assert.That(genericFactory.Name).IsEqualTo(GenericFactoryName);
        await Assert.That(typeSettings.Name).IsEqualTo(TypeSettingsName);
        await Assert.That(typeFactory.Name).IsEqualTo(TypeFactoryName);
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
            GenericSettingsName,
            new RefitSettings { ContentSerializer = genericSettingsSerializer });
        var genericNamedSettings = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            "generic-settings-named",
            new RefitSettings(),
            "generic-settings-client");
        _ = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            GenericFactoryName,
            static _ => new());
        var genericNamedFactory = services.AddKeyedRefitClient<IFooWithOtherAttribute>(
            "generic-factory-named",
            _ => new() { ContentSerializer = genericFactorySerializer },
            "generic-factory-client");
        _ = services.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), TypeNoneKey);
        var typeNamedSettings = services.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            "type-settings-named",
            new RefitSettings { ContentSerializer = typeNamedSerializer },
            "type-settings-client");
        _ = services.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            TypeFactoryName,
            _ => new() { ContentSerializer = typeFactorySerializer });

        await Assert.That(genericNoSettings.Name).IsNotNull();
        await Assert.That(genericNamedSettings.Name).IsEqualTo("generic-settings-client");
        await Assert.That(genericNamedFactory.Name).IsEqualTo("generic-factory-client");
        await Assert.That(typeNamedSettings.Name).IsEqualTo("type-settings-client");

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(GenericSettingsName)
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
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(TypeFactoryName)
                .Settings!
                .ContentSerializer).IsSameReferenceAs(typeFactorySerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<IRequestBuilder<IFooWithOtherAttribute>>(TypeNoneKey))
            .IsNotNull();
        await Assert.That(serviceProvider.GetRequiredKeyedService<IFooWithOtherAttribute>(TypeNoneKey)).IsNotNull();
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

        var returnedBuilder = builder.AddKeyedRefitClient<IFooWithOtherAttribute>(BuilderKey);

        await Assert.That(returnedBuilder.Name).IsEqualTo("builder-keyed");

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetKeyedService<IFooWithOtherAttribute>(BuilderKey)).IsNotNull();
        await Assert.That(
            serviceProvider.GetKeyedService<SettingsFor<IFooWithOtherAttribute>>(BuilderKey)).IsNotNull();
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

        _ = builder.AddKeyedRefitClient<IFooWithOtherAttribute>(BuilderKey, settings);

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(BuilderKey)
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
            BuilderKey,
            serviceProvider => new()
            {
                ContentSerializer = serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer!
            });

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(BuilderKey)
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
        IHttpClientBuilder builder = new ServiceCollection().AddHttpClient(BuilderValidationClientName);

        await AssertValidBuilderRejectsNullArguments(builder);

        builder = null!;
        await AssertNullBuilderRejectsAllOverloads(builder);
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
        var keyedTypeBuilder = builder.AddKeyedRefitClient(typeof(IFooWithOtherAttribute), TypeNoneKey);
        _ = builder.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            TypeSettingsName,
            new RefitSettings { ContentSerializer = keyedTypeSettingsSerializer });
        _ = builder.AddKeyedRefitClient<IFooWithOtherAttribute>(
            GenericFactoryName,
            _ => new() { ContentSerializer = keyedGenericFactorySerializer });

        await Assert.That(typeBuilder.Name).IsEqualTo("builder-matrix");
        await Assert.That(keyedTypeBuilder.Name).IsEqualTo("builder-matrix");

        var serviceProvider = services.BuildServiceProvider();
        await Assert.That(
            serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!
                .ContentSerializer).IsSameReferenceAs(typeSettingsSerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(TypeSettingsName)
                .Settings!
                .ContentSerializer).IsSameReferenceAs(keyedTypeSettingsSerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<SettingsFor<IFooWithOtherAttribute>>(GenericFactoryName)
                .Settings!
                .ContentSerializer).IsSameReferenceAs(keyedGenericFactorySerializer);
        await Assert.That(
            serviceProvider.GetRequiredKeyedService<IRequestBuilder<IFooWithOtherAttribute>>(TypeNoneKey))
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

        _ = services.AddHttpClient(MyHttpClientName, client =>
        {
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Add("X-Powered-By", Environment.OSVersion.VersionString);
        });
        var refitBuilder = services.AddRefitClient<IGitHubApi>(settingsAction: null, MyHttpClientName);

        var sp = services.BuildServiceProvider();
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(MyHttpClientName);

        var gitHubApi = sp.GetRequiredService<IGitHubApi>();

        await Assert.That(refitBuilder.Name).IsEqualTo(MyHttpClientName);
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

    /// <summary>Verifies the generated-only DI helper resolves a source-generated client and injects the supplied settings (#2170).</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientResolvesGeneratedImplementation()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var settings = new RefitSettings(new SystemTextJsonContentSerializer());
        var serviceCollection = new ServiceCollection();
        var builder = serviceCollection.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(settings);
        _ = builder.ConfigureHttpClient(c => c.BaseAddress = new("http://generated/"));

        await Assert.That(serviceCollection).Contains(
            z => z.ServiceType == typeof(SettingsFor<IGeneratedSettingsFactoryApi>));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredService<IGeneratedSettingsFactoryApi>();

        var generated = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        await Assert.That(generated!.Settings).IsSameReferenceAs(settings);
        await Assert.That(generated.Client.BaseAddress).IsEqualTo(new Uri("http://generated/"));
    }

    /// <summary>Verifies the settings-factory overload of the generated-only DI helper resolves settings from the provider (#2170).</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientUsesSettingsFactoryFromProvider()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var serializer = new SystemTextJsonContentSerializer();
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddSingleton(new ClientOptions { Serializer = serializer });
        _ = serviceCollection.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            provider => new RefitSettings(provider.GetRequiredService<ClientOptions>().Serializer!));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredService<IGeneratedSettingsFactoryApi>();

        var generated = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        await Assert.That(generated!.Settings.ContentSerializer).IsSameReferenceAs(serializer);
    }

    /// <summary>Verifies the parameterless generated-only DI helper resolves a client with default settings and the default primary handler (#2170).</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientWithoutSettingsResolvesWithDefaults()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredService<IGeneratedSettingsFactoryApi>();

        var generated = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();

        // No settings were supplied, so ForGenerated receives a fresh default RefitSettings instance.
        await Assert.That(generated!.Settings).IsNotNull();
    }

    /// <summary>Verifies the settings-and-name overload honors the custom client name and builds the handler from the supplied settings (#2170).</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientWithSettingsAndNameUsesCustomNameAndHandler()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var recordingHandler = new RecordingHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => recordingHandler };
        var serviceCollection = new ServiceCollection();
        var builder = serviceCollection.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            settings,
            "generated-named-client");

        await Assert.That(builder.Name).IsEqualTo("generated-named-client");

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredService<IGeneratedSettingsFactoryApi>();

        var generated = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        await Assert.That(generated!.Settings).IsSameReferenceAs(settings);
    }

    /// <summary>Verifies the settings-factory-and-name overload honors the custom client name (#2170).</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitGeneratedClientWithSettingsFactoryAndNameUsesCustomName()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var serviceCollection = new ServiceCollection();
        var builder = serviceCollection.AddRefitGeneratedClient<IGeneratedSettingsFactoryApi>(
            static _ => new RefitSettings(),
            "generated-factory-named-client");

        await Assert.That(builder.Name).IsEqualTo("generated-factory-named-client");

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var resolved = serviceProvider.GetRequiredService<IGeneratedSettingsFactoryApi>();

        _ = await Assert.That(resolved).IsTypeOf<GeneratedSettingsFactoryApiClient>();
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
