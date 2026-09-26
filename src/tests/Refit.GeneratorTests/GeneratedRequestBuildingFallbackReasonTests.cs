// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
extern alias RefitAnalyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using AnalyzerExplanation = RefitAnalyzers::Refit.Analyzers.FallbackExplanation;
using AnalyzerFallbackReason = RefitAnalyzers::Refit.Generator.InlineFallbackReason;

namespace Refit.GeneratorTests;

/// <summary>Verifies RF006 names the reason the shared classifier recorded, at the declaration responsible.</summary>
/// <remarks>Each case is an unsupported fixture: the generator must emit the reflection fallback for it, and the analyzer
/// must report RF006 with the matching <c>Reason</c> property, located on the exact source text named here.</remarks>
public sealed class GeneratedRequestBuildingFallbackReasonTests
{
    /// <summary>The reflection-fallback diagnostic identifier.</summary>
    private const string FallbackDiagnosticId = "RF006";

    /// <summary>A custom HTTP method attribute whose verb is not a string literal.</summary>
    private const string UnreadableVerbSource =
        """
        using System.Net.Http;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public sealed class DynamicVerbAttribute(string path) : HttpMethodAttribute(path)
        {
            private readonly string _verb = "PURGE";

            public override HttpMethod Method => new(_verb);
        }

        public interface IGeneratedClient
        {
            [DynamicVerb("/cache")]
            Task Purge();
        }
        """;

    /// <summary>A path-bound object whose unbound property cannot be flattened into the query.</summary>
    private const string PathObjectQuerySource =
        """
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public sealed class Lookup
        {
            public int Id { get; set; }

            public object? Extra { get; set; }
        }

        public interface IGeneratedClient
        {
            [Get("/items/{lookup.Id}")]
            Task<string> Find(Lookup lookup);
        }
        """;

    /// <summary>A multipart <c>[FormObject]</c> parameter.</summary>
    private const string FormObjectSource =
        """
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public sealed class Form
        {
            public string? Name { get; set; }
        }

        public interface IGeneratedClient
        {
            [Multipart]
            [Post("/upload")]
            Task<string> Upload([FormObject] Form form);
        }
        """;

    /// <summary>The compatibility advice a fallback should carry.</summary>
    public enum Compatibility
    {
        /// <summary>Refit.Reflection can build the method.</summary>
        Reflection = 0,

        /// <summary>The declaration is invalid for both request builders.</summary>
        Invalid = 1,
    }

    /// <summary>Gets the fixtures that need declarations outside the interface.</summary>
    /// <returns>The source, method, reason, location text and compatibility for each fixture.</returns>
    public static IEnumerable<Func<(string Source, string MethodName, string Reason, string LocationText, Compatibility Compatibility)>> DeclarationFixtures()
    {
        yield return static () => (UnreadableVerbSource, "Purge", "UnreadableHttpMethod", "DynamicVerb(\"/cache\")", Compatibility.Reflection);
        yield return static () => (PathObjectQuerySource, "Find", "UnsupportedPathObjectQuery", "lookup", Compatibility.Reflection);
        yield return static () => (FormObjectSource, "Upload", "FormObjectMultipartPart", "form", Compatibility.Reflection);
    }

