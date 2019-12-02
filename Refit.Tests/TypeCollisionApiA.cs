using System.Threading.Tasks;

using CollisionA;

using Refit; // InterfaceStubGenerator looks for this

namespace Refit.Tests
{
    public interface ITypeCollisionApiA
    {
        [Get("")]
        Task<SomeType> SomeARequest();
    }

    public static class TypeCollisionApiA
    {
        public static ITypeCollisionApiA Create()
        {
            return RestService.For<ITypeCollisionApiA>("http://somewhere.com");
        }
    }
}
