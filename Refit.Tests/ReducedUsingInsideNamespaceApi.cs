using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this

namespace Refit.Tests
{
    using ModelNamespace;

    public interface IReducedUsingInsideNamespaceApi
    {
        [Get("/")]
        Task<SomeType> SomeRequest();
    }
}

namespace Refit.Tests.ModelNamespace
{
    public class SomeType { }
}