    /// <summary>Verifies each fallback reports its reason and location, and that the generator really falls back.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="methodName">The method whose fallback is checked.</param>
    /// <param name="reason">The expected <c>InlineFallbackReason</c> name.</param>
    /// <param name="locationText">The source text the diagnostic must span.</param>
    /// <param name="compatibility">The expected compatibility advice kind.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("[Get(\"/sync\")] string Sync();", "Sync", "UnsupportedReturnType", "string", Compatibility.Reflection)]
    [Arguments("[Get(\"/a/{id\")] Task<string> Broken(string id);", "Broken", "UnsupportedPathTemplate", "Get(\"/a/{id\")", Compatibility.Invalid)]
    [Arguments("[Get(\"\")] Task<string> Fetch([Url] int target);", "Fetch", "UrlParameterType", "target", Compatibility.Invalid)]
    [Arguments("[Get(\"/items\")] Task<string> Fetch([Url] string target);", "Fetch", "UrlParameterWithPath", "target", Compatibility.Invalid)]
    [Arguments("[Post(\"/form\")] Task<string> PostForm<T>([Body(BodySerializationMethod.UrlEncoded)] T form);", "PostForm", "GenericUrlEncodedBody", "form", Compatibility.Reflection)]
    [Arguments("[Post(\"/raw\")] Task<string> Raw([Body((BodySerializationMethod)42)] string payload);", "Raw", "UnknownBodySerialization", "payload", Compatibility.Reflection)]
    [Arguments("[Post(\"/users\")] Task<string> TwoBodies([Body] string first, [Body] string second);", "TwoBodies", "MultipleBodies", "second", Compatibility.Invalid)]
    [Arguments("[Post(\"/pair\")] Task<string> Pair(System.IO.Stream first, System.IO.Stream second);", "Pair", "SecondImplicitBody", "second", Compatibility.Invalid)]
    [Arguments("[Get(\"/tokens\")] Task<string> Tokens(CancellationToken first, CancellationToken second);", "Tokens", "MultipleCancellationTokens", "second", Compatibility.Invalid)]
    [Arguments(
        "[Get(\"/h\")] Task<string> Headers([HeaderCollection] IDictionary<string, string> a, [HeaderCollection] IDictionary<string, string> b);",
        "Headers",
        "MultipleHeaderCollections",
        "b",
        Compatibility.Invalid)]
    [Arguments("[Get(\"/auth\")] Task<string> Auth([Authorize] string a, [Authorize] string b);", "Auth", "MultipleAuthorizeParameters", "b", Compatibility.Invalid)]
    [Arguments("[Multipart][Post(\"/upload\")] Task<string> Upload([Body] string payload);", "Upload", "MultipartWithBody", "payload", Compatibility.Invalid)]
    [Arguments("[Get(\"/users/{id}\")] Task<string> GetUser(object id);", "GetUser", "UnsupportedPathParameterType", "id", Compatibility.Reflection)]
    [Arguments("[Get(\"/cal/{**rest}\")] Task<string> RoundTrip([Encoded] int rest);", "RoundTrip", "EncodedRoundTripNotString", "rest", Compatibility.Reflection)]
    [Arguments("[Get(\"/query-map\")] Task<string> Search(object filters);", "Search", "UnsupportedQueryType", "filters", Compatibility.Reflection)]
    [Arguments("[Get(\"/foos/{request.someProperty}\")] Task Sample<T>(T request);", "Sample", "UnresolvedPathProperty", "request", Compatibility.Reflection)]
    [Arguments("[Multipart][Post(\"/upload\")] Task<string> Upload(object payload);", "Upload", "UnsupportedMultipartPart", "payload", Compatibility.Reflection)]
    public async Task FallbackReportsReasonAtResponsibleDeclaration(
        string body,
        string methodName,
        string reason,
        string locationText,
        Compatibility compatibility)
    {
        var result = Fixture.RunGeneratorForBody(body, null);
        var diagnostic = await GetSingleFallback(result);

        await Assert.That(GeneratorFallsBack(result, methodName)).IsTrue();
        await AssertFallback(diagnostic, reason, locationText, compatibility);
    }

    /// <summary>Verifies fallbacks whose fixtures need declarations outside the interface.</summary>
    /// <param name="source">The complete source.</param>
    /// <param name="methodName">The method whose fallback is checked.</param>
    /// <param name="reason">The expected <c>InlineFallbackReason</c> name.</param>
    /// <param name="locationText">The source text the diagnostic must span.</param>
    /// <param name="compatibility">The expected compatibility advice kind.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [MethodDataSource(nameof(DeclarationFixtures))]
    public async Task FallbackWithSupportingDeclarationsReportsReason(
        string source,
        string methodName,
        string reason,
        string locationText,
        Compatibility compatibility)
    {
        var result = Fixture.RunGenerator(source, null);
        var diagnostic = await GetSingleFallback(result);

        await Assert.That(GeneratorFallsBack(result, methodName)).IsTrue();
        await AssertFallback(diagnostic, reason, locationText, compatibility);
    }

