// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.GeneratorTests;

/// <summary>
/// Compiles, loads and invokes generated query request building, asserting the final <see cref="Uri"/> and its
/// parity with the reflection request builder for every query parameter shape that generates inline.
/// </summary>
public sealed partial class QueryRequestBuildingLiveTests
{
    /// <summary>A sample integer query value.</summary>
    private const int SecondValue = 2;

    /// <summary>A sample page number.</summary>
    private const int PageSeven = 7;

    /// <summary>A sample 64-bit identifier beyond the 32-bit range, exercising the long span-formatted fast write.</summary>
    private const long LargeIdentifier = 9_000_000_000L;

    /// <summary>A sample document revision used by the dotted-path scenario.</summary>
    private const int DocRevision = 7;

    /// <summary>A sample price formatted with two decimals.</summary>
    private const double PriceFive = 5D;

    /// <summary>A sample raw double for TreatAsString.</summary>
    private const double RawDouble = 1.5D;

    /// <summary>The enum-valued query method exercised across parity scenarios.</summary>
    private const string SortedMethodName = "Sorted";

    /// <summary>The multi-expanded collection query method exercised across parity scenarios.</summary>
    private const string ExpandedMethodName = "Expanded";

    /// <summary>The full name of the compiled scenario enum type.</summary>
    private const string SearchSortTypeName = "Refit.LiveQuery.SearchSort";

    /// <summary>A value with a space and a slash, exercising percent-encoding of both reserved characters.</summary>
    private const string EscapableValue = "a b/c";

    /// <summary>The lower bound of the formatted complex query-object property scenario.</summary>
    private const int WindowMin = 1;

    /// <summary>The upper bound of the formatted complex query-object property scenario.</summary>
    private const int WindowMax = 9;

    /// <summary>Csv-joined identifiers.</summary>
    private static readonly int[] CsvIds = [1, 2, 3];

    /// <summary>Multi-expanded identifiers.</summary>
    private static readonly int[] ExpandIds = [1, 2];

    /// <summary>Pipe-joined values.</summary>
    private static readonly string[] PipeValues = ["a", "b"];

    /// <summary>Identifiers expanded with the settings default collection format.</summary>
    private static readonly List<int> ListIds = [4, 5];

    /// <summary>Repeated valueless flag values.</summary>
    private static readonly string[] FlagValues = ["a", "b", "c"];

    /// <summary>An empty identifier collection.</summary>
    private static readonly int[] EmptyIds = [];

    /// <summary>A sample timestamp for invariant formatting parity.</summary>
    private static readonly DateTimeOffset SampleTimestamp = new(2026, 7, 4, 12, 30, 0, TimeSpan.Zero);

    /// <summary>Verifies generated query URIs match the reflection builder for scalar shapes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task ScalarQueryParametersMatchReflection()
    {
        using var harness = LiveQueryHarness.Create();

        await harness.AssertParityAsync("Plain", [EscapableValue], "/base/search?q=a%20b%2Fc");
        await harness.AssertParityAsync("Alias", ["me", "beta"], "/base/signin?login=me&kind=beta");
        await harness.AssertParityAsync("Multiple", ["x", SecondValue, true], "/base/multi?a=x&b=2&c=True");
        await harness.AssertParityAsync("NullSkip", [null, "kept"], "/base/nullskip?b=kept");
        await harness.AssertParityAsync("Paged", [PageSeven], "/base/page?page=7");
        await harness.AssertParityAsync("Paged", [null], "/base/page");
        await harness.AssertParityAsync("Big", [LargeIdentifier], "/base/big?id=9000000000");
        await harness.AssertParityAsync("Templated", ["two"], "/base/tmpl?fixed=1&extra=two");
    }

    /// <summary>Verifies generated dotted <c>{param.Property}</c> path URIs match the reflection builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task DottedPathParametersMatchReflection()
    {
        using var harness = LiveQueryHarness.Create();

        var info = harness.CreateApiValue("Refit.LiveQuery.RouteInfo", ("Slug", EscapableValue), ("Version", DocRevision));
        _ = await harness.AssertParityAsync("DottedPath", [info], "/base/docs/a%20b%2Fc/rev/7");

        // Only Slug binds to the path; Version is a residual property flattened into the query, exactly as the
        // reflection builder splits a path-bound object between the path and the query string.
        _ = await harness.AssertParityAsync("DottedPathResidual", [info], "/base/tags/a%20b%2Fc?Version=7");
    }

    /// <summary>Verifies a multi-level dotted <c>{order.Customer.Id}</c> path binds the nested property chain and matches
    /// the reflection builder: the top-level property is consumed by the path (residual sibling flattens into the query),
    /// and a null intermediate renders an empty segment instead of throwing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task NestedPathPropertyMatchesReflection()
    {
        using var harness = LiveQueryHarness.Create();

        var customer = harness.CreateApiValue("Refit.LiveQuery.NestedCustomer", ("Id", EscapableValue));
        var order = harness.CreateApiValue("Refit.LiveQuery.NestedOrder", ("Customer", customer), ("Note", "hi"));
        _ = await harness.AssertParityAsync("NestedPath", [order], "/base/orders/a%20b%2Fc?Note=hi");

        // A null intermediate short-circuits the chain to an empty segment, matching the reflection builder's walk.
        var missing = harness.CreateApiValue("Refit.LiveQuery.NestedOrder", ("Customer", null), ("Note", "x"));
        _ = await harness.AssertParityAsync("NestedPath", [missing], "/base/orders/?Note=x");
    }

