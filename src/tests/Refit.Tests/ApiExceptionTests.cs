// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

namespace Refit.Tests;

/// <summary>Tests for API exception factory and validation exception edge cases.</summary>
public sealed class ApiExceptionTests
{
    /// <summary>The example request URI reused across the exception tests.</summary>
    private const string ExampleUri = "https://example.test";

    /// <summary>The problem-details media type reused across the validation tests.</summary>
    private const string ProblemJsonMediaType = "application/problem+json";

    /// <summary>The message literal reused across the constructor tests.</summary>
    private const string MessageText = "message";

    /// <summary>The redaction placeholder value.</summary>
    private const string RedactedText = "[redacted]";

    /// <summary>A JSON error body that deserializes to <see cref="ResponseModel"/>.</summary>
    private const string ValueJson = "{\"Value\":42}";

    /// <summary>The deserialized value asserted by the content-helper tests.</summary>
    private const int ExpectedValue = 42;

    /// <summary>The problem-details status code asserted by the validation tests.</summary>
    private const int ExpectedStatusCode = 400;

    /// <summary>The integer extension value asserted by the validation tests.</summary>
    private const int ExpectedIntegerExtension = 123;

    /// <summary>The fractional extension value asserted by the validation tests.</summary>
    private const double ExpectedFractionExtension = 12.5;

    /// <summary>The configured maximum exception content length.</summary>
    private const int MaxContentLength = 128;

    /// <summary>The length of the oversized error body used by the truncation tests.</summary>
    private const int LongBodyLength = 10_000;

    /// <summary>Verifies ApiException factory guard clauses.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateRejectsSuccessfulOrMissingResponses()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var success = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };

