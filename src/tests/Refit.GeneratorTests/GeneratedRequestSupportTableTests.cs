// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
extern alias RefitAnalyzers;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Classifier-backed fixtures for the published generated-request support table.</summary>
/// <remarks>Every fixture is compiled through the generator and the RF006 analyzer. A fixture whose "Request generated",
/// reason, JSON metadata or Native AOT smoke value no longer matches fails, so the published table can be refreshed from
/// these fixtures rather than from inspection.</remarks>
public sealed partial class GeneratedRequestSupportTableTests
{
    /// <summary>Marks a shape no native run covers.</summary>
    private const string NotCovered = "—";

    /// <summary>The "Request generated" value for a generated request.</summary>
    private const string Generated = "Yes";

    /// <summary>The JSON metadata value for a request that serializes nothing as JSON.</summary>
    private const string NoJson = "none";

    /// <summary>The JSON metadata value for a request that falls back to reflection.</summary>
    private const string NotApplicable = "n/a";

    /// <summary>The JSON metadata value for a <c>Todo</c> reply.</summary>
    private const string ReplyTodo = "reply `Todo`";

    /// <summary>The declarations the table's examples refer to.</summary>
    private const string Preamble =
        """
        using System;
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Text.Json.Serialization.Metadata;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public class Todo
        {
            public int Id { get; set; }
        }

        public class Form
        {
            public string? Name { get; set; }
        }

        public class Record
        {
            public int Id { get; set; }
        }

        public class Filter
        {
            public string? Term { get; set; }
        }

        public sealed class FilterConverter : IQueryConverter<Filter>
        {
            public void Flatten(Filter value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings)
            {
            }
        }

        """;

    /// <summary>Gets the support fixtures: each shape with its expected generation, JSON metadata and native coverage.</summary>
    /// <returns>One factory per fixture.</returns>
    public static IEnumerable<Func<SupportRow>> Rows()
    {
        SupportRow[] rows =
        [
            new("Path value, JSON reply", "[Get(\"/todos/{id}\")] Task<Todo> Get(int id);", Generated, ReplyTodo, NotCovered),
            new("JSON body", "[Post(\"/todos\")] Task<Todo> Create([Body] Todo item);", Generated, "body `Todo`, reply `Todo`", "CreateTodoAsync"),
            new(
                "JSON body with JsonTypeInfo<T>",
                "[Post(\"/todos\")] Task<Todo> Create([Body] Todo item, JsonTypeInfo<Todo> info);",
                Generated,
                "body `Todo` (parameter), reply `Todo` (parameter)",
                "CreateDescribedTodoAsync"),
            new("Form-url-encoded body", "[Post(\"/forms\")] Task<string> Submit([Body(BodySerializationMethod.UrlEncoded)] Form form);", Generated, NoJson, "SubmitFormAsync"),
            new(
                "Query values and collections",
                "[Get(\"/search\")] Task<string> Search(string q, int? page, [Query(CollectionFormat.Multi)] int[] ids);",
                Generated,
                NoJson,
                "SearchAsync"),
            new("ApiResponse<T> reply", "[Get(\"/status\")] Task<ApiResponse<Todo>> Status();", Generated, ReplyTodo, "GetStatusAsync"),
            new("Generic body and reply", "[Post(\"/echo\")] Task<T> Echo<T>([Body] T item);", Generated, "body `T`, reply `T`", "EchoAsync"),
            new("Observable reply", "[Get(\"/legacy\")] IObservable<HttpResponseMessage> Observe();", Generated, NoJson, NotCovered),
            new(
                "JSON Lines upload",
                "[Post(\"/uploads\")] Task Upload([Body(BodySerializationMethod.JsonLines)] IAsyncEnumerable<Record> records);",
                Generated,
                "JSON Lines `Record`",
                "UploadAsync"),
            new("Streamed reply", "[Get(\"/todos\")] IAsyncEnumerable<Todo> List();", Generated, ReplyTodo, NotCovered),
            new("Multipart stream part", "[Multipart][Post(\"/upload\")] Task Upload(StreamPart file);", Generated, NoJson, NotCovered),
            new("Raw string body", "[Post(\"/notes\")] Task Note([Body] string text);", Generated, NoJson, NotCovered),
            new("Query converter", "[Get(\"/filter\")] Task<string> Find([QueryConverter(typeof(FilterConverter))] Filter filter);", Generated, NoJson, NotCovered),
            new("Query object of unknown shape", "[Get(\"/query\")] Task<string> Search(object filters);", "No: `UnsupportedQueryType`", NotApplicable, NotCovered),
            new("Multipart part of unknown shape", "[Multipart][Post(\"/upload\")] Task Upload(object payload);", "No: `UnsupportedMultipartPart`", NotApplicable, NotCovered),
            new("[FormObject] multipart part", "[Multipart][Post(\"/upload\")] Task Upload([FormObject] Form form);", "No: `FormObjectMultipartPart`", NotApplicable, NotCovered),
            new(
                "Generic form-url-encoded body",
                "[Post(\"/form\")] Task Post<T>([Body(BodySerializationMethod.UrlEncoded)] T form);",
                "No: `GenericUrlEncodedBody`",
                NotApplicable,
                NotCovered),
            new("Path value of unknown shape", "[Get(\"/users/{id}\")] Task<string> Get(object id);", "No: `UnsupportedPathParameterType`", NotApplicable, NotCovered),
            new("Synchronous return", "[Get(\"/sync\")] string Sync();", "No: `UnsupportedReturnType`", NotApplicable, NotCovered),
        ];

        foreach (var row in rows)
        {
            yield return () => row;
        }
    }

