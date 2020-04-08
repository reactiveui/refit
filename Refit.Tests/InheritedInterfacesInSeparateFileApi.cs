using System.Threading.Tasks;

using Refit; // InterfaceStubGenerator looks for this

namespace Refit.Tests.SeparateNamespace
{
    public interface InheritedInterfacesInSeparateFileApi: IAmInterfaceF_RequireUsing
    {
        [Get("/get")]
        Task Get(int i);
    }
}
