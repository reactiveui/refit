// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests for explicit interface implementations and the synchronous Refit pipeline.</summary>
public class ExplicitInterfaceRefitTests
{
    /// <summary>Value returned by the stubbed remote <c>/bar</c> endpoint.</summary>
    private const int RemoteBarValue = 41;

    /// <summary>Result of the default <see cref="IFoo.Bar"/> implementation, which adds one to the remote value.</summary>
    private const int DefaultImplementationBarResult = RemoteBarValue + 1;

    /// <summary>Base address for the stubbed API.</summary>
    private const string BaseUrl = "http://foo";

    /// <summary>Fully qualified URL of the resource endpoint.</summary>
    private const string ResourceUrl = $"{BaseUrl}/resource";

    /// <summary>Plain text content returned by the stubbed responses.</summary>
    private const string HelloContent = "hello";

    /// <summary>The plain text media type used by the stubbed responses.</summary>
    private const string PlainTextMediaType = "text/plain";

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
        var handler = new StubHttp
        {
            {
                Route.Get("http://foo/bar"),
                Reply.Json("41")
            },
        };
        var fixture = handler.CreateClient<IInternalFoo>(BaseUrl);

        var result = ((IFoo)fixture).Bar();
        await Assert.That(result).IsEqualTo(DefaultImplementationBarResult);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies an explicit interface member carrying a Refit attribute is invoked.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Explicit_interface_member_with_refit_attribute_is_invoked()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("http://foo/bar"),
                Reply.Json("41")
            },
        };
        var fixture = handler.CreateClient<IRemoteFoo2>(BaseUrl);

        var result = ((IFoo)fixture).Bar();
        await Assert.That(result).IsEqualTo(RemoteBarValue);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a synchronous string method throws an <see cref="ApiException"/> on an error response.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_throws_ApiException_on_error_response()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ResourceUrl),
                Reply.Status(HttpStatusCode.NotFound)
            },
        };
        var fixture = handler.CreateClient<ISyncPipelineApi>(BaseUrl);

        var ex = await Assert.That(fixture.GetString).ThrowsExactly<ApiException>();
        await Assert.That(ex!.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a synchronous method returning <see cref="HttpResponseMessage"/> bypasses the exception factory.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_HttpResponseMessage_without_running_ExceptionFactory()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ResourceUrl),
                Reply.Status(HttpStatusCode.NotFound)
            },
        };
        var fixture = handler.CreateClient<ISyncPipelineApi>(BaseUrl);

        // Should not throw even for a 404 – caller owns the response
        using var resp = fixture.GetHttpResponseMessage();
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a synchronous method returning <see cref="HttpContent"/> does not dispose the response.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_HttpContent_without_disposing_response()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ResourceUrl),
                Reply.Text(HelloContent, PlainTextMediaType)
            },
        };
        var fixture = handler.CreateClient<ISyncPipelineApi>(BaseUrl);

        var content = fixture.GetHttpContent();
        await Assert.That(content).IsNotNull();
        var text = await content.ReadAsStringAsync();
        await Assert.That(text).IsEqualTo(HelloContent);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a synchronous method returning a <see cref="Stream"/> does not dispose the response.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_Stream_without_disposing_response()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ResourceUrl),
                Reply.Text(HelloContent, PlainTextMediaType)
            },
        };
        var fixture = handler.CreateClient<ISyncPipelineApi>(BaseUrl);

        await using var stream = fixture.GetStream();
        await Assert.That(stream).IsNotNull();
        using var reader = new StreamReader(stream);
        await Assert.That(await reader.ReadToEndAsync()).IsEqualTo(HelloContent);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a synchronous method returning <see cref="IApiResponse{T}"/> reports an error on a bad status.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_IApiResponse_with_error_on_bad_status()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ResourceUrl),
                Reply.Status(HttpStatusCode.InternalServerError)
            },
        };
        var fixture = handler.CreateClient<ISyncPipelineApi>(BaseUrl);

        using var apiResp = fixture.GetApiResponse();
        await Assert.That(apiResp.IsSuccessStatusCode).IsFalse();
        await Assert.That(apiResp.Error).IsNotNull();
        await Assert.That(apiResp.HasResponseError(out var error)).IsTrue();
        await Assert.That(error!.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a synchronous method returning <see cref="IApiResponse{T}"/> carries content on success.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_IApiResponse_with_content_on_success()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ResourceUrl),
                Reply.Json("\"hello\"")
            },
        };
        var fixture = handler.CreateClient<ISyncPipelineApi>(BaseUrl);

        using var apiResp = fixture.GetApiResponse();
        await Assert.That(apiResp.IsSuccessStatusCode).IsTrue();
        await Assert.That(apiResp.Error).IsNull();
        await Assert.That(apiResp.RequestMessage!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(apiResp.RequestMessage.RequestUri?.ToString()).IsEqualTo(ResourceUrl);

        // The string branch reads the raw stream (no JSON unwrapping), same as the async path
        await Assert.That(apiResp.Content).IsEqualTo("\"hello\"");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a synchronous method returning a raw <see cref="IApiResponse"/> succeeds on a good status.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_method_returns_raw_IApiResponse_on_success()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ResourceUrl),
                Reply.Text(HelloContent, PlainTextMediaType)
            },
        };
        var fixture = handler.CreateClient<ISyncPipelineApi>(BaseUrl);

        using var apiResp = fixture.GetRawApiResponse();
        await Assert.That(apiResp.IsSuccessStatusCode).IsTrue();
        await Assert.That(apiResp.Error).IsNull();
        await Assert.That(apiResp.RequestMessage!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(apiResp.RequestMessage.RequestUri?.ToString()).IsEqualTo(ResourceUrl);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a synchronous void method throws an <see cref="ApiException"/> on an error response.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task Sync_void_method_throws_ApiException_on_error_response()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ResourceUrl),
                Reply.Status(HttpStatusCode.BadRequest)
            },
        };
        var fixture = handler.CreateClient<ISyncPipelineApi>(BaseUrl);

        var ex = await Assert.That(fixture.DoVoid).ThrowsExactly<ApiException>();
        await Assert.That(ex!.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a synchronous void method completes without throwing on a successful response.</summary>
    [Test]
    public void Sync_void_method_succeeds_on_ok_response()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ResourceUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };
        var fixture = handler.CreateClient<ISyncPipelineApi>(BaseUrl);

        fixture.DoVoid(); // should not throw

        handler.VerifyAllCalled();
    }
}
