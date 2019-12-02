using System.Threading.Tasks;

using CollisionB;

using Refit; // InterfaceStubGenerator looks for this

namespace Refit.Tests
{
    public interface ITypeCollisionApiB
    {
        [Get("")]
        Task<SomeType> SomeBRequest();
    }

    public static class TypeCollisionApiB
    {
        public static ITypeCollisionApiB Create()
        {
            return RestService.For<ITypeCollisionApiB>("http://somewhere.com");
        }
    }
}
