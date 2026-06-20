// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests for opt-in generated request construction.</summary>
public class GeneratedRequestBuildingTests
{
    /// <summary>The generated implementation source hint name used by these tests.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The reflective request-builder call emitted by fallback paths.</summary>
    private const string ReflectiveRequestBuilderCall = "BuildRestResultFuncForMethod";

    /// <summary>The generated request-runner send call emitted by inline request construction.</summary>
    private const string GeneratedRequestRunnerSendAsync = "GeneratedRequestRunner.SendAsync";

    /// <summary>The generated request-message construction emitted by inline request construction.</summary>
    private const string NewHttpRequestMessage = "new global::System.Net.Http.HttpRequestMessage";

    /// <summary>A simple eligible GET method used by switch tests.</summary>
    private const string SimpleGetMethod =
        """
        [Get("/users")]
        Task<string> Get(CancellationToken cancellationToken);
        """;

    /// <summary>Verifies the default output uses generated request construction for eligible methods.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DefaultUsesGeneratedRequestConstruction()
    {
        var generated = Fixture.GenerateForBody(
            SimpleGetMethod,
            GeneratedClientHintName);

        await Assert.That(generated).Contains(GeneratedRequestRunnerSendAsync);
        await Assert.That(generated).Contains(NewHttpRequestMessage);
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an explicit switch-off keeps using the reflective request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExplicitSwitchOffUsesReflectiveRequestBuilder()
    {
        var generated = Fixture.GenerateForBody(
            SimpleGetMethod,
            GeneratedClientHintName,
            generatedRequestBuilding: false);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain(GeneratedRequestRunnerSendAsync);
    }

    /// <summary>Verifies the legacy source-generator disable switch prevents all generated output.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DisableSourceGeneratorProducesNoSources()
    {
        var result = Fixture.RunGeneratorForBody(
            SimpleGetMethod,
            generatedRequestBuilding: null,
            disableSourceGenerator: true);

        await Assert.That(result.GeneratedSources).IsEmpty();
    }

    /// <summary>Verifies the explicit switch-on emits inline request construction for a simple eligible method.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsInlineRequestConstructionForSimpleMethod()
    {
        var generated = Fixture.GenerateForBody(
            SimpleGetMethod,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(GeneratedRequestRunnerSendAsync);
        await Assert.That(generated).Contains(NewHttpRequestMessage);
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies generated inline request construction compiles in a consumer compilation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnGeneratedRequestConstructionCompiles()
    {
        var errors = Fixture.GenerateErrorsForBody(
            """
            [Post("/users")]
            Task<ApiResponse<string>> Create([Body] string name, CancellationToken cancellationToken);

            [Get("/users")]
            Task<string> HeaderValue([Header("X-Test")] int id);

            [Get("/users")]
            Task<string> HeaderReference([Header("X-Test")] string? id);

            [Get("/users")]
            Task<string> Headers([HeaderCollection] IDictionary<string, string> headers);

            [Get("/users")]
            Task<string> Property([Property("tenant")] int tenantId);

            string Client { get; set; }
            """,
            generatedRequestBuilding: true);

        var errorMessages = string.Join(Environment.NewLine, errors.Select(diagnostic => diagnostic.ToString()));
        await Assert.That(errorMessages).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies static headers are emitted into inline request construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsStaticHeaders()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Headers("X-Test: test")]
            [Get("/users")]
            Task<string> Get(CancellationToken cancellationToken);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(refitRequest, \"X-Test\", \"test\")");
        await Assert.That(generated).Contains(GeneratedRequestRunnerSendAsync);
    }

    /// <summary>Verifies constant inline paths strip URI fragments before request construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnStripsFragmentsFromInlineConstantPaths()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/foo?key=value#name")]
            Task<string> Get();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("refitBasePath + \"/foo?key=value\"");
        await Assert.That(generated).DoesNotContain("#name");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies constant inline paths discard query text after a fragment marker.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnStripsQueryAfterFragmentFromInlineConstantPaths()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/foo#?key=value")]
            Task<string> Get();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("refitBasePath + \"/foo\"");
        await Assert.That(generated).DoesNotContain("?key=value");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies constant inline paths remove empty query keys to match runtime builder semantics.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnRemovesEmptyQueryKeysFromInlineConstantPaths()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/foo?=drop&key=&two=2")]
            Task<string> Get();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("refitBasePath + \"/foo?key=&two=2\"");
        await Assert.That(generated).DoesNotContain("=drop");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies legacy JSON body metadata emits the non-obsolete equivalent enum member.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnMapsLegacyJsonBodySerializationToSerialized()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Post("/users")]
            Task<string> Create([Body(BodySerializationMethod.Json)] string name);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("BodySerializationMethod.Serialized");
        await Assert.That(generated).DoesNotContain("BodySerializationMethod.Json");
    }