    /// <summary>Verifies every reason the classifier can record has its own wording rather than the generic fallback text.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EveryReasonHasSpecificWording()
    {
        var compilation = Fixture.CreateLibrary(
            CSharpSyntaxTree.ParseText(
                """
                using Refit;
                using System.Threading.Tasks;

                public interface IApi
                {
                    [Get("/x")]
                    Task<string> Call(int value);
                }
                """));
        var method = compilation.GetTypeByMetadataName("IApi")!.GetMembers("Call").OfType<IMethodSymbol>().Single();
        var generic = AnalyzerExplanation.Describe(method, new((AnalyzerFallbackReason)int.MaxValue, -1)).Problem;

        foreach (var reason in Enum.GetValues<AnalyzerFallbackReason>())
        {
            if (reason is AnalyzerFallbackReason.None or AnalyzerFallbackReason.MissingHttpMethodAttribute)
            {
                continue;
            }

            var explanation = AnalyzerExplanation.Describe(method, new(reason, 0));
            await Assert.That(explanation.Problem).IsNotEqualTo(generic).Because($"{reason} needs its own explanation");
            await Assert.That(explanation.Remedy).IsNotEmpty();
            await Assert.That(explanation.Compatibility).IsNotEmpty();
        }
    }

    /// <summary>Verifies a supported shape carries no fallback reason, so RF006 disappears without analyzer changes.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("[Get(\"/users/{id}\")] Task<string> GetUser(int id);")]
    [Arguments("[Get(\"/query\")] Task<string> Search([QueryConverter(typeof(FilterConverter))] object filters);")]
    [Arguments("[Get(\"/cal/{**rest}\")] Task<string> RoundTrip([Encoded] string rest);")]
    public async Task SupportedShapesReportNoFallback(string body)
    {
        var result = Fixture.RunGenerator(
            $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class FilterConverter : IQueryConverter<object>
            {
                public void Flatten(object value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings)
                {
                }
            }

            public interface IGeneratedClient
            {
                {{body}}
            }
            """,
            null);
        var diagnostics = await result.OutputCompilation
            .WithAnalyzers([new RefitAnalyzers::Refit.Analyzers.RefitInterfaceAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();

        await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == FallbackDiagnosticId)).IsFalse();
    }

    /// <summary>Runs the analyzer over a generated result and returns its single RF006 diagnostic.</summary>
    /// <param name="result">The generator result.</param>
    /// <returns>The RF006 diagnostic.</returns>
    private static async Task<Diagnostic> GetSingleFallback(GeneratorTestResult result)
    {
        var diagnostics = await result.OutputCompilation
            .WithAnalyzers([new RefitAnalyzers::Refit.Analyzers.RefitInterfaceAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
        var fallbacks = diagnostics.Where(static diagnostic => diagnostic.Id == FallbackDiagnosticId).ToArray();
        await Assert.That(fallbacks.Length).IsEqualTo(1);
        return fallbacks[0];
    }

    /// <summary>Asserts a fallback diagnostic's reason, location and advice.</summary>
    /// <param name="diagnostic">The RF006 diagnostic.</param>
    /// <param name="reason">The expected reason name.</param>
    /// <param name="locationText">The source text the diagnostic must span.</param>
    /// <param name="compatibility">The expected compatibility advice kind.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertFallback(Diagnostic diagnostic, string reason, string locationText, Compatibility compatibility)
    {
        var text = await diagnostic.Location.SourceTree!.GetTextAsync();
        var located = text.ToString(diagnostic.Location.SourceSpan);
        var message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);

        await Assert.That(diagnostic.Properties["Reason"]).IsEqualTo(reason);
        await Assert.That(located).IsEqualTo(locationText);
        await Assert.That(message).Contains(ExpectedAdvice(compatibility));
    }

    /// <summary>Gets the advice RF006 must give for a compatibility kind.</summary>
    /// <param name="compatibility">The compatibility kind.</param>
    /// <returns>The advice text.</returns>
    private static string ExpectedAdvice(Compatibility compatibility) =>
        compatibility == Compatibility.Reflection
            ? AnalyzerExplanation.ReflectionAdvice
            : AnalyzerExplanation.InvalidDeclarationAdvice;

    /// <summary>Determines whether the generator emitted the reflection fallback for a method.</summary>
    /// <param name="result">The generator result.</param>
    /// <param name="methodName">The method name.</param>
    /// <returns><see langword="true"/> when the method is built through the reflection request builder.</returns>
    private static bool GeneratorFallsBack(GeneratorTestResult result, string methodName) =>
        string.Concat(result.GeneratedSources.Values).Contains(
            $"BuildRestResultFuncForMethod(\"{methodName}\"",
            StringComparison.Ordinal);
}
