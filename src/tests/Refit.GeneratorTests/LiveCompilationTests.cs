// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;
using Refit.LiveCompilation;

namespace Refit.GeneratorTests;

/// <summary>Live compilation tests for generated Refit implementations.</summary>
public sealed class LiveCompilationTests
{
    /// <summary>The client base address shared by the live generated-client tests.</summary>
    private const string BaseAddress = "https://example.test/base/";

    /// <summary>Invokes request-building code generated as part of the test assembly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task GeneratedRequestBuildingCanBeInvoked() => AssertGeneratedRequestSentAsync();

    /// <summary>Verifies one generated argument supplies both its path placeholder and dynamic header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PathAndHeaderBindingUsesSameArgument()
    {
        const int Identifier = 42;
        using var handler = new CapturingHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var api = RestService.ForGenerated<ILiveGeneratedApi>(client, new());

        _ = await api.GetPathAndHeader(Identifier);

        await AssertCapturedPathAndHeader(handler, "/base/users/42", "X-Id", "42");
    }

    /// <summary>Verifies an alias lets one generated argument supply both its path placeholder and dynamic header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AliasedPathAndHeaderBindingUsesSameArgument()
    {
        const int Identifier = 42;
        using var handler = new CapturingHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var api = RestService.ForGenerated<ILiveGeneratedApi>(client, new());

        _ = await api.GetAliasedPathAndHeader(Identifier);

        await AssertCapturedPathAndHeader(handler, "/base/users/42", "X-User-Id", "42");
    }

    /// <summary>Instantiates the generated client, invokes it, and asserts the captured request.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    private static async Task AssertGeneratedRequestSentAsync()
    {
        const int HeaderId = 42;
        const int PropertyTenantId = 17;
        const int ParameterTenantId = 23;

        using var handler = new CapturingHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var settings = new RefitSettings();
        var api = RestService.ForGenerated<ILiveGeneratedApi>(client, settings);

        api.TenantId = PropertyTenantId;
        var response = await api.Get(
            HeaderId,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Dynamic"] = "dynamic" },
            ParameterTenantId,
            CancellationToken.None);

        await Assert.That(response).IsEqualTo("done");
        await Assert.That(handler.LastRequest).IsNotNull();
        var request = handler.LastRequest!;

        await Assert.That(request.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(request.RequestUri).IsEqualTo(new("https://example.test/base/users"));
        await Assert.That(request.Headers.GetValues("X-Static")).IsCollectionEqualTo(["static"]);
        await Assert.That(request.Headers.GetValues("X-Id")).IsCollectionEqualTo(["42"]);
        await Assert.That(request.Headers.GetValues("X-Dynamic")).IsCollectionEqualTo(["dynamic"]);

        var parameterTenantKey = new HttpRequestOptionsKey<int>("parameter-tenant");
        await Assert.That(request.Options.TryGetValue(parameterTenantKey, out var parameterTenant)).IsTrue();
        await Assert.That(parameterTenant).IsEqualTo(ParameterTenantId);

        var propertyTenantKey = new HttpRequestOptionsKey<int>("property-tenant");
        await Assert.That(request.Options.TryGetValue(propertyTenantKey, out var propertyTenant)).IsTrue();
        await Assert.That(propertyTenant).IsEqualTo(PropertyTenantId);
    }

    /// <summary>Asserts the captured request path and dynamic header value.</summary>
    /// <param name="handler">The handler that captured the request.</param>
    /// <param name="expectedPath">The expected absolute-path component.</param>
    /// <param name="headerName">The dynamic header name.</param>
    /// <param name="headerValue">The expected dynamic header value.</param>
    /// <returns>A task representing the asynchronous assertions.</returns>
    private static async Task AssertCapturedPathAndHeader(
        CapturingHandler handler,
        string expectedPath,
        string headerName,
        string headerValue)
    {
        await Assert.That(handler.LastRequest).IsNotNull();
        var request = handler.LastRequest!;
        await Assert.That(request.RequestUri!.AbsolutePath).IsEqualTo(expectedPath);
        await Assert.That(request.Headers.GetValues(headerName)).IsCollectionEqualTo([headerValue]);
    }

    /// <summary>Captures the outgoing request and returns a fixed JSON string response.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        /// <summary>Gets the last request sent through the handler.</summary>
        public HttpRequestMessage? LastRequest { get; private set; }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("done", Encoding.UTF8, "text/plain"), });
        }
    }
}
