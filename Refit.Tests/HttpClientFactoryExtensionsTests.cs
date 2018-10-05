namespace Refit.Tests
{
    using Microsoft.Extensions.DependencyInjection;
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
    }
}
