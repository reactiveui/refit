// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;

namespace Refit.Tests;

/// <summary>Tests for the generated request runtime helper.</summary>
public partial class GeneratedRequestRunnerTests
{
    /// <summary>An arbitrary form-field name, alongside "Note" and "Roles"; unrelated to any type in scope.</summary>
    private const string SkipFieldName = "Skip";

    /// <summary>Relative request path shared by the generated-runner fixtures.</summary>
    private const string RelativeResourcePath = "/resource";

    /// <summary>Header name used by the header-assignment tests.</summary>
    private const string TestHeaderName = "X-Test";

    /// <summary>Header name used by the header-collision tests.</summary>
    private const string FirstHeaderName = "X-First";

    /// <summary>Content-Language header value used by the content-header tests.</summary>
    private const string ContentLanguageValue = "en-US";

    /// <summary>Stream body payload shared by the stream-content tests.</summary>
    private const string StreamBodyText = "stream-body";

    /// <summary>Deserialization failure message shared by the error-path tests.</summary>
    private const string BadContentMessage = "bad content";

    /// <summary>Sample body value for the unsupported serialization-mode test.</summary>
    private const int UnsupportedModeBodyValue = 42;

    /// <summary>Out-of-range value cast to an unsupported serialization mode.</summary>
    private const int UnsupportedSerializationMode = 123;

    /// <summary>Expected serialize call count for the serialized-body-modes test.</summary>
    private const int ExpectedSerializeCallCount = 3;

    /// <summary>Typed request option value stored and asserted by the request-property test.</summary>
    private const int RequestPropertyValue = 42;

    /// <summary>Configured request option value applied by the request-options test.</summary>
    private const int ConfiguredOptionValue = 42;

    /// <summary>Deserialized result value used by the successful-response tests.</summary>
    private const int DeserializedResultValue = 42;

    /// <summary>Deserialized result value used by the successful API-response test.</summary>
    private const int SuccessResultValue = 123;

    /// <summary>Deserialized result value used by the buffering-failure test.</summary>
    private const int BufferedResultValue = 321;

    /// <summary>UTF-8 encoded stream body payload shared by the stream-content tests.</summary>
    private static readonly byte[] StreamBodyBytes = "stream-body"u8.ToArray();

    /// <summary>Verifies that already-created HTTP content is reused directly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentReusesHttpContent()
    {
        var content = new StringContent("body");
        var settings = CreateSettings();

        var result = GeneratedRequestRunner.CreateBodyContent(
            settings,
            content,
            BodySerializationMethod.Default,
            streamBody: false);

        await Assert.That(result).IsSameReferenceAs(content);
    }

    /// <summary>Verifies that stream bodies become stream content without serializer involvement.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentUsesStreamContentForStreamBodies()
    {
        await using var stream = new MemoryStream(StreamBodyBytes);
        var settings = CreateSettings();

        var result = GeneratedRequestRunner.CreateBodyContent(
            settings,
            stream,
            BodySerializationMethod.Default,
            streamBody: false);

        await Assert.That(result).IsTypeOf<StreamContent>();
        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo(StreamBodyText);
    }

    /// <summary>Verifies that default string bodies are sent as literal string content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentUsesLiteralStringForDefaultStringBodies()
    {
        var serializer = new RecordingContentSerializer();
        var settings = CreateSettings(serializer);

        var result = GeneratedRequestRunner.CreateBodyContent(
            settings,
            "literal",
            BodySerializationMethod.Default,
            streamBody: false);

        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("literal");
        await Assert.That(serializer.SerializeCallCount).IsEqualTo(0);
    }

    /// <summary>Verifies that serialized body modes use the configured content serializer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentUsesSerializerForSerializedBodyModes()
    {
        var serializer = new RecordingContentSerializer();
        var settings = CreateSettings(serializer);

        const int bodyValue = 42;
        var defaultContent = GeneratedRequestRunner.CreateBodyContent(
            settings,
            bodyValue,
            BodySerializationMethod.Default,
            streamBody: false);
        var serializedContent = GeneratedRequestRunner.CreateBodyContent(
            settings,
            "serialized",
            BodySerializationMethod.Serialized,
            streamBody: false);
#pragma warning disable CS0618 // Generated request building must keep accepting legacy compiled BodySerializationMethod.Json callers.
        var legacyJsonContent = GeneratedRequestRunner.CreateBodyContent(
            settings,
            "legacy-json",
            BodySerializationMethod.Json,
            streamBody: false);
#pragma warning restore CS0618

        await Assert.That(await defaultContent.ReadAsStringAsync()).IsEqualTo("serialized:42");
        await Assert.That(await serializedContent.ReadAsStringAsync()).IsEqualTo("serialized:serialized");
        await Assert.That(await legacyJsonContent.ReadAsStringAsync()).IsEqualTo("serialized:legacy-json");
        await Assert.That(serializer.SerializeCallCount).IsEqualTo(ExpectedSerializeCallCount);
    }

