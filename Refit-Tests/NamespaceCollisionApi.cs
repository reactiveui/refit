using System.Threading.Tasks;
using SomeType = CollisionA.SomeType;
using CollisionB;
using Refit; // InterfaceStubGenerator looks for this

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
}

namespace CollisionB
{
    public class SomeType { }
}
