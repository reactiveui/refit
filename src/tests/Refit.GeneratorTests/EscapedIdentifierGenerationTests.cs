// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Linq;

namespace Refit.GeneratorTests;

/// <summary>
/// Compile-safety tests for interface members whose names are escaped (@-prefixed) C# keyword identifiers, in every
/// position the source generator emits an identifier. Guards the regression where a keyword parameter was emitted as a
/// bare identifier, producing source that does not compile.
/// </summary>
public sealed class EscapedIdentifierGenerationTests
{
    /// <summary>Verifies a path parameter named after a keyword, bound to a matching route placeholder, compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task PathParameter() =>
        AssertCompilesAsync(
            """
                [Get("/lookup/{namespace}")]
                Task<string> ByNamespace(string @namespace);
            """);

    /// <summary>Verifies an optional path segment bound to a nullable keyword parameter compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task OptionalPathParameter() =>
        AssertCompilesAsync(
            """
                [Get("/lookup/{namespace?}")]
                Task<string> ByOptionalNamespace(string? @namespace);
            """);

    /// <summary>Verifies keyword parameters bound implicitly to the query string compile.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task QueryParameters() =>
        AssertCompilesAsync(
            """
                [Get("/query")]
                Task<string> QueryKeywords(string @class, string @event);
            """);

    /// <summary>Verifies an aliased keyword query parameter compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task AliasedQueryParameter() =>
        AssertCompilesAsync(
            """
                [Get("/query")]
                Task<string> AliasedQuery([AliasAs("key")] string @object);
            """);

    /// <summary>Verifies a keyword body parameter compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task BodyParameter() =>
        AssertCompilesAsync(
            """
                [Post("/body")]
                Task<string> BodyKeyword([Body] string @object);
            """);

    /// <summary>Verifies a keyword header parameter compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task HeaderParameter() =>
        AssertCompilesAsync(
            """
                [Get("/header")]
                Task<string> HeaderKeyword([Header("X-Value")] string @internal);
            """);

    /// <summary>Verifies a keyword header-collection parameter compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task HeaderCollectionParameter() =>
        AssertCompilesAsync(
            """
                [Get("/headers")]
                Task<string> HeaderCollectionKeyword([HeaderCollection] IDictionary<string, string> @params);
            """);

    /// <summary>Verifies a keyword property parameter compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task PropertyParameter() =>
        AssertCompilesAsync(
            """
                [Get("/property")]
                Task<string> PropertyKeyword([Property("tenant")] int @int);
            """);

    /// <summary>Verifies a keyword interface property member compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task PropertyMember() =>
        AssertCompilesAsync(
            """
                [Property("tenant")]
                int @int { get; set; }

                [Get("/property")]
                Task<string> WithProperty();
            """);

    /// <summary>Verifies keyword multipart part parameters compile.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task MultipartParameters() =>
        AssertCompilesAsync(
            """
                [Multipart]
                [Post("/upload")]
                Task<string> MultipartKeyword(StreamPart @stream, [AliasAs("file")] ByteArrayPart @byte);
            """);

    /// <summary>Verifies a keyword round-tripping <c>[Encoded]</c> path parameter compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task EncodedRoundTripParameter() =>
        AssertCompilesAsync(
            """
                [Get("/encoded/{string}")]
                Task<string> EncodedKeyword([Encoded] string @string);
            """);

    /// <summary>Verifies a keyword parameter on a build-the-request (<c>Task&lt;HttpRequestMessage&gt;</c>) method compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task BuildRequestReturnShape() =>
        AssertCompilesAsync(
            """
                [Get("/request/{namespace}")]
                Task<HttpRequestMessage> BuildKeyword(string @namespace);
            """);

    /// <summary>Verifies a keyword-named cancellation-token parameter alongside a keyword path parameter compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task CancellationTokenKeywordName() =>
        AssertCompilesAsync(
            """
                [Get("/cancel/{namespace}")]
                Task<string> CancellationKeyword(string @namespace, CancellationToken @default);
            """);

    /// <summary>Verifies an interface method whose own name is an escaped keyword compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task KeywordMethodName() =>
        AssertCompilesAsync(
            """
                [Get("/keyword")]
                Task<string> @class();
            """);

    /// <summary>Verifies a single method combining keyword parameters across path, query, header and body compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task MixedKeywordPositions() =>
        AssertCompilesAsync(
            """
                [Post("/mixed/{namespace}")]
                Task<string> Mixed(
                    string @namespace,
                    string @class,
                    [Header("X-Op")] string @operator,
                    [Body] string @object);
            """);

    /// <summary>Runs the source generator over an interface built from the given members and asserts the generated
    /// output compiles with no errors, surfacing any compilation errors in the assertion message.</summary>
    /// <param name="members">The interface member declarations to place inside the test interface.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertCompilesAsync(string members)
    {
        const string header =
            "using System.Collections.Generic;\n" +
            "using System.Net.Http;\n" +
            "using System.Threading;\n" +
            "using System.Threading.Tasks;\n" +
            "using Refit;\n\n" +
            "namespace Refit.EscapedIdentifierScenarios;\n\n" +
            "public interface IEscapedIdentifierApi\n{\n";
        const string footer = "\n}\n";

        var result = Fixture.RunGenerator(header + members + footer, generatedRequestBuilding: true);

        var report = result.CompilesWithoutErrors
            ? "OK"
            : string.Join(
                Environment.NewLine,
                result.CompilationErrors.Select(static diagnostic => diagnostic.ToString()));

        await Assert.That(report).IsEqualTo("OK");
    }
}
