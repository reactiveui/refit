// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Refit.Tests;

/// <summary>Tests for the AddRefitClient and AddKeyedRefitClient service-collection registration overloads.</summary>
public partial class HttpClientFactoryExtensionsTests
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
            static z => z.ServiceType == typeof(SettingsFor<IFooWithOtherAttribute>));
        await Assert.That(serviceCollection).Contains(
            static z => z.ServiceType == typeof(IRequestBuilder<IFooWithOtherAttribute>));
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
            static z => z.ServiceType == typeof(SettingsFor<IFooWithOtherAttribute>));
        await Assert.That(serviceCollection).Contains(
            static z => z.ServiceType == typeof(IRequestBuilder<IFooWithOtherAttribute>));
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
            static o => o.Serializer = new(EmptySerializerOptions));
        _ = serviceCollection.AddRefitClient<IFooWithOtherAttribute>(
            static _ =>
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
            static o => o.Serializer = new(EmptySerializerOptions));
        _ = serviceCollection.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            static _ =>
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
        var contentSerializer = new SystemTextJsonContentSerializer(EmptySerializerOptions);
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
        var contentSerializer = new SystemTextJsonContentSerializer(EmptySerializerOptions);
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
        var contentSerializer = new SystemTextJsonContentSerializer(EmptySerializerOptions);
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
            static o => o.Serializer = new(EmptySerializerOptions));

        var builder = services.AddKeyedRefitClient(
            typeof(IFooWithOtherAttribute),
            ServiceKey,
            static serviceProvider => new()
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
        var contentSerializer = new SystemTextJsonContentSerializer(EmptySerializerOptions);
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
        var genericSettingsSerializer = new SystemTextJsonContentSerializer(EmptySerializerOptions);
        var genericFactorySerializer = new SystemTextJsonContentSerializer(EmptySerializerOptions);
        var typeNamedSerializer = new SystemTextJsonContentSerializer(EmptySerializerOptions);
        var typeFactorySerializer = new SystemTextJsonContentSerializer(EmptySerializerOptions);
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

    /// <summary>Verifies the generic <c>(IServiceCollection, RefitSettings)</c> overload still exists for binary compatibility.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddRefitClient_ServiceCollectionGenericSettingsOverload_RemainsBinaryCompatible()
    {
        var method = typeof(HttpClientFactoryExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(static method =>
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
            .SingleOrDefault(static method =>
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
}
