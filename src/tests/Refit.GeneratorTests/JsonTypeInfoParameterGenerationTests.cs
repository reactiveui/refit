// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests covering methods that take <c>JsonTypeInfo&lt;T&gt;</c> metadata as a parameter.</summary>
public class JsonTypeInfoParameterGenerationTests
{
    /// <summary>The diagnostic reported for a metadata parameter the generator cannot pass on.</summary>
    private const string InvalidJsonTypeInfoId = "RF014";

    /// <summary>The reason reported for metadata that describes neither the body nor the reply.</summary>
    private const string MatchesNeither = "matches neither the JSON body type nor the type the reply is read as";

    /// <summary>The reason reported for a request that cannot be generated inline.</summary>
    private const string NotInline = "its request cannot be generated inline";

    /// <summary>The types the scenario methods are declared over.</summary>
    private const string Prelude =
        """
        using System;
        using System.Collections.Generic;
        using System.Text.Json.Serialization.Metadata;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace TypeInfoTest;

        public sealed class Order
        {
            public string? Id { get; set; }
        }

        public sealed class Receipt
        {
            public string? Id { get; set; }
        }

        public sealed class Other
        {
            public string? Id { get; set; }
        }

        public sealed class OrderPage
        {
            public List<Order>? Items { get; set; }

            public string? Next { get; set; }
        }
        """;

    /// <summary>Gets methods with metadata parameters that generate.</summary>
    /// <returns>The method declarations.</returns>
    public static IEnumerable<string> ValidMethods() =>
    [
        """[Get("/a")] Task<Order> A(JsonTypeInfo<Order> info);""",
        """[Get("/a/{id}")] Task<Order> A(int id, JsonTypeInfo<Order> info, CancellationToken cancellationToken);""",
        """[Get("/a")] Task<ApiResponse<Order>> A(JsonTypeInfo<Order> info);""",
        """[Get("/a")] Task<List<Order>> A(JsonTypeInfo<List<Order>> info);""",
        """[Post("/a")] Task A([Body] Order order, JsonTypeInfo<Order> info);""",
        """[Post("/a")] Task<Order> A(Order order, JsonTypeInfo<Order> info);""",
        """[Post("/a")] Task<Receipt> A([Body] Order order, JsonTypeInfo<Order> orderInfo, JsonTypeInfo<Receipt> receiptInfo);""",
        """[Post("/a")] Task<Order> A([Body] Order order, JsonTypeInfo<Order>? info = null);""",
        """[Post("/a")] Task<Order> A([Body(BodySerializationMethod.Serialized)] Order order, JsonTypeInfo<Order> info);""",
        """[Post("/a")] Task<Receipt> A([Body(BodySerializationMethod.UrlEncoded)] Order order, JsonTypeInfo<Receipt> info);""",
        """[Get("/a")] IAsyncEnumerable<Order> A(JsonTypeInfo<Order> info);""",
        """[Get("/a")] IObservable<Order> A(JsonTypeInfo<Order> info);""",
        """[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<OrderPage, Order> A(JsonTypeInfo<OrderPage> info, [PageToken] string? token);""",
        """[Get("/a")] Task<Order> @class(JsonTypeInfo<Order> @string);""",
        """
        [Get("/a")] Task<Order> A(JsonTypeInfo<Order> info);
        [Get("/b")] Task<Receipt> A(string other, JsonTypeInfo<Receipt> info);
        """,
    ];

    /// <summary>Gets methods with metadata parameters that cannot be used, with the reason expected in the diagnostic.</summary>
    /// <returns>The method declarations and the text the diagnostic must contain.</returns>
    public static IEnumerable<(string Method, string Reason)> InvalidMethods() =>
    [
        ("""[Get("/a")] Task<Order> A(JsonTypeInfo<Other> info);""", MatchesNeither),
        ("""[Get("/a")] Task A(JsonTypeInfo<Order> info);""", MatchesNeither),
        ("""[Post("/a")] Task<Order> A([Body] Other body, JsonTypeInfo<Receipt> info);""", MatchesNeither),
        ("""[Post("/a")] Task A([Body(BodySerializationMethod.UrlEncoded)] Order order, JsonTypeInfo<Order> info);""", MatchesNeither),
        ("""[Post("/a")] Task A([Body(BodySerializationMethod.JsonLines)] List<Order> order, JsonTypeInfo<List<Order>> info);""", MatchesNeither),
        ("""[Multipart] [Post("/a")] Task A(Order order, JsonTypeInfo<Order> info);""", MatchesNeither),
        ("""[Post("/a")] Task<Order> A([Body] Order order, JsonTypeInfo<Order> first, JsonTypeInfo<Order> second);""", "more than one JsonTypeInfo<global::TypeInfoTest.Order> parameter"),
        ("""[Get("/a\\b")] Task<Order> A(JsonTypeInfo<Order> info);""", NotInline),
    ];

    /// <summary>Verifies every method with a usable metadata parameter generates output that compiles and raises no RF014.</summary>
    /// <param name="method">The method declaration or declarations to place on the interface.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [MethodDataSource(nameof(ValidMethods))]
    public async Task ValidMethod_GeneratesCompilableOutput(string method)
    {
        var result = Fixture.RunGenerator(BuildSource(method), generatedRequestBuilding: true);

        using (Assert.Multiple())
        {
            await Assert.That(result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray()).DoesNotContain(InvalidJsonTypeInfoId);
            await Assert.That(result.CompilationErrors.Select(static error => error.ToString()).ToArray()).IsEmpty();
        }
    }

