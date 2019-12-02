using System.Threading.Tasks;

using CollisionB;

namespace Refit.Tests
{
    public interface ITypeCollisionApiB
    {
        [Get("/")]
        Task<SomeType> SomeARequest();
    }

    public static class TypeCollisionApiB
    {
        public static ITypeCollisionApiB Create()
        {
            return RestService.For<ITypeCollisionApiB>("http://somewhere.com");
        }
    }
}
