using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this
using static System.Math; // This is here to verify https://github.com/paulcbetts/refit/issues/283

namespace Refit.Tests
{
    [Headers("User-Agent: Refit Integration Tests")]
    public interface IAmInterfaceA
    {
        [Get("/ping")]
        Task<string> Ping();
    }

    [Headers("User-Agent: Refit Integration Tests")]
    public interface IAmInterfaceB
    {
        [Get("/pong")]
        Task<string> Pong();
    }

    [Headers("User-Agent: Refit Integration Tests")]
    public interface IAmInterfaceC : IAmInterfaceB, IAmInterfaceA
    {
        [Get("/pang")]
        Task<string> Pang();
    }
}
