// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// Tests for interface methods that return the fully built <see cref="HttpRequestMessage"/> without sending it,
/// covering both the source-generated and reflection request paths and asserting they stay in parity.
/// </summary>
public class HttpRequestMessageReturnTests
{
    /// <summary>The base address used by the request-building HTTP clients.</summary>
    private const string BaseAddress = "http://api/";

    /// <summary>A sample user id substituted into the request path.</summary>
    private const int SampleUserId = 42;

    /// <summary>A second sample user id used by the parity test.</summary>
    private const int SecondUserId = 7;

    /// <summary>The dynamic trace header name.</summary>
    private const string TraceHeaderName = "X-Trace";

    /// <summary>The dynamic trace header value.</summary>
    private const string TraceHeaderValue = "trace-42";

    /// <summary>The static user-agent declared on the POST method.</summary>
    private const string StaticUserAgent = "RefitRequestBuilder";

    /// <summary>The body payload name.</summary>
    private const string PayloadName = "refit";

    /// <summary>The serialized JSON form of the body payload.</summary>
    private const string SerializedPayload = "{\"name\":\"refit\"}";

    /// <summary>The reason asserted when confirming the request was not dispatched.</summary>
    private const string NoDispatchReason = "building the request must not dispatch it";

    /// <summary>The generated GET request carries the method, relative URI, and query without being sent.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedPathBuildsGetRequestWithoutSending()
    {
        var handler = new DispatchGuardHandler();
        var api = RestService.For<IRequestMessageApi>(CreateClient(handler));

        using var request = await api.GetUserRequest(SampleUserId, "active");

        await Assert.That(request.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(request.RequestUri!.ToString()).IsEqualTo("/users/42?filter=active");
        await Assert.That(request.Content).IsNull();
        await Assert.That(handler.WasInvoked).IsFalse().Because(NoDispatchReason);
    }

    /// <summary>The reflection GET request matches the same method, relative URI, and query without being sent.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionPathBuildsGetRequestWithoutSending()
    {
        var handler = new DispatchGuardHandler();

        using var request = await BuildViaReflectionAsync("GetUserRequest", handler, SampleUserId, "active");

        await Assert.That(request.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(request.RequestUri!.ToString()).IsEqualTo("/users/42?filter=active");
        await Assert.That(handler.WasInvoked).IsFalse().Because(NoDispatchReason);
    }

    /// <summary>The generated and reflection paths build byte-identical GET requests.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedAndReflectionBuildIdenticalGetRequests()
    {
        var generatedHandler = new DispatchGuardHandler();
        var reflectionHandler = new DispatchGuardHandler();
        var api = RestService.For<IRequestMessageApi>(CreateClient(generatedHandler));

        using var generated = await api.GetUserRequest(SecondUserId, "b c");
        using var reflection = await BuildViaReflectionAsync("GetUserRequest", reflectionHandler, SecondUserId, "b c");

        await Assert.That(generated.Method).IsEqualTo(reflection.Method);
        await Assert.That(generated.RequestUri!.ToString()).IsEqualTo(reflection.RequestUri!.ToString());
    }

    /// <summary>The generated POST request carries the body and headers, and its content stays readable (not disposed).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedPathBuildsPostRequestWithReadableBody()
    {
        var handler = new DispatchGuardHandler();
        var api = RestService.For<IRequestMessageApi>(CreateClient(handler));
        var payload = new BodyPayload { Name = PayloadName };

        using var request = await api.CreateUserRequest(payload, TraceHeaderValue);

        await Assert.That(request.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(request.RequestUri!.ToString()).IsEqualTo("/users");
        await Assert.That(request.Headers.Contains(TraceHeaderName)).IsTrue();
        await Assert.That(request.Headers.GetValues(TraceHeaderName).Single()).IsEqualTo(TraceHeaderValue);
        await Assert.That(request.Headers.UserAgent.ToString()).IsEqualTo(StaticUserAgent);
        await Assert.That(handler.WasInvoked).IsFalse().Because(NoDispatchReason);

        // The caller owns the request; its content must not have been disposed and stays readable.
        await Assert.That(request.Content).IsNotNull();
        var body = await request.Content!.ReadAsStringAsync();
        await Assert.That(body).IsEqualTo(SerializedPayload);

        // A second read confirms the content is still live after the first read.
        var secondRead = await request.Content.ReadAsStringAsync();
        await Assert.That(secondRead).IsEqualTo(body);
    }

    /// <summary>The generated and reflection paths build POST requests with identical URI, headers, and body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedAndReflectionBuildIdenticalPostRequests()
    {
        var generatedHandler = new DispatchGuardHandler();
        var reflectionHandler = new DispatchGuardHandler();
        var api = RestService.For<IRequestMessageApi>(CreateClient(generatedHandler));

        using var generated = await api.CreateUserRequest(new BodyPayload { Name = PayloadName }, TraceHeaderValue);
        using var reflection = await BuildViaReflectionAsync(
            "CreateUserRequest",
            reflectionHandler,
            new BodyPayload { Name = PayloadName },
            TraceHeaderValue);

        await Assert.That(generated.Method).IsEqualTo(reflection.Method);
        await Assert.That(generated.RequestUri!.ToString()).IsEqualTo(reflection.RequestUri!.ToString());
        await Assert.That(generated.Headers.GetValues(TraceHeaderName).Single())
            .IsEqualTo(reflection.Headers.GetValues(TraceHeaderName).Single());
        await Assert.That(generated.Headers.UserAgent.ToString())
            .IsEqualTo(reflection.Headers.UserAgent.ToString());

        var generatedBody = await generated.Content!.ReadAsStringAsync();
        var reflectionBody = await reflection.Content!.ReadAsStringAsync();
        await Assert.That(generatedBody).IsEqualTo(reflectionBody);
    }

    /// <summary>A cancellation token parameter is allowed on a build-only method and does not trigger a send.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CancellationTokenParameterIsAllowedWhenBuildingOnly()
    {
        var generatedHandler = new DispatchGuardHandler();
        var reflectionHandler = new DispatchGuardHandler();
        var api = RestService.For<IRequestMessageApi>(CreateClient(generatedHandler));

        using var generated = await api.PingRequest(CancellationToken.None);
        using var reflection = await BuildViaReflectionAsync("PingRequest", reflectionHandler, CancellationToken.None);

        await Assert.That(generated.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(generated.RequestUri!.ToString()).IsEqualTo("/ping");
        await Assert.That(generated.RequestUri!.ToString()).IsEqualTo(reflection.RequestUri!.ToString());
        await Assert.That(generatedHandler.WasInvoked).IsFalse();
        await Assert.That(reflectionHandler.WasInvoked).IsFalse();
    }

    /// <summary>Creates an HTTP client whose handler fails the test if a request is ever dispatched.</summary>
    /// <param name="handler">The dispatch-guard handler.</param>
    /// <returns>The configured HTTP client.</returns>
    private static HttpClient CreateClient(DispatchGuardHandler handler) =>
        new(handler) { BaseAddress = new(BaseAddress) };

    /// <summary>Builds a request through the reflection request builder without sending it.</summary>
    /// <param name="methodName">The interface method to build.</param>
    /// <param name="handler">The dispatch-guard handler used by the client.</param>
    /// <param name="args">The method argument values.</param>
    /// <returns>The built request message.</returns>
    private static Task<HttpRequestMessage> BuildViaReflectionAsync(
        string methodName,
        DispatchGuardHandler handler,
        params object[] args)
    {
        var builder = new RequestBuilderImplementation<IRequestMessageApi>();
        var func = builder.BuildRestResultFuncForMethod(methodName);
        return (Task<HttpRequestMessage>)func(CreateClient(handler), args)!;
    }

    /// <summary>An HTTP handler that fails if it is ever asked to send a request.</summary>
    private sealed class DispatchGuardHandler : HttpMessageHandler
    {
        /// <summary>Gets a value indicating whether the handler was asked to send a request.</summary>
        public bool WasInvoked { get; private set; }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            WasInvoked = true;
            throw new InvalidOperationException(
                "This handler must never be invoked when only building a call.");
        }
    }
}
