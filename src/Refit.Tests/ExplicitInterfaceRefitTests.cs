using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

public class ExplicitInterfaceRefitTests
{
    sealed class SyncCapableMockHttpMessageHandler : MockHttpMessageHandler
    {
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

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

    // Interfaces used to test the full sync pipeline
    public interface ISyncPipelineApi
    {
        [Get("/resource")]
        internal string GetString();

        [Get("/resource")]
        internal HttpResponseMessage GetHttpResponseMessage();

        [Get("/resource")]
        internal HttpContent GetHttpContent();

        [Get("/resource")]
        internal Stream GetStream();

        [Get("/resource")]
        internal IApiResponse<string> GetApiResponse();

        [Get("/resource")]
        internal IApiResponse GetRawApiResponse();

        [Get("/resource")]
        internal void DoVoid();
    }

    [Test]
    public void DefaultInterfaceImplementation_calls_internal_refit_method()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/bar")
            .Respond("application/json", "41");

        var fixture = RestService.For<IInternalFoo>("http://foo", settings);

        var result = ((IFoo)fixture).Bar();
        Assert.Equal(42, result);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Explicit_interface_member_with_refit_attribute_is_invoked()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/bar")
            .Respond("application/json", "41");

        var fixture = RestService.For<IRemoteFoo2>("http://foo", settings);

        var result = ((IFoo)fixture).Bar();
        Assert.Equal(41, result);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Sync_method_throws_ApiException_on_error_response()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond(HttpStatusCode.NotFound);

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        var ex = Assert.Throws<ApiException>(() => fixture.GetString());
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Sync_method_returns_HttpResponseMessage_without_running_ExceptionFactory()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond(HttpStatusCode.NotFound);

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        // Should not throw even for a 404 – caller owns the response
        using var resp = fixture.GetHttpResponseMessage();
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Sync_method_returns_HttpContent_without_disposing_response()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond("text/plain", "hello");

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        var content = fixture.GetHttpContent();
        Assert.NotNull(content);
        var text = content.ReadAsStringAsync().GetAwaiter().GetResult();
        Assert.Equal("hello", text);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Sync_method_returns_Stream_without_disposing_response()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond("text/plain", "hello");

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        using var stream = fixture.GetStream();
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        Assert.Equal("hello", reader.ReadToEnd());

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Sync_method_returns_IApiResponse_with_error_on_bad_status()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond(HttpStatusCode.InternalServerError);

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        using var apiResp = fixture.GetApiResponse();
        Assert.False(apiResp.IsSuccessStatusCode);
        Assert.NotNull(apiResp.Error);
        Assert.True(apiResp.HasResponseError(out var error));
        Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Sync_method_returns_IApiResponse_with_content_on_success()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond("application/json", "\"hello\"");

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        using var apiResp = fixture.GetApiResponse();
        Assert.True(apiResp.IsSuccessStatusCode);
        Assert.Null(apiResp.Error);
        Assert.Equal(HttpMethod.Get, apiResp.RequestMessage.Method);
        Assert.Equal("http://foo/resource", apiResp.RequestMessage.RequestUri?.ToString());
        // The string branch reads the raw stream (no JSON unwrapping), same as the async path
        Assert.Equal("\"hello\"", apiResp.Content);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Sync_method_returns_raw_IApiResponse_on_success()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond("text/plain", "hello");

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        using var apiResp = fixture.GetRawApiResponse();
        Assert.True(apiResp.IsSuccessStatusCode);
        Assert.Null(apiResp.Error);
        Assert.Equal(HttpMethod.Get, apiResp.RequestMessage.Method);
        Assert.Equal("http://foo/resource", apiResp.RequestMessage.RequestUri?.ToString());

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Sync_void_method_throws_ApiException_on_error_response()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond(HttpStatusCode.BadRequest);

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        var ex = Assert.Throws<ApiException>(() => fixture.DoVoid());
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public void Sync_void_method_succeeds_on_ok_response()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        fixture.DoVoid(); // should not throw

        mockHttp.VerifyNoOutstandingExpectation();
    }
}
