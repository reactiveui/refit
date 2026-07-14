// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;

namespace Refit.Tests;

/// <summary>Tests for direct <see cref="ApiResponse{T}"/> construction and error handling.</summary>
public sealed class ApiResponseTests
{
    /// <summary>The example request URI reused across the response tests.</summary>
    private const string ExampleUri = "https://example.test";

    /// <summary>The response body content reused across the payload tests.</summary>
    private const string PayloadContent = "payload";

    /// <summary>The content value reused across the successful-response tests.</summary>
    private const string HelloContent = "hello";

    /// <summary>The expected length of the five-character body used by the narrowing tests.</summary>
    private const int ExpectedContentLength = 5;

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
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
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
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request };
        var error = await ApiException.Create(request, HttpMethod.Get, httpResponse, new());
        using var response = new ApiResponse<string>(httpResponse, null, new(), error);

        await Assert.That(response.HasContent).IsFalse();

        IApiResponse<string> asInterface = response;
        await Assert
            .That(async () => await asInterface.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<ApiException>();
    }

    /// <summary>Verifies EnsureSuccessfulAsync returns the response on success and throws the error on failure.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnsureSuccessfulAsyncReturnsOnSuccessAndThrowsOnFailure()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var okResponse = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
        using var success = new ApiResponse<string>(okResponse, "body", new());

        IApiResponse<string> okInterface = success;
        await Assert.That(await okInterface.EnsureSuccessfulAsync()).IsSameReferenceAs(success);

        using var badResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request };
        var error = await ApiException.Create(request, HttpMethod.Get, badResponse, new());
        using var failure = new ApiResponse<string>(badResponse, null, new(), error);

        IApiResponse<string> failInterface = failure;
        await Assert
            .That(async () => await failInterface.EnsureSuccessfulAsync())
            .ThrowsExactly<ApiException>();
    }

    /// <summary>Verifies the success-guard extensions reject a null response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnsureSuccessExtensionsRejectNullResponse()
    {
        const IApiResponse<string> nullResponse = null!;

        await Assert
            .That(static async () => await nullResponse.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<ArgumentNullException>();
        await Assert
            .That(static async () => await nullResponse.EnsureSuccessfulAsync())
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies an unsuccessful response without a captured error throws a descriptive fallback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnsureSuccessThrowsFallbackWhenNoErrorCaptured()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var badResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request };
        using var response = new ApiResponse<string>(badResponse, null, new());

        IApiResponse<string> asInterface = response;
        await Assert
            .That(async () => await asInterface.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies public constructors reject missing response data needed by the wrapper.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstructorsRequireResponseAndRequestMessage()
    {
        await Assert.That(static () => new ApiResponse<string>(null!, "body", new()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(static () => new ApiResponse<string>(new(HttpStatusCode.OK), "body", new()))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies a response without an HTTP response reports unavailable state and throws cleanly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MissingResponseReportsUnavailableState()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
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
        await Assert.That(async () => await response.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies success wrappers expose response metadata and return themselves from ensure methods.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SuccessResponseExposesMetadataAndUsesFastEnsurePath()
    {
        using var responseMessage = CreateResponse(HttpStatusCode.OK, PayloadContent);
        using var response = new ApiResponse<string>(responseMessage, PayloadContent, new());

        var success = await response.EnsureSuccessStatusCodeAsync();
        var successful = await response.EnsureSuccessfulAsync();

        await Assert.That(success).IsSameReferenceAs(response);
        await Assert.That(successful).IsSameReferenceAs(response);
        await Assert.That(response.Content).IsEqualTo(PayloadContent);
        await Assert.That(response.Headers).IsNotNull();
        await Assert.That(response.ContentHeaders).IsNotNull();
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.IsSuccessful).IsTrue();
        await Assert.That(response.IsReceived).IsTrue();
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Version).IsEqualTo(responseMessage.Version);
    }

    /// <summary>Verifies the concrete ensure path builds a fresh exception when an unsuccessful response has no captured error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConcreteEnsureBuildsExceptionWhenNoErrorCaptured()
    {
        using var badResponse = CreateResponse(HttpStatusCode.BadRequest, "boom");
        using var response = new ApiResponse<string>(badResponse, null, new());

        // No captured error, so ThrowsApiExceptionAsync takes the `?? ApiException.Create(...)` branch.
        await Assert.That(async () => await response.EnsureSuccessfulAsync())
            .ThrowsExactly<ApiException>();
    }

    /// <summary>Verifies typed error helpers distinguish request and response errors.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorHelpersDistinguishRequestAndResponseErrors()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
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
        await Assert.That(async () => await responseErrorResponse.EnsureSuccessfulAsync())
            .ThrowsExactly<ApiException>();
    }

    /// <summary>Verifies IsSuccessfulWithContent is true only when the request succeeded and content is present.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsSuccessfulWithContentReflectsSuccessAndContentPresence()
    {
        // Success + content -> true.
        using var withContent = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), "body", new());
        await Assert.That(withContent.IsSuccessful).IsTrue();
        await Assert.That(withContent.IsSuccessfulWithContent).IsTrue();

        // Success but no content (a 2xx with a null/empty body deserializes to null with no error)
        // -> IsSuccessful stays true, but IsSuccessfulWithContent is false. This is the case the old
        // IsSuccessful -> Content narrowing got wrong.
        using var okNoContent = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), null, new());
        await Assert.That(okNoContent.IsSuccessful).IsTrue();
        await Assert.That(okNoContent.HasContent).IsFalse();
        await Assert.That(okNoContent.IsSuccessfulWithContent).IsFalse();

        // 2xx with a captured error (for example a deserialization failure on an otherwise-successful
        // response) -> IsSuccessStatusCode stays true but IsSuccessful is false, so it is not
        // successful-with-content. The error object models the deserialization failure.
        using var okResponse = CreateResponse(HttpStatusCode.OK, "x");
        using var failedResponse = CreateResponse(HttpStatusCode.BadRequest, "boom");
        var deserError = await ApiException.Create(failedResponse.RequestMessage!, HttpMethod.Get, failedResponse, new());
        using var okWithError = new ApiResponse<string>(okResponse, "body", new(), deserError);
        await Assert.That(okWithError.IsSuccessStatusCode).IsTrue();
        await Assert.That(okWithError.IsSuccessful).IsFalse();
        await Assert.That(okWithError.IsSuccessfulWithContent).IsFalse();

        // Non-success with content present -> not successful-with-content.
        using var badWithContent = new ApiResponse<string>(CreateResponse(HttpStatusCode.BadRequest, "x"), "body", new());
        await Assert.That(badWithContent.HasContent).IsTrue();
        await Assert.That(badWithContent.IsSuccessfulWithContent).IsFalse();

        // No response received -> not successful-with-content.
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var noResponse = new ApiResponse<string>(request, null, "body", new());
        await Assert.That(noResponse.IsSuccessfulWithContent).IsFalse();
    }

    /// <summary>
    /// Verifies IsSuccessfulWithContent narrows Content through the covariant <see cref="IApiResponse{T}"/>
    /// (the design relies on <c>out T</c>): an <see cref="IApiResponse{T}"/> of a more-derived type is used
    /// as a less-derived one, and the narrowing still holds with no null-forgiving operator.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsSuccessfulWithContentNarrowsThroughCovariantInterface()
    {
        using var concrete = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), "hi", new());

        // Passing ApiResponse<string> where IApiResponse<object> is expected exercises out-T covariance.
        await AssertNarrows(concrete);

        static async Task AssertNarrows(IApiResponse<object> covariant)
        {
            if (covariant.IsSuccessfulWithContent)
            {
                // Content (viewed as object) is non-null here without `!`.
                await Assert.That(covariant.Content).IsEqualTo("hi");
            }
            else
            {
                Assert.Fail("IsSuccessfulWithContent should have been true.");
            }
        }
    }

    /// <summary>Verifies IsSuccessfulWithContent narrows Content to non-null inside its true branch.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsSuccessfulWithContentNarrowsContent()
    {
        using var response = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), HelloContent, new());

        if (response.IsSuccessfulWithContent)
        {
            // MemberNotNullWhen(true, nameof(Content)) flows here: Content is non-null without `!`.
            await Assert.That(response.Content.Length).IsEqualTo(ExpectedContentLength);
        }
        else
        {
            Assert.Fail("IsSuccessfulWithContent should have been true.");
        }
    }

    /// <summary>Verifies HasContent narrows Content to non-null inside its true branch.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HasContentNarrowsContent()
    {
        using var response = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), "world", new());

        if (response.HasContent)
        {
            // MemberNotNullWhen(true, nameof(Content)) flows here: Content is non-null without `!`.
            await Assert.That(response.Content.Length).IsEqualTo(ExpectedContentLength);
        }
        else
        {
            Assert.Fail("HasContent should have been true.");
        }
    }

    /// <summary>
    /// Verifies the surviving success-state annotations narrow at compile time: a successful response
    /// guarantees the received-response metadata (Headers, StatusCode, Version) is non-null. The
    /// property bodies below dereference each member with no null-forgiving operator, so this fails to
    /// compile if any of those annotations are dropped.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SuccessfulResponseNarrowsReceivedMetadata()
    {
        using var response = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), "body", new());

        if (response.IsSuccessful)
        {
            // No `!` on any of these: the MemberNotNullWhen(true, ...) annotations narrow them.
            await Assert.That(response.Headers.Contains("nonexistent-header")).IsFalse();
            await Assert.That(response.StatusCode.Value).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(response.Version.Major).IsGreaterThanOrEqualTo(1);
        }
        else
        {
            Assert.Fail("IsSuccessful should have been true.");
        }
    }

    /// <summary>
    /// Verifies an unsuccessful response does not guarantee a non-null Error: a failure can carry a
    /// captured error, or none at all (the audit removed MemberNotNullWhen(false, nameof(Error)) because
    /// the two are independent). Refit itself falls back when an unsuccessful response has no error.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnsuccessfulResponseErrorIsNotGuaranteed()
    {
        // Failure WITH a captured error -> Error present.
        using var badResponse = CreateResponse(HttpStatusCode.BadRequest, "boom");
        var error = await ApiException.Create(badResponse.RequestMessage!, HttpMethod.Get, badResponse, new());
        using var withError = new ApiResponse<string>(badResponse, null, new(), error);
        await Assert.That(withError.IsSuccessful).IsFalse();
        await Assert.That(withError.Error).IsNotNull();

        // Failure WITHOUT a captured error -> Error is null even though the response is unsuccessful.
        using var failureResponse = CreateResponse(HttpStatusCode.BadRequest, "boom");
        using var withoutError = new ApiResponse<string>(failureResponse, null, new());
        await Assert.That(withoutError.IsSuccessful).IsFalse();
        await Assert.That(withoutError.IsSuccessStatusCode).IsFalse();
        await Assert.That(withoutError.Error).IsNull();
    }

    /// <summary>Verifies a 204 No Content success carries no deserialized content (the issue headline case).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NoContentSuccessHasNoContent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var noContent = new HttpResponseMessage(HttpStatusCode.NoContent) { RequestMessage = request };
        using var response = new ApiResponse<string>(noContent, null, new());

        // 204 is a 2xx, so the call succeeded...
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.IsSuccessful).IsTrue();

        // ...but there is no deserialized content, so the content signals are false.
        await Assert.That(response.HasContent).IsFalse();
        await Assert.That(response.IsSuccessfulWithContent).IsFalse();
    }

    /// <summary>Verifies a successful response that does carry content reports content present (including a 204 with a body).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SuccessWithContentReportsContentPresent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var noContentWithBody = new HttpResponseMessage(HttpStatusCode.NoContent) { RequestMessage = request };
        using var response = new ApiResponse<string>(noContentWithBody, "body", new());

        await Assert.That(response.IsSuccessful).IsTrue();
        await Assert.That(response.HasContent).IsTrue();
        await Assert.That(response.IsSuccessfulWithContent).IsTrue();
    }

    /// <summary>Verifies a 304 Not Modified is not treated as a success status (it is outside the 2xx range).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NotModifiedIsNotSuccessStatusCode()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var notModified = new HttpResponseMessage(HttpStatusCode.NotModified) { RequestMessage = request };
        using var response = new ApiResponse<string>(notModified, null, new());

        await Assert.That(response.IsSuccessStatusCode).IsFalse();
        await Assert.That(response.IsSuccessful).IsFalse();
        await Assert.That(response.IsReceived).IsTrue();
        await Assert.That(response.IsSuccessfulWithContent).IsFalse();
    }

    /// <summary>Verifies a request error (no response received) is received-false with content-narrowing false.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestErrorWithoutResponseIsNotReceived()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        var requestError = new ApiRequestException("failed", request, HttpMethod.Get, new());
        using var response = new ApiResponse<string>(request, null, "body", new(), requestError);

        await Assert.That(response.IsReceived).IsFalse();
        await Assert.That(response.IsSuccessStatusCode).IsFalse();
        await Assert.That(response.IsSuccessful).IsFalse();
        await Assert.That(response.Error).IsNotNull();

        // Content was supplied but no response was received, so it is not successful-with-content.
        await Assert.That(response.Content).IsEqualTo("body");
        await Assert.That(response.HasContent).IsTrue();
        await Assert.That(response.IsSuccessfulWithContent).IsFalse();
    }

    /// <summary>Verifies empty-but-non-null content counts as content: HasContent checks null, not emptiness.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyStringContentCountsAsContent()
    {
        using var response = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), string.Empty, new());

        await Assert.That(response.HasContent).IsTrue();
        await Assert.That(response.IsSuccessfulWithContent).IsTrue();
        await Assert.That(response.Content).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies a value-type body is always present (a value type is never null), so HasContent tracks success.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueTypeContentIsAlwaysPresent()
    {
        const int bodyValue = 5;
        using var success = new ApiResponse<int>(CreateResponse(HttpStatusCode.OK, "5"), bodyValue, new());
        await Assert.That(success.HasContent).IsTrue();
        await Assert.That(success.IsSuccessfulWithContent).IsTrue();

        // A default value (0) is still non-null for a value type, so content is reported present.
        using var defaulted = new ApiResponse<int>(CreateResponse(HttpStatusCode.OK, "0"), 0, new());
        await Assert.That(defaulted.HasContent).IsTrue();
        await Assert.That(defaulted.IsSuccessfulWithContent).IsTrue();
    }

    /// <summary>Verifies a nullable value-type body tracks null state through the content signals.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullableValueTypeContentTracksNull()
    {
        const int presentValue = 7;
        using var present = new ApiResponse<int?>(CreateResponse(HttpStatusCode.OK, "7"), presentValue, new());
        await Assert.That(present.HasContent).IsTrue();
        await Assert.That(present.IsSuccessfulWithContent).IsTrue();

        using var absent = new ApiResponse<int?>(CreateResponse(HttpStatusCode.OK, "x"), null, new());
        await Assert.That(absent.IsSuccessful).IsTrue();
        await Assert.That(absent.HasContent).IsFalse();
        await Assert.That(absent.IsSuccessfulWithContent).IsFalse();
    }

    /// <summary>Verifies IsSuccessStatusCode narrows the received-response metadata to non-null.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsSuccessStatusCodeNarrowsReceivedMetadata()
    {
        using var response = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), "body", new());

        if (response.IsSuccessStatusCode)
        {
            // No `!`: MemberNotNullWhen(true, ...) narrows Headers/StatusCode/Version here.
            await Assert.That(response.Headers.Contains("nope")).IsFalse();
            await Assert.That(response.StatusCode.Value).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(response.Version.Major).IsGreaterThanOrEqualTo(1);
        }
        else
        {
            Assert.Fail("IsSuccessStatusCode should have been true.");
        }
    }

    /// <summary>Verifies IsReceived narrows the received-response metadata to non-null.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsReceivedNarrowsReceivedMetadata()
    {
        using var response = new ApiResponse<string>(CreateResponse(HttpStatusCode.BadRequest, "x"), null, new());

        if (response.IsReceived)
        {
            // A received-but-unsuccessful response still guarantees the metadata is non-null.
            await Assert.That(response.Headers.Contains("nope")).IsFalse();
            await Assert.That(response.StatusCode.Value).IsEqualTo(HttpStatusCode.BadRequest);
            await Assert.That(response.Version.Major).IsGreaterThanOrEqualTo(1);
        }
        else
        {
            Assert.Fail("IsReceived should have been true.");
        }
    }

    /// <summary>Verifies content signals remain correct after the response is disposed.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ContentSignalsSurviveDisposal()
    {
        var response = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), "kept", new());
        response.Dispose();
        response.Dispose();

        await Assert.That(response.HasContent).IsTrue();
        await Assert.That(response.IsSuccessfulWithContent).IsTrue();
        await Assert.That(response.Content).IsEqualTo("kept");
    }

    /// <summary>
    /// Verifies the reviewer's existing guard `if (!IsSuccessStatusCode || Content is null)` still compiles
    /// and narrows Content in the success-with-content branch (#2155 feedback: existing code is unaffected).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StatusCodeOrNullContentGuardStillNarrows()
    {
        using var ok = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), HelloContent, new());
        if (!ok.IsSuccessStatusCode || ok.Content is null)
        {
            Assert.Fail("Expected a successful response with content.");
        }
        else
        {
            // The explicit null check narrows Content here without `!`.
            await Assert.That(ok.Content.Length).IsEqualTo(ExpectedContentLength);
        }

        // The same guard correctly rejects a successful response that has no content.
        using var okNoContent = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), null, new());
        await Assert.That(!okNoContent.IsSuccessStatusCode || okNoContent.Content is null).IsTrue();
    }

    /// <summary>
    /// Verifies the reviewer's two-condition guard `if (!IsSuccessful || !HasContent)` works, and that the
    /// new single-condition `IsSuccessfulWithContent` is exactly its negation (#2155 single-check request).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SuccessfulOrHasContentGuardEqualsIsSuccessfulWithContent()
    {
        using var ok = new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), HelloContent, new());
        if (!ok.IsSuccessful || !ok.HasContent)
        {
            Assert.Fail("Expected a successful response with content.");
        }
        else
        {
            await Assert.That(ok.Content.Length).IsEqualTo(ExpectedContentLength);
        }

        // The single-condition member equals the negation of the two-condition guard for every state.
        foreach (var response in EnumerateResponseStates())
        {
            using (response)
            {
                var guardSaysHasIt = response is { IsSuccessful: true, HasContent: true };
                await Assert.That(response.IsSuccessfulWithContent).IsEqualTo(guardSaysHasIt);
            }
        }
    }

    /// <summary>
    /// Verifies IsSuccessful and IsSuccessStatusCode are not duplicates (#2155 question): a 2xx with a
    /// captured deserialization error is IsSuccessStatusCode-true but IsSuccessful-false.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsSuccessfulAndIsSuccessStatusCodeDifferOnError()
    {
        using var okResponse = CreateResponse(HttpStatusCode.OK, "x");
        using var failureResponse = CreateResponse(HttpStatusCode.BadRequest, "boom");
        var deserError = await ApiException.Create(failureResponse.RequestMessage!, HttpMethod.Get, failureResponse, new());
        using var response = new ApiResponse<string>(okResponse, "body", new(), deserError);

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.IsSuccessful).IsFalse();
    }

    /// <summary>
    /// Regression for #1949: a captured error on a concrete <see cref="ApiResponse{T}"/> is visible after
    /// casting to the non-generic <see cref="IApiResponse"/>, so a `Verify(IApiResponse r)` method sees it.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorIsVisibleThroughBaseInterfaceOnConcreteResponse()
    {
        using var badResponse = CreateResponse(HttpStatusCode.BadRequest, "boom");
        var error = await ApiException.Create(badResponse.RequestMessage!, HttpMethod.Get, badResponse, new());
        using var response = new ApiResponse<string>(badResponse, null, new(), error);

        await Assert.That(((IApiResponse)response).Error).IsSameReferenceAs(error);
        await Assert.That(((IApiResponse)response).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    /// <summary>Yields one constructed response per interesting state for exhaustive cross-checks.</summary>
    /// <returns>The constructed responses; each must be disposed by the caller.</returns>
    private static IEnumerable<ApiResponse<string>> EnumerateResponseStates()
    {
        // success + content
        yield return new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), "body", new());

        // success + no content
        yield return new ApiResponse<string>(CreateResponse(HttpStatusCode.OK, "x"), null, new());

        // 204 + no content
        yield return new ApiResponse<string>(CreateResponse(HttpStatusCode.NoContent, "x"), null, new());

        // failure + content
        yield return new ApiResponse<string>(CreateResponse(HttpStatusCode.BadRequest, "x"), "body", new());

        // failure + no content
        yield return new ApiResponse<string>(CreateResponse(HttpStatusCode.BadRequest, "x"), null, new());

        // no response received + content
        yield return new ApiResponse<string>(new(HttpMethod.Get, ExampleUri), null, "body", new());
    }

    /// <summary>Creates an HTTP response message with an attached request.</summary>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="content">The response content.</param>
    /// <returns>The response message.</returns>
    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content) =>
        new(statusCode)
        {
            RequestMessage = new(HttpMethod.Get, ExampleUri),
            Content = new StringContent(content)
        };
}
