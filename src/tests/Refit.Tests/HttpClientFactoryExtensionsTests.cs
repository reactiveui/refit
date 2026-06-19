// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Refit.Implementation;

namespace Refit.Tests;

/// <summary>Tests for the Refit HttpClientFactory dependency-injection extension methods.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
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
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
        serviceCollection.AddKeyedRefitClient<IFooWithOtherAttribute>("keyed");

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
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
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
        serviceCollection.AddRefitClient(typeof(IFooWithOtherAttribute));
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
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
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
        serviceCollection.AddRefitClient(typeof(IFooWithOtherAttribute));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        await Assert.That(serviceProvider.GetService<IFooWithOtherAttribute>()).IsNotNull();
    }

    /// <summary>Verifies the generic overload injects settings resolved from the service provider.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientSettingsAreInjectableGivenGenericArgument()
    {
        var serviceCollection = new ServiceCollection().Configure<ClientOptions>(
            o => o.Serializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions()));
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>(
            _ =>
                new RefitSettings
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
            o => o.Serializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions()));
        serviceCollection.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            _ =>
                new RefitSettings
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
        var contentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions());
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>(
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
        var contentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions());
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            new RefitSettings { ContentSerializer = contentSerializer });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        await Assert.That(
            serviceProvider
                .GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!.ContentSerializer).IsSameReferenceAs(contentSerializer);
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

        services.AddHttpClient("MyHttpClient", client =>
        {
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Add("X-Powered-By", Environment.OSVersion.VersionString);
        });
        services.AddRefitClient<IGitHubApi>(settingsAction: null, "MyHttpClient");

        var sp = services.BuildServiceProvider();
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("MyHttpClient");

        var gitHubApi = sp.GetRequiredService<IGitHubApi>();

        var memberInfos = typeof(Generated).GetMember("RefitTestsIGitHubApi", BindingFlags.NonPublic);
        var genApi = Convert.ChangeType(gitHubApi, (Type)memberInfos[0], CultureInfo.InvariantCulture);
        var genApiProperty = genApi.GetType().GetProperty("Client")!;
        var genApiClient = (HttpClient)genApiProperty.GetValue(genApi)!;

        await Assert.That(genApiClient).IsNotSameReferenceAs(httpClient);
        await Assert.That(genApiClient.BaseAddress).IsEqualTo(httpClient.BaseAddress);
        await Assert.That(genApiClient.BaseAddress).IsEqualTo(baseUri);
        await Assert.That(genApiClient.DefaultRequestHeaders).Contains(
            h => h.Key == "X-Powered-By"
                && h.Value.Contains(Environment.OSVersion.VersionString));
    }

    /// <summary>Marker type used as a generic argument to verify unique client naming.</summary>
    [SuppressMessage("Design", "SST1436:Add members to type or remove it", Justification = "Intentional empty fixture used only as a generic type argument to exercise client naming for Refit tests.")]
    private sealed class User;

    /// <summary>Marker type used as a generic argument to verify unique client naming.</summary>
    [SuppressMessage("Design", "SST1436:Add members to type or remove it", Justification = "Intentional empty fixture used only as a generic type argument to exercise client naming for Refit tests.")]
    private sealed class Role;

    /// <summary>Options carrying a content serializer used to verify settings injection.</summary>
    private sealed class ClientOptions
    {
        /// <summary>Gets or sets the content serializer injected into the Refit settings.</summary>
        public SystemTextJsonContentSerializer? Serializer { get; set; }
    }
}
