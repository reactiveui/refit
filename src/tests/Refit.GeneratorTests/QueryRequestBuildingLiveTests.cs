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
public sealed class QueryRequestBuildingLiveTests
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
    private const double PriceFive = 5d;

    /// <summary>A sample raw double for TreatAsString.</summary>
    private const double RawDouble = 1.5d;

    /// <summary>The enum-valued query method exercised across parity scenarios.</summary>
    private const string SortedMethodName = "Sorted";

    /// <summary>The multi-expanded collection query method exercised across parity scenarios.</summary>
    private const string ExpandedMethodName = "Expanded";

    /// <summary>The full name of the compiled scenario enum type.</summary>
    private const string SearchSortTypeName = "Refit.LiveQuery.SearchSort";

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

        public interface ILiveQueryApi
        {
            [Get("/search")]
            Task<string> Plain(string q);

            [Get("/docs/{info.Slug}/rev/{info.Version}")]
            Task<string> DottedPath(RouteInfo info);

            [Get("/tags/{info.Slug}")]
            Task<string> DottedPathResidual(RouteInfo info);

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
        }
        """;

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

        await harness.AssertParityAsync("Plain", ["a b/c"], "/base/search?q=a%20b%2Fc");
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

        var info = harness.CreateApiValue("Refit.LiveQuery.RouteInfo", ("Slug", "a b/c"), ("Version", DocRevision));
        _ = await harness.AssertParityAsync("DottedPath", [info], "/base/docs/a%20b%2Fc/rev/7");

        // Only Slug binds to the path; Version is a residual property flattened into the query, exactly as the
        // reflection builder splits a path-bound object between the path and the query string.
        _ = await harness.AssertParityAsync("DottedPathResidual", [info], "/base/tags/a%20b%2Fc?Version=7");
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
