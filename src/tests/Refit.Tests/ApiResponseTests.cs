// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;

namespace Refit.Tests;

/// <summary>Tests for direct <see cref="ApiResponse{T}"/> construction and error handling.</summary>
public sealed class ApiResponseTests
{
    /// <summary>Verifies the generic interface does not shadow base members, so a single setup is observed via the base interface.</summary>
    /// <param name="memberName">The member expected to be declared only on the non-generic interface.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(nameof(IApiResponse.ContentHeaders))]
    [Arguments(nameof(IApiResponse.Error))]
    [Arguments(nameof(IApiResponse.IsSuccessStatusCode))]
    [Arguments(nameof(IApiResponse.IsSuccessful))]
    public async Task GenericApiResponseDoesNotShadowBaseMembers(string memberName)
    {
        // The generic interface must not redeclare (shadow) the member: a single setup on
        // IApiResponse<T> should be observed through the non-generic IApiResponse as well.
        var declaredOnGeneric = typeof(IApiResponse<string>).GetProperty(
            memberName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        await Assert.That(declaredOnGeneric).IsNull();

        await Assert.That(typeof(IApiResponse).GetProperty(memberName)).IsNotNull();
    }

    /// <summary>Verifies the generic interface declares the covariance-safe <c>HasContent</c> narrowing member.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericApiResponseDeclaresHasContent()
    {
        var hasContent = typeof(IApiResponse<string>).GetProperty(
            "HasContent",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        await Assert.That(hasContent).IsNotNull();
    }

    /// <summary>Verifies HasContent reports content availability and ensure-success returns a successful response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SuccessfulResponseHasContentAndPassesEnsureSuccess()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
        using var response = new ApiResponse<string>(httpResponse, "body", new());

        await Assert.That(response.HasContent).IsTrue();

        IApiResponse<string> asInterface = response;
        var ensured = await asInterface.EnsureSuccessStatusCodeAsync();
        await Assert.That(ensured).IsSameReferenceAs(response);
    }

    /// <summary>Verifies ensure-success on the interface throws the captured error for an unsuccessful response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnsuccessfulResponseEnsureSuccessThrowsCapturedError()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request };
        var error = await ApiException.Create(request, HttpMethod.Get, httpResponse, new());
        using var response = new ApiResponse<string>(httpResponse, null, new(), error);

        await Assert.That(response.HasContent).IsFalse();

        IApiResponse<string> asInterface = response;
        await Assert
            .That(() => (Task)asInterface.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<ApiException>();
    }

    /// <summary>Verifies EnsureSuccessfulAsync returns the response on success and throws the error on failure.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnsureSuccessfulAsyncReturnsOnSuccessAndThrowsOnFailure()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        using var okResponse = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
        using var success = new ApiResponse<string>(okResponse, "body", new());

        IApiResponse<string> okInterface = success;
        await Assert.That(await okInterface.EnsureSuccessfulAsync()).IsSameReferenceAs(success);

        using var badResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request };
        var error = await ApiException.Create(request, HttpMethod.Get, badResponse, new());
        using var failure = new ApiResponse<string>(badResponse, null, new(), error);

        IApiResponse<string> failInterface = failure;
        await Assert
            .That(() => (Task)failInterface.EnsureSuccessfulAsync())
            .ThrowsExactly<ApiException>();
    }

    /// <summary>Verifies the success-guard extensions reject a null response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnsureSuccessExtensionsRejectNullResponse()
    {
        var nullResponse = (IApiResponse<string>)null!;

        await Assert
            .That(() => (Task)nullResponse.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<ArgumentNullException>();
        await Assert
            .That(() => (Task)nullResponse.EnsureSuccessfulAsync())
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies an unsuccessful response without a captured error throws a descriptive fallback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnsureSuccessThrowsFallbackWhenNoErrorCaptured()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        using var badResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request };
        using var response = new ApiResponse<string>(badResponse, null, new());

        IApiResponse<string> asInterface = response;
        await Assert
            .That(() => (Task)asInterface.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<InvalidOperationException>();
    }

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
