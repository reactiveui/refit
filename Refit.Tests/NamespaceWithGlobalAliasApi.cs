using global::System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this

namespace Refit.Tests
{
    using global::Refit.Tests.SomeNamespace;

    public interface NamespaceWithGlobalAliasApi
    {
        [Get("/")]
        Task<SomeType> SomeRequest();
    }
}

namespace Refit.Tests.SomeNamespace
{
    public class SomeType { }
}
