// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Tests;

/// <summary>Tests for the <see cref="ApiResponseExtensions"/> success-guard helpers on both the generic and non-generic response interfaces.</summary>
public sealed class ApiResponseEnsureSuccessTests
{
    /// <summary>The example request URI reused across the guard tests.</summary>
    private const string ExampleUri = "https://example.test";

    /// <summary>Verifies the generic status guard throws the captured error for an unsuccessful response.</summary>
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

    /// <summary>Verifies the generic EnsureSuccessfulAsync returns the response on success and throws the error on failure.</summary>
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

    /// <summary>Verifies the generic success-guard extensions reject a null response.</summary>
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

    /// <summary>Verifies the non-generic status guard returns the response on success and throws the captured error otherwise.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonGenericEnsureSuccessStatusCodeReturnsOnSuccessAndThrowsOnFailure()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var okResponse = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
        using var success = new ApiResponse<string>(okResponse, "body", new());

        IApiResponse okInterface = success;
        await Assert.That(await okInterface.EnsureSuccessStatusCodeAsync()).IsSameReferenceAs(success);

        using var badResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request };
        var error = await ApiException.Create(request, HttpMethod.Get, badResponse, new());
        using var failure = new ApiResponse<string>(badResponse, null, new(), error);

        IApiResponse failInterface = failure;
        await Assert
            .That(async () => await failInterface.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<ApiException>();
    }

    /// <summary>Verifies the non-generic EnsureSuccessfulAsync returns on success and surfaces a transport failure.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonGenericEnsureSuccessfulReturnsOnSuccessAndThrowsTransportFailure()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleUri);
        using var okResponse = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
        using var success = new ApiResponse<string>(okResponse, "body", new());

        IApiResponse okInterface = success;
        await Assert.That(await okInterface.EnsureSuccessfulAsync()).IsSameReferenceAs(success);

        // No response was received, so the captured error is an ApiRequestException (a transport failure).
        var requestError = new ApiRequestException("no response", request, HttpMethod.Get, new());
        using var failure = new ApiResponse<string>(request, null, null, new(), requestError);

        IApiResponse failInterface = failure;
        await Assert
            .That(async () => await failInterface.EnsureSuccessfulAsync())
            .ThrowsExactly<ApiRequestException>();
    }

    /// <summary>Verifies the non-generic success guards reject a null response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonGenericEnsureSuccessGuardsRejectNullResponse()
    {
        const IApiResponse nullResponse = null!;

        await Assert
            .That(static async () => await nullResponse.EnsureSuccessStatusCodeAsync())
            .ThrowsExactly<ArgumentNullException>();
        await Assert
            .That(static async () => await nullResponse.EnsureSuccessfulAsync())
            .ThrowsExactly<ArgumentNullException>();
    }
}