    /// <summary>Verifies dynamic header parameters are emitted into inline request construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsDynamicHeader()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([Header("X-Test")] int id);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(refitRequest, \"X-Test\", @id.ToString())");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies header collections are emitted into inline request construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsHeaderCollection()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([HeaderCollection] IDictionary<string, string> headers);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.AddHeaderCollection(refitRequest, @headers)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies all built-in HTTP method attributes can be emitted inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsAllBuiltInHttpMethodsInline()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Delete("/delete")]
            Task<string> Delete();

            [Head("/head")]
            Task<string> Head();

            [Options("/options")]
            Task<string> Options();

            [Patch("/patch")]
            Task<string> Patch();

            [Post("/post")]
            Task<string> Post();

            [Put("/put")]
            Task<string> Put();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("HttpMethod.Delete");
        await Assert.That(generated).Contains("HttpMethod.Head");
        await Assert.That(generated).Contains("HttpMethod.Options");
        await Assert.That(generated).Contains("new global::System.Net.Http.HttpMethod(\"PATCH\")");
        await Assert.That(generated).Contains("HttpMethod.Post");
        await Assert.That(generated).Contains("HttpMethod.Put");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies built-in HTTP method attributes are discovered when written with qualified names.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnDiscoversQualifiedBuiltInHttpMethodAttributes()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            using R = Refit;

            public interface IGeneratedClient
            {
                [global::Refit.GetAttribute("/global")]
                Task<string> Global();

                [R.PostAttribute("/alias")]
                Task<string> Alias();

