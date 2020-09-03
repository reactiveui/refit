using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this

namespace Refit.Tests
{
    public partial interface PartialInterfacesApi
    {
        [Get("/get?result=Second")]
        Task<string> Second();
    }
}
