// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests;

/// <summary>Generator tests covering which <c>[Paged]</c> methods compile and why an invalid one is reported.</summary>
public class PagedReturnGenerationTests
{
    /// <summary>The diagnostic reported for a misconfigured paged method.</summary>
    private const string InvalidPagedMethodId = "RF013";

    /// <summary>The diagnostic reported when a source-generation-only attribute cannot be honored.</summary>
    private const string SourceGenOnlyAttributeId = "RF007";

    /// <summary>The reason reported for a member that does not exist on the page.</summary>
    private const string MissingMember = "'Missing' is not a readable";

    /// <summary>The reason reported for a link method that does not state exactly one origin setting.</summary>
    private const string OneOriginReason = "exactly one of Origins, SameOrigin or AnyOrigin";

    /// <summary>The reason reported for a header continuation on a page that is not a response.</summary>
    private const string HeaderNeedsResponse = "reading a header needs";

    /// <summary>The page and item types the scenario methods are declared over.</summary>
    private const string Prelude =
        """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace PagedTest;

        public sealed class Item
        {
            public string? Id { get; set; }
        }

        public sealed class Meta
        {
            public string? NextCursor { get; set; }
        }

        public sealed class Page
        {
            public List<Item>? Items { get; set; }

            public string? Next { get; set; }

            public int? NextOffset { get; set; }

            public int Total { get; set; }

            public long? LongTotal { get; set; }

            public string? NextLink { get; set; }

            public Uri? NextUri { get; set; }

            public int Count { get; set; }

            public string Label { get; set; } = string.Empty;

            public Meta? Meta { get; set; }

            private string? Hidden { get; set; }
        }

        public sealed class FieldPage
        {
            public List<Item>? Entries;

            public string? Cursor;
        }

        public sealed class TwoLists
        {
            public List<Item>? First { get; set; }

            public Item[]? Second { get; set; }

            public string? Next { get; set; }
        }

        public sealed class NoLists
        {
            public string? Next { get; set; }
        }

        public sealed class SequencePage
        {
            public IEnumerable<Item>? Entries { get; set; }

            public string? Next { get; set; }
        }

        public sealed class IndexerPage
        {
            public Item this[int index] => new Item();

            public string? WriteOnly { set { } }

            public List<Item>? Items { get; set; }

            public string? Next { get; set; }
        }

        public interface IPageBase
        {
            List<Item>? Items { get; }
        }

        public interface IDerivedPage : IPageBase
        {
            string? Next { get; }
        }
        """;

    /// <summary>Gets paged methods that generate, each with a description of what it exercises.</summary>
    /// <returns>The method declarations.</returns>
    public static IEnumerable<string> ValidMethods() =>
    [
        """[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Items = "Items", Next = "Meta.NextCursor")] PagedEnumerable<Page, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Total = "Total")] PagedEnumerable<Page, Item> A([PageToken] int offset);""",
        """[Get("/a")] [Paged(Total = "LongTotal")] PagedEnumerable<Page, Item> A([PageToken] int offset);""",
        """[Get("/a")] [Paged(Next = "NextOffset")] PagedEnumerable<Page, Item> A([PageToken] int offset);""",
        """[Get("/a")] [Paged(Next = "NextOffset")] PagedEnumerable<Page, Item> A([PageToken] int? offset);""",
        """[Get("/a")] [Paged(Next = "NextLink", Origins = new[] { "https://api.example.com" })] PagedEnumerable<Page, Item> A();""",
        """[Get("/a")] [Paged(Next = "NextUri", SameOrigin = true)] PagedEnumerable<Page, Item> A();""",
        """[Get("/a")] [Paged(Items = "Content.Items", NextHeader = "Link", AnyOrigin = true)] PagedEnumerable<ApiResponse<Page>, Item> A();""",
        """[Get("/a")] [Paged(NextHeader = "x-continuation")] PagedEnumerable<ApiResponse<Page>, Item> A([PageToken, Header("x-continuation")] string? token);""",
        """[Get("/a")] [Paged(NextHeader = "Link", SameOrigin = true)] PagedEnumerable<ApiResponse<List<Item>>, Item> A();""",
        """[Get("/a")] [Paged(Next = "Content.Next")] PagedEnumerable<ApiResponse<Page>, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Items = "Second", Next = "Next")] PagedEnumerable<TwoLists, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Next = "Cursor")] PagedEnumerable<FieldPage, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Items = "Entries", Next = "Cursor")] PagedEnumerable<FieldPage, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<SequencePage, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<IndexerPage, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<IDerivedPage, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Items = " ", Next = "Next", SameOrigin = false)] PagedEnumerable<Page, Item> A([PageToken] string? token);""",
        """[Get("/a")] [Paged(Next = "NextLink", SameOrigin = false, AnyOrigin = true)] PagedEnumerable<Page, Item> A();""",
        """[Get("/a/{token}")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string token);""",
        """[Post("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A([Body] Item filter, [PageToken] string? token);""",
        """
        [Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string? token);
        [Get("/b")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A(string other, [PageToken] string? token);
        """,
        """[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> @class([PageToken] string? @string);""",
    ];

