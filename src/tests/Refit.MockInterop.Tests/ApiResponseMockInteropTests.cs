// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

using Refit.Testing;

namespace Refit.MockInterop.Tests;

/// <summary>
/// Interop tests for <see cref="IApiResponse{T}"/> driven by <see cref="StubApiResponse{T}"/>. These pin the
/// proxy behaviour the concrete <see cref="ApiResponse{T}"/> cannot exercise: that the generic
/// interface does not <c>new</c>-shadow base members (regression for #1933), and that the additive
/// <see cref="IApiResponse{T}.IsSuccessfulWithContent"/> narrows <see cref="IApiResponse{T}.Content"/>
/// correctly through an explicit interface implementation.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1859:Use concrete types when possible for improved performance",
    Justification = "These tests read the stub through IApiResponse/IApiResponse<T> on purpose; the concrete type would bypass the interface dispatch they exist to exercise.")]
public sealed class ApiResponseMockInteropTests
{
    /// <summary>A non-empty content payload used by the content-narrowing tests.</summary>
    private const string Payload = "payload";

    /// <summary>
    /// Verifies a single assignment of <see cref="IApiResponse.IsSuccessful"/> on the generic stub is
    /// observed through the non-generic <see cref="IApiResponse"/> view. A <c>new</c> shadow would
    /// split the member into two interface slots and return <c>default</c> through the base (#1933).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericSetupIsObservedThroughBaseInterfaceForIsSuccessful()
    {
        var response = new StubApiResponse<string> { IsSuccessful = true };

        await Assert.That(response.IsSuccessful).IsTrue();

        IApiResponse baseView = response;
        await Assert.That(baseView.IsSuccessful).IsTrue();
    }

    /// <summary>
    /// Verifies the same no-shadow contract for the other base members that used to be redeclared
    /// with <c>new</c>: a value set through the generic stub is seen through the non-generic interface.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericSetupIsObservedThroughBaseInterfaceForStatusAndReceived()
    {
        IApiResponse baseView = new StubApiResponse<string> { IsSuccessStatusCode = true, IsReceived = true };
        await Assert.That(baseView.IsSuccessStatusCode).IsTrue();
        await Assert.That(baseView.IsReceived).IsTrue();
    }

    /// <summary>
    /// Verifies a single assignment of <see cref="IApiResponse{T}.Content"/> on the generic stub is
    /// observed through the generic interface (the covariance-safe declaration carries the value).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericContentSetupIsObserved()
    {
        var response = new StubApiResponse<string> { Content = "payload" };

        await Assert.That(response.Content).IsEqualTo("payload");
    }

    /// <summary>
    /// Verifies <see cref="IApiResponse{T}.IsSuccessfulWithContent"/> narrows
    /// <see cref="IApiResponse{T}.Content"/> through the interface: inside the <c>true</c> branch the
    /// compiler treats <c>Content</c> as non-null (no null-forgiving operator is used below).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsSuccessfulWithContentNarrowsContentThroughMock()
    {
        IApiResponse<string> response = new StubApiResponse<string>
        {
            IsSuccessfulWithContent = true,
            Content = Payload,
        };

        await Assert.That(response.IsSuccessfulWithContent).IsTrue();
        if (response.IsSuccessfulWithContent)
        {
            // MemberNotNullWhen(true, nameof(Content)) flows here: Content is non-null without `!`.
            await Assert.That(response.Content.Length).IsEqualTo(Payload.Length);
        }
        else
        {
            Assert.Fail("IsSuccessfulWithContent should have been true.");
        }
    }

    /// <summary>
    /// Verifies a stubbed <see cref="IApiResponse{T}.IsSuccessfulWithContent"/> returns its configured
    /// value through the generic view, with no leakage from an unrelated base-member value.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsSuccessfulWithContentIsIndependentlyMockable()
    {
        var response = new StubApiResponse<string> { IsSuccessful = true, IsSuccessfulWithContent = false };

        await Assert.That(response.IsSuccessful).IsTrue();
        await Assert.That(response.IsSuccessfulWithContent).IsFalse();
    }

    /// <summary>Verifies HasContent narrows Content to non-null through the interface.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HasContentNarrowsContentThroughMock()
    {
        IApiResponse<string> response = new StubApiResponse<string>
        {
            HasContent = true,
            Content = Payload,
        };

        if (response.HasContent)
        {
            // MemberNotNullWhen(true, nameof(Content)) flows here: Content is non-null without `!`.
            await Assert.That(response.Content.Length).IsEqualTo(Payload.Length);
        }
        else
        {
            Assert.Fail("HasContent should have been true.");
        }
    }

    /// <summary>
    /// Verifies a successful response with no content headers is a representable interface state, which
    /// is why <see cref="IApiResponse.IsSuccessStatusCode"/> no longer narrows
    /// <see cref="IApiResponse.ContentHeaders"/>. A 204 (No Content), a HEAD response, or a .NET
    /// Framework response whose <c>Content</c> was never set all produce success with null content headers.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SuccessStatusCodeDoesNotImplyContentHeaders()
    {
        var response = new StubApiResponse<string> { IsSuccessStatusCode = true, ContentHeaders = null };

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.ContentHeaders).IsNull();
    }

    /// <summary>
    /// Regression for #1933: a method that takes the non-generic <see cref="IApiResponse"/> must observe
    /// the <see cref="IApiResponse.StatusCode"/> / <see cref="IApiResponse.ContentHeaders"/> set on a
    /// stub of the generic <see cref="IApiResponse{T}"/>. The original <c>new</c>-shadow split those into
    /// separate slots, so the base view read <c>null</c> and the problem-details check wrongly returned false.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ProblemDetailsCheckSeesGenericSetupThroughBaseInterface()
    {
        var headers = new StringContent(string.Empty).Headers;
        headers.ContentType = new("application/problem+json");

        var response = new StubApiResponse<string>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.UnprocessableEntity,
            ContentHeaders = headers,
        };

        await Assert.That(IsProblemDetails(response)).IsTrue();
    }

    /// <summary>
    /// Regression for #1949: reading <see cref="IApiResponse.Error"/> through the non-generic interface must
    /// return the error set on the generic stub. The <c>new</c>-shadow made <c>IApiResponse.Error</c> a
    /// distinct, always-null slot, so a <c>Verify(IApiResponse r)</c> method never saw the captured error.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorIsObservedThroughBaseInterfaceFromGenericMock()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        var error = new ApiRequestException("failed", request, HttpMethod.Get, new RefitSettings());

        // A `Verify(IApiResponse r)` method reads the error through the non-generic interface; the
        // generic value must be observed there (it was a distinct, always-null slot before the fix).
        IApiResponse baseView = new StubApiResponse<string> { Error = error };
        await Assert.That(baseView.Error).IsSameReferenceAs(error);
    }

    /// <summary>Returns whether the response is an RFC 7807 problem-details payload, reading only the non-generic interface.</summary>
    /// <param name="apiResponse">The response, viewed through the non-generic interface.</param>
    /// <returns><c>true</c> when the response is a 422 with a problem-details content type.</returns>
    private static bool IsProblemDetails(IApiResponse apiResponse)
    {
        var contentType = apiResponse.ContentHeaders?.ContentType?.MediaType;

        return apiResponse.StatusCode == HttpStatusCode.UnprocessableEntity
            && string.Equals(contentType, "application/problem+json", StringComparison.Ordinal);
    }
}
