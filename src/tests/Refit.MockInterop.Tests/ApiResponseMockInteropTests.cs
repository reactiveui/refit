// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;

using Moq;

namespace Refit.MockInterop.Tests;

/// <summary>
/// Moq-based interop tests for <see cref="IApiResponse{T}"/>. These pin the proxy behaviour the
/// concrete <see cref="ApiResponse{T}"/> cannot exercise: that the generic interface does not
/// <c>new</c>-shadow base members (regression for #1933), and that the additive
/// <see cref="IApiResponse{T}.IsSuccessfulWithContent"/> narrows <see cref="IApiResponse{T}.Content"/>
/// correctly through a mock. Moq lives only in this isolated interop project.
/// </summary>
public sealed class ApiResponseMockInteropTests
{
    /// <summary>
    /// Verifies a single setup of <see cref="IApiResponse.IsSuccessful"/> on the generic mock is
    /// observed through the non-generic <see cref="IApiResponse"/> view. A <c>new</c> shadow would
    /// split the member into two interface slots and return <c>default</c> through the base (#1933).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericSetupIsObservedThroughBaseInterfaceForIsSuccessful()
    {
        var mock = new Mock<IApiResponse<string>>();
        _ = mock.SetupGet(x => x.IsSuccessful).Returns(true);

        await Assert.That(mock.Object.IsSuccessful).IsTrue();

        IApiResponse baseView = mock.Object;
        await Assert.That(baseView.IsSuccessful).IsTrue();
    }

    /// <summary>
    /// Verifies the same no-shadow contract for the other base members that used to be redeclared
    /// with <c>new</c>: a setup through the generic mock is seen through the non-generic interface.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericSetupIsObservedThroughBaseInterfaceForStatusAndReceived()
    {
        var mock = new Mock<IApiResponse<string>>();
        _ = mock.SetupGet(x => x.IsSuccessStatusCode).Returns(true);
        _ = mock.SetupGet(x => x.IsReceived).Returns(true);

        IApiResponse baseView = mock.Object;
        await Assert.That(baseView.IsSuccessStatusCode).IsTrue();
        await Assert.That(baseView.IsReceived).IsTrue();
    }

    /// <summary>
    /// Verifies a single setup of <see cref="IApiResponse{T}.Content"/> on the generic mock is
    /// observed through the generic interface (the covariance-safe declaration carries the value).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericContentSetupIsObserved()
    {
        var mock = new Mock<IApiResponse<string>>();
        _ = mock.SetupGet(x => x.Content).Returns("payload");

        await Assert.That(mock.Object.Content).IsEqualTo("payload");
    }

    /// <summary>
    /// Verifies <see cref="IApiResponse{T}.IsSuccessfulWithContent"/> narrows
    /// <see cref="IApiResponse{T}.Content"/> through a mock: inside the <c>true</c> branch the
    /// compiler treats <c>Content</c> as non-null (no null-forgiving operator is used below).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsSuccessfulWithContentNarrowsContentThroughMock()
    {
        var mock = new Mock<IApiResponse<string>>();
        _ = mock.SetupGet(x => x.IsSuccessfulWithContent).Returns(true);
        _ = mock.SetupGet(x => x.Content).Returns("hello");

        var response = mock.Object;

        await Assert.That(response.IsSuccessfulWithContent).IsTrue();
        if (response.IsSuccessfulWithContent)
        {
            // MemberNotNullWhen(true, nameof(Content)) flows here: Content is non-null without `!`.
            await Assert.That(response.Content.Length).IsEqualTo(5);
        }
        else
        {
            Assert.Fail("IsSuccessfulWithContent should have been true.");
        }
    }

    /// <summary>
    /// Verifies a mocked <see cref="IApiResponse{T}.IsSuccessfulWithContent"/> returns its configured
    /// value through the generic view, with no leakage from an unrelated base-member setup.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsSuccessfulWithContentIsIndependentlyMockable()
    {
        var mock = new Mock<IApiResponse<string>>();
        _ = mock.SetupGet(x => x.IsSuccessful).Returns(true);
        _ = mock.SetupGet(x => x.IsSuccessfulWithContent).Returns(false);

        await Assert.That(mock.Object.IsSuccessful).IsTrue();
        await Assert.That(mock.Object.IsSuccessfulWithContent).IsFalse();
    }

    /// <summary>Verifies HasContent narrows Content to non-null through a mock.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HasContentNarrowsContentThroughMock()
    {
        var mock = new Mock<IApiResponse<string>>();
        _ = mock.SetupGet(x => x.HasContent).Returns(true);
        _ = mock.SetupGet(x => x.Content).Returns("data");

        var response = mock.Object;

        if (response.HasContent)
        {
            // MemberNotNullWhen(true, nameof(Content)) flows here: Content is non-null without `!`.
            await Assert.That(response.Content.Length).IsEqualTo(4);
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
        var mock = new Mock<IApiResponse<string>>();
        _ = mock.SetupGet(x => x.IsSuccessStatusCode).Returns(true);
        _ = mock.SetupGet(x => x.ContentHeaders).Returns((HttpContentHeaders?)null);

        await Assert.That(mock.Object.IsSuccessStatusCode).IsTrue();
        await Assert.That(mock.Object.ContentHeaders).IsNull();
    }

    /// <summary>
    /// Regression for #1933: a method that takes the non-generic <see cref="IApiResponse"/> must observe
    /// the <see cref="IApiResponse.StatusCode"/> / <see cref="IApiResponse.ContentHeaders"/> set up on a
    /// mock of the generic <see cref="IApiResponse{T}"/>. The original <c>new</c>-shadow split those into
    /// separate slots, so the base view read <c>null</c> and the problem-details check wrongly returned false.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ProblemDetailsCheckSeesGenericSetupThroughBaseInterface()
    {
        var headers = new StringContent(string.Empty).Headers;
        headers.ContentType = new("application/problem+json");

        var mock = new Mock<IApiResponse<string>>();
        _ = mock.SetupGet(x => x.IsSuccessStatusCode).Returns(false);
        _ = mock.SetupGet(x => x.StatusCode).Returns(HttpStatusCode.UnprocessableEntity);
        _ = mock.SetupGet(x => x.ContentHeaders).Returns(headers);

        await Assert.That(IsProblemDetails(mock.Object)).IsTrue();
    }

    /// <summary>
    /// Regression for #1949: reading <see cref="IApiResponse.Error"/> through the non-generic interface must
    /// return the error set up on the generic mock. The <c>new</c>-shadow made <c>IApiResponse.Error</c> a
    /// distinct, always-null slot, so a <c>Verify(IApiResponse r)</c> method never saw the captured error.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorIsObservedThroughBaseInterfaceFromGenericMock()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        var error = new ApiRequestException("failed", request, HttpMethod.Get, new RefitSettings());

        var mock = new Mock<IApiResponse<string>>();
        _ = mock.SetupGet(x => x.Error).Returns(error);

        // A `Verify(IApiResponse r)` method reads the error through the non-generic interface; the
        // generic setup must be observed there (it was a distinct, always-null slot before the fix).
        IApiResponse baseView = mock.Object;
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
