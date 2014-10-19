using System.Threading.Tasks;
using SomeType = CollisionA.SomeType;
using CollisionB;

namespace Refit.Tests
{
    public interface INamespaceCollisionApi
    {
        [Get("/")]
        Task<SomeType> SomeRequest();
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
