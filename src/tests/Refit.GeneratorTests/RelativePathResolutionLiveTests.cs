// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;

namespace Refit.GeneratorTests;

/// <summary>
/// Compiles, loads and invokes generated request building for a no-leading-slash relative path, asserting it builds
/// inline and matches the reflection request builder: resolving against the base address under RFC 3986, and rejecting
/// the path under legacy resolution exactly as the reflection builder does.
/// </summary>
public sealed class RelativePathResolutionLiveTests
{
    /// <summary>The base address the relative request URIs resolve against; the sub-path makes RFC 3986 resolution observable.</summary>
    private const string BaseAddress = "https://example.test/api/v1/";

    /// <summary>The no-leading-slash relative-path method exercised across scenarios.</summary>
    private const string RelativeMethod = "Relative";

    /// <summary>The rooted-path method exercised across scenarios.</summary>
    private const string RootedMethod = "Rooted";

    /// <summary>The sample path-argument value.</summary>
    private const string SampleId = "42";

    /// <summary>The interface source compiled through the generator for every scenario.</summary>
    private const string ApiSource =
        """
        using System.Threading.Tasks;
        using Refit;

        namespace Refit.LiveResolution;

        public interface IResolutionApi
        {
            [Get("/rooted/{id}")]
            Task<string> Rooted(string id);

            [Get("relative/{id}")]
            Task<string> Relative(string id);
        }
        """;

    /// <summary>Verifies a no-leading-slash path builds inline instead of falling back to the reflection request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedInlinesNoLeadingSlashPath()
    {
        var generated = string.Concat(Fixture.RunGenerator(ApiSource, generatedRequestBuilding: true).GeneratedSources.Values);

        await Assert.That(generated).Contains("BuildRequestPath(\"relative/{id}\"");
        await Assert.That(generated).DoesNotContain("BuildRestResultFuncForMethod(\"Relative\"");
    }

    /// <summary>Verifies the generated no-leading-slash and rooted paths match the reflection builder under RFC 3986: the
    /// relative path resolves against the base address's sub-path, and the rooted path replaces the whole path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task Rfc3986RelativePathMatchesReflection()
    {
        using var context = Compile(out var interfaceType, out var generatedType);
        using var handler = new CapturingHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var requestBuilder = RequestBuilder.ForType(interfaceType, new RefitSettings { UrlResolution = UrlResolutionMode.Rfc3986 });
        var generatedApi = Activator.CreateInstance(generatedType, [client, requestBuilder])!;

        await AssertParityAsync(interfaceType, generatedApi, requestBuilder, client, handler, RelativeMethod, SampleId);
        await AssertParityAsync(interfaceType, generatedApi, requestBuilder, client, handler, RootedMethod, SampleId);

        // The relative path resolved against the base sub-path; the rooted path replaced it.
        var relative = await InvokeGeneratedAsync(interfaceType, generatedApi, handler, RelativeMethod, SampleId);
        await Assert.That(relative.RequestUri!.AbsoluteUri).IsEqualTo("https://example.test/api/v1/relative/42");
        var rooted = await InvokeGeneratedAsync(interfaceType, generatedApi, handler, RootedMethod, SampleId);
        await Assert.That(rooted.RequestUri!.AbsoluteUri).IsEqualTo("https://example.test/rooted/42");
    }

