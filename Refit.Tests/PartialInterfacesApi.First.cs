using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this

namespace Refit.Tests
{
    public partial interface PartialInterfacesApi
    {
        [Get("/get?result=First")]
        Task<string> First();
    }
}
