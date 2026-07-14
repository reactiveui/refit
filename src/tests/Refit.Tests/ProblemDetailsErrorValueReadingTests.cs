// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>
/// Verifies how <see cref="ValidationApiException"/> reads non-string RFC 7807 values: a validation error message
/// that is not a JSON string is preserved as raw text, and a plain (non date-time) string extension member is read
/// back as its string value.
/// </summary>
public sealed class ProblemDetailsErrorValueReadingTests
{
    /// <summary>The example request URI reused across the tests.</summary>
    private const string ExampleUri = "https://example.test";

    /// <summary>The problem-details media type reused across the tests.</summary>
    private const string ProblemJsonMediaType = "application/problem+json";

    /// <summary>Verifies a non-string validation error value is preserved as its raw JSON text.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA1849:Call async methods when in an async method", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    [SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    public async Task NonStringErrorMessageIsPreservedAsRawJson()
    {
        using var response = CreateErrorResponse(
            "{\"title\":\"invalid\",\"errors\":{\"Payload\":[{\"nested\":\"value\"}]}}",
            ProblemJsonMediaType);
        var apiException = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var validationException = ValidationApiException.Create(apiException);

        await Assert.That(validationException.Content).IsNotNull();
        await Assert.That(validationException.Content!.Errors["Payload"][0]).IsEqualTo("{\"nested\":\"value\"}");
    }

    /// <summary>Verifies a plain (non date-time) string extension member is read back as its string value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA1849:Call async methods when in an async method", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    [SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "This test intentionally covers the synchronous compatibility factory.")]
    public async Task PlainStringExtensionIsReadAsString()
    {
        using var response = CreateErrorResponse(
            "{\"title\":\"invalid\",\"note\":\"plain-text\"}",
            ProblemJsonMediaType);
        var apiException = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            new());

        var validationException = ValidationApiException.Create(apiException);

        await Assert.That(validationException.Content).IsNotNull();
        await Assert.That(validationException.Content!.Extensions["note"]).IsEqualTo("plain-text");
    }

    /// <summary>Creates a bad-request error response carrying the given problem-details body.</summary>
    /// <param name="content">The response content.</param>
    /// <param name="mediaType">The response media type.</param>
    /// <returns>The response message.</returns>
    private static HttpResponseMessage CreateErrorResponse(string content, string mediaType)
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = new(HttpMethod.Get, ExampleUri),
            Content = new StringContent(content)
        };

        response.Content.Headers.ContentType = new(mediaType);
        return response;
    }
}