    /// <summary>Verifies that streaming serialized bodies are copied through push-stream content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentWrapsSerializedContentForStreamingBodies()
    {
        var settings = CreateSettings(new RecordingContentSerializer());

        var result = GeneratedRequestRunner.CreateBodyContent(
            settings,
            "streamed",
            BodySerializationMethod.Serialized,
            streamBody: true);

        await Assert.That(result).IsTypeOf<PushStreamContent>();
        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("serialized:streamed");
    }

    /// <summary>Verifies URL-encoded string bodies are sent as escaped form content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentEscapesStringBodies()
    {
        var settings = CreateSettings();

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(
            settings,
            "url&string");

        await Assert.That(result.Headers.ContentType?.MediaType).IsEqualTo("application/x-www-form-urlencoded");
        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("url%26string");
    }

    /// <summary>Verifies URL-encoded HTTP content bodies are reused directly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentReusesHttpContent()
    {
        var settings = CreateSettings();
        var content = new StringContent("content-body");

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(
            settings,
            content);

        await Assert.That(result).IsSameReferenceAs(content);
    }

    /// <summary>Verifies URL-encoded stream bodies are wrapped as stream content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentUsesStreamContentForStreams()
    {
        var settings = CreateSettings();
        await using var stream = new MemoryStream(StreamBodyBytes);

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(
            settings,
            stream);

        await Assert.That(result).IsTypeOf<StreamContent>();
        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo(StreamBodyText);
    }

    /// <summary>Verifies URL-encoded object bodies use the declared body type.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentUsesDeclaredBodyType()
    {
        var settings = CreateSettings();
        DeclaredFormBody body = new DerivedFormBody
        {
            Name = "Ada",
            Hidden = "ignored"
        };

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent<DeclaredFormBody>(
            settings,
            body);

        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("Name=Ada");
    }

