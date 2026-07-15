// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
extern alias RefitAnalyzers;

using Microsoft.CodeAnalysis.Diagnostics;

namespace Refit.GeneratorTests;

/// <summary>Guards against drift between the source generator's inline-eligibility decision and the RF006 analyzer diagnostic.</summary>
/// <remarks>
/// The analyzer compiles the generator's request classification sources directly, so RF006 fires for exactly the
/// methods the generator emits against the reflection request builder (<c>BuildRestResultFuncForMethod</c>). These
/// cases pin the contract from both sides: if either half changes eligibility without the other, a case fails -
/// which is the intended alarm.
/// </remarks>
public sealed class GeneratedRequestBuildingFallbackContractTests
{
    /// <summary>The reflection-fallback diagnostic identifier.</summary>
    private const string FallbackDiagnosticId = "RF006";

    /// <summary>Verifies that methods the generator builds inline are not flagged by the analyzer.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="methodName">The method whose fallback state is checked.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("[Get(\"/users\")] Task<string> List();", "List")]
    [Arguments("[Post(\"/users\")] Task<string> Create([Body] string payload);", "Create")]
    [Arguments("[Get(\"/ping\")] Task Ping([Header(\"X-Trace\")] string trace, CancellationToken token);", "Ping")]
    [Arguments("[Get(\"/signin\")] Task<string> SignIn([AliasAs(\"login\")] string login);", "SignIn")]
    [Arguments("[Get(\"/users/{id}\")] Task<string> GetUser(string id);", "GetUser")]
    [Arguments("[Get(\"/items\")] Task<string> Items([Query(CollectionFormat.Multi)] int[] ids);", "Items")]
    [Arguments("[Post(\"/create\")] Task<string> Post(System.IO.Stream payload, string tag);", "Post")]
    [Arguments("[Get(\"/values\")] Task<T> GetValue<T>();", "GetValue")]
    [Arguments("[Post(\"/echo\")] Task<T> Echo<T>([Body] T payload);", "Echo")]
    [Arguments("[Get(\"/cal/{**rest}\")] Task<string> RoundTrip(string rest);", "RoundTrip")]
    [Arguments("[Multipart][Post(\"/upload\")] Task<string> Upload([AliasAs(\"file\")] StreamPart stream);", "Upload")]
    [Arguments("[Get(\"/stream\")] IObservable<string> Observe();", "Observe")]
    public Task InlineMethodsAreNotFlagged(string body, string methodName) =>
        AssertGeneratorAndAnalyzerAgree(body, methodName, expectedFallback: false);

    /// <summary>Verifies that methods the generator builds via reflection are flagged by the analyzer.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="methodName">The method whose fallback state is checked.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("[Get(\"/query-map\")] Task<string> Search(object filters);", "Search")]
    [Arguments("[Post(\"/form\")] Task<string> PostForm<T>([Body(BodySerializationMethod.UrlEncoded)] T form);", "PostForm")]
    [Arguments("[Multipart][Post(\"/upload\")] Task<string> Upload(object payload);", "Upload")]
    public Task FallbackMethodsAreFlagged(string body, string methodName) =>
        AssertGeneratorAndAnalyzerAgree(body, methodName, expectedFallback: true);

    /// <summary>Verifies a constrained generic path-bound method is generated inline and is not flagged by RF006, while an
    /// otherwise-identical unconstrained method still falls back to reflection and is flagged (issue #2218).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstrainedGenericPathBoundMethodIsNotFlaggedWhileUnconstrainedIs()
    {
        const string constrainedSource =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public class PathBoundObject
            {
                public int SomeProperty { get; init; }

                public string? SomeProperty2 { get; init; }
            }

            public interface IGeneratedClient
            {
                [Get("/foos/{request.someProperty}/bar/{request.someProperty2}")]
                Task Sample<T>(T request)
                    where T : PathBoundObject;
            }
            """;

        const string unconstrainedSource =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/foos/{request.someProperty}/bar/{request.someProperty2}")]
                Task Sample<T>(T request);
            }
            """;

        await AssertGeneratorAndAnalyzerAgree(Fixture.RunGenerator(constrainedSource, null), "Sample", expectedFallback: false);
        await AssertGeneratorAndAnalyzerAgree(Fixture.RunGenerator(unconstrainedSource, null), "Sample", expectedFallback: true);
    }

    /// <summary>Asserts the generator and analyzer produce the same reflection-fallback verdict for a method.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="methodName">The method whose fallback state is checked.</param>
    /// <param name="expectedFallback">The expected fallback verdict.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    private static Task AssertGeneratorAndAnalyzerAgree(
        string body,
        string methodName,
        bool expectedFallback) =>
        AssertGeneratorAndAnalyzerAgree(Fixture.RunGeneratorForBody(body, null), methodName, expectedFallback);

    /// <summary>Asserts the generator and analyzer agree on the reflection-fallback verdict for a generated result.</summary>
    /// <param name="result">The generator result to inspect.</param>
    /// <param name="methodName">The method whose fallback state is checked.</param>
    /// <param name="expectedFallback">The expected fallback verdict.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    private static async Task AssertGeneratorAndAnalyzerAgree(
        GeneratorTestResult result,
        string methodName,
        bool expectedFallback)
    {
        var generatedText = string.Concat(result.GeneratedSources.Values);
        var generatorFallsBack = generatedText.Contains(
            $"BuildRestResultFuncForMethod(\"{methodName}\"",
            StringComparison.Ordinal);

        var analyzerDiagnostics = await result.OutputCompilation
            .WithAnalyzers([new RefitAnalyzers::Refit.Analyzers.RefitInterfaceAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
        var analyzerFallsBack = analyzerDiagnostics.Any(static diagnostic => diagnostic.Id == FallbackDiagnosticId);

        // The generator is the source of truth; the analyzer must agree with it, and both with the expectation.
        await Assert.That(generatorFallsBack).IsEqualTo(expectedFallback);
        await Assert.That(analyzerFallsBack).IsEqualTo(expectedFallback);
    }
}
