// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Refit.Tests;

/// <summary>Tests for API exception factory and validation exception edge cases.</summary>
[RequiresUnreferencedCode("ValidationApiException.Create uses System.Text.Json reflection-based deserialization.")]
[RequiresDynamicCode("ValidationApiException.Create uses System.Text.Json reflection-based deserialization.")]
public sealed class ApiExceptionTests
{
    /// <summary>Verifies ApiException factory guard clauses.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateRejectsSuccessfulOrMissingResponses()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        using var success = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };

        await Assert.That(() => (Task)ApiException.Create(request, HttpMethod.Get, success, new()))
            .ThrowsExactly<ArgumentException>();
        await Assert.That(() => (Task)ApiException.Create("message", request, HttpMethod.Get, null!, new()))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies ApiException factory handles absent and unreadable content without hiding the original response error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateHandlesMissingAndUnreadableContent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
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
        using var response = CreateErrorResponse("{\"Value\":42}");
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

        await Assert.That(model!.Value).IsEqualTo(42);
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
        var messageOnly = new ValidationApiException("message");
        var withInner = new ValidationApiException("message", inner);

        await Assert.That(messageOnly.Message).IsEqualTo("message");
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
            "application/problem+json");
        var apiException = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var validationException = ValidationApiException.Create(apiException);

        await Assert.That(validationException.Content).IsNotNull();
        await Assert.That(validationException.Content!.Title).IsEqualTo("invalid");
        await Assert.That(validationException.Content.Status).IsEqualTo(400);
        await Assert.That(validationException.Content.Errors["Name"][0]).IsEqualTo("Required");
    }

    /// <summary>Creates an error response with an attached request.</summary>
    /// <param name="content">The response content.</param>
    /// <param name="mediaType">The optional media type.</param>
    /// <returns>The response message.</returns>
    private static HttpResponseMessage CreateErrorResponse(string content, string? mediaType = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = new(HttpMethod.Get, "https://example.test"),
            Content = new StringContent(content)
        };

        if (mediaType is not null)
        {
            response.Content.Headers.ContentType = new(mediaType);
        }

        return response;
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

    /// <summary>Response model used by content deserialization tests.</summary>
    /// <param name="Value">The deserialized value.</param>
    private sealed record ResponseModel(int Value);
}