    /// <summary>Verifies a row's "Request generated" column against the generator and RF006.</summary>
    /// <param name="row">The table row.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task RequestGeneratedColumnMatchesTheClassifier(SupportRow row)
    {
        var result = Fixture.RunGenerator(BuildSource(row.Example), null);
        var diagnostics = await result.OutputCompilation
            .WithAnalyzers([new RefitAnalyzers::Refit.Analyzers.RefitInterfaceAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
        var fallback = diagnostics.SingleOrDefault(static diagnostic => diagnostic.Id == "RF006");
        var generatorFallsBack = string.Concat(result.GeneratedSources.Values)
            .Contains("BuildRestResultFuncForMethod(", StringComparison.Ordinal);

        var expected = fallback is null ? Generated : $"No: `{fallback.Properties["Reason"]}`";
        await Assert.That(row.Generated).IsEqualTo(expected);
        await Assert.That(generatorFallsBack).IsEqualTo(fallback is not null);
    }

    /// <summary>Verifies a row's JSON metadata column against the types the parsed request serializes.</summary>
    /// <param name="row">The table row.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task JsonMetadataColumnMatchesTheParsedRequest(SupportRow row)
    {
        var request = ClassifyExample(row.Example);
        var expected = request.CanGenerateInline ? DescribeJsonMetadata(request) : NotApplicable;

        await Assert.That(row.JsonMetadata).IsEqualTo(expected);
    }

    /// <summary>Verifies a row's Native AOT smoke column names a method the smoke program declares and calls.</summary>
    /// <param name="row">The table row.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task NativeAotSmokeColumnNamesAnExecutedMethod(SupportRow row)
    {
        if (row.NativeSmoke == NotCovered)
        {
            return;
        }

        var api = await File.ReadAllTextAsync(RefitPackageLayout.FindRepositoryFile("Refit.NativeAotSmoke/INativeAotApi.cs"));
        var program = await File.ReadAllTextAsync(RefitPackageLayout.FindRepositoryFile("Refit.NativeAotSmoke/Program.cs"));

        await Assert.That(row.Generated).IsEqualTo(Generated);
        await Assert.That(api.Contains($" {row.NativeSmoke}(", StringComparison.Ordinal) || api.Contains($" {row.NativeSmoke}<", StringComparison.Ordinal))
            .IsTrue();
        await Assert.That(program).Contains($".{row.NativeSmoke}");
    }

    /// <summary>Describes the JSON metadata a generated request needs, in the table's wording.</summary>
    /// <param name="request">The parsed request.</param>
    /// <returns>The JSON metadata cell text.</returns>
    private static string DescribeJsonMetadata(in RequestModel request)
    {
        var supplied = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == RequestParameterKind.JsonTypeInfo)
            {
                _ = supplied.Add(parameter.JsonTypeInfoTarget!);
            }
        }

        var parts = new List<string>();
        foreach (var parameter in request.Parameters)
        {
            if (DescribeBody(parameter, supplied) is { } body)
            {
                parts.Add(body);
            }
        }

        if (IsJsonReply(request.DeserializedResultType))
        {
            parts.Add($"reply {Describe(request.DeserializedResultType, supplied)}");
        }

        return parts.Count == 0 ? NoJson : string.Join(", ", parts);
    }

