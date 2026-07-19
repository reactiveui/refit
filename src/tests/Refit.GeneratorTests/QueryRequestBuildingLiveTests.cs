// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

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

    /// <summary>The method name for the Indexed collection scenario.</summary>
    private const string IndexedSearchMethodName = "IndexedSearch";

    /// <summary>The full type name of the <c>Item</c> scenario type used by the Indexed test.</summary>
    private const string IndexedItemTypeName = "Refit.LiveQuery.Item";

    /// <summary>The name of the Indexed parameter.</summary>
    private const string IndexedParameterName = "items";

    /// <summary>The name of the Id property on <c>Item</c>.</summary>
    private const string IndexedIdPropertyName = "Id";

    /// <summary>The name of the Value property on <c>Item</c>.</summary>
    private const string IndexedValuePropertyName = "Value";

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

        const string id = IndexedIdPropertyName;
        const string value = IndexedValuePropertyName;
        const string parameter = IndexedParameterName;
        await harness.AssertParityAsync(IndexedSearchMethodName, [null], "/base/indexed");
        var item0 = harness.CreateApiValue(IndexedItemTypeName, (id, 1), (value, "a"));
        var indexedList = harness.CreateApiList(IndexedItemTypeName, item0);
        await harness.AssertParityAsync(IndexedSearchMethodName, [indexedList], $"/base/indexed?{parameter}%5B0%5D.{id}=1&{parameter}%5B0%5D.{value}=a");
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

    /// <summary>Formats every value through the default rules and upper-cases the result.</summary>
    private sealed class UpperCaseUrlParameterFormatter : DefaultUrlParameterFormatter
    {
        /// <inheritdoc/>
        public override string? Format(
            object? value,
            System.Reflection.ICustomAttributeProvider attributeProvider,
            Type type) =>
            base.Format(value, attributeProvider, type)?.ToUpperInvariant();
    }

    /// <summary>Hosts one compiled generated client plus the reflection builder for parity assertions.</summary>
    /// <param name="context">The collectible load context holding the compiled assembly.</param>
    /// <param name="handler">The capturing message handler.</param>
    /// <param name="client">The HTTP client shared by both request paths.</param>
    /// <param name="interfaceType">The compiled Refit interface type.</param>
    /// <param name="generatedApi">The generated client instance.</param>
    /// <param name="requestBuilder">The reflection request builder for the compiled interface.</param>
    private sealed class LiveQueryHarness(
        CollectibleAssemblyLoadContext context,
        CapturingHandler handler,
        HttpClient client,
        Type interfaceType,
        object generatedApi,
        IRequestBuilder requestBuilder) : IDisposable
    {
        /// <summary>The base address the relative request URIs resolve against.</summary>
        private const string BaseAddress = "https://example.test/base/";

        /// <summary>The interface source compiled through the generator for every scenario.</summary>
        private const string ApiSource =
            """
            using System;
            using System.Collections.Generic;
            using System.Runtime.Serialization;
            using System.Threading.Tasks;
            using Refit;

            namespace Refit.LiveQuery;

            public enum SearchSort
            {
                [EnumMember(Value = "date-desc")]
                DateDescending,
                Name,
            }

            public sealed class CreatePayload
            {
                public string? Name { get; set; }
            }

            public sealed class RouteInfo
            {
                public string? Slug { get; set; }

                public int Version { get; set; }
            }

            public sealed class NestedCustomer
            {
                public string? Id { get; set; }
            }

            public sealed class NestedOrder
            {
                public NestedCustomer? Customer { get; set; }

                public string? Note { get; set; }
            }

            public sealed class RouteToken
            {
                public string? Value { get; set; }

                public override string ToString() => Value ?? string.Empty;
            }

            public sealed class Bounds
            {
                public int Min { get; set; }

                public int Max { get; set; }

                public override string ToString() => Min + ".." + Max;
            }

            public sealed class RangeQuery
            {
                [Query(Format = "g")]
                public Bounds? Window { get; set; }
            }

            // Deliberately not sealed: exercises the concrete (non-sealed) declared-type flatten. The test value is not a
            // subtype, so the declared-type flatten matches the reflection builder's runtime-type flatten exactly.
            public class Facet
            {
                public string? Name { get; set; }

                public int Count { get; set; }
            }

            // The HTTP QUERY method (currently a draft standard): a custom verb attribute carrying a body.
            public sealed class QueryVerbAttribute : HttpMethodAttribute
            {
                public QueryVerbAttribute(string path) : base(path) { }

                public override System.Net.Http.HttpMethod Method => new System.Net.Http.HttpMethod("QUERY");
            }

            public sealed class Item
            {
                public int Id { get; set; }

                public string? Value { get; set; }
            }

            public interface ILiveQueryApi
            {
                [Get("/search")]
                Task<string> Plain(string q);

                [Get("/token/{token}")]
                Task<string> TokenPath(RouteToken token);

                [Get("/docs/{info.Slug}/rev/{info.Version}")]
                Task<string> DottedPath(RouteInfo info);

                [Get("/tags/{info.Slug}")]
                Task<string> DottedPathResidual(RouteInfo info);

                [Get("/orders/{order.Customer.Id}")]
                Task<string> NestedPath(NestedOrder order);

                [Get("/signin")]
                Task<string> Alias([AliasAs("login")] string user, [AliasAs("kind")] string kind);

                [Get("/multi")]
                Task<string> Multiple(string a, int b, bool c);

                [Get("/nullskip")]
                Task<string> NullSkip(string? a, string b);

                [Get("/fmt")]
                Task<string> Formatted([Query(Format = "0.00")] double price);

                [Get("/csv")]
                Task<string> Csv([Query(CollectionFormat.Csv)] int[] ids);

                [Get("/expand")]
                Task<string> Expanded([Query(CollectionFormat.Multi)] int[] ids);

                [Get("/pipes")]
                Task<string> Pipes([Query(CollectionFormat.Pipes)] string[] values);

                [Get("/list")]
                Task<string> DefaultList(List<int> ids);

                [Get("/enum")]
                Task<string> Sorted(SearchSort sort);

                [Get("/page")]
                Task<string> Paged(int? page);

                [Get("/big")]
                Task<string> Big(long id);

                [Get("/treat")]
                Task<string> Treated([Query(TreatAsString = true)] double raw);

                [Get("/tmpl?fixed=1")]
                Task<string> Templated(string extra);

                [QueryUriFormat(UriFormat.Unescaped)]
                [Get("/soql")]
                Task<string> UnescapedQuery(string q);

                [Get("/range")]
                Task<string> RangeSearch([Query] RangeQuery query);

                [Get("/facets")]
                Task<string> Facets(Dictionary<string, Facet> facets);

                [QueryVerb("/documents")]
                Task<string> QueryDocuments([Body] CreatePayload body);

                [QueryVerb("/rows")]
                Task<string> QueryRows([Query] RangeQuery filter);

                [Get("/when")]
                Task<string> When(DateTimeOffset at);

                [Post("/create")]
                Task<string> Create(CreatePayload payload, string tag);

                [Get("/flags")]
                Task<string> Flag([QueryName] string flag);

                [Get("/flags/many")]
                Task<string> Flags([QueryName] string[] flags);

                [Get("/encq")]
                Task<string> EncodedQuery([Encoded] string v);

                [Get("/encp/{id}")]
                Task<string> EncodedPath([Encoded] string id);

                [Get("/cal/{**rest}")]
                Task<string> EncodedRoundTrip([Encoded] string rest);

                [Get("/push/{deviceId}/{notifMsgId?}")]
                Task<string> TrailingOptional(string deviceId, string? notifMsgId);

                [Get("/indexed")]
                Task<string> IndexedSearch([Query(CollectionFormat.Indexed)] List<Item>? items);
            }
            """;

        /// <summary>Gets the body content captured for the most recent request, or null.</summary>
        public string? LastCapturedContent => handler.LastContent;

        /// <summary>Compiles the scenario interface and creates the generated and reflection clients.</summary>
        /// <param name="settings">The Refit settings, or null for defaults.</param>
        /// <returns>The live harness.</returns>
        [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
        public static LiveQueryHarness Create(RefitSettings? settings = null)
        {
            settings ??= new RefitSettings();
            var result = Fixture.RunGenerator(ApiSource, generatedRequestBuilding: true);
            if (!result.CompilesWithoutErrors)
            {
                throw new InvalidOperationException(
                    "Generated compilation failed: " + string.Join(Environment.NewLine, result.CompilationErrors));
            }

            var (assembly, loadContext) = Fixture.EmitAndLoad(result);
            var interfaceType = assembly.GetType("Refit.LiveQuery.ILiveQueryApi", throwOnError: true)!;
            var generatedType = assembly
                .GetTypes()
                .Single(type => type.IsClass && interfaceType.IsAssignableFrom(type));

            var handler = new CapturingHandler();
            var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
            var requestBuilder = RequestBuilder.ForType(interfaceType, settings);
            var generatedApi = Activator.CreateInstance(generatedType, [client, requestBuilder])!;
            return new(loadContext, handler, client, interfaceType, generatedApi, requestBuilder);
        }

        /// <summary>Creates a compiled scenario enum value from its underlying value.</summary>
        /// <param name="typeName">The compiled enum type's full name.</param>
        /// <param name="value">The underlying enum value.</param>
        /// <returns>The boxed enum value.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        public object CreateEnumValue(string typeName, int value) =>
            Enum.ToObject(interfaceType.Assembly.GetType(typeName, throwOnError: true)!, value);

        /// <summary>Creates an instance of a compiled scenario type with the given properties assigned.</summary>
        /// <param name="typeName">The compiled type's full name.</param>
        /// <param name="properties">The property name/value pairs to assign.</param>
        /// <returns>The created instance.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        public object CreateApiValue(string typeName, params (string Name, object? Value)[] properties)
        {
            var type = interfaceType.Assembly.GetType(typeName, throwOnError: true)!;
            var instance = Activator.CreateInstance(type)!;
            foreach (var (name, value) in properties)
            {
                type.GetProperty(name)!.SetValue(instance, value);
            }

            return instance;
        }

        /// <summary>Creates a <c>Dictionary&lt;string, TValue&gt;</c> of a compiled scenario value type.</summary>
        /// <param name="valueTypeName">The compiled value type's full name.</param>
        /// <param name="entries">The key/value entries to add.</param>
        /// <returns>The created dictionary.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        [RequiresDynamicCode("Constructs a closed Dictionary type over the compiled scenario value type.")]
        public object CreateStringKeyedDictionary(string valueTypeName, params (string Key, object? Value)[] entries)
        {
            var valueType = interfaceType.Assembly.GetType(valueTypeName, throwOnError: true)!;
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
            var dictionary = Activator.CreateInstance(dictionaryType)!;
            var add = dictionaryType.GetMethod("Add")!;
            foreach (var (key, value) in entries)
            {
                _ = add.Invoke(dictionary, [key, value]);
            }

            return dictionary;
        }

        /// <summary>Creates a <c>List&lt;TValue&gt;</c> of a compiled scenario value type.</summary>
        /// <param name="valueTypeName">The compiled value type's full name.</param>
        /// <param name="items">The items to add to the list.</param>
        /// <returns>The created list instance.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        [RequiresDynamicCode("Constructs a closed List type over the compiled scenario value type.")]
        public object CreateApiList(string valueTypeName, params object?[] items)
        {
            var valueType = interfaceType.Assembly.GetType(valueTypeName, throwOnError: true)!;
            var listType = typeof(List<>).MakeGenericType(valueType);
            var list = Activator.CreateInstance(listType)!;
            var add = listType.GetMethod("Add")!;
            foreach (var item in items)
            {
                _ = add.Invoke(list, [item]);
            }

            return list;
        }

        /// <summary>Invokes a method on the generated client and asserts the captured relative URI.</summary>
        /// <param name="methodName">The interface method name.</param>
        /// <param name="args">The argument values.</param>
        /// <param name="expectedPathAndQuery">The expected path and query, or null to skip the assertion.</param>
        /// <returns>The captured request.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        public async Task<HttpRequestMessage> InvokeGeneratedAsync(
            string methodName,
            object?[] args,
            string? expectedPathAndQuery)
        {
            var task = (Task)interfaceType.GetMethod(methodName)!.Invoke(generatedApi, args)!;
            await task.ConfigureAwait(false);
            var request = handler.TakeLastRequest();
            if (expectedPathAndQuery is not null)
            {
                await Assert.That(request.RequestUri!.PathAndQuery).IsEqualTo(expectedPathAndQuery);
            }

            return request;
        }

        /// <summary>Invokes a method through both request paths and asserts the URIs are identical.</summary>
        /// <param name="methodName">The interface method name.</param>
        /// <param name="args">The argument values.</param>
        /// <param name="expectedPathAndQuery">The expected path and query, or null to only assert parity.</param>
        /// <returns>The request captured from the generated path.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        [RequiresDynamicCode("Builds the reflection request delegate for parity comparison.")]
        public async Task<HttpRequestMessage> AssertParityAsync(
            string methodName,
            object?[] args,
            string? expectedPathAndQuery)
        {
            var generatedRequest = await InvokeGeneratedAsync(methodName, args, expectedPathAndQuery).ConfigureAwait(false);

            var reflectionFunc = requestBuilder.BuildRestResultFuncForMethod(methodName);
            var reflectionTask = (Task)reflectionFunc(client, args!)!;
            await reflectionTask.ConfigureAwait(false);
            var reflectionRequest = handler.TakeLastRequest();

            await Assert.That(generatedRequest.Method).IsEqualTo(reflectionRequest.Method);
            await Assert.That(generatedRequest.RequestUri!.AbsoluteUri)
                .IsEqualTo(reflectionRequest.RequestUri!.AbsoluteUri);
            return generatedRequest;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            client.Dispose();
            handler.Dispose();
            context.Dispose();
        }
    }

    /// <summary>Captures each outgoing request and returns a fixed JSON string response.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        /// <summary>The last request sent through the handler.</summary>
        private HttpRequestMessage? _lastRequest;

        /// <summary>Gets the body content captured for the last request, or null.</summary>
        public string? LastContent { get; private set; }

        /// <summary>Takes the last captured request, clearing the slot.</summary>
        /// <returns>The captured request.</returns>
        public HttpRequestMessage TakeLastRequest()
        {
            var request = _lastRequest ?? throw new InvalidOperationException("No request was captured.");
            _lastRequest = null;
            return request;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _lastRequest = request;

            // Streamed request bodies are disposed with the request, so snapshot the content here.
            LastContent = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"done\"", Encoding.UTF8, "application/json")
            };
        }
    }
}