                [Refit.Put("/qualified")]
                Task<string> Qualified();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("refitBasePath + \"/global\"");
        await Assert.That(generated).Contains("refitBasePath + \"/alias\"");
        await Assert.That(generated).Contains("refitBasePath + \"/qualified\"");
        await Assert.That(generated).Contains("HttpMethod.Get");
        await Assert.That(generated).Contains("HttpMethod.Post");
        await Assert.That(generated).Contains("HttpMethod.Put");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies string literal escaping in inline request paths and header values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEscapesInlineStringLiterals()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Headers("X-Quote: value\"with\\slashes\tand\nlines")]
            [Get("/escaped?name=value\"with\tand")]
            Task<string> Escaped();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("\\\"with\\\\slashes\\tand");
        await Assert.That(generated).Contains("and\\nlines");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies invalid header collection semantics fall back to the runtime builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnFallsBackForInvalidHeaderCollection()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([HeaderCollection] IDictionary<string, object> headers);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain("GeneratedRequestRunner.AddHeaderCollection");
    }

    /// <summary>Verifies unsupported inline path forms and metadata fall back to the runtime builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnFallsBackForUnsupportedInlinePathAndMetadata()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("relative")]
            Task<string> Relative();

            [Get("/users/{id}")]
            Task<string> Templated(int id);

            [Get("/bad\r\npath")]
            Task<string> ControlCharacters();

            [Multipart]
            [Post("/multipart")]
            Task<string> Multipart([Body] string body);

            [QueryUriFormat(UriFormat.Unescaped)]
            [Get("/format")]
            Task<string> QueryFormat();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain("refitBasePath + \"relative\"");
        await Assert.That(generated).DoesNotContain("refitBasePath + \"/users/{id}\"");
        await Assert.That(generated).DoesNotContain("refitBasePath + \"/bad");
    }

    /// <summary>Verifies custom HTTP method attributes are discovered but fall back to the runtime builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnFallsBackForCustomHttpMethodAttributes()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public sealed class CustomAttribute(string path) : HttpMethodAttribute(path);

            public interface IGeneratedClient
            {
                [Custom("/custom")]
                Task<string> Custom();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain(NewHttpRequestMessage);
    }

    /// <summary>Verifies synchronous Refit methods use the reflective fallback emitter shapes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsReflectiveFallbackForSynchronousReturnShapes()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/sync")]
            string Sync();

            [Post("/void")]
            void SyncVoid();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("return (string)refitFunc(this.Client, refitArguments);");
        await Assert.That(generated).Contains("refitFunc(this.Client, refitArguments);");
        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies generic methods use reflective fallback arrays and emit constraints.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsReflectiveFallbackForGenericMethodsWithConstraints()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/generic")]
            Task<T> Generic<T, TStruct, TUnmanaged, TNotNull, TNew, TConstraint>(
                T value,
                List<T> values,
                TStruct structValue,
                TUnmanaged unmanagedValue,
                TNotNull notNullValue,
                TNew newValue,
                TConstraint constraintValue)
                where T : class
                where TStruct : struct
                where TUnmanaged : unmanaged
                where TNotNull : notnull
                where TNew : new()
                where TConstraint : IDisposable;
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("new global::System.Type[] { typeof(T), typeof(global::System.Collections.Generic.List<T>)");
        await Assert.That(generated).Contains("new global::System.Type[] { typeof(T), typeof(TStruct), typeof(TUnmanaged), typeof(TNotNull), typeof(TNew), typeof(TConstraint) }");
        await Assert.That(generated).Contains("where T : class");
        await Assert.That(generated).Contains("where TStruct : struct");
        await Assert.That(generated).Contains("where TUnmanaged : unmanaged");
        await Assert.That(generated).Contains("where TNotNull : notnull");
        await Assert.That(generated).Contains("where TNew : new()");
        await Assert.That(generated).Contains("where TConstraint : global::System.IDisposable");
        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies inline query normalization drops empty and whitespace query keys.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnRemovesWhitespaceAndEmptyQueryKeysFromInlineConstantPaths()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/foo?& \t =drop&one=1&&two& =also-drop")]
            Task<string> Get();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("refitBasePath + \"/foo?one=1&two\"");
        await Assert.That(generated).DoesNotContain("drop");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies request property parameters use typed generated helper calls.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsTypedPropertyParameter()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([Property("tenant")] int tenantId);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.AddRequestProperty<int>(refitRequest, \"tenant\", @tenantId)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies property parameters without explicit keys use the parameter name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnUsesParameterNameForPropertyWithoutExplicitKey()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([Property] int tenantId);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.AddRequestProperty<int>(refitRequest, \"tenantId\", @tenantId)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies inline header parameters handle nullable value and reference types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsNullableHeaderValues()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([Header("X-Value")] int? value, [Header("X-Name")] string? name);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(refitRequest, \"X-Value\", @value?.ToString())");
        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(refitRequest, \"X-Name\", @name?.ToString())");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies unsupported header attributes and duplicate special parameters fall back to the runtime builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnFallsBackForUnsupportedParametersAndDuplicateSpecialParameters()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/headers")]
            Task<string> EmptyHeader([Header(" ")] string value);

            [Post("/bodies")]
            Task<string> MultipleBodies([Body] string first, [Body] string second);

            [Get("/tokens")]
            Task<string> MultipleTokens(CancellationToken first, CancellationToken second);

            [Get("/collections")]
            Task<string> MultipleCollections(
                [HeaderCollection] IDictionary<string, string> first,
                [HeaderCollection] IDictionary<string, string> second);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain("GeneratedRequestRunner.SetHeader(refitRequest, \" \",");
        await Assert.That(generated).DoesNotContain("var refitSettings = _requestBuilder.Settings;");
    }

    /// <summary>Verifies body buffering and serialization modes are emitted for supported inline bodies.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsBodyBufferModesAndSerializationModes()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Post("/default")]
            Task<string> DefaultBody([Body] string body);

            [Post("/buffered")]
            Task<string> BufferedBody([Body(true)] string body);

            [Post("/streaming")]
            Task<string> StreamingBody([Body(false)] string body);

            [Post("/serialized")]
            Task<string> SerializedBody([Body(BodySerializationMethod.Serialized, false)] string body);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("BodySerializationMethod.Default");
        await Assert.That(generated).Contains("refitSettings.Buffered");
        await Assert.That(generated).Contains("BodySerializationMethod.Serialized");
        await Assert.That(generated).Contains("!refitSettings.Buffered");
        await Assert.That(generated).Contains("true,");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies unsupported body serialization values fall back to the runtime builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnFallsBackForUnsupportedInlineBodySerialization()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Post("/form")]
            Task<string> Form([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> form);

            [Post("/unknown")]
            Task<string> Unknown([Body((BodySerializationMethod)123)] string body);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain("GeneratedRequestRunner.CreateBodyContent<global::System.Collections.Generic.Dictionary<string, string>>");
    }

    /// <summary>Verifies return-type metadata for API response wrappers and raw response body types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsApiResponseAndRawBodyReturnShapes()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/api-response")]
            Task<ApiResponse<string>> ApiResponse();

            [Get("/iapi-response")]
            Task<IApiResponse<string>> GenericIApiResponse();

            [Get("/bare-iapi-response")]
            Task<IApiResponse> BareIApiResponse();

            [Get("/response")]
            Task<HttpResponseMessage> ResponseMessage();

            [Get("/content")]
            Task<HttpContent> Content();

            [Get("/stream")]
            Task<System.IO.Stream> Stream();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("SendAsync<global::Refit.ApiResponse<string>, string>");
        await Assert.That(generated).Contains("SendAsync<global::Refit.IApiResponse<string>, string>");
        await Assert.That(generated).Contains("SendAsync<global::Refit.IApiResponse, global::System.Net.Http.HttpContent>");
        await Assert.That(generated).Contains("SendAsync<global::System.Net.Http.HttpResponseMessage, global::System.Net.Http.HttpResponseMessage>");
        await Assert.That(generated).Contains("SendAsync<global::System.Net.Http.HttpContent, global::System.Net.Http.HttpContent>");
        await Assert.That(generated).Contains("SendAsync<global::System.IO.Stream, global::System.IO.Stream>");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies ValueTask return types wrap the generated runner task.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnWrapsValueTaskInlineReturns()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            ValueTask<string> Get(CancellationToken? cancellationToken);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("return new global::System.Threading.Tasks.ValueTask<string>(");
        await Assert.That(generated).Contains("@cancellationToken.GetValueOrDefault()");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies property attributes on interface properties are implemented and passed into generated requests.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsInterfacePropertyRequestProperty()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Property("tenant")]
                int TenantId { get; set; }

                [Get("/users")]
                Task<string> Get();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("public int TenantId {  get; set; }");
        await Assert.That(generated).Contains("GeneratedRequestRunner.AddRequestProperty<int>(refitRequest, \"tenant\", this.TenantId)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a get-only HttpClient Client interface property is satisfied by the generated infrastructure property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DoesNotReemitGeneratedClientProperty()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                HttpClient Client { get; }

                [Get("/users")]
                Task<string> Get();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        var clientPropertyCount = generated.Split("public global::System.Net.Http.HttpClient Client").Length - 1;
        await Assert.That(clientPropertyCount).IsEqualTo(1);
    }

    /// <summary>Verifies inherited non-Refit interface properties are implemented explicitly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ImplementsInheritedRegularInterfaceProperty()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IBaseApi
            {
                string BaseUri { get; set; }
            }

            public interface IGeneratedClient : IBaseApi
            {
                [Get("/users")]
                Task<string> Get();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("global::IBaseApi.BaseUri");
        await Assert.That(generated).DoesNotContain("Either this method has no Refit HTTP method attribute");
    }

    /// <summary>Verifies inherited generated-client properties and methods are emitted through explicit interfaces.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsInheritedPropertiesMethodsAndDispose()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IBaseApi : IDisposable
            {
                [Property("base-tenant")]
                int BaseTenant { get; }

                string Name { set; }

                [Get("/base")]
                Task<string> GetBase();

                string Helper<T>(T value)
                    where T : class, new();
            }

            public interface IGeneratedClient : IBaseApi
            {
                [Get("/users")]
                Task<string> Get();

                string IBaseApi.Helper<T>(T value) => string.Empty;
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("global::IBaseApi.BaseTenant");
        await Assert.That(generated).Contains("global::IBaseApi.Name");
        await Assert.That(generated).Contains("global::IBaseApi.GetBase()");
        await Assert.That(generated).Contains("void global::System.IDisposable.Dispose()");
        await Assert.That(generated).DoesNotContain("global::IBaseApi.Helper");
    }
}