    /// <summary>Gets misconfigured paged methods with the reason expected in the diagnostic.</summary>
    /// <returns>The method declarations and the text the diagnostic must contain.</returns>
    public static IEnumerable<(string Method, string Reason)> InvalidMethods() =>
    [
        ("""[Get("/a")] PagedEnumerable<Page, Item> A([PageToken] string? token);""", "add a [Paged] attribute"),
        ("""[Get("/a")] [Paged(Next = "Next")] Task<Page> A([PageToken] string? token);""", "applies only to a method that returns PagedEnumerable"),
        ("""[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A<T>([PageToken] string? token);""", "cannot be generic"),
        ("""[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string? token, CancellationToken cancellationToken);""", "cannot declare a CancellationToken"),
        ("""[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string? first, [PageToken] string? second);""", "only one parameter can be marked [PageToken]"),
        ("""[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<NoLists, Item> A([PageToken] string? token);""", "has no member that is a sequence"),
        ("""[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<TwoLists, Item> A([PageToken] string? token);""", "more than one member"),
        ("""[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, string> A([PageToken] string? token);""", "has no member that is a sequence of 'string'"),
        ("""[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, char> A([PageToken] string? token);""", "has no member that is a sequence of 'char'"),
        ("""[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Item[], Item> A([PageToken] string? token);""", "has no member that is a sequence of"),
        ("""[Get("/a")] [Paged(NextHeader = "x-continuation")] PagedEnumerable<IApiResponse, Item> A([PageToken] string? token);""", "has no member that is a sequence"),
        ("""[Get("/a")] [Paged(Next = "NextLink", Origins = new string[0])] PagedEnumerable<Page, Item> A();""", OneOriginReason),
        ("""[Get("/a")] [Paged(Next = "NextLink", Origins = new[] { " " })] PagedEnumerable<Page, Item> A();""", OneOriginReason),
        ("""[Get("/a")] [Paged(Items = "Missing", Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string? token);""", MissingMember),
        ("""[Get("/a")] [Paged(Items = "Hidden", Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string? token);""", "'Hidden' is not a readable"),
        ("""[Get("/a")] [Paged(Items = "Next", Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string? token);""", "is not a sequence of"),
        ("""[Get("/a")] [Paged] PagedEnumerable<Page, Item> A([PageToken] string? token);""", "name exactly one of Next, NextHeader or Total"),
        ("""[Get("/a")] [Paged(Next = "Next", Total = "Total")] PagedEnumerable<Page, Item> A([PageToken] string? token);""", "name exactly one of Next, NextHeader or Total"),
        ("""[Get("/a")] [Paged(Next = "Count")] PagedEnumerable<Page, Item> A([PageToken] int offset);""", "must be nullable"),
        ("""[Get("/a")] [Paged(Next = "NextOffset")] PagedEnumerable<Page, Item> A([PageToken] string? token);""", "but the [PageToken] parameter is a"),
        ("""[Get("/a")] [Paged(Next = "Meta.Missing")] PagedEnumerable<Page, Item> A([PageToken] string? token);""", MissingMember),
        ("""[Get("/a")] [Paged(NextHeader = "x-continuation")] PagedEnumerable<Page, Item> A([PageToken] string? token);""", HeaderNeedsResponse),
        ("""[Get("/a")] [Paged(NextHeader = "x-continuation")] PagedEnumerable<ApiResponse<Page>, Item> A([PageToken] int token);""", "must be a string"),
        ("""[Get("/a")] [Paged(Total = "Total")] PagedEnumerable<Page, Item> A([PageToken] string? token);""", "must be an int offset"),
        ("""[Get("/a")] [Paged(Total = "Next")] PagedEnumerable<Page, Item> A([PageToken] int offset);""", "must be an int or a long"),
        ("""[Get("/a")] [Paged(Total = "Missing")] PagedEnumerable<Page, Item> A([PageToken] int offset);""", MissingMember),
        ("""[Get("/a")] [Paged(Next = "Next", SameOrigin = true)] PagedEnumerable<Page, Item> A([PageToken] string? token);""", "apply only to a method that follows links"),
        ("""[Get("/a")] [Paged(Next = "NextLink")] PagedEnumerable<Page, Item> A();""", OneOriginReason),
        ("""[Get("/a")] [Paged(Next = "NextLink", SameOrigin = true, AnyOrigin = true)] PagedEnumerable<Page, Item> A();""", OneOriginReason),
        ("""[Get("/a")] [Paged(Next = "NextLink", Origins = new[] { "not a url" })] PagedEnumerable<Page, Item> A();""", "is not an absolute http or https origin"),
        ("""[Get("/a")] [Paged(Next = "NextLink", Origins = new[] { "ftp://files.example.com" })] PagedEnumerable<Page, Item> A();""", "is not an absolute http or https origin"),
        ("""[Get("/a")] [Paged(Total = "Total", SameOrigin = true)] PagedEnumerable<Page, Item> A();""", "names exactly one of Next or NextHeader"),
        ("""[Get("/a")] [Paged(SameOrigin = true)] PagedEnumerable<Page, Item> A();""", "names exactly one of Next or NextHeader"),
        ("""[Get("/a")] [Paged(Next = "Count", SameOrigin = true)] PagedEnumerable<Page, Item> A();""", "must be a string or a Uri"),
        ("""[Get("/a")] [Paged(Next = "Missing", SameOrigin = true)] PagedEnumerable<Page, Item> A();""", MissingMember),
        ("""[Get("/a")] [Paged(NextHeader = "Link", SameOrigin = true)] PagedEnumerable<Page, Item> A();""", HeaderNeedsResponse),
    ];

