// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace Refit.GeneratorTests;

/// <summary> Helpers used on any QueryRequestBuildingLiveTests.</summary>
public sealed partial class QueryRequestBuildingLiveTests
{
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

            // A value-type (struct) query object used through a nullable parameter: exercises flattening the underlying
            // struct's properties through .Value inside the parameter's HasValue guard.
            public struct GeoPoint
            {
                public string? Name { get; set; }

                [Query(Format = "0.00")]
                public double Lat { get; set; }
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

                [Get("/point")]
                Task<string> NullableStructQuery([Query] GeoPoint? point);

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
}
