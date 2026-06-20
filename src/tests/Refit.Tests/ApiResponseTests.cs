// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Tests;

/// <summary>Tests for direct <see cref="ApiResponse{T}"/> construction and error handling.</summary>
public sealed class ApiResponseTests
{
    /// <summary>Verifies public constructors reject missing response data needed by the wrapper.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstructorsRequireResponseAndRequestMessage()
    {
        await Assert.That(() => new ApiResponse<string>(null!, "body", new()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => new ApiResponse<string>(new(HttpStatusCode.OK), "body", new()))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies a response without an HTTP response reports unavailable state and throws cleanly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MissingResponseReportsUnavailableState()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        using var response = new ApiResponse<string>(request, null, null, new());

        await Assert.That(response.Content).IsNull();
        await Assert.That(response.Headers).IsNull();
        await Assert.That(response.ContentHeaders).IsNull();
        await Assert.That(response.IsSuccessStatusCode).IsFalse();
        await Assert.That(response.IsSuccessful).IsFalse();
        await Assert.That(response.IsReceived).IsFalse();
        await Assert.That(response.ReasonPhrase).IsNull();
        await Assert.That(response.StatusCode).IsNull();
        await Assert.That(response.Version).IsNull();
        await Assert.That(response.RequestMessage).IsSameReferenceAs(request);
        await Assert.That(() => (Task)response.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies success wrappers expose response metadata and return themselves from ensure methods.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SuccessResponseExposesMetadataAndUsesFastEnsurePath()
    {
        using var responseMessage = CreateResponse(HttpStatusCode.OK, "payload");
        using var response = new ApiResponse<string>(responseMessage, "payload", new());

        var success = await response.EnsureSuccessStatusCodeAsync();
        var successful = await response.EnsureSuccessfulAsync();

        await Assert.That(success).IsSameReferenceAs(response);
        await Assert.That(successful).IsSameReferenceAs(response);
        await Assert.That(response.Content).IsEqualTo("payload");
        await Assert.That(response.Headers).IsNotNull();
        await Assert.That(response.ContentHeaders).IsNotNull();
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.IsSuccessful).IsTrue();
        await Assert.That(response.IsReceived).IsTrue();
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Version).IsEqualTo(responseMessage.Version);
    }

    /// <summary>Verifies typed error helpers distinguish request and response errors.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorHelpersDistinguishRequestAndResponseErrors()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        var requestError = new ApiRequestException("failed", request, HttpMethod.Get, new());
        var innerRequestError = new ApiRequestException(
            request,
            HttpMethod.Post,
            new(),
            new InvalidOperationException("inner failure"));
        var requestErrorWithInner = new ApiRequestException(
            "failed with inner",
            request,
            HttpMethod.Put,
            new(),
            new InvalidOperationException("inner"));
        using var requestErrorResponse = new ApiResponse<string>(request, null, null, new(), requestError);
        using var apiResponseMessage = CreateResponse(HttpStatusCode.BadRequest, "bad");
        var apiError = await ApiException.Create(
            apiResponseMessage.RequestMessage!,
            HttpMethod.Get,
            apiResponseMessage,
            new());
        using var responseErrorResponse = new ApiResponse<string>(
            apiResponseMessage.RequestMessage!,
            apiResponseMessage,
            null,
            new(),
            apiError);

        await Assert.That(requestErrorResponse.HasRequestError(out var typedRequestError)).IsTrue();
        await Assert.That(typedRequestError).IsSameReferenceAs(requestError);
        await Assert.That(innerRequestError.Message).IsEqualTo("inner failure");
        await Assert.That(innerRequestError.Uri).IsEqualTo(request.RequestUri);
        await Assert.That(requestErrorWithInner.InnerException).IsTypeOf<InvalidOperationException>();
        await Assert.That(requestErrorResponse.HasResponseError(out _)).IsFalse();
        await Assert.That(responseErrorResponse.HasResponseError(out var typedResponseError)).IsTrue();
        await Assert.That(typedResponseError).IsSameReferenceAs(apiError);
        await Assert.That(responseErrorResponse.HasRequestError(out _)).IsFalse();
        await Assert.That(() => (Task)responseErrorResponse.EnsureSuccessfulAsync())
            .ThrowsExactly<ApiException>();
    }

    /// <summary>Creates an HTTP response message with an attached request.</summary>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="content">The response content.</param>
    /// <returns>The response message.</returns>
    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content) =>
        new(statusCode)
        {
            RequestMessage = new(HttpMethod.Get, "https://example.test"),
            Content = new StringContent(content)
        };
}