    /// <summary>Verifies a sealed <c>ToString</c>-only type in a <c>{token}</c> slot matches the reflection builder: the
    /// generated inline path calls the same URL parameter formatter (ultimately <c>ToString</c>) the reflection path does.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task SealedToStringPathParameterMatchesReflection()
    {
        using var harness = LiveQueryHarness.Create();

        var token = harness.CreateApiValue("Refit.LiveQuery.RouteToken", ("Value", EscapableValue));
        _ = await harness.AssertParityAsync("TokenPath", [token], "/base/token/a%20b%2Fc");
    }

    /// <summary>Verifies a <c>[QueryUriFormat(UriFormat.Unescaped)]</c> method matches the reflection builder: the whole
    /// path and query is re-encoded with the attribute's format, decoding the escapes the query builder emitted.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task QueryUriFormatMatchesReflection()
    {
        using var harness = LiveQueryHarness.Create();

        // The exact captured form depends on Uri normalization applied identically to both paths, so assert parity only.
        _ = await harness.AssertParityAsync("UnescapedQuery", ["Select Id From Account"], null);
    }

    /// <summary>Verifies a <c>[Query(Format)]</c> on a complex query-object property matches the reflection builder: the
    /// whole value is rendered as a single pair through the form formatter, not flattened into its own properties.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task FormattedComplexQueryPropertyMatchesReflection()
    {
        using var harness = LiveQueryHarness.Create();

        var bounds = harness.CreateApiValue("Refit.LiveQuery.Bounds", ("Min", WindowMin), ("Max", WindowMax));
        var query = harness.CreateApiValue("Refit.LiveQuery.RangeQuery", ("Window", bounds));
        _ = await harness.AssertParityAsync("RangeSearch", [query], "/base/range?Window=1..9");
    }

    /// <summary>Verifies a nullable value-type (struct) query object matches the reflection builder: a present value
    /// reaches its properties through <c>.Value</c> inside the parameter's <c>HasValue</c> guard, and a null value is
    /// omitted entirely.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task NullableStructQueryObjectMatchesReflection()
    {
        using var harness = LiveQueryHarness.Create();

        // A present nullable struct flattens its underlying properties; parity guards the exact key order and encoding.
        var point = harness.CreateApiValue("Refit.LiveQuery.GeoPoint", ("Name", "peak"), ("Lat", RawDouble));
        _ = await harness.AssertParityAsync("NullableStructQuery", [point], null);

        // A null nullable-struct parameter contributes no query pairs, matching the reflection builder.
        _ = await harness.AssertParityAsync("NullableStructQuery", [null], "/base/point");
    }

    /// <summary>Verifies a dictionary of a concrete (non-sealed) complex value type flattens each entry under the entry key, matching
    /// the reflection builder's per-value nested map (<c>key.Property=value</c>).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task DictionaryOfConcreteValuesMatchesReflection()
    {
        using var harness = LiveQueryHarness.Create();

        var facet = harness.CreateApiValue("Refit.LiveQuery.Facet", ("Name", "blue"), ("Count", WindowMin));
        var facets = harness.CreateStringKeyedDictionary("Refit.LiveQuery.Facet", ("color", facet));
        _ = await harness.AssertParityAsync("Facets", [facets], "/base/facets?color.Name=blue&color.Count=1");
    }

    /// <summary>Verifies a custom HTTP QUERY verb attribute (a draft-standard body-carrying method) generates inline and
    /// matches the reflection builder: the request uses the custom verb, carries an explicit body, and - since the verb is
    /// not yet body-capable - flattens an un-attributed complex parameter into the query, all at parity.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task QueryVerbMatchesReflection()
    {
        const string queryVerb = "QUERY";
        using var harness = LiveQueryHarness.Create();

        // An explicit [Body] on the QUERY verb: the generated request uses the custom verb and carries the body.
        var payload = harness.CreateApiValue("Refit.LiveQuery.CreatePayload", ("Name", "report"));
        var generated = await harness.InvokeGeneratedAsync("QueryDocuments", [payload], "/base/documents");
        await Assert.That(generated.Method.Method).IsEqualTo(queryVerb);
        await Assert.That(harness.LastCapturedContent).IsNotNull();
        _ = await harness.AssertParityAsync("QueryDocuments", [payload], "/base/documents");

        // The verb is not body-capable for an un-attributed complex parameter yet, so both paths flatten it into the query.
        var bounds = harness.CreateApiValue("Refit.LiveQuery.Bounds", ("Min", WindowMin), ("Max", WindowMax));
        var filter = harness.CreateApiValue("Refit.LiveQuery.RangeQuery", ("Window", bounds));
        var rows = await harness.AssertParityAsync("QueryRows", [filter], "/base/rows?Window=1..9");
        await Assert.That(rows.Method.Method).IsEqualTo(queryVerb);
    }

