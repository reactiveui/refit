using System;
using System.Threading.Tasks;
using Common.Helper;
// InterfaceStubGenerator looks for this
using Refit;
using Refit.Tests.Common;

namespace Refit.Tests
{
    [SomeHelper]
    public interface INamespaceOverlapApi
    {
        [Get("/")]
        Task<SomeOtherType> SomeRequest();
    }

    public static class NamespaceOverlapApi
    {
        public static INamespaceOverlapApi Create()
        {
            return RestService.For<INamespaceOverlapApi>("http://somewhere.com");
        }
    }
}

namespace Common.Helper
{
    public class SomeHelperAttribute : Attribute { }
}

namespace Refit.Tests.Common
{
    public class SomeOtherType { }
}
