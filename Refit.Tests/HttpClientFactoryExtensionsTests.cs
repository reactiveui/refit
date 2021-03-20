
using Microsoft.Extensions.Options;

namespace Refit.Tests
{
    using Microsoft.Extensions.DependencyInjection;

    using System.Text.Json;
    using Xunit;

    public class HttpClientFactoryExtensionsTests
    {
        class User
        {
        }

        class Role
        {
        }

        [Fact]
        public void GenericHttpClientsAreAssignedUniqueNames()
        {
            var services = new ServiceCollection();

            var userClientName = services.AddRefitClient<IBoringCrudApi<User, string>>().Name;
            var roleClientName = services.AddRefitClient<IBoringCrudApi<Role, string>>().Name;

            Assert.NotEqual(userClientName, roleClientName);
        }

        [Fact]
        public void HttpClientServicesAreAddedCorrectlyGivenGenericArgument()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddRefitClient<IFooWithOtherAttribute>();
            Assert.Contains(serviceCollection, z => z.ServiceType == typeof(SettingsFor<IFooWithOtherAttribute>));
            Assert.Contains(serviceCollection, z => z.ServiceType == typeof(IRequestBuilder<IFooWithOtherAttribute>));
        }

        [Fact]
        public void HttpClientServicesAreAddedCorrectlyGivenTypeArgument()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddRefitClient(typeof(IFooWithOtherAttribute));
            Assert.Contains(serviceCollection, z => z.ServiceType == typeof(SettingsFor<IFooWithOtherAttribute>));
            Assert.Contains(serviceCollection, z => z.ServiceType == typeof(IRequestBuilder<IFooWithOtherAttribute>));
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
            var serviceCollection = new ServiceCollection()
                .Configure<ClientOptions>(o => o.Serializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions()));
            serviceCollection.AddRefitClient<IFooWithOtherAttribute>(_ => new RefitSettings() {ContentSerializer = _.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer});
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.Same(
                serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer,
                serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings!.ContentSerializer
            );
        }

        [Fact]
        public void HttpClientSettingsAreInjectableGivenTypeArgument()
        {
            var serviceCollection = new ServiceCollection()
                .Configure<ClientOptions>(o => o.Serializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions()));
            serviceCollection.AddRefitClient(typeof(IFooWithOtherAttribute), _ => new RefitSettings() {ContentSerializer = _.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer});
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.Same(
                serviceProvider.GetRequiredService<IOptions<ClientOptions>>().Value.Serializer,
                serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings!.ContentSerializer
            );
        }

        [Fact]
        public void HttpClientSettingsCanBeProvidedStaticallyGivenGenericArgument()
        {
            var contentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions());
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddRefitClient<IFooWithOtherAttribute>(new RefitSettings() {ContentSerializer = contentSerializer });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.Same(
                contentSerializer,
                serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings!.ContentSerializer
            );
        }

        [Fact]
        public void HttpClientSettingsCanBeProvidedStaticallyGivenTypeArgument()
        {
            var contentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions());
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddRefitClient<IFooWithOtherAttribute>(new RefitSettings() {ContentSerializer = contentSerializer });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.Same(
                contentSerializer,
                serviceProvider.GetRequiredService<SettingsFor<IFooWithOtherAttribute>>().Settings!.ContentSerializer
            );
        }

        class ClientOptions
        {
            public SystemTextJsonContentSerializer Serializer { get; set; }
        }
    }
}
