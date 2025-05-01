using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Refit.Implementation;

using Xunit;

namespace Refit.Tests;

public class HttpClientFactoryExtensionsTests
{
    class User { }

    class Role { }

    [Fact]
    public void GenericHttpClientsAreAssignedUniqueNames()
    {
        var services = new ServiceCollection();

        var userClientName = services.AddRefitClient<IBoringCrudApi<User, string>>().Name;
        var roleClientName = services.AddRefitClient<IBoringCrudApi<Role, string>>().Name;

        Assert.NotEqual(userClientName, roleClientName);
    }

    [Fact]
    public void HttpClientServicesAreDifferentThanKeyedServices()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
        serviceCollection.AddKeyedRefitClient<IFooWithOtherAttribute>("keyed");

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var nonKeyedService = serviceProvider.GetService<IFooWithOtherAttribute>();
        var keyedService = serviceProvider.GetKeyedService<IFooWithOtherAttribute>("keyed");

        Assert.NotNull(nonKeyedService);
        Assert.NotNull(keyedService);
        Assert.NotSame(nonKeyedService, keyedService);

        var nonKeyedSettings = serviceProvider.GetService<SettingsFor<IFooWithOtherAttribute>>();
        var keyedSettings = serviceProvider.GetKeyedService<SettingsFor<IFooWithOtherAttribute>>("keyed");
        Assert.NotSame(nonKeyedSettings, keyedSettings);

        var nonKeyedRequestBuilder = serviceProvider.GetService<IRequestBuilder<IFooWithOtherAttribute>>();
        var keyedRequestBuilder = serviceProvider.GetKeyedService<IRequestBuilder<IFooWithOtherAttribute>>("keyed");
        Assert.NotSame(nonKeyedRequestBuilder, keyedRequestBuilder);
    }

    [Fact]
    public void HttpClientServicesAreAddedCorrectlyGivenGenericArgument()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
        Assert.Contains(
            serviceCollection,
            z => z.ServiceType == typeof(SettingsFor<IFooWithOtherAttribute>)
        );
        Assert.Contains(
            serviceCollection,
            z => z.ServiceType == typeof(IRequestBuilder<IFooWithOtherAttribute>)
        );
    }

    [Fact]
    public void HttpClientServicesAreAddedCorrectlyGivenTypeArgument()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddRefitClient(typeof(IFooWithOtherAttribute));
        Assert.Contains(
            serviceCollection,
            z => z.ServiceType == typeof(SettingsFor<IFooWithOtherAttribute>)
        );
        Assert.Contains(
            serviceCollection,
            z => z.ServiceType == typeof(IRequestBuilder<IFooWithOtherAttribute>)
        );
    }

    [Fact]
    public void HttpClientReturnsClientGivenGenericArgument()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IFooWithOtherAttribute>());
    }

    [Fact]
    public void HttpClientReturnsClientGivenTypeArgument()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddRefitClient(typeof(IFooWithOtherAttribute));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IFooWithOtherAttribute>());
    }

    [Fact]
    public void HttpClientSettingsAreInjectableGivenGenericArgument()
    {
        var serviceCollection = new ServiceCollection().Configure<ClientOptions>(
            o => o.Serializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions())
        );
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>(
            _ =>
                new RefitSettings()
                {
                    ContentSerializer = _.GetRequiredService<
                        IOptions<ClientOptions>
                    >().Value.Serializer
                }
        );
        var serviceProvider = serviceCollection.BuildServiceProvider();
        Assert.Same(
            serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer,
            serviceProvider
                .GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!.ContentSerializer
        );
    }

    [Fact]
    public void HttpClientSettingsAreInjectableGivenTypeArgument()
    {
        var serviceCollection = new ServiceCollection().Configure<ClientOptions>(
            o => o.Serializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions())
        );
        serviceCollection.AddRefitClient(
            typeof(IFooWithOtherAttribute),
            _ =>
                new RefitSettings()
                {
                    ContentSerializer = _.GetRequiredService<
                        IOptions<ClientOptions>
                    >().Value.Serializer
                }
        );
        var serviceProvider = serviceCollection.BuildServiceProvider();
        Assert.Same(
            serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer,
            serviceProvider
                .GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!.ContentSerializer
        );
    }

    [Fact]
    public void HttpClientSettingsCanBeProvidedStaticallyGivenGenericArgument()
    {
        var contentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions());
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>(
            new RefitSettings() { ContentSerializer = contentSerializer }
        );
        var serviceProvider = serviceCollection.BuildServiceProvider();
        Assert.Same(
            contentSerializer,
            serviceProvider
                .GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!.ContentSerializer
        );
    }

    [Fact]
    public void HttpClientSettingsCanBeProvidedStaticallyGivenTypeArgument()
    {
        var contentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions());
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddRefitClient<IFooWithOtherAttribute>(
            new RefitSettings() { ContentSerializer = contentSerializer }
        );
        var serviceProvider = serviceCollection.BuildServiceProvider();
        Assert.Same(
            contentSerializer,
            serviceProvider
                .GetRequiredService<SettingsFor<IFooWithOtherAttribute>>()
                .Settings!.ContentSerializer
        );
    }

    [Fact]
    public void ProvidedHttpClientIsUsedAsNamedClient()
    {
        var baseUri = new Uri("https://0:1337");
        var services = new ServiceCollection();

        services.AddHttpClient("MyHttpClient", client => {
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Add("X-Powered-By", Environment.OSVersion.VersionString);
        });
        services.AddRefitClient<IGitHubApi>(null, "MyHttpClient");

        var sp = services.BuildServiceProvider();
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("MyHttpClient");

        var gitHubApi = sp.GetRequiredService<IGitHubApi>();

        var memberInfos = typeof(Generated).GetMember("RefitTestsIGitHubApi", BindingFlags.NonPublic);
        var genApi = Convert.ChangeType(gitHubApi, (Type)memberInfos[0], CultureInfo.InvariantCulture);
        var genApiProperty = genApi.GetType().GetProperty("Client")!;
        var genApiClient = (HttpClient)genApiProperty.GetValue(genApi)!;

        Assert.NotSame(httpClient, genApiClient);
        Assert.Equal(httpClient.BaseAddress, genApiClient.BaseAddress);
        Assert.Equal(baseUri, genApiClient.BaseAddress);
        Assert.Contains(
            new KeyValuePair<string, IEnumerable<string>>("X-Powered-By",
                new[] { Environment.OSVersion.VersionString }), genApiClient.DefaultRequestHeaders);
    }

    class ClientOptions
    {
        public SystemTextJsonContentSerializer Serializer { get; set; }
    }
}