    /// <summary>Describes the JSON metadata a body parameter needs.</summary>
    /// <param name="parameter">The parsed parameter.</param>
    /// <param name="supplied">The types a <c>JsonTypeInfo&lt;T&gt;</c> parameter supplies.</param>
    /// <returns>The cell fragment, or <see langword="null"/> when the parameter sends no JSON.</returns>
    private static string? DescribeBody(in RequestParameterModel parameter, HashSet<string> supplied) =>
        parameter.Kind != RequestParameterKind.Body
            ? null
            : parameter.BodySerializationMethod switch
            {
                "UrlEncoded" => null,
                "JsonLines" => $"JSON Lines {Describe(parameter.JsonLinesElementType!, supplied)}",
                "Default" when parameter.Type == "string" => null,
                _ when parameter.Type == "global::System.IO.Stream" => null,
                _ => $"body {Describe(parameter.Type, supplied)}",
            };

    /// <summary>Determines whether a reply is read through a JSON serializer.</summary>
    /// <param name="deserializedResultType">The fully-qualified type the reply is read as.</param>
    /// <returns><see langword="false"/> for no reply and for the types read raw.</returns>
    private static bool IsJsonReply(string deserializedResultType) =>
        deserializedResultType is not ("global::System.Threading.Tasks.Task"
            or "string"
            or "global::System.IO.Stream"
            or "global::System.Net.Http.HttpContent"
            or "global::System.Net.Http.HttpResponseMessage"
            or "global::System.Net.Http.HttpRequestMessage");

    /// <summary>Formats a type as the table shows it.</summary>
    /// <param name="type">The fully-qualified type.</param>
    /// <param name="supplied">The types a <c>JsonTypeInfo&lt;T&gt;</c> parameter supplies.</param>
    /// <returns>The short type name in code format, marked when a parameter supplies its metadata.</returns>
    private static string Describe(string type, HashSet<string> supplied) =>
        $"`{QualifierPattern().Replace(type, string.Empty)}`{(supplied.Contains(type) ? " (parameter)" : string.Empty)}";

    /// <summary>Parses the example's request with the generator's own classifier.</summary>
    /// <param name="example">The interface member source.</param>
    /// <returns>The parsed request.</returns>
    private static RequestModel ClassifyExample(string example)
    {
        var compilation = Fixture.CreateLibrary(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(BuildSource(example)));
        var method = compilation.GetTypeByMetadataName("RefitGeneratorTest.IGeneratedClient")!
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Single();
        var adapterInterface = Parser.ResolveReturnTypeAdapterInterface(compilation);
        return Parser.ClassifyRequest(
            method,
            compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute")!,
            compilation.GetTypeByMetadataName("System.IFormattable"),
            adapterInterface,
            Parser.DiscoverReturnTypeAdapters(compilation, adapterInterface, CancellationToken.None),
            Parser.ResolveIndexedCollectionFormatValue(compilation));
    }

    /// <summary>Wraps an example member in the table's supporting declarations.</summary>
    /// <param name="example">The interface member source.</param>
    /// <returns>The complete source.</returns>
    private static string BuildSource(string example) =>
        $$"""
        {{Preamble}}
        public interface IGeneratedClient
        {
            {{example}}
        }
        """;

    /// <summary>Matches the <c>global::</c> alias and namespace qualifiers of a fully-qualified type.</summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"global::(?:\w+\.)*")]
    private static partial Regex QualifierPattern();

    /// <summary>One row of the support table.</summary>
    /// <param name="Shape">The shape the row describes.</param>
    /// <param name="Example">The interface member compiled for the row.</param>
    /// <param name="Generated">The "Request generated" cell.</param>
    /// <param name="JsonMetadata">The "JSON metadata" cell.</param>
    /// <param name="NativeSmoke">The Native AOT smoke method, or a dash.</param>
    public sealed record SupportRow(string Shape, string Example, string Generated, string JsonMetadata, string NativeSmoke)
    {
        /// <inheritdoc/>
        public override string ToString() => Shape;
    }
}