    /// <summary>Verifies the descriptor overload uses generated field metadata for the built-in JSON serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentWithFieldsUsesDescriptorsForSystemTextJson()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());
        var body = new DeclaredFormBody { Name = "Ada" };
        var fields = new[]
        {
            new FormField<DeclaredFormBody>(static _ => "from-getter", "Name", "renamed", null, null, null, false)
        };

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, body, fields);

        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("renamed=from-getter");
    }

    /// <summary>Verifies the descriptor overload falls back to reflection for a custom content serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentWithFieldsFallsBackForCustomSerializer()
    {
        var settings = CreateSettings();
        var body = new DeclaredFormBody { Name = "Ada" };
        var fields = new[]
        {
            new FormField<DeclaredFormBody>(static _ => "from-getter", "Name", "renamed", null, null, null, false)
        };

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, body, fields);

        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("Name=Ada");
    }

    /// <summary>Verifies the descriptor overload routes dictionaries through the reflection path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentWithFieldsHandlesDictionary()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());
        var body = new Dictionary<string, object> { ["a"] = "1" };

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, body, []);

        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("a=1");
    }

    /// <summary>Verifies the descriptor overload escapes string bodies and skips descriptors.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentWithFieldsEscapesStringBodies()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, "url&string", []);

        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("url%26string");
    }

    /// <summary>Verifies the descriptor overload serializes null fields and collections per metadata.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentWithFieldsSerializesNullsAndCollections()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());
        var body = new DeclaredFormBody();
        var fields = new[]
        {
            new FormField<DeclaredFormBody>(static _ => null, "Note", "note", null, null, null, serializeNull: true),
            new FormField<DeclaredFormBody>(static _ => new List<string> { "a", "b" }, "Roles", "roles", null, null, CollectionFormat.Multi, false),
            new FormField<DeclaredFormBody>(static _ => null, SkipFieldName, "skip", null, null, null, serializeNull: false)
        };

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, body, fields);

        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("note=&roles=a&roles=b");
    }

    /// <summary>Verifies the descriptor overload applies a precomputed prefix segment to the field name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentWithFieldsAppliesPrefixSegment()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());
        var body = new DeclaredFormBody { Name = "v" };
        var fields = new[]
        {
            new FormField<DeclaredFormBody>(static b => b.Name, "Name", null, "pre-", null, null, false)
        };

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, body, fields);

        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo("pre-Name=v");
    }

    /// <summary>Verifies the descriptor path and the reflection path produce identical form output.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DescriptorAndReflectionFormPathsProduceIdenticalOutput()
    {
        var body = new ParityFormBody
        {
            WebPropertyId = "UA-1",
            Plain = "p",
            Note = null,
            Roles = ["x", "y"]
        };
        var fields = new[]
        {
            new FormField<ParityFormBody>(static b => b.WebPropertyId, "WebPropertyId", "tid", null, null, null, false),
            new FormField<ParityFormBody>(static b => b.Plain, "Plain", null, null, null, null, false),
            new FormField<ParityFormBody>(static b => b.Note, "Note", null, null, null, null, serializeNull: true),
            new FormField<ParityFormBody>(static b => b.Roles, "Roles", null, null, null, CollectionFormat.Multi, false)
        };

        // SystemTextJson takes the generated descriptor path; the recording serializer forces the reflection path.
        var descriptor = await GeneratedRequestRunner
            .CreateUrlEncodedBodyContent(new RefitSettings(new SystemTextJsonContentSerializer()), body, fields)
            .ReadAsStringAsync();
        var reflection = await GeneratedRequestRunner
            .CreateUrlEncodedBodyContent(CreateSettings(), body, fields)
            .ReadAsStringAsync();

        await Assert.That(descriptor).IsEqualTo(reflection);
    }

    /// <summary>Verifies that unsupported body serialization modes are rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentRejectsUnsupportedBodySerializationMode()
    {
        var settings = CreateSettings();

        await Assert
            .That(
                () => GeneratedRequestRunner.CreateBodyContent(
                    settings,
                    new { Value = UnsupportedModeBodyValue },
                    (BodySerializationMethod)UnsupportedSerializationMode,
                    streamBody: false))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    /// <summary>Verifies the invariant formatter renders both a value-type and a reference-type formattable value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FormatInvariantFormatsValueAndReferenceTypes()
    {
        const int value = 42;

        await Assert.That(GeneratedRequestRunner.FormatInvariant(value, "D3")).IsEqualTo("042");
        await Assert.That(GeneratedRequestRunner.FormatInvariant(new FormattableReference(), "custom")).IsEqualTo("custom");
    }

    /// <summary>Verifies CanUnrollForm accepts a plain object body and rejects the null, HttpContent, Stream, string and
    /// dictionary bodies the reflection path special-cases.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CanUnrollFormAcceptsPlainObjectsAndRejectsSpecialShapes()
    {
        using var httpContent = new StringContent("x");
        await using var stream = new MemoryStream();

        await Assert.That(GeneratedRequestRunner.CanUnrollForm(new DeclaredFormBody())).IsTrue();
        await Assert.That(GeneratedRequestRunner.CanUnrollForm(null)).IsFalse();
        await Assert.That(GeneratedRequestRunner.CanUnrollForm(httpContent)).IsFalse();
        await Assert.That(GeneratedRequestRunner.CanUnrollForm(stream)).IsFalse();
        await Assert.That(GeneratedRequestRunner.CanUnrollForm("body")).IsFalse();
        await Assert.That(GeneratedRequestRunner.CanUnrollForm(new Dictionary<string, string>())).IsFalse();
    }

    /// <summary>Verifies the multipart serializer wraps a serialization failure in a descriptive argument exception for
    /// both a typed part value and a null part value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SerializeMultipartPartWrapsSerializerFailure()
    {
        var settings = new RefitSettings(
            new RecordingContentSerializer { SerializeException = new NotSupportedException("boom") });

        await Assert
            .That(() => GeneratedRequestRunner.SerializeMultipartPart(settings, new DeclaredFormBody(), "field"))
            .ThrowsExactly<ArgumentException>();
        await Assert
            .That(() => GeneratedRequestRunner.SerializeMultipartPart<object?>(settings, null, "field"))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies the catch-all path escaper substitutes an empty string when the formatter yields null for a
    /// section, preserving the separators between the (now-empty) sections.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RoundTripEscapePathSubstitutesEmptyForNullFormattedSection()
    {
        var result = GeneratedRequestRunner.RoundTripEscapePath(
            "a/b",
            new NullUrlParameterFormatter(),
            typeof(string),
            typeof(string));

        await Assert.That(result).IsEqualTo("/");
    }

    /// <summary>Creates settings backed by the test serializer.</summary>
    /// <param name="serializer">The serializer to assign, or null for a recording serializer.</param>
    /// <returns>The configured settings.</returns>
    private static RefitSettings CreateSettings(IHttpContentSerializer? serializer = null) =>
        new(serializer ?? new RecordingContentSerializer());

    /// <summary>Creates an HTTP client that can send generated relative request URIs.</summary>
    /// <param name="handler">The handler that will receive generated requests.</param>
    /// <returns>The configured client.</returns>
    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler)
        {
            BaseAddress = new("https://api.example")
        };

    /// <summary>Captures request details sent by generated response helpers.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        /// <summary>The send delegate used by this handler.</summary>
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

        /// <summary>Initializes a new instance of the <see cref="CapturingHandler"/> class.</summary>
        public CapturingHandler()
            : this(
                static (_, _) => Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(string.Empty)
                    }))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CapturingHandler"/> class.</summary>
        /// <param name="sendAsync">The send delegate to invoke.</param>
        public CapturingHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) =>
            _sendAsync = sendAsync;

        /// <summary>Gets the authorization parameter captured from the sent request.</summary>
        public string? AuthorizationParameter { get; private set; }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return SendAndAttachRequestAsync(request, cancellationToken);
        }

        /// <summary>Runs the send delegate and mirrors HttpClientHandler by attaching the request to the response.</summary>
        /// <param name="request">The sent request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response with a request message.</returns>
        private async Task<HttpResponseMessage> SendAndAttachRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await _sendAsync(request, cancellationToken).ConfigureAwait(false);
            response.RequestMessage ??= request;
            return response;
        }
    }

    /// <summary>Records serializer usage and returns configured test values.</summary>
    private sealed class RecordingContentSerializer : IHttpContentSerializer
    {
        /// <summary>Gets the number of serialization calls.</summary>
        public int SerializeCallCount { get; private set; }

        /// <summary>Gets the number of deserialization calls.</summary>
        public int DeserializeCallCount { get; private set; }

        /// <summary>Gets the value returned from deserialization.</summary>
        public object? DeserializedValue { get; init; }

        /// <summary>Gets the exception thrown from deserialization.</summary>
        public Exception? DeserializeException { get; init; }

        /// <summary>Gets the exception thrown from serialization.</summary>
        public Exception? SerializeException { get; init; }

        /// <inheritdoc />
        public HttpContent ToHttpContent<T>(T item)
        {
            SerializeCallCount++;
            if (SerializeException is not null)
            {
                throw SerializeException;
            }

            return new StringContent($"serialized:{item}");
        }

        /// <inheritdoc />
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The method implements Refit's published serializer interface.")]
        public Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default)
        {
            DeserializeCallCount++;
            if (DeserializeException is not null)
            {
                throw DeserializeException;
            }

            return Task.FromResult((T?)DeserializedValue);
        }

        /// <inheritdoc />
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo) =>
            propertyInfo.Name;
    }

    /// <summary>Content that fails when buffering attempts to serialize it into memory.</summary>
    private sealed class ThrowingLoadContent : HttpContent
    {
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context) =>
            throw new InvalidOperationException("buffering failed");

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 1;
            return true;
        }
    }

    /// <summary>A reference type that renders itself through its format string, exercising the reference-type path of
    /// the invariant formatter.</summary>
    private sealed class FormattableReference : IFormattable
    {
        /// <inheritdoc/>
        public string ToString(string? format, IFormatProvider? formatProvider) => format ?? "reference";
    }

    /// <summary>A URL parameter formatter whose Format always yields null.</summary>
    private sealed class NullUrlParameterFormatter : IUrlParameterFormatter
    {
        /// <inheritdoc/>
        public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type) => null;
    }

    /// <summary>Declared form model used to verify generated URL-encoded bodies use compile-time metadata.</summary>
    private class DeclaredFormBody
    {
        /// <summary>Gets or sets the declared form name.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Derived form model with an extra property that should not be emitted by the declared-type path.</summary>
    private sealed class DerivedFormBody : DeclaredFormBody
    {
        /// <summary>Gets or sets a derived-only value.</summary>
        public string? Hidden { get; set; }
    }

    /// <summary>Form model used to compare the descriptor and reflection serialization paths.</summary>
    private sealed class ParityFormBody
    {
        /// <summary>Gets or sets the aliased property.</summary>
        [AliasAs("tid")]
        public string? WebPropertyId { get; set; }

        /// <summary>Gets or sets a plain property.</summary>
        public string? Plain { get; set; }

        /// <summary>Gets or sets a property serialized even when null.</summary>
        [Query(SerializeNull = true)]
        public string? Note { get; set; }

        /// <summary>Gets or sets a multi-value collection property.</summary>
        [Query(CollectionFormat.Multi)]
        public List<string>? Roles { get; set; }
    }

    /// <summary>Simple deserialized response model for generated runtime tests.</summary>
    /// <param name="Value">The model value.</param>
    private sealed record GeneratedResult(int Value);
}
