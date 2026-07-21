// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;

namespace Refit.GeneratorTests;

/// <summary>
/// Compiles, loads and invokes a generated client whose members use escaped (@-prefixed) keyword identifiers, asserting
/// each escaped value binds into the request under its unescaped name - proving the generated code is not merely valid
/// C# but semantically correct.
/// </summary>
public sealed class EscapedIdentifierBindingTests
{
    /// <summary>Verifies a keyword path parameter's value is substituted into the request path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    public async Task KeywordPathParameterFlowsIntoRequestPath()
    {
        using var harness = LiveHarness.Create();

        var request = await harness.InvokeAsync("ByNamespace", ["system"]);

        await Assert.That(request.RequestUri!.PathAndQuery).IsEqualTo("/lookup/system");
    }

    /// <summary>Verifies keyword query parameters bind under their unescaped names.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    public async Task KeywordQueryParametersFlowIntoQueryString()
    {
        using var harness = LiveHarness.Create();

        var request = await harness.InvokeAsync("Query", ["alpha", "beta"]);

        await Assert.That(request.RequestUri!.PathAndQuery).IsEqualTo("/query?class=alpha&event=beta");
    }

    /// <summary>Verifies a keyword header parameter sets its header value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    public async Task KeywordHeaderParameterSetsHeaderValue()
    {
        using var harness = LiveHarness.Create();

        var request = await harness.InvokeAsync("WithHeader", ["secret"]);

        await Assert.That(request.Headers.GetValues("X-Value").Single()).IsEqualTo("secret");
    }

    /// <summary>Builds, loads and invokes the escaped-identifier interface through the generated request path.</summary>
    /// <param name="context">The load context owning the compiled assembly.</param>
    /// <param name="handler">The capturing handler that records each outgoing request.</param>
    /// <param name="client">The HTTP client the generated stub sends through.</param>
    /// <param name="interfaceType">The compiled escaped-identifier interface type.</param>
    /// <param name="generatedApi">The generated stub instance.</param>
    private sealed class LiveHarness(
        CollectibleAssemblyLoadContext context,
        CapturingHandler handler,
        HttpClient client,
        Type interfaceType,
        object generatedApi) : IDisposable
    {
        /// <summary>The base address the generated client sends through.</summary>
        private const string BaseAddress = "https://api.example.com";

        /// <summary>The interface source exercised by the harness.</summary>
        private const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace Refit.EscapedIdentifierLive;

            public interface IEscapedIdentifierLiveApi
            {
                [Get("/lookup/{namespace}")]
                Task<string> ByNamespace(string @namespace);

                [Get("/query")]
                Task<string> Query(string @class, string @event);

                [Get("/header")]
                Task<string> WithHeader([Header("X-Value")] string @internal);
            }
            """;

        /// <summary>Compiles the interface, loads the generated client, and wires it to a capturing handler.</summary>
        /// <returns>The live harness.</returns>
        [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
        public static LiveHarness Create()
        {
            var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
            if (!result.CompilesWithoutErrors)
            {
                throw new InvalidOperationException(
                    $"Generated compilation failed: {string.Join(Environment.NewLine, result.CompilationErrors)}");
            }

            var (assembly, loadContext) = Fixture.EmitAndLoad(result);
            var interfaceType = assembly.GetType("Refit.EscapedIdentifierLive.IEscapedIdentifierLiveApi", throwOnError: true)!;
            var generatedType = assembly
                .GetTypes()
                .Single(type => type.IsClass && interfaceType.IsAssignableFrom(type));

            var capturingHandler = new CapturingHandler();
            var httpClient = new HttpClient(capturingHandler) { BaseAddress = new(BaseAddress) };
            var requestBuilder = RequestBuilder.ForType(interfaceType, new RefitSettings());
            var generatedApi = Activator.CreateInstance(generatedType, [httpClient, requestBuilder])!;
            return new(loadContext, capturingHandler, httpClient, interfaceType, generatedApi);
        }

        /// <summary>Invokes a generated method and returns the request it produced.</summary>
        /// <param name="methodName">The interface method name.</param>
        /// <param name="args">The argument values.</param>
        /// <returns>The captured request.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        public async Task<HttpRequestMessage> InvokeAsync(string methodName, object?[] args)
        {
            var task = (Task)interfaceType.GetMethod(methodName)!.Invoke(generatedApi, args)!;
            await task.ConfigureAwait(false);
            return handler.TakeLastRequest();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            client.Dispose();
            handler.Dispose();
            context.Dispose();
        }
    }

    /// <summary>Captures each outgoing request and returns a fixed JSON response.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        /// <summary>The last request sent through the handler.</summary>
        private HttpRequestMessage? _lastRequest;

        /// <summary>Takes the last captured request, clearing the slot.</summary>
        /// <returns>The captured request.</returns>
        public HttpRequestMessage TakeLastRequest()
        {
            var request = _lastRequest ?? throw new InvalidOperationException("No request was captured.");
            _lastRequest = null;
            return request;
        }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _lastRequest = request;
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("\"done\"", Encoding.UTF8, "application/json")
                });
        }
    }
}