    /// <summary>Verifies every valid paged method generates output that compiles and raises no paging diagnostic.</summary>
    /// <param name="method">The method declaration or declarations to place on the interface.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [MethodDataSource(nameof(ValidMethods))]
    public async Task ValidMethod_GeneratesCompilableOutput(string method)
    {
        var result = Fixture.RunGenerator(BuildSource(method), generatedRequestBuilding: true);

        var ids = result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray();
        using (Assert.Multiple())
        {
            await Assert.That(ids).DoesNotContain(InvalidPagedMethodId);
            await Assert.That(ids).DoesNotContain(SourceGenOnlyAttributeId);
            await Assert.That(result.CompilationErrors.Select(static error => error.ToString()).ToArray()).IsEmpty();
        }
    }

    /// <summary>Verifies every misconfigured paged method is reported with the reason it cannot be generated.</summary>
    /// <param name="method">The method declaration to place on the interface.</param>
    /// <param name="reason">Text the diagnostic message must contain.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [MethodDataSource(nameof(InvalidMethods))]
    public async Task InvalidMethod_IsReportedWithItsReason(string method, string reason)
    {
        var result = Fixture.RunGenerator(BuildSource(method), generatedRequestBuilding: true);

        var messages = result.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Id == InvalidPagedMethodId)
            .Select(static diagnostic => diagnostic.GetMessage())
            .ToArray();
        using (Assert.Multiple())
        {
            await Assert.That(messages.Length).IsEqualTo(1);
            await Assert.That(messages[0]).Contains(reason);
            await Assert.That(result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray()).DoesNotContain(SourceGenOnlyAttributeId);
        }
    }

    /// <summary>Verifies a paged method is reported when generated request building is switched off.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedRequestBuildingOff_ReportsThePagedAttribute()
    {
        var result = Fixture.RunGenerator(
            BuildSource("""[Get("/a")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string? token);"""),
            generatedRequestBuilding: false);

        var messages = result.GeneratorDiagnostics
            .Where(static diagnostic => diagnostic.Id == SourceGenOnlyAttributeId)
            .Select(static diagnostic => diagnostic.GetMessage())
            .ToArray();
        await Assert.That(messages.Length).IsEqualTo(1);
        await Assert.That(messages[0]).Contains("[PagedAttribute]");
    }

    /// <summary>Verifies a valid paged method whose request cannot be generated inline is reported through the attribute, not as misconfigured.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestThatCannotBeGeneratedInline_ReportsThePagedAttribute()
    {
        var result = Fixture.RunGenerator(
            BuildSource("""[Get("/a\\b")] [Paged(Next = "Next")] PagedEnumerable<Page, Item> A([PageToken] string? token);"""),
            generatedRequestBuilding: true);

        var ids = result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray();
        using (Assert.Multiple())
        {
            await Assert.That(ids).Contains(SourceGenOnlyAttributeId);
            await Assert.That(ids).DoesNotContain(InvalidPagedMethodId);
        }
    }

    /// <summary>Verifies a return type that merely shares the name of the paged sequence is not treated as one.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForeignPagedEnumerable_IsNotTreatedAsAPagedReturn()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace Elsewhere;

            public sealed class PagedEnumerable<TPage, TItem>;

            public interface IApi
            {
                [Get("/a")]
                PagedEnumerable<string, string> A();
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray()).DoesNotContain(InvalidPagedMethodId);
    }

    /// <summary>Verifies a paged method inherited from a base interface is implemented explicitly and compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedMethod_IsImplementedExplicitly()
    {
        const string source =
            $$"""
            {{Prelude}}

            public interface IBaseApi
            {
                [Get("/a")]
                [Paged(Next = "Next")]
                PagedEnumerable<Page, Item> A([PageToken] string? token);
            }

            public interface IApi : IBaseApi
            {
                [Get("/b")]
                Task<string> B();
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        using (Assert.Multiple())
        {
            await Assert.That(result.CompilationErrors.Select(static error => error.ToString()).ToArray()).IsEmpty();
            await Assert.That(result.GeneratedSources.Values.Any(static generated => generated.Contains("global::PagedTest.IBaseApi.A(", StringComparison.Ordinal))).IsTrue();
        }
    }

    /// <summary>Verifies a paged method on a generic interface generates output that compiles.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericInterface_GeneratesCompilableOutput()
    {
        const string source =
            """
            using System.Collections.Generic;
            using Refit;

            namespace PagedGeneric;

            public sealed class Envelope<T>
            {
                public List<T>? Items { get; set; }

                public string? Next { get; set; }
            }

            public interface IApi<TItem>
            {
                [Get("/a")]
                [Paged(Next = "Next")]
                PagedEnumerable<Envelope<TItem>, TItem> A([PageToken] string? token);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        using (Assert.Multiple())
        {
            await Assert.That(result.CompilationErrors.Select(static error => error.ToString()).ToArray()).IsEmpty();
            await Assert.That(result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray()).DoesNotContain(InvalidPagedMethodId);
        }
    }

    /// <summary>Verifies a paged method generates output that compiles at the C# 7.3 language version.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CSharp73Baseline_GeneratesCompilableOutput()
    {
        const string source =
            """
            using System.Collections.Generic;
            using Refit;

            namespace PagedOld
            {
                public class Item
                {
                    public string Id { get; set; }
                }

                public class Page
                {
                    public List<Item> Items { get; set; }

                    public string Next { get; set; }

                    public int? NextOffset { get; set; }

                    public int Total { get; set; }

                    public string NextLink { get; set; }
                }

                public interface IApi
                {
                    [Get("/a")]
                    [Paged(Next = "Next")]
                    PagedEnumerable<Page, Item> A([PageToken] string token);

                    [Get("/b")]
                    [Paged(Next = "NextOffset")]
                    PagedEnumerable<Page, Item> B([PageToken] int offset);

                    [Get("/c")]
                    [Paged(Total = "Total")]
                    PagedEnumerable<Page, Item> C([PageToken] int offset);

                    [Get("/d")]
                    [Paged(Next = "NextLink", SameOrigin = true)]
                    PagedEnumerable<Page, Item> D();
                }
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true, LanguageVersion.CSharp7_3);

        await Assert.That(result.CompilationErrors.Select(static error => error.ToString()).ToArray()).IsEmpty();
        await Assert.That(result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray()).DoesNotContain(InvalidPagedMethodId);
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