    /// <summary>Verifies every unusable metadata parameter is reported once with its reason.</summary>
    /// <param name="method">The method declaration to place on the interface.</param>
    /// <param name="reason">Text the diagnostic message must contain.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [MethodDataSource(nameof(InvalidMethods))]
    public async Task InvalidMethod_IsReportedWithItsReason(string method, string reason)
    {
        var result = Fixture.RunGenerator(BuildSource(method), generatedRequestBuilding: true);

        var messages = result.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Id == InvalidJsonTypeInfoId)
            .Select(static diagnostic => diagnostic.GetMessage())
            .ToArray();
        using (Assert.Multiple())
        {
            await Assert.That(messages.Length).IsEqualTo(1);
            await Assert.That(messages[0]).Contains(reason);
        }
    }

    /// <summary>Verifies a metadata parameter is reported when generated request building is switched off.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedRequestBuildingOff_ReportsTheMetadataParameter()
    {
        var result = Fixture.RunGenerator(
            BuildSource("""[Get("/a")] Task<Order> A(JsonTypeInfo<Order> info);"""),
            generatedRequestBuilding: false);

        var messages = result.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Id == InvalidJsonTypeInfoId)
            .Select(static diagnostic => diagnostic.GetMessage())
            .ToArray();
        using (Assert.Multiple())
        {
            await Assert.That(messages.Length).IsEqualTo(1);
            await Assert.That(messages[0]).Contains("generated request building is switched off");
        }
    }

    /// <summary>Verifies switching generated request building off reports nothing for a method without a metadata parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedRequestBuildingOff_ReportsNothingWithoutAMetadataParameter()
    {
        var result = Fixture.RunGenerator(
            BuildSource("""[Get("/a")] Task<Order> A(int id);"""),
            generatedRequestBuilding: false);

        await Assert.That(result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray()).DoesNotContain(InvalidJsonTypeInfoId);
    }

    /// <summary>Verifies a type that only shares the name of the metadata type is an ordinary parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForeignJsonTypeInfo_IsNotTreatedAsMetadata()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace Elsewhere;

            public sealed class JsonTypeInfo<T>
            {
                public string? Text { get; set; }
            }

            public sealed class Order
            {
                public string? Id { get; set; }
            }

            public interface IApi
            {
                [Get("/a")]
                Task<Order> A([Query] JsonTypeInfo<Order> filter);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        using (Assert.Multiple())
        {
            await Assert.That(result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray()).DoesNotContain(InvalidJsonTypeInfoId);
            await Assert.That(result.GeneratedSources.Values.Any(static generated => generated.Contains("SetRequestJsonTypeInfo", StringComparison.Ordinal))).IsFalse();
        }
    }

    /// <summary>Verifies the body and the reply each receive their own metadata in the emitted code.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BodyAndReplyMetadata_AreEmittedIntoTheRequest()
    {
        var result = Fixture.RunGenerator(
            BuildSource("""[Post("/a")] Task<Receipt> A([Body] Order order, JsonTypeInfo<Order> orderInfo, JsonTypeInfo<Receipt> receiptInfo);"""),
            generatedRequestBuilding: true);

        var generated = string.Concat(result.GeneratedSources.Values);
        using (Assert.Multiple())
        {
            await Assert.That(generated).Contains("CreateBodyContent<global::TypeInfoTest.Order>(");
            await Assert.That(generated).Contains("@orderInfo,");
            await Assert.That(generated).Contains("SetRequestJsonTypeInfo<global::TypeInfoTest.Receipt>(refitRequest, @receiptInfo);");
            await Assert.That(generated.Contains("SetRequestJsonTypeInfo<global::TypeInfoTest.Order>", StringComparison.Ordinal)).IsFalse();
        }
    }

    /// <summary>Verifies a body without metadata keeps the lookup-based serialization call.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BodyWithoutMetadata_KeepsTheLookupBasedCall()
    {
        var result = Fixture.RunGenerator(
            BuildSource("""[Post("/a")] Task<Order> A([Body] Order order);"""),
            generatedRequestBuilding: true);

        var generated = string.Concat(result.GeneratedSources.Values);
        using (Assert.Multiple())
        {
            await Assert.That(generated).Contains("CreateBodyContent<global::TypeInfoTest.Order>(");
            await Assert.That(generated.Contains("SetRequestJsonTypeInfo", StringComparison.Ordinal)).IsFalse();
        }
    }

    /// <summary>Verifies a metadata parameter that describes the reply is emitted for a streamed reply.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamedReplyMetadata_IsEmittedIntoTheRequest()
    {
        var result = Fixture.RunGenerator(
            BuildSource("""[Get("/a")] IAsyncEnumerable<Order> A(JsonTypeInfo<Order> info);"""),
            generatedRequestBuilding: true);

        await Assert.That(string.Concat(result.GeneratedSources.Values)).Contains("SetRequestJsonTypeInfo<global::TypeInfoTest.Order>(refitRequest, @info);");
    }

    /// <summary>Wraps method declarations in a compilable source with the scenario types.</summary>
    /// <param name="methods">The method declarations to place on the interface.</param>
    /// <returns>The complete source.</returns>
    private static string BuildSource(string methods) =>
        $$"""
        {{Prelude}}

        public interface IApi
        {
        {{methods}}
        }
        """;
}