        await Assert.That(() => (Task)ApiException.Create(request, HttpMethod.Get, success, new()))
            .ThrowsExactly<ArgumentException>();
        await Assert.That(() => (Task)ApiException.Create(MessageText, request, HttpMethod.Get, null!, new()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => (Task)ApiException.Create(request, HttpMethod.Get, null!, new(), null))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies ApiException factory handles absent and unreadable content without hiding the original response error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateHandlesMissingAndUnreadableContent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var noContentResponse = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            RequestMessage = request,
            Content = null
        };
        using var throwingContentResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = request,
            Content = new ThrowingReadContent()
        };

        var noContentException = await ApiException.Create(request, HttpMethod.Get, noContentResponse, new());
        var throwingContentException = await ApiException.Create(
            "custom",
            request,
            HttpMethod.Get,
            throwingContentResponse,
            new(),
            new InvalidOperationException("inner"));

        await Assert.That(noContentException.Content).IsEqualTo(string.Empty);
        await Assert.That(noContentException.ContentHeaders).IsNotNull();
        await Assert.That(throwingContentException.Content).IsNull();
        await Assert.That(throwingContentException.InnerException).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Verifies protected constructors and content deserialization helpers.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstructorsAndContentHelpersPreserveContext()
    {
        using var response = CreateErrorResponse(ValueJson);
        var settings = new RefitSettings();
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);
        var derived = new DerivedApiException(
            "custom",
            response.RequestMessage!,
            HttpMethod.Get,
            "content",
            HttpStatusCode.Conflict,
            "Conflict",
            response.Headers,
            settings);
        var emptyDerived = new DerivedApiException(
            response.RequestMessage!,
            HttpMethod.Get,
            null,
            HttpStatusCode.BadRequest,
            "Bad Request",
            response.Headers,
            settings);

        var model = await exception.GetContentAsAsync<ResponseModel>();
        var missing = await emptyDerived.GetContentAsAsync<ResponseModel>();

        await Assert.That(model!.Value).IsEqualTo(ExpectedValue);
        await Assert.That(missing).IsNull();
        await Assert.That(derived.Message).IsEqualTo("custom");
        await Assert.That(derived.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(emptyDerived.HasContent).IsFalse();
    }

    /// <summary>Verifies ValidationApiException constructors and creation guards.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValidationApiExceptionConstructorsAndCreateGuards()
    {
        var inner = new InvalidOperationException("inner");
        var messageOnly = new ValidationApiException(MessageText);
        var withInner = new ValidationApiException(MessageText, inner);

        await Assert.That(messageOnly.Message).IsEqualTo(MessageText);
        await Assert.That(withInner.InnerException).IsSameReferenceAs(inner);
        await Assert.That(() => ValidationApiException.Create(null!))
            .ThrowsExactly<ArgumentNullException>();

        using var emptyResponse = CreateErrorResponse(" ");
        var emptyException = await ApiException.Create(
            emptyResponse.RequestMessage!,
            HttpMethod.Get,
            emptyResponse,
            new());
        await Assert.That(() => ValidationApiException.Create(emptyException))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies the synchronous ValidationApiException factory deserializes problem details.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA1849:Call async methods when in an async method", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    [SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    public async Task ValidationApiExceptionCreateDeserializesProblemDetails()
    {
        using var response = CreateErrorResponse(
            "{\"title\":\"invalid\",\"status\":400,\"errors\":{\"Name\":[\"Required\"]}}",
            ProblemJsonMediaType);
        var apiException = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var validationException = ValidationApiException.Create(apiException);

        await Assert.That(validationException.Content).IsNotNull();
        await Assert.That(validationException.Content!.Title).IsEqualTo("invalid");
        await Assert.That(validationException.Content.Status).IsEqualTo(ExpectedStatusCode);
        await Assert.That(validationException.Content.Errors["Name"][0]).IsEqualTo("Required");
    }

    /// <summary>Verifies the synchronous ValidationApiException factory hydrates problem detail extensions and scalar errors.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA1849:Call async methods when in an async method", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    [SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    public async Task ValidationApiExceptionCreateReadsExtensionsAndScalarErrors()
    {
        using var response = CreateErrorResponse(
            """
            {
              "TYPE": null,
              "title": "invalid",
              "status": 400,
              "detail": null,
              "instance": null,
              "errors": {
                "Name": null,
                "Count": 42
              },
              "enabled": true,
              "disabled": false,
              "integer": 123,
              "fraction": 12.5,
              "timestamp": "2024-02-03T04:05:06Z",
              "message": "hello",
              "metadata": { "nested": "value" }
            }
            """,
            ProblemJsonMediaType);
        var apiException = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var validationException = ValidationApiException.Create(apiException);

        await Assert.That(validationException.Content).IsNotNull();
        await Assert.That(validationException.Content!.Type).IsNull();
        await Assert.That(validationException.Content.Detail).IsNull();
        await Assert.That(validationException.Content.Instance).IsNull();
        await Assert.That(validationException.Content.Errors["Name"]).IsEmpty();
        await Assert.That(validationException.Content.Errors["Count"][0]).IsEqualTo("42");
        await Assert.That((bool)validationException.Content.Extensions["enabled"]).IsTrue();
        await Assert.That((bool)validationException.Content.Extensions["disabled"]).IsFalse();
        await Assert.That(validationException.Content.Extensions["integer"]).IsEqualTo((long)ExpectedIntegerExtension);
        await Assert.That(validationException.Content.Extensions["fraction"]).IsEqualTo(ExpectedFractionExtension);
        await Assert.That(validationException.Content.Extensions["timestamp"]).IsTypeOf<DateTime>();
        await Assert.That(validationException.Content.Extensions[MessageText]).IsEqualTo("hello");
        await Assert.That(validationException.Content.Extensions["metadata"]).IsTypeOf<JsonElement>();
    }

    /// <summary>Verifies the synchronous ValidationApiException factory ignores malformed validation error bags.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA1849:Call async methods when in an async method", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    [SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    public async Task ValidationApiExceptionCreateIgnoresNonObjectErrors()
    {
        using var response = CreateErrorResponse(
            "{\"title\":\"invalid\",\"errors\":\"not-an-object\"}",
            ProblemJsonMediaType);
        var apiException = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var validationException = ValidationApiException.Create(apiException);

        await Assert.That(validationException.Content).IsNotNull();
        await Assert.That(validationException.Content!.Errors).IsEmpty();
    }

    /// <summary>Verifies the synchronous ValidationApiException factory rejects non-object problem details.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA1849:Call async methods when in an async method", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    [SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    public async Task ValidationApiExceptionCreateRejectsNonObjectProblemDetails()
    {
        using var response = CreateErrorResponse("[1,2,3]", ProblemJsonMediaType);
        var apiException = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(() => ValidationApiException.Create(apiException))
            .ThrowsExactly<JsonException>();
    }

    /// <summary>Verifies the ValidationApiException inner-exception constructor rejects null inner exceptions.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValidationApiExceptionInnerConstructorRejectsNullInnerException() =>
        await Assert.That(static () => new ValidationApiException(MessageText, null!))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies the problem+json media type is detected case-insensitively per RFC 7231 (#1702).</summary>
    /// <param name="mediaType">The problem details media type with varied casing.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(ProblemJsonMediaType)]
    [Arguments("application/PROBLEM+JSON")]
    [Arguments("Application/Problem+Json")]
    public async Task CreateDetectsProblemJsonMediaTypeCaseInsensitively(string mediaType)
    {
        using var response = CreateErrorResponse(
            "{\"type\":\"about:blank\",\"title\":\"Bad Request\",\"status\":400}",
            mediaType);

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception).IsTypeOf<ValidationApiException>();
    }

    /// <summary>Verifies the synchronous GetContentAs deserializes the buffered error body (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA1849:Call async methods when in an async method", Justification = "This test intentionally covers the synchronous content helper.")]
    [SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "This test intentionally covers the synchronous content helper.")]
    public async Task SyncGetContentAsDeserializesContent()
    {
        using var response = CreateErrorResponse(ValueJson);
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var model = exception.GetContentAs<ResponseModel>();

        await Assert.That(model!.Value).IsEqualTo(ExpectedValue);
    }

    /// <summary>Verifies the synchronous GetContentAs returns default when there is no body (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA1849:Call async methods when in an async method", Justification = "This test intentionally covers the synchronous content helper.")]
    [SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "This test intentionally covers the synchronous content helper.")]
    public async Task SyncGetContentAsReturnsDefaultWhenNoContent()
    {
        using var response = CreateErrorResponse(" ");
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception.GetContentAs<ResponseModel>()).IsNull();
    }

    /// <summary>Verifies TryGetContentAs returns the value inside an exception filter (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncTryGetContentAsReturnsTrueAndValue()
    {
        using var response = CreateErrorResponse(ValueJson);
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var ok = exception.TryGetContentAs<ResponseModel>(out var model);

        await Assert.That(ok).IsTrue();
        await Assert.That(model!.Value).IsEqualTo(ExpectedValue);
    }

    /// <summary>Verifies TryGetContentAs returns false for missing or malformed content (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncTryGetContentAsReturnsFalseForMissingOrInvalidContent()
    {
        using var empty = CreateErrorResponse(" ");
        using var invalid = CreateErrorResponse("not json");
        var emptyException = await ApiException.Create(empty.RequestMessage!, HttpMethod.Get, empty, new());
        var invalidException = await ApiException.Create(invalid.RequestMessage!, HttpMethod.Get, invalid, new());

        await Assert.That(emptyException.TryGetContentAs<ResponseModel>(out var missing)).IsFalse();
        await Assert.That(missing).IsNull();
        await Assert.That(invalidException.TryGetContentAs<ResponseModel>(out var broken)).IsFalse();
        await Assert.That(broken).IsNull();
    }

    /// <summary>Verifies the synchronous helpers behave when the serializer lacks sync support (#1591).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncGetContentAsWithoutSyncSerializerSupport()
    {
        using var response = CreateErrorResponse(ValueJson);
        var settings = new RefitSettings(new AsyncOnlySerializer());
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);

        await Assert.That(exception.TryGetContentAs<ResponseModel>(out _)).IsFalse();
        await Assert.That(exception.GetContentAs<ResponseModel>)
            .ThrowsExactly<NotSupportedException>();
    }

    /// <summary>Verifies opt-in request content capture is surfaced on the exception (#1189).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestContentCapturedWhenOptionPresent()
    {
        using var response = CreateErrorResponse("{}");
        GeneratedRequestRunner.AddRequestProperty(
            response.RequestMessage!,
            HttpRequestMessageOptions.RequestContent,
            "{\"sent\":true}");

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception.HasRequestContent).IsTrue();
        await Assert.That(exception.RequestContent).IsEqualTo("{\"sent\":true}");
    }

    /// <summary>Verifies request content is absent when capture was not requested (#1189).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestContentAbsentWhenNotCaptured()
    {
        using var response = CreateErrorResponse("{}");
        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception.HasRequestContent).IsFalse();
        await Assert.That(exception.RequestContent).IsNull();
    }

    /// <summary>Verifies the error body is truncated to the configured maximum length.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MaxExceptionContentLengthTruncatesErrorBody()
    {
        using var response = CreateErrorResponse(new string('a', LongBodyLength));
        var settings = new RefitSettings { MaxExceptionContentLength = MaxContentLength };

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);

        await Assert.That(exception.Content!.Length).IsEqualTo(MaxContentLength);
    }

    /// <summary>Verifies an unset maximum length leaves the error body intact.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MaxExceptionContentLengthUnsetReadsFullBody()
    {
        using var response = CreateErrorResponse(new string('a', LongBodyLength));

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception.Content!.Length).IsEqualTo(LongBodyLength);
    }

    /// <summary>Verifies a zero maximum length yields an empty error body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MaxExceptionContentLengthZeroReturnsEmptyContent()
    {
        const int bodyLength = 100;
        using var response = CreateErrorResponse(new string('a', bodyLength));
        var settings = new RefitSettings { MaxExceptionContentLength = 0 };

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);

        await Assert.That(exception.Content).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies the error body is still read when the response carries no content type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateReadsContentWhenContentTypeMissing()
    {
        using var response = CreateErrorResponse("{\"Value\":1}");
        response.Content!.Headers.ContentType = null;

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception).IsTypeOf<ApiException>();
        await Assert.That(exception.Content).IsEqualTo("{\"Value\":1}");
    }

    /// <summary>Verifies the error body is read when the content read genuinely suspends asynchronously.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateReadsContentThatCompletesAsynchronously()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = new(HttpMethod.Get, ExampleUri),
            Content = new AsyncReadContent("{\"Value\":7}")
        };

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        await Assert.That(exception.Content).IsEqualTo("{\"Value\":7}");
    }

    /// <summary>Verifies a problem+json body is built into a validation exception even when the serializer suspends asynchronously.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateBuildsValidationExceptionWhenSerializerCompletesAsynchronously()
    {
        using var response = CreateErrorResponse("{\"title\":\"invalid\"}", ProblemJsonMediaType);
        var settings = new RefitSettings(new AsyncYieldingSerializer());

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);

        await Assert.That(exception).IsTypeOf<ValidationApiException>();
    }

    /// <summary>Verifies the redaction hook can scrub credentials and bodies before the exception propagates.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExceptionRedactorScrubsSensitiveData()
    {
        using var response = CreateErrorResponse("{\"secret\":\"value\"}");
        response.RequestMessage!.Headers.Authorization = new("Bearer", "super-secret-token");
        GeneratedRequestRunner.AddRequestProperty(
            response.RequestMessage!,
            HttpRequestMessageOptions.RequestContent,
            "{\"password\":\"hunter2\"}");
        var settings = new RefitSettings
        {
            ExceptionRedactor = static ex =>
            {
                ex.RequestMessage.Headers.Authorization = null;
                ex.RequestContent = RedactedText;
                ((ApiException)ex).Content = RedactedText;
            }
        };

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            settings);

        await Assert.That(exception.RequestMessage.Headers.Authorization).IsNull();
        await Assert.That(exception.RequestContent).IsEqualTo(RedactedText);
        await Assert.That(exception.Content).IsEqualTo(RedactedText);
    }

    /// <summary>Creates an error response with an attached request.</summary>
    /// <param name="content">The response content.</param>
    /// <param name="mediaType">The optional media type.</param>
    /// <returns>The response message.</returns>
    private static HttpResponseMessage CreateErrorResponse(string content, string? mediaType = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = new(HttpMethod.Get, ExampleUri),
            Content = new StringContent(content)
        };

        if (mediaType is not null)
        {
            response.Content.Headers.ContentType = new(mediaType);
        }

        return response;
    }

    /// <summary>Content whose read genuinely suspends, forcing the asynchronous completion path when the exception reads the body.</summary>
    private sealed class AsyncReadContent : HttpContent
    {
        /// <summary>The UTF-8 payload bytes.</summary>
        private readonly byte[] _payload;

        /// <summary>Initializes a new instance of the <see cref="AsyncReadContent"/> class.</summary>
        /// <param name="payload">The string payload.</param>
        public AsyncReadContent(string payload) => _payload = System.Text.Encoding.UTF8.GetBytes(payload);

        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await Task.Yield();
            await stream.WriteAsync(_payload.AsMemory()).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = _payload.Length;
            return true;
        }
    }

    /// <summary>Content that throws when read as a string.</summary>
    private sealed class ThrowingReadContent : HttpContent
    {
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            throw new InvalidOperationException("read failed");

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 1;
            return true;
        }
    }

    /// <summary>Derived exception exposing protected constructors for coverage.</summary>
    [SuppressMessage(
        "Usage",
        "CA1032:Implement standard exception constructors",
        Justification = "This test fixture exposes only the protected ApiException constructors under test.")]
    [SuppressMessage(
        "Major Code Smell",
        "S4027:Exceptions should provide standard constructors",
        Justification = "This test fixture exposes only the protected ApiException constructors under test.")]
    private sealed class DerivedApiException : ApiException
    {
        /// <inheritdoc />
        public DerivedApiException(
            string exceptionMessage,
            HttpRequestMessage message,
            HttpMethod httpMethod,
            string? content,
            HttpStatusCode statusCode,
            string? reasonPhrase,
            System.Net.Http.Headers.HttpResponseHeaders headers,
            RefitSettings refitSettings)
            : base(
                exceptionMessage,
                message,
                httpMethod,
                content,
                statusCode,
                reasonPhrase,
                headers,
                refitSettings)
        {
        }

        /// <inheritdoc />
        public DerivedApiException(
            HttpRequestMessage message,
            HttpMethod httpMethod,
            string? content,
            HttpStatusCode statusCode,
            string? reasonPhrase,
            System.Net.Http.Headers.HttpResponseHeaders headers,
            RefitSettings refitSettings)
            : base(message, httpMethod, content, statusCode, reasonPhrase, headers, refitSettings)
        {
        }
    }

    /// <summary>A serializer that only supports asynchronous deserialization (no sync capability).</summary>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Mirrors the explicit type parameters of the wrapped IHttpContentSerializer contract.")]
    private sealed class AsyncOnlySerializer : IHttpContentSerializer
    {
        /// <summary>The wrapped serializer that does the real work.</summary>
        private readonly SystemTextJsonContentSerializer _inner = new();

        /// <inheritdoc />
        public HttpContent ToHttpContent<T>(T item) => _inner.ToHttpContent(item);

        /// <inheritdoc />
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            _inner.FromHttpContentAsync<T>(content, cancellationToken);

        /// <inheritdoc />
        public string? GetFieldNameForProperty(System.Reflection.PropertyInfo propertyInfo) =>
            _inner.GetFieldNameForProperty(propertyInfo);
    }

    /// <summary>A serializer that genuinely suspends before delegating, forcing the async deserialization path.</summary>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Mirrors the explicit type parameters of the wrapped IHttpContentSerializer contract.")]
    private sealed class AsyncYieldingSerializer : IHttpContentSerializer
    {
        /// <summary>The wrapped serializer that does the real work.</summary>
        private readonly SystemTextJsonContentSerializer _inner = new();

        /// <inheritdoc />
        public HttpContent ToHttpContent<T>(T item) => _inner.ToHttpContent(item);

        /// <inheritdoc />
        public async Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return await _inner.FromHttpContentAsync<T>(content, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public string? GetFieldNameForProperty(System.Reflection.PropertyInfo propertyInfo) =>
            _inner.GetFieldNameForProperty(propertyInfo);
    }

    /// <summary>Response model used by content deserialization tests.</summary>
    /// <param name="Value">The deserialized value.</param>
    private sealed record ResponseModel(int Value);
}
