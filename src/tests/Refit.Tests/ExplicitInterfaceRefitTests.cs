// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Tests for explicit interface implementations and the synchronous Refit pipeline.</summary>
public class ExplicitInterfaceRefitTests
{
    /// <summary>A simple interface whose member is reused by explicit-implementation fixtures.</summary>
    public interface IFoo
    {
        /// <summary>Returns an integer value supplied by the implementing fixture.</summary>
        /// <returns>The integer value.</returns>
        int Bar();
    }

    /// <summary>Derived interface that explicitly implements <see cref="IFoo.Bar"/> and marks it as a Refit method.</summary>
    public interface IRemoteFoo2 : IFoo
    {
        /// <summary>Returns the value fetched from the remote <c>/bar</c> endpoint.</summary>
        /// <returns>The integer value returned by the remote endpoint.</returns>
        [Get("/bar")]
        abstract int IFoo.Bar();
    }

    /// <summary>Interface used to exercise the full synchronous Refit pipeline across return types.</summary>
    public interface ISyncPipelineApi
    {
        /// <summary>Gets the resource as a string synchronously.</summary>
        /// <returns>The resource content.</returns>
        [Get("/resource")]
        internal string GetString();

        /// <summary>Gets the resource as an <see cref="HttpResponseMessage"/> synchronously.</summary>
        /// <returns>The raw response message.</returns>
        [Get("/resource")]
        internal HttpResponseMessage GetHttpResponseMessage();

        /// <summary>Gets the resource as <see cref="HttpContent"/> synchronously.</summary>
        /// <returns>The response content.</returns>
        [Get("/resource")]
        internal HttpContent GetHttpContent();

        /// <summary>Gets the resource as a <see cref="Stream"/> synchronously.</summary>
        /// <returns>The response stream.</returns>
        [Get("/resource")]
        internal Stream GetStream();

        /// <summary>Gets the resource as an <see cref="IApiResponse{T}"/> synchronously.</summary>
        /// <returns>The typed API response.</returns>
        [Get("/resource")]
        internal IApiResponse<string> GetApiResponse();

        /// <summary>Gets the resource as a raw <see cref="IApiResponse"/> synchronously.</summary>
        /// <returns>The raw API response.</returns>
        [Get("/resource")]
        internal IApiResponse GetRawApiResponse();

        /// <summary>Invokes the resource endpoint synchronously without returning a value.</summary>
        [Get("/resource")]
        internal void DoVoid();
    }

    /// <summary>Internal interface with a default implementation of <see cref="IFoo.Bar"/> that calls an internal Refit method.</summary>
    internal interface IInternalFoo : IFoo
    {
        /// <inheritdoc/>
        int IFoo.Bar() => InternalBar() + 1;

        /// <summary>Gets the remote value used by the default <see cref="IFoo.Bar"/> implementation.</summary>
        /// <returns>The integer value returned by the remote endpoint.</returns>
        [Get("/bar")]
        internal int InternalBar();
    }

    /// <summary>Verifies a default interface implementation can invoke an internal Refit method.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task DefaultInterfaceImplementation_calls_internal_refit_method()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/bar")
            .Respond("application/json", "41");

        var fixture = RestService.For<IInternalFoo>("http://foo", settings);

        var result = ((IFoo)fixture).Bar();
        await Assert.That(result).IsEqualTo(42);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies an explicit interface member carrying a Refit attribute is invoked.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Explicit_interface_member_with_refit_attribute_is_invoked()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/bar")
            .Respond("application/json", "41");

        var fixture = RestService.For<IRemoteFoo2>("http://foo", settings);

        var result = ((IFoo)fixture).Bar();
        await Assert.That(result).IsEqualTo(41);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a synchronous string method throws an <see cref="ApiException"/> on an error response.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_throws_ApiException_on_error_response()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond(HttpStatusCode.NotFound);

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        var ex = await Assert.That(fixture.GetString).ThrowsExactly<ApiException>();
        await Assert.That(ex!.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a synchronous method returning <see cref="HttpResponseMessage"/> bypasses the exception factory.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_HttpResponseMessage_without_running_ExceptionFactory()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond(HttpStatusCode.NotFound);

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        // Should not throw even for a 404 – caller owns the response
        using var resp = fixture.GetHttpResponseMessage();
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a synchronous method returning <see cref="HttpContent"/> does not dispose the response.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_HttpContent_without_disposing_response()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond("text/plain", "hello");

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        var content = fixture.GetHttpContent();
        await Assert.That(content).IsNotNull();
        var text = await content.ReadAsStringAsync();
        await Assert.That(text).IsEqualTo("hello");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a synchronous method returning a <see cref="Stream"/> does not dispose the response.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_Stream_without_disposing_response()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond("text/plain", "hello");

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        await using var stream = fixture.GetStream();
        await Assert.That(stream).IsNotNull();
        using var reader = new StreamReader(stream);
        await Assert.That(await reader.ReadToEndAsync()).IsEqualTo("hello");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a synchronous method returning <see cref="IApiResponse{T}"/> reports an error on a bad status.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_IApiResponse_with_error_on_bad_status()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond(HttpStatusCode.InternalServerError);

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        using var apiResp = fixture.GetApiResponse();
        await Assert.That(apiResp.IsSuccessStatusCode).IsFalse();
        await Assert.That(apiResp.Error).IsNotNull();
        await Assert.That(apiResp.HasResponseError(out var error)).IsTrue();
        await Assert.That(error!.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a synchronous method returning <see cref="IApiResponse{T}"/> carries content on success.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_IApiResponse_with_content_on_success()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond("application/json", "\"hello\"");

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        using var apiResp = fixture.GetApiResponse();
        await Assert.That(apiResp.IsSuccessStatusCode).IsTrue();
        await Assert.That(apiResp.Error).IsNull();
        await Assert.That(apiResp.RequestMessage!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(apiResp.RequestMessage.RequestUri?.ToString()).IsEqualTo("http://foo/resource");

        // The string branch reads the raw stream (no JSON unwrapping), same as the async path
        await Assert.That(apiResp.Content).IsEqualTo("\"hello\"");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a synchronous method returning a raw <see cref="IApiResponse"/> succeeds on a good status.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_raw_IApiResponse_on_success()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond("text/plain", "hello");

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        using var apiResp = fixture.GetRawApiResponse();
        await Assert.That(apiResp.IsSuccessStatusCode).IsTrue();
        await Assert.That(apiResp.Error).IsNull();
        await Assert.That(apiResp.RequestMessage!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(apiResp.RequestMessage.RequestUri?.ToString()).IsEqualTo("http://foo/resource");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a synchronous void method throws an <see cref="ApiException"/> on an error response.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_void_method_throws_ApiException_on_error_response()
    {
        var mockHttp = new SyncCapableMockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "http://foo/resource")
            .Respond(HttpStatusCode.BadRequest);

        var fixture = RestService.For<ISyncPipelineApi>("http://foo", settings);

        var ex = await Assert.That(fixture.DoVoid).ThrowsExactly<ApiException>();
        await Assert.That(ex!.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a synchronous void method completes without throwing on a successful response.</summary>
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

    /// <summary>Mock HTTP handler that also supports the synchronous <see cref="HttpMessageHandler.Send"/> path.</summary>
    internal sealed class SyncCapableMockHttpMessageHandler : MockHttpMessageHandler
    {
        /// <inheritdoc/>
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }
}
