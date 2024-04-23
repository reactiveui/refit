using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this
using Refit.Tests.SeparateNamespaceWithModel;

using static System.Math; // This is here to verify https://github.com/reactiveui/refit/issues/283

namespace Refit.Tests
{
    [Headers("User-Agent: Refit Integration Tests")]
    [PathPrefix("/ping")]
    public interface IPathPrefix
    {
        [Get("/get?result=Ping")]
        Task<string> Ping();
    }

    [Headers("User-Agent: Refit Integration Tests")]
    public interface IInheritingPathPrefix : IPathPrefix
    {
        [Get("/get?result=Pang")]
        Task<string> Pang();
    }

}
