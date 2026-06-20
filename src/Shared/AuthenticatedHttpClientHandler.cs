// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net.Http.Headers;

namespace Refit;

/// <summary>A delegating handler that adds an authorization token to outgoing requests when an authorization header is present.</summary>
internal sealed class AuthenticatedHttpClientHandler : DelegatingHandler
{
    /// <summary>The function used to retrieve the authentication token for a request.</summary>
    private readonly Func<HttpRequestMessage, CancellationToken, Task<string>> _getToken;

    /// <summary>Initializes a new instance of the <see cref="AuthenticatedHttpClientHandler"/> class.</summary>
    /// <param name="getToken">The function to get the authentication token.</param>
    /// <param name="innerHandler">The optional inner handler.</param>
    /// <exception cref="ArgumentNullException"><paramref name="getToken"/> must not be null.</exception>
    /// <remarks>
    /// Warning: This constructor sets the <see cref="DelegatingHandler.InnerHandler"/> to an instance
    /// of <see cref="HttpClientHandler"/>, when <paramref name="innerHandler"/> is <c>null</c>. This is
    /// a behavior which is incompatible with the <c>IHttpClientBuilder</c>.
    /// </remarks>
    public AuthenticatedHttpClientHandler(
        Func<HttpRequestMessage, CancellationToken, Task<string>> getToken,
        HttpMessageHandler? innerHandler = null)
        : base(innerHandler ?? new HttpClientHandler())
    {
        ArgumentExceptionHelper.ThrowIfNull(getToken);
        _getToken = getToken;
    }

    /// <summary>Initializes a new instance of the <see cref="AuthenticatedHttpClientHandler"/> class.</summary>
    /// <param name="innerHandler">The optional inner handler.</param>
    /// <param name="getToken">The function to get the authentication token.</param>
    /// <exception cref="ArgumentNullException"><paramref name="getToken"/> must not be null.</exception>
    /// <remarks>
    /// This function doesn't set the <see cref="DelegatingHandler.InnerHandler"/> automatically to an
    /// instance of <see cref="HttpClientHandler"/> when <paramref name="innerHandler"/> is null,
    /// which is different from the old (legacy) constructor, and compliant with the behavior expected
    /// by the <c>IHttpClientBuilder</c>.
    /// </remarks>
    public AuthenticatedHttpClientHandler(
        HttpMessageHandler? innerHandler,
        Func<HttpRequestMessage, CancellationToken, Task<string>> getToken)
    {
        ArgumentExceptionHelper.ThrowIfNull(getToken);
        _getToken = getToken;
        if (innerHandler is null)
        {
            return;
        }

        InnerHandler = innerHandler;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // See if the request has an authorize header
        var auth = request.Headers.Authorization;
        if (auth is not null)
        {
            var token = await _getToken(request, cancellationToken).ConfigureAwait(false);
            request.Headers.Authorization = new(auth.Scheme, token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
