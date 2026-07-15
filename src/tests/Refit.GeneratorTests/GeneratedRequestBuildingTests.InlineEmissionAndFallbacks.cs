// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests for inline versus reflective-fallback emission across method, verb, body and parameter shapes.</summary>
public partial class GeneratedRequestBuildingTests
{
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
            [Get("/bad\r\npath")]
            Task<string> ControlCharacters();

            [Multipart]
            [Post("/multipart")]
            Task<string> Multipart([Body] string body);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain("BuildRelativeUri(this.Client, \"/bad");
    }

    /// <summary>Verifies a <c>[QueryUriFormat]</c> method generates inline, re-encoding the URI with the attribute's
    /// format instead of falling back to the reflection request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnGeneratesInlineForQueryUriFormat()
    {
        var generated = Fixture.GenerateForBody(
            """
            [QueryUriFormat(UriFormat.Unescaped)]
            [Get("/query")]
            Task<string> Query(string filter);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains(".UrlResolution, (global::System.UriFormat)");
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

    /// <summary>Verifies a custom HTTP method attribute whose <c>Method</c> getter is a literal <c>new HttpMethod("VERB")</c>
    /// generates inline with that verb instead of falling back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnGeneratesInlineForCustomHttpVerbLiteral()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public sealed class PurgeAttribute : HttpMethodAttribute
            {
                public PurgeAttribute(string path) : base(path) { }
                public override System.Net.Http.HttpMethod Method => new System.Net.Http.HttpMethod("PURGE");
            }

            public interface IGeneratedClient
            {
                [Purge("/cache/{id}")]
                Task Evict(string id);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains(NewHttpRequestMessage);

        // The custom verb is allocated once in a static field and the request references it, not a per-call allocation.
        await Assert.That(generated).Contains("private static readonly global::System.Net.Http.HttpMethod ______httpMethod = new global::System.Net.Http.HttpMethod(\"PURGE\");");
        await Assert.That(generated).Contains("new global::System.Net.Http.HttpRequestMessage(______httpMethod,");
    }

    /// <summary>Verifies a custom HTTP QUERY verb attribute (a draft-standard body-carrying method) with an explicit
    /// <c>[Body]</c> generates inline, emitting the custom verb and serializing the body instead of falling back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnGeneratesInlineForQueryVerbWithBody()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public sealed class QueryVerbAttribute : HttpMethodAttribute
            {
                public QueryVerbAttribute(string path) : base(path) { }
                public override System.Net.Http.HttpMethod Method => new System.Net.Http.HttpMethod("QUERY");
            }

            public sealed class SearchBody { public string? Term { get; set; } }

            public interface IGeneratedClient
            {
                [QueryVerb("/documents")]
                Task<string> Query([Body] SearchBody body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains(NewHttpRequestMessage);
        await Assert.That(generated).Contains("new global::System.Net.Http.HttpMethod(\"QUERY\")");
        await Assert.That(generated).Contains("GeneratedRequestRunner.CreateBodyContent");
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

        await Assert.That(generated).Contains("BuildRelativeUri(this.Client, \"/foo?one=1&two\"");
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

        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(refitRequest, \"X-Value\", @value?.ToString(), refitSettings.ValidateHeaders)");
        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(refitRequest, \"X-Name\", @name?.ToString(), refitSettings.ValidateHeaders)");
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

    /// <summary>Verifies URL-encoded bodies use generated request construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsUrlEncodedBodyContent()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Post("/form")]
            Task<string> Form([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> form);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("GeneratedRequestRunner.CreateUrlEncodedBodyContent<global::System.Collections.Generic.Dictionary<string, string>>");
    }

    /// <summary>Verifies unsupported body serialization values fall back to the runtime builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnFallsBackForUnsupportedInlineBodySerialization()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Post("/unknown")]
            Task<string> Unknown([Body((BodySerializationMethod)123)] string body);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain("GeneratedRequestRunner.CreateBodyContent<");
        await Assert.That(generated).DoesNotContain("GeneratedRequestRunner.CreateUrlEncodedBodyContent<");
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
}