    /// <summary>Verifies generated query URIs match the reflection builder for formatted values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task FormattedQueryParametersMatchReflection()
    {
        using var harness = LiveQueryHarness.Create();

        await harness.AssertParityAsync("Formatted", [PriceFive], "/base/fmt?price=5.00");
        await harness.AssertParityAsync(SortedMethodName, [harness.CreateEnumValue(SearchSortTypeName, 0)], "/base/enum?sort=date-desc");
        await harness.AssertParityAsync(SortedMethodName, [harness.CreateEnumValue(SearchSortTypeName, 1)], "/base/enum?sort=Name");
        await harness.AssertParityAsync("Treated", [RawDouble], null);
        await harness.AssertParityAsync("When", [SampleTimestamp], null);
    }

    /// <summary>Verifies generated query URIs match the reflection builder for collection expansion.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task CollectionQueryParametersMatchReflection()
    {
        using var harness = LiveQueryHarness.Create();

        await harness.AssertParityAsync("Csv", [CsvIds], "/base/csv?ids=1%2C2%2C3");
        await harness.AssertParityAsync(ExpandedMethodName, [ExpandIds], "/base/expand?ids=1&ids=2");
        await harness.AssertParityAsync("Pipes", [PipeValues], "/base/pipes?values=a%7Cb");
        await harness.AssertParityAsync("DefaultList", [ListIds], "/base/list?ids=4%2C5");
        await harness.AssertParityAsync("Csv", [EmptyIds], "/base/csv?ids=");
        await harness.AssertParityAsync(ExpandedMethodName, [EmptyIds], "/base/expand");
    }

    /// <summary>Verifies generated query URIs match the reflection builder for collection expansion.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task IndexedCollectionQueryParametersMatchReflection()
    {
        using var harness = LiveQueryHarness.Create();

        const string id = "Id";
        const string value = "Value";
        const string parameter = "items";
        const string indexedSearchMethodName = "IndexedSearch";
        const string typeName = "Refit.LiveQuery.Item";
        await harness.AssertParityAsync(indexedSearchMethodName, [null], "/base/indexed");
        var item0 = harness.CreateApiValue(typeName, (id, 1), (value, "a"));
        var indexedList = harness.CreateApiList(typeName, item0);
        _ = await harness.AssertParityAsync(indexedSearchMethodName, [indexedList], $"/base/indexed?{parameter}[0].{id}=1&{parameter}[0].{value}=a");
    }

    /// <summary>Verifies a custom URL parameter formatter still runs for every generated value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task CustomFormatterMatchesReflection()
    {
        var settings = new RefitSettings { UrlParameterFormatter = new UpperCaseUrlParameterFormatter() };
        using var harness = LiveQueryHarness.Create(settings);

        await harness.AssertParityAsync("Plain", ["abc"], "/base/search?q=ABC");
        await harness.AssertParityAsync(ExpandedMethodName, [ExpandIds], null);
        await harness.AssertParityAsync(SortedMethodName, [harness.CreateEnumValue(SearchSortTypeName, 0)], "/base/enum?sort=DATE-DESC");
    }

    /// <summary>Verifies POST methods combine an implicit body with generated query parameters.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task ImplicitBodyWithQueryMatchesReflection()
    {
        using var harness = LiveQueryHarness.Create();

        var payload = harness.CreateApiValue("Refit.LiveQuery.CreatePayload", ("Name", "Widget"));
        _ = await harness.AssertParityAsync("Create", [payload, "new"], "/base/create?tag=new");

        await Assert.That(harness.LastCapturedContent).IsNotNull();
        await Assert.That(harness.LastCapturedContent!).Contains("Widget");
    }

    /// <summary>Verifies source-generation-only query flags render as valueless segments.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task QueryNameFlagsRenderValuelessSegments()
    {
        using var harness = LiveQueryHarness.Create();

        _ = await harness.InvokeGeneratedAsync("Flag", ["ready"], "/base/flags?ready");
        _ = await harness.InvokeGeneratedAsync("Flag", ["needs escape"], "/base/flags?needs%20escape");
        _ = await harness.InvokeGeneratedAsync("Flag", [null], "/base/flags");
        _ = await harness.InvokeGeneratedAsync("Flags", [FlagValues], "/base/flags/many?a&b&c");
    }

    /// <summary>Verifies source-generation-only encoded values pass through verbatim.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task EncodedValuesPassThroughVerbatim()
    {
        using var harness = LiveQueryHarness.Create();

        _ = await harness.InvokeGeneratedAsync("EncodedQuery", ["a%2Fb%20c"], "/base/encq?v=a%2Fb%20c");
        _ = await harness.InvokeGeneratedAsync("EncodedPath", ["0001%40zoho.com"], "/base/encp/0001%40zoho.com");
        _ = await harness.InvokeGeneratedAsync(
            "EncodedRoundTrip",
            ["9000/events/3bf@zoho.com"],
            "/base/cal/9000/events/3bf@zoho.com");
    }
}
