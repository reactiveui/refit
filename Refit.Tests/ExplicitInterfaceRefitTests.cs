using System.Net.Http;
using System.Threading.Tasks;
using Refit;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests;

public class ExplicitInterfaceRefitTests
{
    public interface IFoo
    {
        int Bar();
    }

    // Internal interface with a default implementation of IFoo.Bar that calls an internal Refit method
    internal interface IInternalFoo : IFoo
    {
        int IFoo.Bar() => InternalBar() + 1;

        [Get("/bar")]
        internal int InternalBar();
    }

    // Derived interface that explicitly implements IFoo.Bar and marks it as a Refit method
    public interface IRemoteFoo2 : IFoo
    {
        [Get("/bar")]
        abstract int IFoo.Bar();
    }

    [Fact]
    public void DefaultInterfaceImplementation_calls_internal_refit_method()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/bar")
            .Respond("application/json", "41");

        var fixture = RestService.For<IInternalFoo>("http://foo", settings);

        var result = ((IFoo)fixture).Bar();
        Assert.Equal(42, result);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void Explicit_interface_member_with_refit_attribute_is_invoked()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/bar")
            .Respond("application/json", "41");

        var fixture = RestService.For<IRemoteFoo2>("http://foo", settings);

        var result = ((IFoo)fixture).Bar();
        Assert.Equal(41, result);

        mockHttp.VerifyNoOutstandingExpectation();
    }
}
