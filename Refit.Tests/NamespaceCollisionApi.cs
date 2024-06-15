using System.Threading.Tasks;
using CollisionB;
using Refit; // InterfaceStubGenerator looks for this
using SomeType = CollisionA.SomeType;

namespace Refit.Tests
{
    public interface INamespaceCollisionApi
    {
        [Get("/")]
        Task<SomeType> SomeRequest();
    }

    public static class NamespaceCollisionApi
    {
        public static INamespaceCollisionApi Create()
        {
            return RestService.For<INamespaceCollisionApi>("http://somewhere.com");
        }
    }
}

namespace CollisionA
{
    public class SomeType { }

    public interface INamespaceCollisionApi
    {
        [Get("/")]
        Task<SomeType> SomeRequest();
    }
}

namespace CollisionB
{
    public class SomeType { }

    public interface INamespaceCollisionApi
    {
        [Get("/")]
        Task<SomeType> SomeRequest();
    }
}
