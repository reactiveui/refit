// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;

namespace Refit.GeneratorTests;

/// <summary>Live compilation tests for generated Refit implementations.</summary>
public sealed class LiveCompilationTests
{
    /// <summary>Compiles, loads, and invokes generated request-building code.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("The test deliberately reflects over a live-generated assembly.")]
    [RequiresDynamicCode("The test deliberately emits and invokes a live-generated assembly.")]
    [SuppressMessage(
        "Major Code Smell",
        "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
        Justification = "The test owns the generated assembly and must instantiate Refit's internal generated implementation.")]
    public async Task GeneratedRequestBuilding_CanBeEmittedLoadedAndInvoked()
    {
        const int HeaderId = 42;
        const int PropertyTenantId = 17;
        const int ParameterTenantId = 23;

        var result = Fixture.RunGenerator(
            """
            using System;
            using System.Collections.Generic;
            using System.Net.Http;
            using System.Threading;
            using System.Threading.Tasks;
            using Refit;

            namespace Refit.LiveCompilation;

            public interface ILiveGeneratedApi
            {
                [Property("property-tenant")]
                int TenantId { get; set; }

                [Headers("X-Static: static")]
                [Get("/users")]
                Task<string> Get(
                    [Header("X-Id")] int id,
                    [HeaderCollection] IDictionary<string, string> headers,
                    [Property("parameter-tenant")] int tenantId,
                    CancellationToken cancellationToken);
            }
            """,
            generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();

        var (assembly, context) = Fixture.EmitAndLoad(result);
        using (context)
        {
            var interfaceType = assembly.GetType(
                "Refit.LiveCompilation.ILiveGeneratedApi",
                throwOnError: true)!;
            var generatedType = assembly
                .GetTypes()
                .Single(type => type.IsClass && interfaceType.IsAssignableFrom(type));

            using var handler = new CapturingHandler();
            using var client = new HttpClient(handler)
            {
                BaseAddress = new("https://example.test/base/")
            };
            var settings = new RefitSettings();
            var requestBuilder = RequestBuilder.ForType(interfaceType, settings);
            var api = Activator.CreateInstance(
                generatedType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: [client, requestBuilder],
                culture: null)!;

            interfaceType.GetProperty("TenantId")!.SetValue(api, PropertyTenantId);
            var task = (Task)interfaceType.GetMethod("Get")!.Invoke(
                api,
                [
                    HeaderId,
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Dynamic"] = "dynamic" },
                    ParameterTenantId,
                    CancellationToken.None
                ])!;

            await task.ConfigureAwait(false);
            var response = (string?)task.GetType().GetProperty("Result")!.GetValue(task);

            await Assert.That(response).IsEqualTo("done");
            await Assert.That(handler.LastRequest).IsNotNull();
            var request = handler.LastRequest!;

            await Assert.That(request.Method).IsEqualTo(HttpMethod.Get);
            await Assert.That(request.RequestUri).IsEqualTo(new Uri("https://example.test/base/users"));
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
