using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this
using static System.Math; // This is here to verify https://github.com/paulcbetts/refit/issues/283

namespace Refit.Tests
{
    [Headers("User-Agent: Refit Integration Tests")]
    public interface IAmInterfaceA
    {
        [Get("/get?result=Ping")]
        Task<string> Ping();
    }

    [Headers("User-Agent: Refit Integration Tests")]
    public interface IAmInterfaceB
    {
        [Get("/get?result=Pong")]
        Task<string> Pong();
    }

    [Headers("User-Agent: Refit Integration Tests")]
    public interface IAmInterfaceC : IAmInterfaceB, IAmInterfaceA
    {
        [Get("/get?result=Pang")]
        Task<string> Pang();
    }
}