    /// <summary>Verifies the generated no-leading-slash path throws under legacy resolution, exactly as the reflection
    /// request builder rejects it (both raise an <see cref="ArgumentException"/>).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Builds the reflection request delegate for parity comparison.")]
    public async Task LegacyRejectsNoLeadingSlashLikeReflection()
    {
        using var context = Compile(out var interfaceType, out var generatedType);
        using var handler = new CapturingHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));

        // The generated client, built from settings alone, rejects the relative path under legacy resolution.
        var generatedApi = Activator.CreateInstance(generatedType, [client, new RefitSettings()])!;
        ArgumentException? generatedRejection = null;
        try
        {
            _ = await InvokeGeneratedAsync(interfaceType, generatedApi, handler, RelativeMethod, SampleId);
        }
        catch (ArgumentException ex)
        {
            generatedRejection = ex;
        }

        await Assert.That(generatedRejection).IsNotNull();

        // The reflection request builder rejects the same interface under legacy resolution.
        await Assert.That(() => RequestBuilder.ForType(interfaceType, new RefitSettings()))
            .Throws<ArgumentException>();
    }

    /// <summary>Compiles the scenario interface and loads it into a collectible context.</summary>
    /// <param name="interfaceType">The compiled Refit interface type.</param>
    /// <param name="generatedType">The generated client type.</param>
    /// <returns>The collectible load context holding the compiled assembly.</returns>
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    private static CollectibleAssemblyLoadContext Compile(out Type interfaceType, out Type generatedType)
    {
        var result = Fixture.RunGenerator(ApiSource, generatedRequestBuilding: true);
        if (!result.CompilesWithoutErrors)
        {
            throw new InvalidOperationException(
                $"Generated compilation failed: {string.Join(Environment.NewLine, result.CompilationErrors)}");
        }

        var (assembly, loadContext) = Fixture.EmitAndLoad(result);
        interfaceType = assembly.GetType("Refit.LiveResolution.IResolutionApi", throwOnError: true)!;
        var resolvedInterface = interfaceType;
        generatedType = assembly.GetTypes().Single(type => type.IsClass && resolvedInterface.IsAssignableFrom(type));
        return loadContext;
    }

    /// <summary>Invokes a method on the generated client and returns the captured request.</summary>
    /// <param name="interfaceType">The compiled Refit interface type.</param>
    /// <param name="generatedApi">The generated client instance.</param>
    /// <param name="handler">The capturing message handler.</param>
    /// <param name="methodName">The interface method name.</param>
    /// <param name="id">The path argument value.</param>
    /// <returns>The captured request.</returns>
    [RequiresUnreferencedCode("Reflects over generated types and members.")]
    private static async Task<HttpRequestMessage> InvokeGeneratedAsync(
        Type interfaceType,
        object generatedApi,
        CapturingHandler handler,
        string methodName,
        string id)
    {
        try
        {
            await ((Task)interfaceType.GetMethod(methodName)!.Invoke(generatedApi, [id])!).ConfigureAwait(false);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }

        return handler.TakeLastRequest();
    }

    /// <summary>Invokes a method through both request paths and asserts the URIs are identical.</summary>
    /// <param name="interfaceType">The compiled Refit interface type.</param>
    /// <param name="generatedApi">The generated client instance.</param>
    /// <param name="requestBuilder">The reflection request builder.</param>
    /// <param name="client">The HTTP client shared by both paths.</param>
    /// <param name="handler">The capturing message handler.</param>
    /// <param name="methodName">The interface method name.</param>
    /// <param name="id">The path argument value.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    [RequiresUnreferencedCode("Reflects over generated types and members.")]
    [RequiresDynamicCode("Builds the reflection request delegate for parity comparison.")]
    private static async Task AssertParityAsync(
        Type interfaceType,
        object generatedApi,
        IRequestBuilder requestBuilder,
        HttpClient client,
        CapturingHandler handler,
        string methodName,
        string id)
    {
        var generatedRequest = await InvokeGeneratedAsync(interfaceType, generatedApi, handler, methodName, id).ConfigureAwait(false);

        var reflectionFunc = requestBuilder.BuildRestResultFuncForMethod(methodName);
        await ((Task)reflectionFunc(client, [id])!).ConfigureAwait(false);
        var reflectionRequest = handler.TakeLastRequest();

        await Assert.That(generatedRequest.RequestUri!.AbsoluteUri).IsEqualTo(reflectionRequest.RequestUri!.AbsoluteUri);
    }

    /// <summary>Captures every outgoing request and returns a fixed JSON string response.</summary>
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
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"done\"", Encoding.UTF8, "application/json")
            });
        }
    }
}
