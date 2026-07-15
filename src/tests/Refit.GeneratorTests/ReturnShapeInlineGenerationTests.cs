// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests covering which non-<c>Task</c> return shapes build their request inline versus falling back
/// to the reflective request builder.</summary>
public class ReturnShapeInlineGenerationTests
{
    /// <summary>The generated implementation source hint name used by these tests.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The reflective request-builder call emitted by fallback paths.</summary>
    private const string ReflectiveRequestBuilderCall = "BuildRestResultFuncForMethod";

    /// <summary>The generated request-message construction emitted by inline request construction.</summary>
    private const string NewHttpRequestMessage = "new global::System.Net.Http.HttpRequestMessage";

    /// <summary>The inline streaming send call emitted for an <c>IAsyncEnumerable</c> return.</summary>
    private const string StreamAsyncCall = "GeneratedRequestRunner.StreamAsync<";

    /// <summary>The inline cold-observable send call emitted for an <c>IObservable</c> return.</summary>
    private const string SendObservableCall = "GeneratedRequestRunner.SendObservable<";

    /// <summary>The inline build-and-return statement emitted for a <c>Task&lt;HttpRequestMessage&gt;</c> return.</summary>
    private const string TaskFromResultCall = "global::System.Threading.Tasks.Task.FromResult";

    /// <summary>The inline request-processing send call emitted for a response-returning method.</summary>
    private const string SendAsyncCall = "GeneratedRequestRunner.SendAsync<";

    /// <summary>Verifies an IAsyncEnumerable method is generated inline through StreamAsync, not the reflective builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IAsyncEnumerableUsesInlineStreamAsync()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/items")]
            IAsyncEnumerable<string> Stream();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(StreamAsyncCall);
        await Assert.That(generated).Contains(NewHttpRequestMessage);
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an IAsyncEnumerable method falls back to the reflective builder when inline generation is off.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IAsyncEnumerableSwitchOffUsesReflectiveRequestBuilder()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/items")]
            IAsyncEnumerable<string> Stream();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: false);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain(StreamAsyncCall);
    }

    /// <summary>Verifies an IObservable method is generated inline through SendObservable, not the reflective builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObservableUsesInlineSendObservable()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/items")]
            IObservable<string> Watch();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(SendObservableCall);
        await Assert.That(generated).Contains(NewHttpRequestMessage);
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an IObservable method falls back to the reflective builder when inline generation is off.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObservableSwitchOffUsesReflectiveRequestBuilder()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/items")]
            IObservable<string> Watch();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: false);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain(SendObservableCall);
    }

    /// <summary>Verifies a Task&lt;HttpRequestMessage&gt; method builds the request inline and returns it without sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestMessageReturnBuildsRequestInlineWithoutSending()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/items/{id}")]
            Task<HttpRequestMessage> BuildRequest(int id);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(NewHttpRequestMessage);
        await Assert.That(generated).Contains(TaskFromResultCall);
        await Assert.That(generated).DoesNotContain(SendAsyncCall);
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a Task&lt;HttpRequestMessage&gt; method falls back to the reflective builder when inline generation is off.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestMessageReturnSwitchOffUsesReflectiveRequestBuilder()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/items/{id}")]
            Task<HttpRequestMessage> BuildRequest(int id);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: false);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain(TaskFromResultCall);
    }
}
