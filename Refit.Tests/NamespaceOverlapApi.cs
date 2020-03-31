using System;
using System.Threading.Tasks;
using Common.Helper;
using Refit.Tests.Common;
// InterfaceStubGenerator looks for this
using Refit;

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
