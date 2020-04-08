using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this
using Refit.Tests.SeparateNamespaceWithModel;

using static System.Math; // This is here to verify https://github.com/reactiveui/refit/issues/283

namespace Refit.Tests
{
    [Headers("User-Agent: Refit Integration Tests")]
    public interface IAmInterface : IAmInterfaceB, IAmInterfaceA
    {
        [Get("/get?result=Pang")]
        Task<string> Pang();
    }

    [Headers("User-Agent: Refit Integration Tests")]
    public interface IAmInterfaceA
    {
        [Get("/get?result=Ping")]
        Task<string> Ping();
    }

    [Headers("User-Agent: Refit Integration Tests")]
    public interface IAmInterfaceB : IAmInterfaceD
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

    public interface IAmInterfaceD
    {
        [Get("/get?result=Test")]
        Task<string> Test();
    }

    public interface IAmInterfaceF_RequireUsing
    {
        [Get("/get-requiring-using")]
        Task<ResponseModel> Get(List<Guid> guids);
    }
}

namespace Refit.Tests.SeparateNamespaceWithModel
{
    public class ResponseModel { }
}
