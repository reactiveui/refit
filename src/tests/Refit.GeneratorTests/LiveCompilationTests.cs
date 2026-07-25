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
    /// <summary>Invokes request-building code generated as part of the test assembly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task GeneratedRequestBuildingCanBeInvoked() => AssertGeneratedRequestSentAsync();

    /// <summary>Instantiates the generated client, invokes it, and asserts the captured request.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    private static async Task AssertGeneratedRequestSentAsync()
    {
        const int HeaderId = 42;
        const int PropertyTenantId = 17;
        const int ParameterTenantId = 23;

        using var handler = new CapturingHandler();
        using var client = HttpClientTestFactory.Create(handler, new("https://example.test/base/"));
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
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("done", Encoding.UTF8, "text/plain")
                });
        }
    }
}
